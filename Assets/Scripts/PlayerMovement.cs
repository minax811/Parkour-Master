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
    public float hardLandSpeed = 22f;
    public float maxLandDip = 0.35f;
    public float landRecoverySpeed = 7f;
 
    [Header("Head Bob")]
    public float bobFrequency = 1.9f;
    public float bobAmount = 0.055f;
    public float bobRollAmount = 1.2f;
 
    [Header("Vault")]
    public float vaultDetectDistance = 2.5f;
    public float vaultDuration = 0.5f;
    public float vaultForwardDistance = 2.5f;
    public float maxVaultHeight = 1.8f;
    public float vaultClearance = 0.4f;
 
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
 
    private float verticalVelocity;
    private Vector3 horizontalVelocity;
 
    private bool isVaulting = false;
    private bool isSliding = false;
    private bool wasGrounded = true;
    private float jumpLockout = 0f;
 
    private float defaultHeight;
    private Vector3 defaultCenter;
    private float defaultCameraY;
 
    private float currentCamY;
    private float landDip;
    private float bobTimer;
    private float bobY;
    private float bobRoll;
 
    void Start()
    {
        defaultHeight = controller.height;
        defaultCenter = controller.center;
 
        if (cameraTransform != null)
        {
            defaultCameraY = cameraTransform.localPosition.y;
            currentCamY = defaultCameraY;
        }
    }
 
    void Update()
    {
        if (isVaulting || isSliding) return;
 
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
 
        bool grounded = controller.isGrounded && jumpLockout <= 0f;
        float fallSpeed = verticalVelocity;
 
        if (grounded && !wasGrounded)
            OnLand(fallSpeed);
 
        wasGrounded = grounded;
 
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
 
        if (Input.GetKeyDown(KeyCode.LeftControl) && sprint && grounded && inputDir.magnitude > 0.1f)
        {
            StartCoroutine(Slide());
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
            else if (grounded)
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
        float impact = Mathf.InverseLerp(4f, hardLandSpeed, -fallSpeed);
        if (impact <= 0f) return;
 
        landDip = -maxLandDip * impact;
    }
 
    void UpdateCamera()
    {
        if (cameraTransform == null) return;
 
        float targetY = isSliding ? slideCameraHeight : defaultCameraY;
        currentCamY = Mathf.Lerp(currentCamY, targetY, Time.deltaTime * slideCameraDropSpeed);
 
        landDip = Mathf.Lerp(landDip, 0f, Time.deltaTime * landRecoverySpeed);
 
        HandleBob();
 
        Vector3 p = cameraTransform.localPosition;
        p.y = currentCamY + landDip + bobY;
        cameraTransform.localPosition = p;
 
        if (mouseLook != null)
            mouseLook.bobRoll = bobRoll;
    }
 
    void HandleBob()
    {
        bool bobbing = controller.isGrounded && !isSliding && !isVaulting
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
}