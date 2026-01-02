using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileInputController : MonoBehaviour
{
    public static MobileInputController Instance { get; private set; }

    [Header("Mobile Detection")]
    public static bool IsMobile => Application.isMobilePlatform || UnityEngine.Device.Application.isMobilePlatform;

    [Header("Joystick")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField] private float joystickRadius = 50f;

    [Header("Jump Button")]
    [SerializeField] private Button jumpButton;

    [Header("Interact Button")]
    [SerializeField] private Button interactButton;
    private bool interactButtonPressed = false;

    private Vector2 joystickInput = Vector2.zero;
    private bool isJumpPressed = false;
    private bool jumpPressedThisFrame = false;
    private int touchFingerId = -1;
    private int joystickFingerId = -1;
    private Vector2 lastTouchPosition = Vector2.zero;
    private bool isTouching = false;
    private bool isInputEnabled = true;
    private Vector2 joystickStartPos = Vector2.zero;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Only enable on mobile
            gameObject.SetActive(IsMobile);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Setup jump button with simple EventTrigger for press/release
        if (jumpButton != null)
        {
            EventTrigger trigger = jumpButton.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = jumpButton.gameObject.AddComponent<EventTrigger>();
            }

            // Clear existing triggers
            trigger.triggers.Clear();

            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { OnJumpPressed(); });
            trigger.triggers.Add(pointerDown);

            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { OnJumpReleased(); });
            trigger.triggers.Add(pointerUp);
        }

        // Show joystick background always (will be hidden when UI screens open)
        if (joystickBackground != null)
        {
            joystickBackground.gameObject.SetActive(true);
            joystickBackground.anchoredPosition = new Vector2(150f, 150f);
            
            // Disable raycast blocking on joystick background so it doesn't interfere with shooting touches
            UnityEngine.UI.Image bgImage = joystickBackground.GetComponent<UnityEngine.UI.Image>();
            if (bgImage != null)
            {
                bgImage.raycastTarget = false;
            }
        }
        
        // Hide joystick handle initially
        if (joystickHandle != null)
        {
            joystickHandle.gameObject.SetActive(false);
            joystickHandle.localPosition = Vector2.zero;
            
            // Disable raycast blocking on joystick handle so it doesn't interfere with shooting touches
            UnityEngine.UI.Image handleImage = joystickHandle.GetComponent<UnityEngine.UI.Image>();
            if (handleImage != null)
            {
                handleImage.raycastTarget = false;
            }
        }

        // Setup interact button
        if (interactButton != null)
        {
            interactButton.onClick.AddListener(OnInteractPressed);
            interactButton.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsMobile) return;

        // Check if input should be disabled (when UI screens are showing)
        CheckInputState();

        // Only process input if enabled
        if (isInputEnabled)
        {
            HandleJoystickInput();
            HandleTouchInput();
        }
        else
        {
            // Reset all input when disabled
            joystickInput = Vector2.zero;
            isJumpPressed = false;
            jumpPressedThisFrame = false;
            isTouching = false;
            touchFingerId = -1;
            
            // Hide joystick handle when input is disabled
            if (joystickHandle != null)
                joystickHandle.gameObject.SetActive(false);
            // Background visibility is handled by CheckInputState()
        }
    }
    
    private void OnJumpPressed()
    {
        if (!isInputEnabled) return;
        isJumpPressed = true;
        jumpPressedThisFrame = true;
    }

    private void OnJumpReleased()
    {
        if (!isInputEnabled) return;
        isJumpPressed = false;
    }

    private void CheckInputState()
    {
        // Use GameManager's method to check if any UI is displaying
        // This covers: pause menu, level up screen (all upgrades), settings, weapon select, death screen
        bool shouldHideUI = false;
        if (GameManager.Instance != null)
        {
            isInputEnabled = !GameManager.Instance.IsAnyUIDisplaying();
            shouldHideUI = GameManager.Instance.IsAnyUIDisplaying();
        }
        else
        {
            isInputEnabled = true;
            shouldHideUI = false;
        }

        // Update visibility of joystick background and jump button
        // Hide only when upgrade/settings screens are open (not pause menu)
        bool hideControls = false;
        if (GameManager.Instance != null)
        {
            // Hide controls when level up screen (upgrades) or settings are showing
            hideControls = (GameManager.Instance.levelUpScreen != null && GameManager.Instance.levelUpScreen.IsPanelActive())
                        || (GameManager.Instance.settingsPanel != null && GameManager.Instance.settingsPanel.activeInHierarchy);
        }

        if (joystickBackground != null)
        {
            joystickBackground.gameObject.SetActive(!hideControls);
        }
        if (jumpButton != null)
        {
            jumpButton.gameObject.SetActive(!hideControls);
        }
        if (interactButton != null)
        {
            // Interact button visibility is controlled separately via ShowInteractButton/HideInteractButton
            // But also hide when upgrade/settings screens are open
            if (hideControls && interactButton.gameObject.activeSelf)
            {
                interactButton.gameObject.SetActive(false);
            }
        }
    }

    private void HandleJoystickInput()
    {
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                // Skip if this is the shooting touch
                if (touch.fingerId == touchFingerId) continue;

                // Check if touch is in joystick area (left side of screen)
                Vector2 touchPos = touch.position;
                bool isInJoystickArea = touchPos.x < Screen.width * 0.35f;

                if (touch.phase == TouchPhase.Began && isInJoystickArea)
                {
                    // Start joystick tracking
                    joystickFingerId = touch.fingerId;
                    joystickStartPos = touchPos;
                    
                    // Ensure joystick background is visible
                    if (joystickBackground != null)
                    {
                        joystickBackground.gameObject.SetActive(true);
                        
                        // Position handle at initial touch position (relative to background center)
                        if (joystickHandle != null)
                        {
                            // Convert screen position to local position relative to background
                            Vector2 localPoint;
                            Canvas canvas = joystickBackground.GetComponentInParent<Canvas>();
                            if (canvas != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                joystickBackground, touchPos, canvas.worldCamera, out localPoint))
                            {
                                // Clamp to radius
                                Vector2 clampedPoint = Vector2.ClampMagnitude(localPoint, joystickRadius);
                                joystickHandle.localPosition = clampedPoint;
                                joystickHandle.gameObject.SetActive(true);
                                
                                // Set initial input
                                joystickInput = clampedPoint / joystickRadius;
                            }
                        }
                    }
                }
                else if (touch.fingerId == joystickFingerId)
                {
                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        // Calculate offset from initial touch position
                        Vector2 delta = touchPos - joystickStartPos;
                        
                        // Clamp to joystick radius
                        Vector2 clampedDelta = Vector2.ClampMagnitude(delta, joystickRadius);
                        
                        // Set joystick input (normalized)
                        joystickInput = clampedDelta / joystickRadius;
                        
                        // Update handle position
                        if (joystickHandle != null && joystickBackground != null)
                        {
                            // Convert screen delta to local space
                            Canvas canvas = joystickBackground.GetComponentInParent<Canvas>();
                            if (canvas != null)
                            {
                                float scaleFactor = canvas.scaleFactor;
                                Vector2 localDelta = clampedDelta / scaleFactor;
                                joystickHandle.localPosition = localDelta;
                                joystickHandle.gameObject.SetActive(true);
                            }
                        }
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        // Reset joystick
                        joystickInput = Vector2.zero;
                        joystickFingerId = -1;
                        if (joystickHandle != null)
                            joystickHandle.gameObject.SetActive(false);
                        // Background stays visible
                    }
                }
            }
        }
        else
        {
            // No touches - reset joystick
            if (joystickFingerId != -1)
            {
                joystickInput = Vector2.zero;
                joystickFingerId = -1;
                if (joystickHandle != null)
                    joystickHandle.gameObject.SetActive(false);
            }
        }
    }

    private void HandleTouchInput()
    {
        // Find a touch that's not for joystick or UI buttons
        bool foundValidTouch = false;
        
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                // Skip if this is the joystick touch
                if (touch.fingerId == joystickFingerId)
                    continue;

                // If this is already our shooting touch, process it
                if (touch.fingerId == touchFingerId)
                {
                    foundValidTouch = true;
                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        lastTouchPosition = touch.position;
                        isTouching = true;
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        touchFingerId = -1;
                        isTouching = false;
                        lastTouchPosition = Vector2.zero;
                    }
                    break;
                }

                // For new touches, check if they should become the shooting touch
                if (touch.phase == TouchPhase.Began && touchFingerId == -1)
                {
                    // Skip joystick area (entire left side reserved for movement)
                    if (touch.position.x < Screen.width * 0.35f)
                        continue;

                    // Check if touch is specifically over a UI button (not just any UI element)
                    // This prevents the joystick/background from blocking, but still respects actual buttons
                    bool isOverButton = false;
                    if (EventSystem.current != null)
                    {
                        PointerEventData eventData = new PointerEventData(EventSystem.current);
                        eventData.position = touch.position;
                        var results = new System.Collections.Generic.List<RaycastResult>();
                        EventSystem.current.RaycastAll(eventData, results);
                        
                        foreach (var result in results)
                        {
                            // Only block if it's specifically the jump or interact button
                            if (result.gameObject == jumpButton?.gameObject || 
                                result.gameObject == interactButton?.gameObject)
                            {
                                isOverButton = true;
                                break;
                            }
                        }
                    }
                    
                    if (isOverButton)
                        continue;

                    // This touch is valid for shooting
                    touchFingerId = touch.fingerId;
                    lastTouchPosition = touch.position;
                    isTouching = true;
                    foundValidTouch = true;
                    break;
                }
            }
        }

        if (!foundValidTouch && touchFingerId != -1)
        {
            // Lost our touch
            isTouching = false;
            touchFingerId = -1;
        }
    }

    private void LateUpdate()
    {
        // Reset frame-specific flags
        jumpPressedThisFrame = false;
        interactButtonPressed = false;
    }

    // Public API for getting input values
    public Vector2 GetMovementInput()
    {
        // Return zero if input is disabled
        if (!isInputEnabled) return Vector2.zero;
        return joystickInput;
    }

    public bool GetJumpInput()
    {
        // Return false if input is disabled
        if (!isInputEnabled) return false;
        // Return true if jump was pressed this frame or is currently held
        return isJumpPressed;
    }

    public bool GetJumpInputDown()
    {
        // Return false if input is disabled
        if (!isInputEnabled) return false;
        // Return true only on the frame the jump button was first pressed
        return jumpPressedThisFrame;
    }

    public Vector2 GetTouchDelta()
    {
        // Return zero if input is disabled
        if (!isInputEnabled) return Vector2.zero;
        
        if (!isTouching) return Vector2.zero;

        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.fingerId == touchFingerId)
                {
                    return touch.deltaPosition;
                }
            }
        }

        return Vector2.zero;
    }

    public bool IsShooting()
    {
        // Return false if input is disabled
        if (!isInputEnabled) return false;
        return isTouching;
    }

    // Interact button methods
    private void OnInteractPressed()
    {
        interactButtonPressed = true;
    }

    public bool GetInteractInput()
    {
        return interactButtonPressed;
    }

    public void ShowInteractButton()
    {
        if (interactButton != null && isInputEnabled)
        {
            interactButton.gameObject.SetActive(true);
        }
    }

    public void HideInteractButton()
    {
        if (interactButton != null)
        {
            interactButton.gameObject.SetActive(false);
        }
    }
}

