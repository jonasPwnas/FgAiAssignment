using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHitboxHandler : MonoBehaviour
{
    //Editor exposed
    [SerializeField] private float damageToDeal = 25f; //get this from player config instead?
    [SerializeField] private EnemyStatus enemyStatus; //get this from player config instead?
    [SerializeField] private EnemyAudio enemyAudio;
    [SerializeField] private bool useCompoundCollision = false;
    
    //privates
    private bool m_hitboxEnabled = false;
    private Collider m_hitbox;
    private float m_damageMultiplier = 1f;
    private List<Collider> m_hitboxes = new List<Collider>();


    private void Awake()
    {
        m_hitbox = GetComponent<Collider>();
        if (m_hitbox == null)
        {
            m_hitboxes.AddRange(GetComponentsInChildren<Collider>());
        }
    }
    
    public void DoToggleHitbox(bool enableBox, float damageMultiplier)
    {
        
        m_damageMultiplier = damageMultiplier;

        if (m_damageMultiplier < 0.2f)
        {
            m_damageMultiplier = 1f;
        }
        
        if (enableBox)
        {
            m_hitboxEnabled = true;

            if (useCompoundCollision)
            {
                foreach (Collider col in m_hitboxes)
                {
                    col.enabled = m_hitboxEnabled;
                }

                return;
            }
            
            m_hitbox.enabled = m_hitboxEnabled;
            return;
        }
        if (!enableBox)
        {
            m_hitboxEnabled = false;
            
            if (useCompoundCollision)
            {
                foreach (Collider col in m_hitboxes)
                {
                    col.enabled = m_hitboxEnabled;
                }
                return;
            }
            m_hitbox.enabled = m_hitboxEnabled;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<IDamageableElementalSource>() != null)
        {
            print("Dealt damage to: " + other.name);
            
            IDamageableElementalSource target = other.GetComponent<IDamageableElementalSource>();
            target.TakeDamage(damageToDeal * m_damageMultiplier, enemyStatus.GetSourceElement(), false);
        }
    }
}
