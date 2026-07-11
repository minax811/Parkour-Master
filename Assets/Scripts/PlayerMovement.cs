using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private bool sprint;

    public Animator animator;
    public CharacterController controller;
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;
    public float gravity = -20f;

    // --- new jump fields ---
    public float jumpHeight = 1.5f;

    private float verticalVelocity;

    void Update()
    {
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
    
    void Move()
    {
        Debug.Log("Grounded: " + controller.isGrounded);
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        float speed = sprint ? sprintSpeed : walkSpeed;

        // reset downward velocity when grounded
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        // --- jump: debug version ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space pressed. Grounded: " + controller.isGrounded);
            if (controller.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.SetTrigger("jump");
            }
        }

        // apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = move * speed + Vector3.up * verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);
    }
}