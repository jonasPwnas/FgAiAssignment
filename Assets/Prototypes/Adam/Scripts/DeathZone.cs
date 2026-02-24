using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private ElementTypes.ElementType elementType = ElementTypes.ElementType.NoElement;
    private float deathZoneDamage = 1000000;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<IDamageableElementalSource>() != null)
        {
            IDamageableElementalSource target = other.gameObject.GetComponent<IDamageableElementalSource>();

            target.TakeDamage(deathZoneDamage, elementType, false);
        }
    }
}
