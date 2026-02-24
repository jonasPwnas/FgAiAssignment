using System.Collections;
using UnityEngine;

public class DropdElementalCharge : MonoBehaviour, IDamageableElementalSource
{
    [Header("Elemental Charge Settings")]
    [Tooltip("The element type of this dropped elemental charge.")]
    [SerializeField] private ElementTypes.ElementType m_elementType = ElementTypes.ElementType.NoElement;

    [Tooltip("Materials for different element types.")]
    [SerializeField] private Material m_fireMaterial, m_WaterMaterial, m_AirMaterial, m_DefualtMaterial;

    [Tooltip("Amount of element to give to the player on pickup.")]
    [SerializeField] private int m_elementAmountToGive = 1;

    [Tooltip("Speed at which the charge moves towards the player.")]
    [SerializeField, Range(10f, 20f)] private float m_moveSpeedToPlayer = 2f;

    [Tooltip("Time before the charge starts flying towards the player.")]
    [SerializeField, Range(0f, 10f)] private float m_timeBeforeFlyingToPlayer = 5f;

    [Tooltip("Randomness added to the time before flying to the player.")]
    [SerializeField, Range (0f, 1f)] private float m_timeBeforeFlyingToPlayerRandomnes = 0.5f;

    private PlayerStatus m_playerStatus;
    private Transform m_playerTransform;

    private void Awake()
    {
        m_playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        m_playerStatus = m_playerTransform.gameObject.GetComponent<PlayerStatus>();
    }

    private void Start()
    {
        SetMaterial();

        m_timeBeforeFlyingToPlayer = m_timeBeforeFlyingToPlayer + Random.Range(-m_timeBeforeFlyingToPlayerRandomnes, m_timeBeforeFlyingToPlayerRandomnes);

        StartCoroutine(GoToPlayer());

        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerStatus>() != null)
        {
            other.GetComponent<PlayerStatus>().AddElement(m_elementType, m_elementAmountToGive, false);
            Destroy(gameObject);
        }
    }

    public void AddElement(ElementTypes.ElementType elementType, int amount, bool fromCharge)
    {
       //NO
    }

    public float ChargeElementTime()
    {
        return 0f;
    }

    public int ElementAmountToGive()
    {
      return 1;
    }

    public int EquippedElementAmount()
    {
           return 0;
    }

    public ElementTypes.ElementType GetSourceElement()
    {
       return ElementTypes.ElementType.NoElement;
    }

    public void Heal(float healAmount, ElementTypes.ElementType elementType)
    {
        //NO
    }

    public IDamageableElementalSource.ElementInteractType InteractType()
    {
        return IDamageableElementalSource.ElementInteractType.Source;
    }

    public void RemoveElement(ElementTypes.ElementType elementType, int amount)
    {
        //NO
    }

    public void TakeDamage(float damageAmount, ElementTypes.ElementType elementType, bool UsedSword)
    {
        //NO
    }


    private void SetMaterial()
    {         Renderer renderer = GetComponent<Renderer>();
        switch (m_elementType)
        {
            case ElementTypes.ElementType.Fire:
                renderer.material = m_fireMaterial;
                break;
            case ElementTypes.ElementType.Water:
                renderer.material = m_WaterMaterial;
                break;
            case ElementTypes.ElementType.Air:
                renderer.material = m_AirMaterial;
                break;
            default:
                renderer.material = m_DefualtMaterial;
                break;
        }
    }
    public void SetElementType(ElementTypes.ElementType newElementType)
    {
        m_elementType = newElementType;
        SetMaterial();
    }

    private IEnumerator GoToPlayer()
    {
        yield return new WaitForSeconds(m_timeBeforeFlyingToPlayer);
        Vector3 temp = Vector3.zero;
        temp.y = 1f;
        while (Vector3.Distance(transform.position, m_playerTransform.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, m_playerTransform.position + temp, Time.deltaTime * m_moveSpeedToPlayer);
            yield return null;
        }
    }
}
