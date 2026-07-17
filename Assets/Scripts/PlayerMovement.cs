using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    private bool sprint;

    [Header("References")]
    public Animator animator;
    public CharacterController controller;
    public MouseLook mouseLook;
    public Transform cameraTransform;
    public LevelManager levelManager;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 8f;
    public float gravity = -35f;
    public float jumpHeight = 1.5f;

    [Header("Air")]
    public float airControl = 0.3f;
    public float airSteerSpeed = 5f;
    public float jumpForwardBoost = 1.15f;

    [Header("Landing")]
    public float minLandSpeed = 9f;
    public float hardLandSpeed = 24f;
    public float maxLandDip = 0.5f;
    public float landRecoverySpeed = 6f;

    [Header("Landing Roll")]
    public float rollBufferTime = 0.3f;
    public float rollMinFallSpeed = 12f;
    public float rollDuration = 0.6f;
    public float rollSpeed = 11f;
    public float rollHeight = 1.2f;
    public float rollCameraSpin = 360f;
    public float rollFOV = 72f;

    [Header("Camera Shake")]
    public float landShakeAmount = 2.5f;
    public float slideShakeAmount = 0.4f;
    public float shakeDecay = 4f;
    public float shakePosAmount = 0.12f;
    public float shakeRotAmount = 4f;
    public float shakeFrequency = 22f;

    [Header("Head Bob")]
    public float bobFrequency = 1.9f;
    public float bobAmount = 0.055f;
    public float bobRollAmount = 1.2f;

    [Header("Wall Run")]
    public float wallCheckDistance = 0.9f;
    public float wallRunSpeed = 14f;
    public float wallRunGravity = -1.5f;
    public float wallRunMaxDuration = 2.5f;
    public float wallRunMinSpeed = 4f;
    public float wallRunCooldown = 0.25f;
    public float wallStickForce = 4f;

    public float wallRunEntryPop = 3f;
    public float wallRunShake = 0.3f;

    [Header("Wall Jump")]
    public float wallJumpUpForce = 9f;
    public float wallJumpSideForce = 9f;
    public float wallJumpForwardForce = 4f;

    [Header("Wall Run Camera")]
    public float wallRunCameraTilt = 18f;
    public float wallTiltSpeed = 10f;

    [Header("FOV")]
    public float baseFOV = 60f;
    public float sprintFOV = 68f;
    public float wallRunFOV = 76f;
    public float slideFOV = 80f;
    public float fovSpeed = 7f;

    [Header("Vault")]
    public float vaultDetectDistance = 2.5f;
    public float vaultDuration = 0.5f;
    public float vaultForwardDistance = 2.5f;
    public float maxVaultHeight = 1.8f;
    public float vaultClearance = 0.4f;

    [Header("Mantle / Ledge Climb")]
    public float mantleDetectDistance = 1.2f;
    public float minMantleHeight = 1.0f;
    public float maxMantleHeight = 3.2f;
    public float mantleDuration = 0.65f;
    public float mantleForwardOffset = 0.7f;
    public float mantleDipAmount = 14f;
    public bool autoMantleInAir = true;

    [Header("Vault Camera Feel")]
    public float vaultTiltAmount = 18f;
    public float vaultDipAmount = 12f;

    [Header("Slide")]
    public float slideDuration = 1.3f;
    public float slideSpeed = 15f;
    public float slideHeight = 1.2f;

    [Header("Slide Camera Feel")]
    public float slideCameraHeight = 0.25f;
    public float slideTiltAmount = 16f;
    public float slideDipAmount = 10f;
    public float slideCameraDropSpeed = 8f;

    [Header("Pole Slide")]
    public float poleDetectDistance = 4f;
    public float poleSlideSpeed = 8f;      // descend speed
    public float poleClimbSpeed = 5f;      // up/down with W/S
    public float poleGrabOffset = 0.6f;    // distance player sits from pole center
    public float poleJumpOffForce = 10f;   // launch when jumping off
    public float poleJumpUpForce = 6f;
    public float poleTiltAmount = 6f;      // subtle camera lean while sliding

    [Header("Monkey Bars")]
    public float barDetectDistance = 4f;   // how far forward we look for a bar
    public float barHangDrop = 2f;         // how far below the bar the player hangs
    public float barSwingForce = 12f;      // forward launch when jumping to next bar
    public float barSwingUpForce = 7f;     // upward launch when jumping to next bar
    public float barSideSpeed = 3f;        // optional A/D shuffle while hanging
    public float barTiltAmount = 4f;       // subtle camera sway while hanging

    private float verticalVelocity;
    private Vector3 horizontalVelocity;

    private bool isVaulting = false;
    private bool isSliding = false;
    private bool wasGrounded = true;
    private float jumpLockout = 0f;

    private bool isRolling = false;
    private float rollBuffer = 0f;
    private bool isMantling = false;
    private bool isWallRunning = false;
    private bool isPoleSliding = false;
    private bool isBarHanging = false;
    private bool wallLeft = false;
    private bool wallRight = false;
    private RaycastHit wallHit;
    private Vector3 wallNormal;
    private float wallRunTimer = 0f;
    private float wallCooldownTimer = 0f;
    private float wallTilt = 0f;

    private float defaultHeight;
    private Vector3 defaultCenter;
    private float defaultCameraY;

    private Camera cam;
    private float currentCamY;
    private float defaultCameraX;
    private float defaultCameraZ;
    private float landDip;
    private float shakeIntensity;
    private float shakeSeed;
    private float bobTimer;
    private float bobY;
    private float bobRoll;

    void Awake()
    {
        defaultHeight = controller.height;
        defaultCenter = controller.center;

        if (cameraTransform != null)
        {
            defaultCameraY = cameraTransform.localPosition.y;
            defaultCameraX = cameraTransform.localPosition.x;
            defaultCameraZ = cameraTransform.localPosition.z;
            currentCamY = defaultCameraY;

            cam = cameraTransform.GetComponent<Camera>();
        }

        shakeSeed = Random.Range(0f, 100f);

        if (cam != null)
            cam.fieldOfView = baseFOV;
    }

    void Update()
    {
        if (isVaulting || isSliding || isMantling || isRolling || isPoleSliding || isBarHanging) return;

        Sprint();
        AnimationWalking();
        Move();
    }

    void LateUpdate()
    {
        UpdateCamera();
    }

    void Sprint()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift)) sprint = true;
        if (Input.GetKeyUp(KeyCode.LeftShift)) sprint = false;
    }

    void AnimationWalking()
    {
        animator.SetFloat("walkForwardValue", Input.GetAxis("Vertical"));
        animator.SetFloat("sidewalkValue", Input.GetAxis("Horizontal"));
        animator.SetBool("sprint", sprint);
        animator.SetBool("grounded", controller.isGrounded && jumpLockout <= 0f);
    }

    void SetAnimatorIdle()
    {
        animator.SetFloat("walkForwardValue", 0f);
        animator.SetFloat("sidewalkValue", 0f);
        animator.SetBool("sprint", false);
        animator.SetBool("grounded", true);
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 inputDir = transform.right * h + transform.forward * v;
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        float speed = sprint ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = inputDir * speed;

        if (jumpLockout > 0f) jumpLockout -= Time.deltaTime;
        if (wallCooldownTimer > 0f) wallCooldownTimer -= Time.deltaTime;
        if (rollBuffer > 0f) rollBuffer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.C))
            rollBuffer = rollBufferTime;

        bool grounded = controller.isGrounded && jumpLockout <= 0f;
        float fallSpeed = verticalVelocity;

        if (grounded && !wasGrounded)
        {
            if (rollBuffer > 0f && -fallSpeed > rollMinFallSpeed)
            {
                wasGrounded = true;
                rollBuffer = 0f;
                StartCoroutine(Roll());
                return;
            }

            OnLand(fallSpeed);
        }

        wasGrounded = grounded;

        CheckWalls();

        if (isWallRunning)
        {
            WallRunUpdate(v);
            return;
        }

        if (CanStartWallRun(grounded, v))
        {
            StartWallRun();
            return;
        }

        if (grounded)
        {
            if (verticalVelocity < 0) verticalVelocity = -2f;
            horizontalVelocity = targetVelocity;
        }
        else
        {
            float steer = airControl * airSteerSpeed * Time.deltaTime;
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, steer);
        }

        // --- Grab (press E): try pole first, then overhead bar ---
        if (Input.GetKeyDown(KeyCode.E))
        {
            Transform pole;
            if (TryGetPole(out pole))
            {
                StartCoroutine(PoleSlide(pole));
                return;
            }

            Transform bar;
            if (TryGetBar(out bar))
            {
                StartCoroutine(BarHang(bar));
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftControl) && sprint && grounded && inputDir.magnitude > 0.1f)
        {
            StartCoroutine(Slide());
            return;
        }

        Vector3 ledgePoint;

        if (autoMantleInAir && !grounded && v > 0.1f && verticalVelocity < 1f
            && TryGetMantle(out ledgePoint))
        {
            StartCoroutine(Mantle(ledgePoint));
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 landingPoint;
            float obstacleTop;

            if (TryGetVault(out landingPoint, out obstacleTop))
            {
                StartCoroutine(Vault(landingPoint, obstacleTop));
                return;
            }

            if (TryGetMantle(out ledgePoint))
            {
                StartCoroutine(Mantle(ledgePoint));
                return;
            }

            if (grounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                horizontalVelocity *= jumpForwardBoost;
                jumpLockout = 0.2f;
                animator.SetTrigger("jump");
            }
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);
    }

    void OnLand(float fallSpeed)
    {
        float impact = Mathf.InverseLerp(minLandSpeed, hardLandSpeed, -fallSpeed);
        if (impact <= 0f) return;

        landDip = -maxLandDip * impact;
        shakeIntensity = Mathf.Max(shakeIntensity, landShakeAmount * impact);
    }

    void UpdateCamera()
    {
        if (cameraTransform == null) return;

        float targetY = isSliding ? slideCameraHeight : defaultCameraY;
        currentCamY = Mathf.Lerp(currentCamY, targetY, Time.deltaTime * slideCameraDropSpeed);

        landDip = Mathf.Lerp(landDip, 0f, Time.deltaTime * landRecoverySpeed);

        HandleBob();

        float targetWallTilt = 0f;
        if (isWallRunning)
            targetWallTilt = wallRight ? wallRunCameraTilt : -wallRunCameraTilt;

        wallTilt = Mathf.Lerp(wallTilt, targetWallTilt, Time.deltaTime * wallTiltSpeed);

        if (isSliding)
            shakeIntensity = Mathf.Max(shakeIntensity, slideShakeAmount);

        if (isWallRunning)
            shakeIntensity = Mathf.Max(shakeIntensity, wallRunShake);

        shakeIntensity = Mathf.Lerp(shakeIntensity, 0f, Time.deltaTime * shakeDecay);

        if (cam != null)
        {
            float targetFOV = baseFOV;

            if (isWallRunning) targetFOV = wallRunFOV;
            else if (isRolling) targetFOV = rollFOV;
            else if (isSliding) targetFOV = slideFOV;
            else if (sprint && horizontalVelocity.magnitude > 1f) targetFOV = sprintFOV;

            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovSpeed);
        }

        float n = Time.time * shakeFrequency;
        float sx = (Mathf.PerlinNoise(shakeSeed, n) - 0.5f) * 2f;
        float sy = (Mathf.PerlinNoise(shakeSeed + 13f, n) - 0.5f) * 2f;
        float sp = (Mathf.PerlinNoise(shakeSeed + 27f, n) - 0.5f) * 2f;
        float sr = (Mathf.PerlinNoise(shakeSeed + 41f, n) - 0.5f) * 2f;

        Vector3 p = cameraTransform.localPosition;
        p.x = defaultCameraX + sx * shakePosAmount * shakeIntensity;
        p.y = currentCamY + landDip + bobY + sy * shakePosAmount * shakeIntensity;
        p.z = defaultCameraZ;
        cameraTransform.localPosition = p;

        if (mouseLook != null)
        {
            mouseLook.bobRoll = bobRoll;
            mouseLook.shakePitch = sp * shakeRotAmount * shakeIntensity;
            mouseLook.shakeRoll = sr * shakeRotAmount * shakeIntensity;
            mouseLook.wallTilt = wallTilt;
        }
    }

    void HandleBob()
    {
        bool bobbing = controller.isGrounded && !isSliding && !isVaulting && !isWallRunning && !isMantling && !isRolling
                       && horizontalVelocity.magnitude > 0.5f;

        if (bobbing)
        {
            float speedFactor = Mathf.Clamp01(horizontalVelocity.magnitude / sprintSpeed);
            bobTimer += Time.deltaTime * bobFrequency * (0.6f + speedFactor);

            bobY = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmount * speedFactor;
            bobRoll = Mathf.Cos(bobTimer * Mathf.PI) * bobRollAmount * speedFactor;
        }
        else
        {
            bobY = Mathf.Lerp(bobY, 0f, Time.deltaTime * 8f);
            bobRoll = Mathf.Lerp(bobRoll, 0f, Time.deltaTime * 8f);
        }
    }

    IEnumerator Roll()
    {
        isRolling = true;
        SetAnimatorIdle();

        Vector3 rollDir = horizontalVelocity.sqrMagnitude > 1f
            ? new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z).normalized
            : transform.forward;

        float entrySpeed = Mathf.Max(horizontalVelocity.magnitude, rollSpeed);

        controller.height = rollHeight;
        controller.center = new Vector3(defaultCenter.x, rollHeight / 2f, defaultCenter.z);

        verticalVelocity = -2f;

        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rollDuration);

            float e = t * t * (3f - 2f * t);

            if (mouseLook != null)
                mouseLook.rollPitch = e * rollCameraSpin;

            verticalVelocity += gravity * Time.deltaTime;
            horizontalVelocity = rollDir * entrySpeed;

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            yield return null;
        }

        float stuck = 0f;
        while (IsBlocked(transform.position) && stuck < 2f)
        {
            stuck += Time.deltaTime;
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move((rollDir * 3f + Vector3.up * verticalVelocity) * Time.deltaTime);
            yield return null;
        }

        controller.height = defaultHeight;
        controller.center = defaultCenter;

        if (mouseLook != null)
            mouseLook.rollPitch = 0f;

        wasGrounded = controller.isGrounded;
        isRolling = false;
    }

    bool TryGetMantle(out Vector3 ledgePoint)
    {
        ledgePoint = Vector3.zero;

        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Debug.DrawRay(origin, transform.forward * mantleDetectDistance, Color.yellow);

        RaycastHit hit;
        if (!Physics.Raycast(origin, transform.forward, out hit, mantleDetectDistance))
            return false;

        Vector3 topProbe = hit.point + transform.forward * 0.25f + Vector3.up * (maxMantleHeight + 0.5f);
        RaycastHit topHit;
        if (!Physics.Raycast(topProbe, Vector3.down, out topHit, maxMantleHeight + 1.5f))
            return false;

        float ledgeHeight = topHit.point.y - transform.position.y;
        if (ledgeHeight < minMantleHeight || ledgeHeight > maxMantleHeight)
            return false;

        if (Vector3.Dot(topHit.normal, Vector3.up) < 0.7f)
            return false;

        Vector3 standPoint = topHit.point + transform.forward * mantleForwardOffset;

        if (IsBlocked(standPoint))
            return false;

        ledgePoint = standPoint;
        return true;
    }

    IEnumerator Mantle(Vector3 target)
    {
        isMantling = true;
        SetAnimatorIdle();

        Vector3 start = transform.position;
        Vector3 mid = new Vector3(start.x, target.y + 0.15f, start.z);

        float risePhase = mantleDuration * 0.55f;
        float pushPhase = mantleDuration - risePhase;

        float elapsed = 0f;
        while (elapsed < risePhase)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / risePhase);
            float e = t * t * (3f - 2f * t);

            controller.enabled = false;
            transform.position = Vector3.Lerp(start, mid, e);
            controller.enabled = true;

            if (mouseLook != null)
                mouseLook.vaultDip = Mathf.Sin(t * Mathf.PI * 0.5f) * mantleDipAmount;

            yield return null;
        }

        elapsed = 0f;
        while (elapsed < pushPhase)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pushPhase);
            float e = t * t * (3f - 2f * t);

            controller.enabled = false;
            transform.position = Vector3.Lerp(mid, target, e);
            controller.enabled = true;

            if (mouseLook != null)
                mouseLook.vaultDip = Mathf.Cos(t * Mathf.PI * 0.5f) * mantleDipAmount;

            yield return null;
        }

        controller.enabled = false;
        transform.position = target + Vector3.up * 0.05f;
        controller.enabled = true;

        if (mouseLook != null)
            mouseLook.vaultDip = 0f;

        verticalVelocity = 0f;
        horizontalVelocity = transform.forward * walkSpeed;
        wasGrounded = true;
        jumpLockout = 0f;
        isMantling = false;
    }

    void CheckWalls()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;

        RaycastHit rightHit;
        RaycastHit leftHit;

        wallRight = Physics.Raycast(origin, transform.right, out rightHit, wallCheckDistance);
        wallLeft = Physics.Raycast(origin, -transform.right, out leftHit, wallCheckDistance);

        Debug.DrawRay(origin, transform.right * wallCheckDistance, Color.cyan);
        Debug.DrawRay(origin, -transform.right * wallCheckDistance, Color.cyan);

        if (wallRight) wallHit = rightHit;
        else if (wallLeft) wallHit = leftHit;
    }

    bool CanStartWallRun(bool grounded, float forwardInput)
    {
        if (grounded) return false;
        if (isVaulting || isSliding || isMantling || isRolling || isPoleSliding || isBarHanging) return false;
        if (wallCooldownTimer > 0f) return false;
        if (!wallLeft && !wallRight) return false;
        if (forwardInput <= 0.1f) return false;
        if (horizontalVelocity.magnitude < wallRunMinSpeed) return false;
        if (verticalVelocity > 2f) return false;

        return true;
    }

    void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = wallRunMaxDuration;
        wallNormal = wallHit.normal;
        verticalVelocity = wallRunEntryPop;
    }

    void WallRunUpdate(float forwardInput)
    {
        wallRunTimer -= Time.deltaTime;

        bool stillOnWall = wallLeft || wallRight;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            WallJump();
            return;
        }

        if (!stillOnWall || wallRunTimer <= 0f || forwardInput <= 0.1f || controller.isGrounded)
        {
            EndWallRun();
            return;
        }

        wallNormal = wallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(wallForward, transform.forward) < 0f)
            wallForward = -wallForward;

        verticalVelocity += wallRunGravity * Time.deltaTime;

        horizontalVelocity = wallForward * wallRunSpeed;

        Vector3 stick = -wallNormal * wallStickForce;
        Vector3 move = horizontalVelocity + stick + Vector3.up * verticalVelocity;

        controller.Move(move * Time.deltaTime);

        animator.SetBool("grounded", false);
    }

    void WallJump()
    {
        Vector3 launch = wallNormal * wallJumpSideForce + transform.forward * wallJumpForwardForce;
        horizontalVelocity = launch;
        verticalVelocity = wallJumpUpForce;

        jumpLockout = 0.2f;
        animator.SetTrigger("jump");

        EndWallRun();
    }

    void EndWallRun()
    {
        isWallRunning = false;
        wallCooldownTimer = wallRunCooldown;
    }

    bool TryGetVault(out Vector3 landingPoint, out float obstacleTop)
    {
        landingPoint = Vector3.zero;
        obstacleTop = 0f;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Debug.DrawRay(origin, transform.forward * vaultDetectDistance, Color.red);

        RaycastHit hit;
        if (!Physics.Raycast(origin, transform.forward, out hit, vaultDetectDistance))
            return false;

        if (!hit.collider.CompareTag("Vaultable"))
            return false;

        Vector3 topProbe = hit.point + transform.forward * 0.1f + Vector3.up * (maxVaultHeight + 1f);
        RaycastHit topHit;
        if (!Physics.Raycast(topProbe, Vector3.down, out topHit, maxVaultHeight + 2f))
            return false;

        obstacleTop = topHit.point.y;

        if (obstacleTop - transform.position.y > maxVaultHeight)
            return false;

        Vector3 landProbe = hit.point + transform.forward * vaultForwardDistance;
        landProbe.y = obstacleTop + 1f;

        RaycastHit groundHit;
        if (!Physics.Raycast(landProbe, Vector3.down, out groundHit, maxVaultHeight + 4f))
            return false;

        landingPoint = groundHit.point;

        if (IsBlocked(landingPoint))
            return false;

        return true;
    }

    bool IsBlocked(Vector3 footPos)
    {
        float r = controller.radius * 0.9f;
        Vector3 bottom = footPos + Vector3.up * (controller.radius + 0.05f);
        Vector3 top = footPos + Vector3.up * (defaultHeight - controller.radius);

        controller.enabled = false;
        bool blocked = Physics.CheckCapsule(bottom, top, r);
        controller.enabled = true;

        return blocked;
    }

    IEnumerator Vault(Vector3 landingPoint, float obstacleTop)
    {
        isVaulting = true;
        SetAnimatorIdle();

        Vector3 start = transform.position;
        Vector3 end = landingPoint;

        float peakY = obstacleTop + vaultClearance;
        float peakOffset = Mathf.Max(peakY - Mathf.Max(start.y, end.y), 0.2f);

        float elapsed = 0f;
        while (elapsed < vaultDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / vaultDuration);
            float easedT = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(start, end, easedT);
            pos.y = Mathf.Lerp(start.y, end.y, easedT) + Mathf.Sin(t * Mathf.PI) * peakOffset;

            controller.enabled = false;
            transform.position = pos;
            controller.enabled = true;

            float curve = Mathf.Sin(t * Mathf.PI);
            if (mouseLook != null)
            {
                mouseLook.vaultTilt = curve * vaultTiltAmount;
                mouseLook.vaultDip = curve * vaultDipAmount;
            }

            yield return null;
        }

        controller.enabled = false;
        transform.position = end + Vector3.up * 0.05f;
        controller.enabled = true;

        if (mouseLook != null)
        {
            mouseLook.vaultTilt = 0f;
            mouseLook.vaultDip = 0f;
        }

        verticalVelocity = 0f;
        horizontalVelocity = transform.forward * sprintSpeed * 0.6f;
        wasGrounded = true;
        isVaulting = false;
    }

    IEnumerator Slide()
    {
        isSliding = true;
        SetAnimatorIdle();
        animator.SetTrigger("slide");

        Vector3 slideDirection = transform.forward;

        controller.height = slideHeight;
        controller.center = new Vector3(defaultCenter.x, slideHeight / 2f, defaultCenter.z);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;

            SetAnimatorIdle();

            if (IsWallAhead(slideDirection))
                break;

            float decay = Mathf.Max(0f, (t - 0.55f) / 0.45f);
            float speedCurve = Mathf.Lerp(slideSpeed, sprintSpeed, decay);

            verticalVelocity += gravity * Time.deltaTime;
            horizontalVelocity = slideDirection * speedCurve;

            Vector3 slideMove = horizontalVelocity + Vector3.up * verticalVelocity;
            controller.Move(slideMove * Time.deltaTime);

            float entry = Mathf.Min(t * 5f, 1f);
            float exit = 1f - Mathf.Max((t - 0.75f) * 4f, 0f);
            float curve = entry * exit;

            if (mouseLook != null)
            {
                mouseLook.vaultTilt = curve * slideTiltAmount;
                mouseLook.vaultDip = curve * slideDipAmount;
            }

            yield return null;
        }

        float stuckTimer = 0f;
        while (IsBlocked(transform.position) && stuckTimer < 3f)
        {
            stuckTimer += Time.deltaTime;
            SetAnimatorIdle();

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 crawl = IsWallAhead(slideDirection) ? Vector3.zero : slideDirection * 3f;
            controller.Move((crawl + Vector3.up * verticalVelocity) * Time.deltaTime);

            yield return null;
        }

        controller.height = defaultHeight;
        controller.center = defaultCenter;

        if (mouseLook != null)
        {
            mouseLook.vaultTilt = 0f;
            mouseLook.vaultDip = 0f;
        }

        wasGrounded = controller.isGrounded;
        isSliding = false;
    }

    bool IsWallAhead(Vector3 dir)
    {
        Vector3 origin = transform.position + Vector3.up * (slideHeight * 0.5f);
        return Physics.Raycast(origin, dir, controller.radius + 0.3f);
    }

    // ---------------------------------------------------------------
    // POLE SLIDE
    // ---------------------------------------------------------------
    bool TryGetPole(out Transform pole)
    {
        pole = null;
        Vector3 origin = transform.position + Vector3.up * 1f;
        Debug.DrawRay(origin, transform.forward * poleDetectDistance, Color.magenta);

        RaycastHit hit;
        if (!Physics.Raycast(origin, transform.forward, out hit, poleDetectDistance))
            return false;

        if (!hit.collider.CompareTag("Pole"))
            return false;

        pole = hit.collider.transform;
        return true;
    }

    IEnumerator PoleSlide(Transform pole)
    {
        isPoleSliding = true;
        SetAnimatorIdle();

        Debug.Log("POLE: coroutine started. Pole=" + pole.name);

        verticalVelocity = 0f;
        horizontalVelocity = Vector3.zero;

        // Work out a fixed spot beside the pole to hang at.
        Vector3 poleXZ = new Vector3(pole.position.x, 0f, pole.position.z);
        Vector3 playerXZ = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 awayFromPole = (playerXZ - poleXZ);
        awayFromPole.y = 0f;

        if (awayFromPole.sqrMagnitude < 0.01f)
            awayFromPole = -transform.forward;
        awayFromPole = awayFromPole.normalized;

        Vector3 hangXZ = poleXZ + awayFromPole * poleGrabOffset;

        controller.enabled = false;
        transform.position = new Vector3(hangXZ.x, transform.position.y, hangXZ.z);
        controller.enabled = true;

        float groundY = transform.position.y - 100f;
        RaycastHit floorHit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out floorHit, 200f))
            groundY = floorHit.point.y;

        float currentY = transform.position.y;

        Debug.Log("POLE: hanging at " + transform.position + " groundY=" + groundY);

        // Wait one frame so the same E press that grabbed the pole
        // isn't immediately read as a release.
        yield return null;

        while (isPoleSliding)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("POLE: E pressed - dropping");
                EndPoleSlide(false);
                yield break;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("POLE: Space pressed - jumping off");
                EndPoleSlide(true);
                yield break;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                currentY -= poleSlideSpeed * Time.deltaTime;
                Debug.Log("POLE: Q held, currentY=" + currentY);
            }
            else if (Input.GetKey(KeyCode.W))
            {
                currentY += poleClimbSpeed * Time.deltaTime;
                Debug.Log("POLE: W held, currentY=" + currentY);
            }

            controller.enabled = false;
            transform.position = new Vector3(hangXZ.x, currentY, hangXZ.z);
            controller.enabled = true;

            if (mouseLook != null)
                mouseLook.vaultTilt = poleTiltAmount;

            if (currentY <= groundY + 0.3f)
            {
                Debug.Log("POLE: reached bottom - releasing");
                EndPoleSlide(false);
                yield break;
            }

            yield return null;
        }

        Debug.Log("POLE: while loop exited (isPoleSliding went false)");
    }

    void EndPoleSlide(bool jumpOff)
    {
        if (mouseLook != null)
            mouseLook.vaultTilt = 0f;

        // Nudge the player away from the pole so they don't re-collide and get stuck.
        controller.enabled = false;
        transform.position += transform.forward * (poleGrabOffset + 0.3f);
        controller.enabled = true;

        if (jumpOff)
        {
            horizontalVelocity = transform.forward * poleJumpOffForce;
            verticalVelocity = poleJumpUpForce;
            jumpLockout = 0.2f;
            animator.SetTrigger("jump");
        }
        else
        {
            verticalVelocity = -2f;
            horizontalVelocity = Vector3.zero;
        }

        wasGrounded = controller.isGrounded;
        isPoleSliding = false;
    }

    // ---------------------------------------------------------------
    // MONKEY BARS (grab overhead bar, hang, swing to the next)
    // ---------------------------------------------------------------
    bool TryGetBar(out Transform bar)
    {
        bar = null;

        // Look forward and slightly up, since bars are at/above head height.
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 dir = (transform.forward + Vector3.up * 0.35f).normalized;
        Debug.DrawRay(origin, dir * barDetectDistance, Color.green);

        RaycastHit hit;
        if (!Physics.Raycast(origin, dir, out hit, barDetectDistance))
            return false;

        if (!hit.collider.CompareTag("Bar"))
            return false;

        bar = hit.collider.transform;
        return true;
    }

    IEnumerator BarHang(Transform bar)
    {
        isBarHanging = true;
        SetAnimatorIdle();

        verticalVelocity = 0f;
        horizontalVelocity = Vector3.zero;

        // Hang directly under the bar's center, body dropped below it.
        Vector3 hangPos = new Vector3(
            bar.position.x,
            bar.position.y - barHangDrop,
            bar.position.z);

        controller.enabled = false;
        transform.position = hangPos;
        controller.enabled = true;

        // Wait one frame so the grab-E isn't read as a release this frame.
        yield return null;

        while (isBarHanging)
        {
            // Release: E drops straight down, Space swings forward to the next bar.
            if (Input.GetKeyDown(KeyCode.E))
            {
                EndBarHang(false);
                yield break;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                EndBarHang(true);
                yield break;
            }

            // Optional: shuffle side to side along the bar with A/D.
            float side = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(side) > 0.1f)
                hangPos += transform.right * side * barSideSpeed * Time.deltaTime;

            controller.enabled = false;
            transform.position = hangPos;
            controller.enabled = true;

            if (mouseLook != null)
                mouseLook.vaultTilt = barTiltAmount;

            yield return null;
        }
    }

    void EndBarHang(bool swing)
    {
        if (mouseLook != null)
            mouseLook.vaultTilt = 0f;

        if (swing)
        {
            // Launch forward and up toward the next bar / platform.
            horizontalVelocity = transform.forward * barSwingForce;
            verticalVelocity = barSwingUpForce;
            jumpLockout = 0.2f;
            animator.SetTrigger("jump");
        }
        else
        {
            verticalVelocity = -2f;
            horizontalVelocity = Vector3.zero;
        }

        wasGrounded = false;
        isBarHanging = false;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (levelManager != null && hit.collider.CompareTag("Killzone"))
            levelManager.Respawn();
    }

    public void ResetMovement()
    {
        StopAllCoroutines();

        isVaulting = false;
        isSliding = false;
        isMantling = false;
        isRolling = false;
        isWallRunning = false;
        isPoleSliding = false;
        isBarHanging = false;

        verticalVelocity = 0f;
        horizontalVelocity = Vector3.zero;
        sprint = false;

        jumpLockout = 0f;
        wallCooldownTimer = 0f;
        rollBuffer = 0f;

        landDip = 0f;
        shakeIntensity = 0f;
        wallTilt = 0f;
        bobY = 0f;
        bobRoll = 0f;

        if (defaultHeight > 0f)
        {
            controller.height = defaultHeight;
            controller.center = defaultCenter;
        }

        if (mouseLook != null)
        {
            mouseLook.vaultTilt = 0f;
            mouseLook.vaultDip = 0f;
            mouseLook.bobRoll = 0f;
            mouseLook.shakePitch = 0f;
            mouseLook.shakeRoll = 0f;
            mouseLook.wallTilt = 0f;
            mouseLook.rollPitch = 0f;
        }

        if (cameraTransform != null)
        {
            currentCamY = defaultCameraY;
            Vector3 p = cameraTransform.localPosition;
            p.y = defaultCameraY;
            cameraTransform.localPosition = p;
        }

        if (cam != null)
            cam.fieldOfView = baseFOV;

        SetAnimatorIdle();
        wasGrounded = true;
    }
}
