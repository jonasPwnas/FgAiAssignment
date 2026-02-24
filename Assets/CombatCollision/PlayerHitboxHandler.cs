using System;
using UnityEngine;

public class PlayerHitboxHandler : MonoBehaviour
{
    //Delegates
    public delegate void PlayerDealtDamage(ElementTypes.ElementType elementType);
    public static event PlayerDealtDamage OnPlayerDealtDamage;

    //Editor exposed
    [SerializeField] private float damageToDeal = 25f; //get this from player config instead?
    [SerializeField] private PlayerStatus elementalSource; //get this from player config instead?

    //privates
    private bool m_hitboxEnabled = false;
    private Collider m_hitbox;
    private PlayerStatus m_playerStatus;


    private void Awake()
    {
        m_hitbox = GetComponent<Collider>();
        m_playerStatus = FindAnyObjectByType<PlayerStatus>();
    }

    private void OnEnable()
    {
        PlayerAttackEventReciever.OnToggleHitbox += DoToggleHitbox;
    }

    private void OnDisable()
    {
        PlayerAttackEventReciever.OnToggleHitbox -= DoToggleHitbox;
    }

    private void DoToggleHitbox(bool enable)
    {
        if (enable)
        {
            //print("ENABLED MURDER LÅDDA");
            m_hitboxEnabled = true;
            m_hitbox.enabled = m_hitboxEnabled;
            return;
        }
        if (!enable)
        {
            //print("DISABLED MURDER LÅDDAnnnnnnnn");
            m_hitboxEnabled = false;
            m_hitbox.enabled = m_hitboxEnabled;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<IDamageableElementalSource>() != null)
        {
            IDamageableElementalSource target = other.GetComponent<IDamageableElementalSource>();
            
            print("try dealt  " + m_playerStatus.GetSourceElement() + "  damage?");
            
            switch (target.InteractType())
            {
                case IDamageableElementalSource.ElementInteractType.Enemy:
                    switch (m_playerStatus.GetSourceElement())
                    {
                        case ElementTypes.ElementType.Fire:
                            target.TakeDamage(damageToDeal, ElementTypes.ElementType.Fire, true);
                            print("sword dealt fire damage to:  " + other.name);
                            break;
                        case ElementTypes.ElementType.Water:
                            target.TakeDamage(damageToDeal, ElementTypes.ElementType.Water, true);
                            print("sword dealt water damage to:  " + other.name);
                            break;
                        case ElementTypes.ElementType.Air:
                            target.TakeDamage(damageToDeal, ElementTypes.ElementType.Air, true);
                            print("sword dealt air damage to:  " + other.name);
                            break;
                        case ElementTypes.ElementType.NoElement:
                            target.TakeDamage(damageToDeal, ElementTypes.ElementType.NoElement, true);
                            print("sword dealt physical damage to:  " + other.name);
                            break;
                    }
                    OnPlayerDealtDamage?.Invoke(m_playerStatus.GetSourceElement());
                    break;
                case IDamageableElementalSource.ElementInteractType.User:
                    target.TakeDamage(damageToDeal, m_playerStatus.GetSourceElement(), false);
                    break;
                case IDamageableElementalSource.ElementInteractType.Source: //Change this to be a generalized "charging" of elements, not attacks, seperate button/logic
                    break;
            }
        }
    }
}
