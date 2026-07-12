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
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
 
    [Header("Vault")]
    public float vaultDetectDistance = 2.5f;
    public float vaultDuration = 0.6f;
    public float vaultForwardDistance = 2f;
    public float vaultUpHeight = 1.2f;
 
    [Header("Vault Camera Feel")]
    public float vaultTiltAmount = 18f;
    public float vaultDipAmount = 12f;
 
    [Header("Slide")]
    public float slideDuration = 1.3f;
    public float slideSpeed = 15f;
    public float slideHeight = 1f;
 
    [Header("Slide Camera Feel")]
    public float slideCameraHeight = 0.25f;
    public float slideTiltAmount = 16f;
    public float slideDipAmount = 10f;
    public float slideCameraDropSpeed = 8f;
 
    private float verticalVelocity;
    private bool isVaulting = false;
    private bool isSliding = false;
 
    private float defaultHeight;
    private Vector3 defaultCenter;
    private float defaultCameraY;
 
    void Start()
    {
        defaultHeight = controller.height;
        defaultCenter = controller.center;
        if (cameraTransform != null)
            defaultCameraY = cameraTransform.localPosition.y;
    }
 
    void Update()
    {
        if (isVaulting || isSliding) return;
 
        Sprint();
        AnimationWalking();
        Move();
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
    }
 
    void SetAnimatorIdle()
    {
        animator.SetFloat("walkForwardValue", 0f);
        animator.SetFloat("sidewalkValue", 0f);
        animator.SetBool("sprint", false);
    }
 
    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
 
        Vector3 move = transform.right * h + transform.forward * v;
        float speed = sprint ? sprintSpeed : walkSpeed;
 
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;
 
        if (Input.GetKeyDown(KeyCode.LeftControl) && sprint && controller.isGrounded && move.magnitude > 0.1f)
        {
            StartCoroutine(Slide());
            return;
        }
 
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (CanVault())
            {
                StartCoroutine(Vault());
                return;
            }
            else if (controller.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.SetTrigger("jump");
            }
        }
 
        verticalVelocity += gravity * Time.deltaTime;
 
        Vector3 finalMove = move * speed + Vector3.up * verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);
    }
 
    bool CanVault()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
 
        Debug.DrawRay(origin, transform.forward * vaultDetectDistance, Color.red);
 
        if (Physics.Raycast(origin, transform.forward, out hit, vaultDetectDistance))
        {
            if (hit.collider.CompareTag("Vaultable"))
                return true;
        }
        return false;
    }
 
    IEnumerator Vault()
    {
        isVaulting = true;
        SetAnimatorIdle();
 
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * vaultForwardDistance;
 
        float elapsed = 0f;
        while (elapsed < vaultDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / vaultDuration;
 
            float easedT = t * t * (3f - 2f * t);
 
            Vector3 horizontal = Vector3.Lerp(start, end, easedT);
            float height = Mathf.Sin(t * Mathf.PI) * vaultUpHeight;
 
            Vector3 newPos = horizontal;
            newPos.y = Mathf.Lerp(start.y, end.y, easedT) + height;
 
            controller.enabled = false;
            transform.position = newPos;
            controller.enabled = true;
 
            float curve = Mathf.Sin(t * Mathf.PI);
            if (mouseLook != null)
            {
                mouseLook.vaultTilt = curve * vaultTiltAmount;
                mouseLook.vaultDip = curve * vaultDipAmount;
            }
 
            yield return null;
        }
 
        if (mouseLook != null)
        {
            mouseLook.vaultTilt = 0f;
            mouseLook.vaultDip = 0f;
        }
 
        verticalVelocity = 0f;
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
 
            float decay = Mathf.Max(0f, (t - 0.55f) / 0.45f);
            float speedCurve = Mathf.Lerp(slideSpeed, sprintSpeed, decay);
 
            verticalVelocity += gravity * Time.deltaTime;
            Vector3 slideMove = slideDirection * speedCurve + Vector3.up * verticalVelocity;
            controller.Move(slideMove * Time.deltaTime);
 
            if (cameraTransform != null)
            {
                Vector3 camPos = cameraTransform.localPosition;
                camPos.y = Mathf.Lerp(camPos.y, slideCameraHeight, Time.deltaTime * slideCameraDropSpeed);
                cameraTransform.localPosition = camPos;
            }
 
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
 
        while (!CanStandUp())
        {
            SetAnimatorIdle();
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move((slideDirection * 2f + Vector3.up * verticalVelocity) * Time.deltaTime);
            yield return null;
        }
 
        controller.height = defaultHeight;
        controller.center = defaultCenter;
 
        float standElapsed = 0f;
        while (standElapsed < 0.2f)
        {
            standElapsed += Time.deltaTime;
 
            if (cameraTransform != null)
            {
                Vector3 camPos = cameraTransform.localPosition;
                camPos.y = Mathf.Lerp(camPos.y, defaultCameraY, Time.deltaTime * slideCameraDropSpeed);
                cameraTransform.localPosition = camPos;
            }
 
            if (mouseLook != null)
            {
                mouseLook.vaultTilt = Mathf.Lerp(mouseLook.vaultTilt, 0f, Time.deltaTime * 10f);
                mouseLook.vaultDip = Mathf.Lerp(mouseLook.vaultDip, 0f, Time.deltaTime * 10f);
            }
 
            yield return null;
        }
 
        if (cameraTransform != null)
        {
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = defaultCameraY;
            cameraTransform.localPosition = camPos;
        }
 
        if (mouseLook != null)
        {
            mouseLook.vaultTilt = 0f;
            mouseLook.vaultDip = 0f;
        }
 
        isSliding = false;
    }
 
    bool CanStandUp()
    {
        Vector3 origin = transform.position + Vector3.up * slideHeight;
        float checkDistance = defaultHeight - slideHeight;
 
        Debug.DrawRay(origin, Vector3.up * checkDistance, Color.green);
 
        return !Physics.Raycast(origin, Vector3.up, checkDistance);
    }
}