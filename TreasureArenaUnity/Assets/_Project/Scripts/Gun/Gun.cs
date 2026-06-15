using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("枪位置")]
    public Transform holdPoint;       // 枪模型挂点
    public Transform muzzlePoint;     // 射击点

    [Header("子弹")]
    public GameObject bulletPrefab;   // 子弹预制体
    public float bulletSpeed = 50f;   // 子弹发射速度

    private bool isPickedUp = false;

    // 拾枪
    public void PickUp(Transform playerHoldPoint)
    {
        if (isPickedUp) return;
        isPickedUp = true;

        transform.SetParent(playerHoldPoint);
        if (holdPoint != null)
        {
            transform.localPosition = holdPoint.localPosition;
            transform.localRotation = holdPoint.localRotation;
        }
        else
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    // 射击
    public void HandleShooting()
    {
        if (Input.GetButtonDown("Fire1") && muzzlePoint != null && bulletPrefab != null)
        {
            Shoot();
        }
    }

 void Shoot()
{
    GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

    // 给子弹加一个空父物体，父物体朝向枪口
    GameObject wrapper = new GameObject("BulletWrapper");
    wrapper.transform.position = muzzlePoint.position;
    wrapper.transform.rotation = muzzlePoint.rotation;

    bullet.transform.SetParent(wrapper.transform);
    bullet.transform.localRotation = Quaternion.Euler(90, 0, 0); // 调整模型竖直->水平

    Rigidbody rb = bullet.GetComponent<Rigidbody>();
    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    rb.velocity = wrapper.transform.forward * 80f; // 子弹速度
}
}