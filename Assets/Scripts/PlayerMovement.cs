using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private bool sprint;

    public Animator animator;

    // --- new movement fields ---
    public CharacterController controller;
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;
    public float gravity = -9.81f;
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
        // read input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // build a movement direction relative to where the character faces
        Vector3 move = transform.right * h + transform.forward * v;

        float speed = sprint ? sprintSpeed : walkSpeed;

        // gravity so the character stays on the ground
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = move * speed + Vector3.up * verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);
    }
}