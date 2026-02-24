using System.Collections.Generic;
using UnityEngine;

public class ChargeManager : MonoBehaviour
{
    SelectionWheel selectionWheel;

    [Header("?????? / References")]
    [SerializeField] private Transform m_container;
    [SerializeField] private GameObject m_pipPrefab;
    

    [Header("??????? ??????? / Current Element")]
    public Sprite CurrentActiveSprite;   // ?????? ?????????? (?????...) / Fill Icon (Fire...)

    [Header("??????? ???? ????????? / Glow Color")]
    public Color CurrentGlowColor = new Color(1f, 0.5f, 0f); // ????????? / Orange

    private List<ChargePip> m_pips = new List<ChargePip>();

    // ???????? ?? ???? / Call from gamesystem 
    // Added 'glowColor' optional parameter

    private void Start()
    {
       selectionWheel = FindAnyObjectByType<SelectionWheel>();
    }


    public void UpdateDisplay(int max, int current, Sprite sprite = null, Color? glowColor = null)
    {
        if (sprite != null) CurrentActiveSprite = sprite;
        if (glowColor.HasValue) CurrentGlowColor = glowColor.Value;

        // 1. ??????? ?????? ?????????? ????? / Creating the required number of cells
        while (m_pips.Count < max)
        {
            var go = Instantiate(m_pipPrefab, m_container);
            m_pips.Add(go.GetComponent<ChargePip>());
        }

        // 2. ????????? ?????? / Update each one
        for (int i = 0; i < m_pips.Count; i++)
        {
            if (i < max)
            {
                m_pips[i].gameObject.SetActive(true);
                bool isFilled = i < current;

                // ???????? ?????????, ?????? ? ???? / Passing state, sprite AND COLOR
                m_pips[i].UpdateState(isFilled, CurrentActiveSprite, CurrentGlowColor);
            }
            else
            {
                m_pips[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        PlayerStatus.OnElementEquipped += UpdateElementTypeStackChanged;;
        PlayerStatus.OnElementStackChanged += UpdateElementTypeStackChanged;
    }

    private void OnDisable()
    {
        PlayerStatus.OnElementEquipped -= UpdateElementTypeStackChanged;
        PlayerStatus.OnElementStackChanged -= UpdateElementTypeStackChanged;
    }

    private void UpdateElementTypeStackChanged(ElementTypes.ElementType element, int currentStack)
    {
        if (element != selectionWheel.currentElement)
        {
            print("unequipped element change");
            return;
        }
        
        switch (element)
        {
            case ElementTypes.ElementType.Fire:
                UpdateDisplay(3, currentStack, selectionWheel.chargeFire, new Color(1f, 0.5f, 0f));
                break;
            case ElementTypes.ElementType.Water:
                UpdateDisplay(3, currentStack, selectionWheel.chargeWater, Color.blue); 
                break;
            case ElementTypes.ElementType.Air:
                UpdateDisplay(3, currentStack, selectionWheel.chargeAir, Color.gray);
                break;
            case ElementTypes.ElementType.NoElement:
                foreach (ChargePip pip in m_pips)
                {
                    pip.gameObject.SetActive(false);
                }
                break;
        }
    }
}
