using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;
    public float lifeTime = 3f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint hit = collision.contacts[0];

        // 生成弹孔
        BulletHoleManager.Instance.SpawnBulletHoleFromCollision(hit);

        Destroy(gameObject);
    }
}