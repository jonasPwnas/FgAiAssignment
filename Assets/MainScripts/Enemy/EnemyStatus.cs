using System;
using Characters;
using Unity.VisualScripting;
using UnityEngine;

public enum EnemyInterruptBehavior{FromAllAttacks, FromHeavyAttacks, DoesNotInterrupt} //poopykins

public class EnemyStatus : MonoBehaviour, IDamageableElementalSource
{
    //Delegates
    public delegate void BossIsDeaderThanDisco();
    public static BossIsDeaderThanDisco OnBossIsDeaderThanDisco;
    
    [SerializeField] private bool isBossEnemy = false;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private ElementTypes.ElementType thisElementType;
    [SerializeField] private float maxSuperArmor = 50f;
    [SerializeField] private EnemyInterruptBehavior interruptBehavior = EnemyInterruptBehavior.FromAllAttacks;
    [SerializeField] private GameObject elementDropPrefab;
    [SerializeField] private int elementAmountToGivePlayer = 2;
    [SerializeField] private Transform healthBarPlacer;
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private ParticleSystem airParticles;
    [SerializeField] private ParticleSystem waterParticles;
    [Header("Governs how much the air push moves the enemy, bigger value means further move distance")]
    [SerializeField] public float airPushImpactRatio = 1f;
    [SerializeField] private bool canBePushedOffLedges = false;

    [Header("Damage multipliers on interacting elements")] 
    [SerializeField] public float strongElementMultiplier = 2f;
    [SerializeField] public float weakElementMultiplier = 0.25f;
    [SerializeField] public float sameElementMultiplier = 1f;
    [SerializeField] public float noElementMultiplier = 0.5f;

    //private WorldSettings m_worldSettings;
    private EnemyAudio m_audio;
    private EnemyHealthBarManager  m_healthBarManager;
    private EnemyHealthBar m_healthBar;
    //private GoapUnitBrain m_goapUnitBrain;
    private EnemyFsm m_fsm;
    private NpcRootMotionController m_controller;
    private float m_currentHealth;
    private float m_currentSuperArmor;
    private bool m_isDead;
    

    private void Awake()
    {
        m_currentHealth = maxHealth;
        m_currentSuperArmor = maxSuperArmor;
        m_controller = GetComponent<NpcRootMotionController>();
        m_fsm = GetComponent<EnemyFsm>();
        m_healthBarManager = FindAnyObjectByType<EnemyHealthBarManager>();
        m_audio = GetComponent<EnemyAudio>();
        
        if (fireParticles == null)
        {
            print("PLEASE ADD VFX OBJECTS TO THIS ENEMY: " + gameObject.name);
            return;
        }
        
        switch (thisElementType)
        {
            case ElementTypes.ElementType.Fire:
                fireParticles.gameObject.SetActive(true);
                Destroy(waterParticles.gameObject);
                Destroy(airParticles.gameObject);
                break;
            case ElementTypes.ElementType.Water:
                waterParticles.gameObject.SetActive(true);
                Destroy(fireParticles.gameObject);
                Destroy(airParticles.gameObject);
                break;
            case ElementTypes.ElementType.Air:
                airParticles.gameObject.SetActive(true);
                Destroy(fireParticles.gameObject);
                Destroy(waterParticles.gameObject);
                break;
            case ElementTypes.ElementType.NoElement:
                Destroy(fireParticles.gameObject);
                Destroy(waterParticles.gameObject);
                Destroy(airParticles.gameObject);
                break;
        }
    }

    private void Start()
    {
        if (m_fsm.npcFsmController.UseBossHealthBar())
        {
            m_healthBar = m_healthBarManager.GetBossBar();
        }
    }
    
    public float ChargeElementTime()
    {
        //no
        return 0f;
    }

    private float GetHealthPercentage()
    {
        return m_currentHealth / maxHealth;
    }

    private float GetDamageMultiplier(ElementTypes.ElementType damageType)
    {
        float multiplier = 0.5f;

        switch (GetSourceElement()) 
        {
            case ElementTypes.ElementType.Fire: //this-enemy element
                switch (damageType) //player element
                {
                    case ElementTypes.ElementType.Fire:
                        return sameElementMultiplier;
                    case ElementTypes.ElementType.Water:
                        return strongElementMultiplier;
                    case ElementTypes.ElementType.Air:
                        return weakElementMultiplier;
                    case ElementTypes.ElementType.NoElement:
                        return noElementMultiplier;
                }
                break;
            case ElementTypes.ElementType.Water: //this-enemy element
                switch (damageType) //player element
                {
                    case ElementTypes.ElementType.Fire:
                        return weakElementMultiplier;
                    case ElementTypes.ElementType.Water:
                        return sameElementMultiplier;
                    case ElementTypes.ElementType.Air:
                        return strongElementMultiplier;
                    case ElementTypes.ElementType.NoElement:
                        return noElementMultiplier;
                }
                break;
            case ElementTypes.ElementType.Air: //this-enemy element
                switch (damageType) //player element
                {
                    case ElementTypes.ElementType.Fire:
                        return strongElementMultiplier;
                    case ElementTypes.ElementType.Water:
                        return weakElementMultiplier;
                    case ElementTypes.ElementType.Air:
                        return sameElementMultiplier;
                    case ElementTypes.ElementType.NoElement:
                        return noElementMultiplier;
                }
                break;
            case ElementTypes.ElementType.NoElement:
                switch (damageType) //player element
                {
                    case ElementTypes.ElementType.Fire:
                        return strongElementMultiplier;
                    case ElementTypes.ElementType.Water:
                        return strongElementMultiplier;
                    case ElementTypes.ElementType.Air:
                        return strongElementMultiplier;
                    case ElementTypes.ElementType.NoElement:
                        return sameElementMultiplier;
                }
                break;
        }
        return multiplier;
    }
    
    public void TakeDamage(float damageAmount, ElementTypes.ElementType elementType, bool UsedSword)
    {
        if (m_isDead)
            return;
        print("Status Got Dealt Damage:  " + damageAmount);
        m_currentHealth -= damageAmount * GetDamageMultiplier(elementType);
        m_currentSuperArmor -= damageAmount * GetDamageMultiplier(elementType);
        print("current damage multiplier:  " + GetDamageMultiplier(elementType));
        Mathf.Clamp(m_currentHealth, 0, maxHealth);
        if(m_healthBar == null)
        {
            m_healthBar = m_healthBarManager.InitHealthbar();
        }

        m_healthBar.UpdateHealth(GetHealthPercentage(), healthBarPlacer);
        
        if (elementType == ElementTypes.ElementType.Air)
        {
            if (canBePushedOffLedges && UsedSword == false)
                GetComponent<FsmRootMotionController>().SetIsBeingPushed(true);
        }
        
        if (m_currentHealth <= 0.5f)
        {
            m_isDead = true;
            m_healthBar.StopUsingHealthbar();
            DropElementCharges();
            m_audio.DeathSfx();
            
            if(isBossEnemy)
                OnBossIsDeaderThanDisco?.Invoke(); //ooooh yeah
            
            if(m_controller != null)
            {
                m_controller.OnDeath();
                m_controller.enabled = false;
            }
            
            if (m_fsm != null)
            {
                print("enemy " + gameObject.name + " died!!!! in status");
                m_fsm.OnDeath();
            }
            
        }
        else
        {
            if (interruptBehavior == EnemyInterruptBehavior.DoesNotInterrupt)
                return;
            
            if (interruptBehavior == EnemyInterruptBehavior.FromHeavyAttacks && damageAmount < 45f)
                return;
            
            m_audio.DamagedSound(elementType);
            
            if(m_fsm != null)
                m_fsm.OnDamaged();
            else if(m_controller != null)
                    m_controller.OnDamaged();

            if (m_currentSuperArmor < 0.1f)
            {
                m_currentSuperArmor = maxSuperArmor;
                m_fsm.SetStunnedState();
            }
        }
    }

    private void DropElementCharges()
    {
        if (thisElementType != ElementTypes.ElementType.NoElement)
        {
            for (int i = 0; i < elementAmountToGivePlayer; i++)
            {
                DropdElementalCharge charge = Instantiate(elementDropPrefab, transform.position, Quaternion.identity).GetComponent<DropdElementalCharge>();
                charge.SetElementType(GetSourceElement());
            }
        }
    }
    
    public void Heal(float healAmount, ElementTypes.ElementType elementType)
    {
        //no
    }

    public IDamageableElementalSource.ElementInteractType InteractType()
    {
        return IDamageableElementalSource.ElementInteractType.Enemy;
    }

    public int EquippedElementAmount()
    {
        return 0;
    }

    public int ElementAmountToGive()
    {
        return elementAmountToGivePlayer;
    }

    public ElementTypes.ElementType GetSourceElement()
    {
        return thisElementType;
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
