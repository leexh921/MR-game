using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Gun : MonoBehaviour
{
    public Transform muzzlePoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 80f;

    XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        grabInteractable.activated.AddListener(OnActivate);
    }

    void OnDestroy()
    {
        grabInteractable.activated.RemoveListener(OnActivate);
    }

    private void OnActivate(ActivateEventArgs args)
    {
        Shoot();
    }

    void Shoot()
    {
        if (bulletPrefab == null)
        {
            return;
        }

        if (muzzlePoint == null)
        {
            return;
        }
        GameObject bullet =
            Instantiate(
                bulletPrefab,
                muzzlePoint.position,
                muzzlePoint.rotation);

        bullet.transform.Rotate(90, 0, 0);
        

        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity =
                muzzlePoint.forward * bulletSpeed;
        }
    }
}
