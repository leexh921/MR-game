using UnityEngine;

public class Gun : MonoBehaviour
{
    public Transform muzzlePoint;

    public GameObject bulletPrefab;

    public float bulletSpeed = 60f;

    public void Fire()
    {
        GameObject bullet =
            Instantiate(
                bulletPrefab,
                muzzlePoint.position,
                muzzlePoint.rotation);

        Rigidbody rb =
            bullet.GetComponent<Rigidbody>();

        rb.velocity =
            muzzlePoint.forward * bulletSpeed;
    }
}