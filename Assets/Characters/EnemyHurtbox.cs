using System.Collections;
using UnityEngine;

public class EnemyHurtbox : MonoBehaviour, IDamageableElementalSource
{
    [SerializeField] private float invincibleTimeAfterDamaged = 0.7f;
    private EnemyStatus m_enemyStatus;
    private bool m_canTakeDamage = true;
    
    private void Awake()
    {
        m_enemyStatus = GetComponentInParent<EnemyStatus>();
    }

    public void TakeDamage(float damageAmount, ElementTypes.ElementType elementType, bool UsedSword)
    {
        if(m_canTakeDamage)
        {
            m_enemyStatus.TakeDamage(damageAmount, elementType, UsedSword);
            m_canTakeDamage = false;
            StartCoroutine(OnHitIframes(invincibleTimeAfterDamaged));
        }
        else
            print("sword damage not allowed");
    }

    public void SetAsInvincible()
    {
        m_canTakeDamage = false;
    }
    
    public void SetAsNotInvincible()
    {
        m_canTakeDamage = true;
    }
    
    IEnumerator OnHitIframes(float iFramesTime)
    {
        yield return new WaitForSeconds(iFramesTime);
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
