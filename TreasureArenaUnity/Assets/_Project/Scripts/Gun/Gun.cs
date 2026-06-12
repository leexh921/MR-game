using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("枪位置")]
    public Transform holdPoint;       // 枪模型本身挂的点
    public Transform muzzlePoint;     // 射击点
    public GameObject bulletHolePrefab;
    public float range = 50f;

    private bool isPickedUp = false;

    // 拾枪
    public void PickUp(Transform playerHoldPoint)
    {
        if (isPickedUp) return;
        isPickedUp = true;

        // 挂在玩家手上
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

        // 禁用物理
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    // 射击
    public void HandleShooting()
    {
        if (Input.GetButtonDown("Fire1") && muzzlePoint != null)
        {
            Shoot();
        }
    }

    void Shoot()
{
    Ray ray = new Ray(muzzlePoint.position, muzzlePoint.forward);

    if (Physics.Raycast(ray, out RaycastHit hit, range))
    {
        // 🔴 1. 强制检测 prefab
        if (bulletHolePrefab == null)
        {
            Debug.LogError("BulletHolePrefab 没有绑定！");
            return;
        }

        // 🔴 2. 生成弹孔（稍微往外推一点）
       GameObject hole = Instantiate(
    bulletHolePrefab,
    hit.point + hit.normal * 0.01f,
    Quaternion.LookRotation(hit.normal)
);

hole.transform.localScale = Vector3.one * 0.05f;
hole.transform.SetParent(hit.collider.transform, true);

        // 🔴 5. 物理反应
        if (hit.rigidbody != null)
        {
            hit.rigidbody.AddForce(ray.direction * 10f, ForceMode.Impulse);
        }
    }
}
}