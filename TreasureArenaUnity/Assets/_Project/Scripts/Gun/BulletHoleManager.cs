using UnityEngine;

public class BulletHoleManager : MonoBehaviour
{
    // ✅ 单例
    public static BulletHoleManager Instance;

    public GameObject bulletHolePrefab;

    private void Awake()
    {
        // 保证只有一个实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 从射线生成弹孔
    public void SpawnBulletHole(RaycastHit hit)
    {
        GameObject hole = Instantiate(bulletHolePrefab);
        hole.transform.position = hit.point;
        hole.transform.rotation = Quaternion.LookRotation(hit.normal);
        hole.transform.position += hit.normal * 0.01f;
        hole.transform.SetParent(hit.collider.transform);
    }

    // 从碰撞生成弹孔（子弹用）
    public void SpawnBulletHoleFromCollision(ContactPoint hit)
    {
        GameObject hole = Instantiate(bulletHolePrefab);
        hole.transform.position = hit.point;
        hole.transform.rotation = Quaternion.LookRotation(hit.normal);
        hole.transform.position += hit.normal * 0.01f;
        hole.transform.SetParent(hit.otherCollider.transform);
    }
}