using DG.Tweening;
using Player;
using System.Collections;
using UnityEngine;
using static ElementTypes;


public class WaterSpellHealing : MonoBehaviour
{
    [SerializeField] private SphereCollider healTrigger;

    [SerializeField] private float healAmount = 30;

    [SerializeField] private ParticleSystem waterHealingVFX;

    ElementType elementType = ElementType.Water;
    private void OnEnable()
    {
        PlayerController.OnCastSpell += WaterHealSpell;
    }

    private void OnDisable()
    {
        PlayerController.OnCastSpell -= WaterHealSpell;
    }

    public void WaterHealSpell(ElementTypes.ElementType sourceElement)
    {
        if (sourceElement == ElementTypes.ElementType.Water)
        {
            healTrigger.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<IDamageableElementalSource>() != null)
        {
            print("heals");

            StartCoroutine(PlayVFX());

            IDamageableElementalSource target = other.gameObject.GetComponent<IDamageableElementalSource>();

            target.Heal(healAmount, ElementTypes.ElementType.Water);

            healTrigger.enabled = false;
        }

    }

    private IEnumerator PlayVFX()
    {
        yield return new WaitForSeconds(0.5f);

        waterHealingVFX.Play();
    }
}
