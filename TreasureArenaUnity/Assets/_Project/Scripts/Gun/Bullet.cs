using UnityEngine;
using TreasureArenaMR.Gameplay;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 3f; // 子弹存在时间
    public GameObject bulletHolePrefab; // 弹孔预制体

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 如果你想让子弹靠 Rigidbody 推动就可以不用这里的移动
        // transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint hit = collision.contacts[0];
        BulletSurface surface = collision.collider.GetComponentInParent<BulletSurface>();
        bool shouldSpawnDecal = surface == null || surface.decal_enabled;

        if (shouldSpawnDecal && BulletHoleManager.Instance != null)
        {
            BulletHoleManager.Instance.SpawnBulletHoleFromCollision(hit);
        }
        else if (shouldSpawnDecal && bulletHolePrefab != null)
        {

            // 生成弹孔（稍微往外推一点）
            GameObject hole = Instantiate(
                bulletHolePrefab,
                hit.point + hit.normal * 0.001f,
                Quaternion.LookRotation(-hit.normal)
            );

            hole.transform.localScale = Vector3.one * 0.05f;
            hole.transform.SetParent(collision.collider.transform, true);
        }

        Destroy(gameObject);
    }
}
