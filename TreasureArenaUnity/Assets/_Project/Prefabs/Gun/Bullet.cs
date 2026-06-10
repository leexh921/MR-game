using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 5f;

    public GameObject bulletHolePrefab;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
{
    if(collision.gameObject.layer ==
       LayerMask.NameToLayer("Wall"))
    {
        ContactPoint cp = collision.contacts[0];

        Instantiate(
            bulletHolePrefab,
            cp.point + cp.normal * 0.01f,
            Quaternion.LookRotation(cp.normal)
        );
    }

    Destroy(gameObject);
}
}