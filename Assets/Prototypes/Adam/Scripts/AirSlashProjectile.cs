using System.Collections;
using UnityEngine;

public class AirSlashProjectile : MonoBehaviour
{
    [Header("Air Push Settings")]
    [SerializeField] private float airPushForce;
    [SerializeField] private float airProjectileDamage = 10f;

    [Header("References")]
    [SerializeField] private GameObject projectile;
    [SerializeField] private ParticleSystem onImpactParticle;
    [SerializeField] private ParticleSystem airProjectileParticle;

    private ElementTypes.ElementType elementType = ElementTypes.ElementType.Air;

    private void Start()
    {
        airProjectileParticle.Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        StartCoroutine(ParticleEffects());

        if (other.gameObject.GetComponent<IDamageableElementalSource>() != null)
        {
            IDamageableElementalSource target = other.gameObject.GetComponent<IDamageableElementalSource>();

            target.TakeDamage(airProjectileDamage, elementType, false);

            if (other.gameObject.GetComponent<Rigidbody>() != null)
            {
                Rigidbody targetRb = other.gameObject.GetComponent<Rigidbody>();

                if (targetRb.isKinematic)
                {
                    targetRb.isKinematic = false;
                }

                targetRb.AddForce(projectile.transform.forward * airPushForce);
            }
            else if (other.gameObject.GetComponent<CharacterController>() != null)
            {
                StartCoroutine(MoveEnemy(other.gameObject.GetComponent<CharacterController>(), projectile.transform.forward, other.GetComponent<EnemyStatus>().airPushImpactRatio, 0.05f));
            }
        }
        else
        {
            if (other.gameObject.GetComponent<Rigidbody>() != null && !other.CompareTag("Immovable"))
            {
                if (other.gameObject.GetComponent<Rigidbody>().isKinematic)
                {
                    other.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                }

                other.gameObject.GetComponent<Rigidbody>().AddForce(projectile.transform.forward * airPushForce);
            }

            //Destroy(projectile);
        }
    }

    private IEnumerator MoveEnemy(CharacterController targetController, Vector3 projectileHitDirection, float move, float timeBetweenMove)
    {
        int timesToMove = 10;
        Vector3 pushDirection = projectileHitDirection;

        for(int i = 0; i <= timesToMove; i++)
        {
            pushDirection.y = -200f * Time.deltaTime;
            targetController.Move(pushDirection * (move * Time.deltaTime));

            yield return new WaitForSeconds(timeBetweenMove);
        }

        //Destroy(projectile);
    }

    private IEnumerator ParticleEffects()
    {
        onImpactParticle.Play();

        airProjectileParticle.Stop();

        yield return new WaitForSeconds(1f);

        Destroy(projectile);
    }
}


