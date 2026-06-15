using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Transform cameraTransform;

    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    public float interactDistance = 3f;

    public Transform gunHoldPoint;

    private CharacterController cc;
    private float xRotation;

    private Gun targetedGun;
    private Gun currentGun;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Move();
        Look();

        DetectGun();
        PickUpGun();

        if (currentGun != null)
            currentGun.HandleShooting();
    }

    // ---------------- 移动 ----------------
    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        cc.Move(move * moveSpeed * Time.deltaTime);
    }

    // ---------------- 视角 ----------------
    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.Rotate(Vector3.up * mouseX);
    }

    // ---------------- 射线检测 ----------------
    void DetectGun()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            targetedGun = hit.collider.GetComponent<Gun>();
        }
        else
        {
            targetedGun = null;
        }

        // ⭐ 关键：画“运行时可见射线”
        Debug.DrawRay(cameraTransform.position,
            cameraTransform.forward * interactDistance,
            Color.red);
    }

    // ---------------- 拾枪 ----------------
    void PickUpGun()
    {
        if (targetedGun != null && Input.GetKeyDown(KeyCode.E))
        {
            currentGun = targetedGun;
            currentGun.PickUp(gunHoldPoint);
        }
    }

    // ---------------- Gizmos（Game + Scene都能看到） ----------------
    void OnDrawGizmos()
    {
        if (cameraTransform == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(cameraTransform.position,
            cameraTransform.position + cameraTransform.forward * interactDistance);
    }
}