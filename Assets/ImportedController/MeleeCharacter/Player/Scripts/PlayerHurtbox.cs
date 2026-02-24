using System;
using System.Collections;
using UnityEngine;

public class PlayerHurtbox : MonoBehaviour,IDamageableElementalSource
{
    [SerializeField] private float invincibleTimeAfterDamaged = 0.5f;
    private Collider m_collider;
    private PlayerStatus m_playerStatus;
    private bool m_canTakeDamage = true;
    
    private void Awake()
    {
        m_collider = GetComponent<Collider>();
        m_playerStatus = GetComponentInParent<PlayerStatus>();
    }

    public void TakeDamage(float damageAmount, ElementTypes.ElementType elementType, bool UsedSword)
    {
        if(m_canTakeDamage)
        {
            m_playerStatus.TakeDamage(damageAmount, elementType, UsedSword);
            StartCoroutine(OnHitIframes(invincibleTimeAfterDamaged));
        }
    }

    IEnumerator OnHitIframes(float iFramesTime)
    {
        m_canTakeDamage = false;
        yield return new WaitForSeconds(iFramesTime);
        m_canTakeDamage = true;
    }

    public void SetAsInvincible()
    {
        m_canTakeDamage = false;
    }
    
    public void SetAsNotInvincible()
    {
        m_canTakeDamage = true;
    }
    
    
    
    
    
    //Unused methods from interface beneath this :')
    
    public float ChargeElementTime()
    {
        //No
        return 0f;
    }

    public void Heal(float healAmount, ElementTypes.ElementType elementType)
    {
        //no
    }

    public IDamageableElementalSource.ElementInteractType InteractType()
    {
        
        //no
        return IDamageableElementalSource.ElementInteractType.Enemy;
    }

    public int EquippedElementAmount()
    {
        //no
        return 0;
    }

    public int ElementAmountToGive()
    {
        //no
        return 0;
    }

    public ElementTypes.ElementType GetSourceElement()
    {
        //no
        return ElementTypes.ElementType.NoElement;
    }

    public void AddElement(ElementTypes.ElementType elementType, int amount, bool fromCharge)
    {
        //no
    }

    public void RemoveElement(ElementTypes.ElementType elementType, int amount)
    {
        //no
    }
}
