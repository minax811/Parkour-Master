using UnityEngine;
 
public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 200f;
    public Transform playerBody;
 
    private float xRotation = 0f;
 
    public float vaultTilt = 0f;
    public float vaultDip = 0f;
    public float bobRoll = 0f;
 
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
 
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
 
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -70f, 60f);
 
        transform.localRotation = Quaternion.Euler(xRotation + vaultDip, 0f, vaultTilt + bobRoll);
 
        playerBody.Rotate(Vector3.up * mouseX);
    }
}