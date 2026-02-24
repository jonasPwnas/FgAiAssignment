using Player;
using System.Collections;
using UnityEngine;
using static ElementTypes;

public class FireAoESpell : MonoBehaviour
{
    [SerializeField] private SphereCollider fireSpellTrigger;
    [SerializeField] private ParticleSystem fireSpellParticleEffect;

    [SerializeField] private float fireSpellDamage;
    [SerializeField] private float fireSpellDuration;

    private ElementType elementType = ElementType.Fire;

    private void OnEnable()
    {
        PlayerController.OnCastSpell += SpawnFireSpell;
    }

    private void OnDisable()
    {
        PlayerController.OnCastSpell -= SpawnFireSpell;
    }

    private void Start()
    {
        fireSpellParticleEffect.Stop();
    }

    public void SpawnFireSpell(ElementTypes.ElementType sourceElement)
    {
        if (sourceElement == ElementTypes.ElementType.Fire)
        {
            fireSpellTrigger.enabled = true;

            StartCoroutine(FireSpellDuration(fireSpellDuration));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<IDamageableElementalSource>() != null)
        {
            print("Takes Damage");

            IDamageableElementalSource target = other.gameObject.GetComponent<IDamageableElementalSource>();

            target.TakeDamage(fireSpellDamage, ElementTypes.ElementType.Fire, false);


        }
    }

    private IEnumerator FireSpellDuration(float fireSpellDuration)
    {
        if (fireSpellParticleEffect.isStopped)
            fireSpellParticleEffect.Play();

        yield return new WaitForSeconds(fireSpellDuration);

        if (fireSpellParticleEffect.isPlaying)
            fireSpellParticleEffect.Stop();

        fireSpellTrigger.enabled = false;
    }
}
