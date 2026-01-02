using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController characterController;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -18f;
    [SerializeField] private float friction = 6f;
    [SerializeField] private float acceleration = 14f;
    [SerializeField] private float airAcceleration = 2f;
    [SerializeField] private float stopSpeed = 1f;
    [SerializeField] private float maxSpeedMultiplier = 1.75f;

    [Header("Camera Settings")]
    [SerializeField] private float sensitivity = 2.0f;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;
    [SerializeField] private float mobileLookSensitivity = 2.0f; // Separate sensitivity for mobile (increased default)

    // Internal State
    private Vector3 velocity;
    private bool isGrounded;
    private float verticalLookRotation;
    private bool wasGrounded; // For landing sound control

    // Player Stats reference
    private PlayerStats playerStats;

    // Footstep sound optimization
    private float lastFootstepTime;
    private const float FOOTSTEP_INTERVAL = 0.4f;
    private SoundManager cachedSoundManager;

    // Mobile support
    private bool isMobile;

    // --- Required for Weapon Camera Stacking ---
    private void SetupWeaponCamera()
    {
        // URP stacking logic must remain. Do not remove, do not edit.
        var cam = playerCamera.GetComponent<Camera>();
        if (cam != null)
        {
            foreach (var weaponCam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (weaponCam != cam && weaponCam.CompareTag("WeaponCamera"))
                {
                    weaponCam.transform.SetParent(cam.transform);
                    weaponCam.transform.localPosition = Vector3.zero;
                }
            }
        }
    }

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Get PlayerStats component if present
        playerStats = GetComponent<PlayerStats>();

        // Cache SoundManager reference to avoid repeated Instance access
        cachedSoundManager = SoundManager.Instance;

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = 103f;
            playerCamera.nearClipPlane = 0.01f; // Set near clipping plane to 0.01m for close object rendering
        }
    }

    private void Start()
    {
        SetupWeaponCamera();
        
        // Detect mobile platform
        isMobile = Application.isMobilePlatform || UnityEngine.Device.Application.isMobilePlatform;
        
        if (!isMobile)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Load sensitivity from PlayerPrefs if available
        if (PlayerPrefs.HasKey("MouseSensitivity"))
        {
            sensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
            // Mobile uses same sensitivity value but with a multiplier
            mobileLookSensitivity = sensitivity;
        }
        else
        {
            // Save default sensitivity
            PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
            mobileLookSensitivity = sensitivity;
        }

        // Refresh SoundManager cache if it wasn't available in Awake
        if (cachedSoundManager == null)
            cachedSoundManager = SoundManager.Instance;
    }

    private void Update()
    {
        // Don't process input if game is not active or weapon select screen is showing
        if (GameManager.Instance != null)
        {
            bool isGameActive = GameManager.Instance.isGameActive;
            bool isWeaponSelectShowing = GameManager.Instance.weaponSelectScreen != null && 
                                         GameManager.Instance.weaponSelectScreen.gameObject.activeInHierarchy;
            
            // Only allow movement and mouse look if game is active and weapon select is not showing
            if (!isGameActive || isWeaponSelectShowing)
            {
                return;
            }
        }

        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        float mouseX = 0f;
        float mouseY = 0f;

        if (isMobile && MobileInputController.Instance != null)
        {
            // Use touch input for mobile
            Vector2 touchDelta = MobileInputController.Instance.GetTouchDelta();
            // Mobile sensitivity - touchDelta is in pixels, convert to degrees
            // Use sensitivity from settings (same as PC) with a multiplier for touch screens
            float mobileSensitivityMultiplier = mobileLookSensitivity * 0.15f; // Convert pixels to degrees
            mouseX = touchDelta.x * mobileSensitivityMultiplier;
            mouseY = touchDelta.y * mobileSensitivityMultiplier;
        }
        else
        {
            // Use mouse input for PC
            mouseX = Input.GetAxis("Mouse X") * sensitivity * 100f * Time.deltaTime;
            mouseY = Input.GetAxis("Mouse Y") * sensitivity * 100f * Time.deltaTime;
        }

        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, minVerticalAngle, maxVerticalAngle);

        // Camera goes up/down (local X), player turns left/right (global Y)
        if (playerCamera != null)
            playerCamera.transform.localEulerAngles = new Vector3(verticalLookRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        isGrounded = characterController.isGrounded;

        // --- SOUND: Jump ---
        bool jumpPressedDown = false;
        if (isMobile && MobileInputController.Instance != null)
        {
            jumpPressedDown = MobileInputController.Instance.GetJumpInputDown();
        }
        else
        {
            jumpPressedDown = Input.GetButtonDown("Jump");
        }

        if (isGrounded && jumpPressedDown)
        {
            if (cachedSoundManager != null)
                cachedSoundManager.PlayJump();
        }

        // --- SOUND: Land ---
        if (!wasGrounded && isGrounded && velocity.y < -2f)
        {
            if (cachedSoundManager != null)
                cachedSoundManager.PlayLand();
        }

        // --- SOUND: Walk (optimized with time-based throttling and sqrMagnitude) ---
        if (isGrounded && Time.time >= lastFootstepTime + FOOTSTEP_INTERVAL)
        {
            // Use sqrMagnitude to avoid expensive sqrt calculation (2f * 2f = 4f)
            Vector3 flatVel = characterController.velocity;
            flatVel.y = 0f;
            if (flatVel.sqrMagnitude > 4f)
            {
                if (cachedSoundManager != null)
                    cachedSoundManager.PlayWalk();
                lastFootstepTime = Time.time;
            }
        }

        // --- Dynamic movement speed via PlayerStats (walkSpeed from Inspector is always respected) ---
        float targetWalkSpeed = walkSpeed * (playerStats != null ? (1f + playerStats.GetStatBonus(StatType.MoveSpeed)) : 1f);
        float dynamicMaxSpeed = targetWalkSpeed * maxSpeedMultiplier;
        // --------------------------------------------------------------------------------------------

        // Get movement input relative to player orientation
        float moveX = 0f;
        float moveZ = 0f;

        if (isMobile && MobileInputController.Instance != null)
        {
            // Use mobile joystick input
            Vector2 mobileInput = MobileInputController.Instance.GetMovementInput();
            moveX = mobileInput.x;
            moveZ = mobileInput.y;
        }
        else
        {
            // Use keyboard input for PC
            moveX = Input.GetAxisRaw("Horizontal");
            moveZ = Input.GetAxisRaw("Vertical");
        }

        Vector3 input = (transform.right * moveX + transform.forward * moveZ);
        Vector3 wishDir = input.normalized;
        float wishSpeed = targetWalkSpeed;

        Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (isGrounded)
        {
            // Apply ground friction
            ApplyFriction(1f);

            // Accelerate ground
            Accelerate(wishDir, wishSpeed, acceleration);

            // Allow auto-bunnyhop via holding jump button
            bool jumpHeld = false;
            if (isMobile && MobileInputController.Instance != null)
            {
                jumpHeld = MobileInputController.Instance.GetJumpInput();
            }
            else
            {
                jumpHeld = Input.GetButton("Jump");
            }

            if (jumpHeld)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            else
            {
                // Keep player grounded with a small force (classic Quake -2)
                if (velocity.y < 0f) velocity.y = -2f;
            }
        }
        else
        {
            // Accelerate air
            Accelerate(wishDir, wishSpeed, airAcceleration);

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
        }

        // --- Clamp horizontal (XZ) velocity BEFORE moving ---
        Vector3 horizVel = new Vector3(velocity.x, 0f, velocity.z);
        float horizSpeed = horizVel.magnitude;
        if (horizSpeed > dynamicMaxSpeed)
        {
            horizVel = horizVel.normalized * dynamicMaxSpeed;
            velocity.x = horizVel.x;
            velocity.z = horizVel.z;
        }
        // ---------------------------------------------------

        // Move Character Controller by velocity
        Vector3 moveDelta = velocity * Time.deltaTime;
        CollisionFlags flags = characterController.Move(moveDelta);

        // After moving, check if grounded via characterController.isGrounded in next frame.
        // Clamp velocity.x and velocity.z if needed (not necessary for basic bhop, but prevents crazy speeds).

        // Optionally, can clamp max horizontal speed here if desired (not in basic Quake).

        wasGrounded = isGrounded; // Update previous grounded state at end
    }

    /// <summary>
    /// Quake-style acceleration: Accelerate flat velocity toward wishDir by up to (accel * dt).
    /// </summary>
    private void Accelerate(Vector3 wishDir, float wishSpeed, float accel)
    {
        if (wishDir.sqrMagnitude < 0.0001f)
            return;

        // Only operate in XZ plane for acceleration
        Vector3 flatVel = new Vector3(velocity.x, 0f, velocity.z);

        float currentSpeed = Vector3.Dot(flatVel, wishDir);

        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f)
            return;

        float accelSpeed = accel * Time.deltaTime * wishSpeed;
        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        Vector3 accelVector = wishDir * accelSpeed;
        velocity.x += accelVector.x;
        velocity.z += accelVector.z;
    }

    /// <summary>
    /// Applies friction to flat (XZ) velocity when grounded.
    /// Friction t should be 1f (or scaled for other effects).
    /// </summary>
    private void ApplyFriction(float t)
    {
        Vector3 flatVel = new Vector3(velocity.x, 0f, velocity.z);
        float speed = flatVel.magnitude;
        if (speed < 0.0001f)
            return;

        float control = speed < stopSpeed ? stopSpeed : speed;
        float drop = control * friction * t * Time.deltaTime;

        float newSpeed = speed - drop;
        if (newSpeed < 0f)
            newSpeed = 0f;
        if (newSpeed != speed)
        {
            newSpeed /= speed;
            velocity.x *= newSpeed;
            velocity.z *= newSpeed;
        }
    }

    // ---- Required External API ----

    /// <summary>
    /// Adds recoil to camera (used by Weapon).
    /// DO NOT EDIT OR REMOVE.
    /// </summary>
    public void AddRecoil(float amount)
    {
        verticalLookRotation -= amount;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, minVerticalAngle, maxVerticalAngle);
    }

    public void SetSensitivity(float value)
    {
        sensitivity = Mathf.Clamp(value, 0.1f, 20f);
        // Update mobile sensitivity too (uses same value)
        mobileLookSensitivity = sensitivity;
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
    }

    public float GetSensitivity()
    {
        return sensitivity;
    }
}
