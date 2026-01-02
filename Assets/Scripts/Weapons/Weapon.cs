using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour
{
    [Header("Setup")]
    public WeaponDefinition definition;
    public Transform firePoint; // Assign a child object where bullets come out

    [Header("Current State")]
    private float currentCooldown;
    private int currentAmmo;
    private bool isReloading = false;
    private Camera mainCam;

    // --- WeaponStats reference ---
    private WeaponStats weaponStats;

    [Header("Visual Kickback")]
    [SerializeField] private Transform weaponModel; // Assign the mesh part in inspector
    [SerializeField] private float kickbackForce = 0.2f; // Distance to move back
    [SerializeField] private float kickbackRecovery = 10f;
    [SerializeField] private float cameraRecoilAmount = 2f; // Degrees to look up
    private float currentKickback = 0f;
    private Vector3 initialWeaponPos;

    [Header("Reload Animation")]
    [SerializeField] private Vector3 reloadOffset = new Vector3(0f, -1f, -1f); // 1m down, 1m back

    [Header("Sway & Bob")]
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float maxSwayAmount = 0.06f;
    [SerializeField] private float swaySmooth = 4.0f;
    [SerializeField] private float bobSpeed = 10.0f;
    [SerializeField] private float bobAmount = 0.05f;

    // -- Jump Sway --
    [SerializeField] private float jumpSwayAmount = 0.5f;
    [SerializeField] private float jumpSwayRecovery = 5f;
    private float currentJumpSway = 0f;

    // --- VFX ---
    [Header("VFX")]
    [SerializeField] private ParticleSystem muzzleFlash;

    private Vector3 swayPos = Vector3.zero;
    private Vector3 swayTarget = Vector3.zero;
    private Vector3 bobPos = Vector3.zero;

    private void OnEnable()
    {
        isReloading = false;
    }

    private void OnDisable()
    {
        isReloading = false;
        currentCooldown = 0f;
    }

    private void Awake()
    {
        // Initialization happens in Start()
    }

    private void Start()
    {
        // Get WeaponStats component
        weaponStats = GetComponent<WeaponStats>();
        // Initialize ammo properly
        currentAmmo = weaponStats != null ? weaponStats.GetMagSize() : 10;
        isReloading = false;

        // Cache initial local position for kickback reset
        if (weaponModel != null)
        {
            initialWeaponPos = weaponModel.localPosition;
        }

        // Initialize camera reference
        mainCam = Camera.main;

        // --- HUD Ammo Init ---
        if (GameManager.Instance != null && GameManager.Instance.hudManager != null && weaponStats != null)
        {
            GameManager.Instance.hudManager.UpdateAmmo(currentAmmo, weaponStats.GetMagSize());
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null)
        {
            // Don't process weapon input if game is not active or weapon select screen is showing
            if (!GameManager.Instance.isGameActive) return;
            
            bool isWeaponSelectShowing = GameManager.Instance.weaponSelectScreen != null && 
                                         GameManager.Instance.weaponSelectScreen.gameObject.activeInHierarchy;
            if (isWeaponSelectShowing) return;
        }

        if (currentCooldown > 0f)
            currentCooldown -= Time.deltaTime;

        // --- JUMP SWAY ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentJumpSway = -jumpSwayAmount; // Moves gun down sharply on jump
        }
        // Always Lerp jump-sway back to 0
        currentJumpSway = Mathf.Lerp(currentJumpSway, 0f, Time.deltaTime * jumpSwayRecovery);

        // --- STRICT RELOAD CHECK ---
        // We allow the kickback decay to run (below), but we skip other inputs
        bool swayBobActive = true;
        if (isReloading)
            swayBobActive = false;

        // --- Auto-Reload Trigger ---
        if (currentAmmo <= 0 && currentCooldown <= 0f && !isReloading)
        {
            StartCoroutine(Reload());
            return;
        }

        // --- Manual Reload Trigger ---
        if (weaponStats != null)
        {
            if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmo < weaponStats.GetMagSize())
            {
                StartCoroutine(Reload());
                return;
            }
        }

        // --- Automatic Fire input ---
        bool isShooting = false;
        if (MobileInputController.IsMobile && MobileInputController.Instance != null)
        {
            // Use mobile touch input for shooting
            isShooting = MobileInputController.Instance.IsShooting();
        }
        else
        {
            // Use mouse input for PC
            isShooting = Input.GetMouseButton(0);
        }

        if (isShooting && currentCooldown <= 0f)
        {
            if (currentAmmo > 0)
            {
                Shoot();
            }
        }

        // --- Visual Weapon Kickback (positional) ---
        // Allow kickback to decay even during reload
        currentKickback = Mathf.Lerp(currentKickback, 0f, Time.deltaTime * kickbackRecovery);

        // --- Procedural Sway ---
        if (swayBobActive && weaponModel != null)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Vector3 target = new Vector3(mouseX, mouseY, 0f) * -swayAmount;
            target.x = Mathf.Clamp(target.x, -maxSwayAmount, maxSwayAmount);
            target.y = Mathf.Clamp(target.y, -maxSwayAmount, maxSwayAmount);

            swayTarget = target;
            swayPos = Vector3.Lerp(swayPos, swayTarget, Time.deltaTime * swaySmooth);
        }
        else
        {
            swayPos = Vector3.Lerp(swayPos, Vector3.zero, Time.deltaTime * swaySmooth);
        }

        // --- Procedural Bobbing ---
        if (swayBobActive && weaponModel != null)
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            bool isMoving = Mathf.Abs(moveX) > 0.01f || Mathf.Abs(moveY) > 0.01f;

            if (isMoving)
            {
                float bobX = Mathf.Sin(Time.time * (bobSpeed * 0.5f)) * bobAmount * 0.5f;
                float bobY = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
                bobPos = new Vector3(bobX, Mathf.Abs(bobY), 0f); // y always positive to avoid gun intersection/lowering
            }
            else
            {
                bobPos = Vector3.Lerp(bobPos, Vector3.zero, Time.deltaTime * bobSpeed);
            }
        }
        else
        {
            bobPos = Vector3.Lerp(bobPos, Vector3.zero, Time.deltaTime * bobSpeed);
        }

        // --- Combine All Offsets (Kickback, Sway, Bob, JumpSway) ---
        // Only apply (kickback, sway, bob, jumpSway) if not reloading.
        if (weaponModel != null)
        {
            Vector3 kickbackPos = new Vector3(0f, 0f, -currentKickback);
            Vector3 jumpSwayPos = new Vector3(0f, currentJumpSway, 0f);

            // If reloading, show reload animation ONLY (kickback decays, but sway, bob, and jumpsway stay at zero)
            if (!isReloading)
            {
                Vector3 finalPos = initialWeaponPos + kickbackPos + swayPos + bobPos + jumpSwayPos;
                weaponModel.localPosition = Vector3.Lerp(weaponModel.localPosition, finalPos, Time.deltaTime * 20f);
            }
            // If reloading, reload coroutine owns the weaponModel.localPosition, so we don't adjust here.
        }
    }

    private void Shoot()
    {
        // --- Play Muzzle Flash VFX (if exists) ---
        if (muzzleFlash != null)
        {
            muzzleFlash.Play(true);
        }

        // --- Play Shoot Sound ---
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayShoot();
        }

        // Visual Kickback & Recoil (keep logic)
        currentKickback = kickbackForce;

        PlayerController pc = GetComponentInParent<PlayerController>();
        if (pc != null)
            pc.AddRecoil(cameraRecoilAmount);

        // Use WeaponStats for all values
        if (weaponStats == null || firePoint == null || mainCam == null)
            return;

        int count = weaponStats.GetProjectileCount();
        if (count < 1) count = 1;
        float spreadAngle = weaponStats.GetSpreadAngle();

        // --- Decrease ammo ---
        currentAmmo--;

        // --- HUD Ammo Update ---
        if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
        {
            GameManager.Instance.hudManager.UpdateAmmo(currentAmmo, weaponStats.GetMagSize());
        }

        // 2. Determine Perfect Aim Target
        Ray aimRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;
        if (Physics.Raycast(aimRay, out RaycastHit hit, weaponStats.GetRange()))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = aimRay.GetPoint(weaponStats.GetRange());
        }

        // 3. Determine Spawn Origin (Fix for Close Range)
        // If target is very close, spawn slightly forward from camera to avoid spawning inside enemy
        float distToTarget = Vector3.Distance(mainCam.transform.position, targetPoint);
        float distToBarrel = Vector3.Distance(mainCam.transform.position, firePoint.position);

        Vector3 spawnOrigin = firePoint.position;
        if (distToTarget < distToBarrel)
        {
            // Spawn 0.3 units forward from camera to avoid spawning inside the enemy
            spawnOrigin = mainCam.transform.position + mainCam.transform.forward * 0.3f;
        }

        // 4. Calculate Base Rotation (Aim at target)
        Vector3 aimDir = (targetPoint - spawnOrigin).normalized;
        Quaternion baseRotation = Quaternion.LookRotation(aimDir);

        // 5. Spawn Projectiles
        for (int i = 0; i < count; i++)
        {
            Quaternion finalRot = baseRotation;

            // Apply Spread
            if (count > 1 || spreadAngle > 0f)
            {
                float x = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
                float y = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
                finalRot = Quaternion.Euler(finalRot.eulerAngles.x + x, finalRot.eulerAngles.y + y, finalRot.eulerAngles.z);
            }

            GameObject obj = ObjectPool.Instance.SpawnFromPool("Projectile", spawnOrigin, finalRot);
            if (obj != null)
            {
                Projectile p = obj.GetComponent<Projectile>();
                if (p != null)
                {
                    float projectileSpeed = weaponStats.GetProjectileSpeed();
                    if (projectileSpeed <= 0f) projectileSpeed = 50f;
                    // Cap projectile speed at 50 to prevent hit registration issues
                    projectileSpeed = Mathf.Clamp(projectileSpeed, 1f, 50f);

                    p.Initialize(
                        weaponStats.GetDamage(),
                        projectileSpeed,
                        weaponStats.GetRange(),
                        finalRot * Vector3.forward,
                        weaponStats.GetCritChance(),
                        weaponStats.GetCritDamage(),
                        weaponStats.GetPierce(),
                        weaponStats.GetRicochetBounces(),
                        weaponStats.GetKnockback(),
                        weaponStats.GetAoERadius()
                    );
                    if (!p.gameObject.activeSelf)
                        p.gameObject.SetActive(true);
                }
            }
        }

        // --- Set Cooldown to Fire Rate ---
        currentCooldown = weaponStats.GetFireRate();
    }

    private IEnumerator Reload()
    {
        isReloading = true;

        // --- Play Reload Sound ---
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayReload();
        }

        // Debug.Log("Start Reload...");

        float reloadTime = weaponStats != null ? weaponStats.GetReloadSpeed() : 2f;
        int maxMag = weaponStats != null ? weaponStats.GetMagSize() : 10;

        // --- Animation Logic ---
        // User request: 0.5s transition for a 2.0s reload base. Ratio = 0.25.
        float transitionDuration = reloadTime * 0.25f; 
        float holdDuration = reloadTime - (transitionDuration * 2f);
        
        // Safety against negative hold time
        if (holdDuration < 0f) holdDuration = 0f;

        Vector3 targetPos = initialWeaponPos + reloadOffset;

        // --- HUD Reload Bar: Start ---
        if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
        {
            GameManager.Instance.hudManager.UpdateReload(0f, true);
        }

        float totalElapsed = 0f;

        // Phase 1: Move Down/Back
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float delta = Time.deltaTime;
            elapsed += delta;
            totalElapsed += delta;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            if (weaponModel != null)
                weaponModel.localPosition = Vector3.Lerp(initialWeaponPos, targetPos, t);

            // HUD update during phase 1
            if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
            {
                GameManager.Instance.hudManager.UpdateReload(Mathf.Clamp01(totalElapsed / reloadTime), true);
            }
            yield return null;
        }
        // Ensure exact end position
        if (weaponModel != null) weaponModel.localPosition = targetPos;

        // Phase 2: Hold
        if (holdDuration > 0f)
        {
            float holdElapsed = 0f;
            while (holdElapsed < holdDuration)
            {
                float delta = Time.deltaTime;
                holdElapsed += delta;
                totalElapsed += delta;

                // HUD update during phase 2
                if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
                {
                    GameManager.Instance.hudManager.UpdateReload(Mathf.Clamp01(totalElapsed / reloadTime), true);
                }
                yield return null;
            }
        }

        // Phase 3: Move Back to Start
        elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float delta = Time.deltaTime;
            elapsed += delta;
            totalElapsed += delta;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            if (weaponModel != null)
                weaponModel.localPosition = Vector3.Lerp(targetPos, initialWeaponPos, t);

            // HUD update during phase 3
            if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
            {
                GameManager.Instance.hudManager.UpdateReload(Mathf.Clamp01(totalElapsed / reloadTime), true);
            }
            yield return null;
        }
        // Ensure exact start position
        if (weaponModel != null) weaponModel.localPosition = initialWeaponPos;

        // --- Logic End ---
        currentAmmo = maxMag;
        isReloading = false;

        // --- HUD Ammo & Reload Finalize ---
        if (GameManager.Instance != null && GameManager.Instance.hudManager != null)
        {
            GameManager.Instance.hudManager.UpdateAmmo(currentAmmo, maxMag);
            GameManager.Instance.hudManager.UpdateReload(1f, false);
        }

        // Debug.Log("End Reload.");
    }
}