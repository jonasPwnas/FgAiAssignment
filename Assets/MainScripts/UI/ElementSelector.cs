using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using AbilityUI;

namespace UxPrototype
{
    public class ElementSelector : MonoBehaviour
    {
        [System.Serializable]
        public class ElementData
        {
            public string Name;
            public Sprite IconActive;   
            public Sprite IconInactive; 
        }

        [Header("Config")]
        [SerializeField] private List<ElementData> m_elements = new List<ElementData>();
        [SerializeField] private float m_animationSpeed = 10f;
        [SerializeField] private float m_spacing = 250f;
        [SerializeField] private float m_activeScale = 1.1f;
        [SerializeField] private float m_inactiveScale = 0.75f; 
        
        [Header("Refs")]
        [SerializeField] private RectTransform m_container;
        [SerializeField] private GameObject m_slotPrefab;

        private SelectionWheel m_selectionWheel;
        private AbilityUIController m_abilityUIController;
        private List<RectTransform> m_spawnedSlots = new List<RectTransform>();
        private List<Image> m_spawnedImages = new List<Image>();
        
        private int m_currentIndex = 0;
        private float m_currentFloatIndex = 0f;

        private void Awake()
        {
            m_selectionWheel = FindAnyObjectByType<SelectionWheel>();
            m_abilityUIController = FindAnyObjectByType<AbilityUIController>();
        }

        private void Start()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            if(m_elements.Count == 0 || m_container == null || m_slotPrefab == null) return;

            foreach(Transform child in m_container) Destroy(child.gameObject);
            m_spawnedSlots.Clear();
            m_spawnedImages.Clear();

            for (int i = 0; i < m_elements.Count; i++)
            {
                var obj = Instantiate(m_slotPrefab, m_container);
                obj.SetActive(true);
                
                var img = obj.GetComponent<Image>(); 
                if (img) img.sprite = m_elements[i].IconInactive;

                m_spawnedSlots.Add(obj.GetComponent<RectTransform>());
                m_spawnedImages.Add(img);
            }

            m_currentFloatIndex = m_currentIndex;
        }

        private void Update()
        {
            if (m_spawnedSlots.Count != m_elements.Count) Rebuild();

            Animate();
            
            // Sync in Animate
        }

        public void ChangeSelection(int direction)
        {
            m_currentIndex += direction;
        }

        public void SwitchToChargingElement(ElementTypes.ElementType chargingElement) //Yeah it's not pretty but it works.
        {
            switch (chargingElement)
            {
                case ElementTypes.ElementType.Fire:
                    m_currentIndex = 0;
                    m_abilityUIController.CycleToFire();
                    m_selectionWheel.UpdateElements(chargingElement);
                    break;
                case ElementTypes.ElementType.Water:
                    m_currentIndex = 1;
                    m_abilityUIController.CycleToWater();
                    m_selectionWheel.UpdateElements(chargingElement);
                    break;
                case ElementTypes.ElementType.Air:;
                    m_currentIndex = 2;
                    m_abilityUIController.CycleToAir();
                    m_selectionWheel.UpdateElements(chargingElement);
                    break;
            }
        }
        
        private void Animate()
        {
            m_currentFloatIndex = Mathf.Lerp(m_currentFloatIndex, m_currentIndex, Time.deltaTime * m_animationSpeed);
            
            int count = m_elements.Count;
            for (int i = 0; i < count; i++)
            {
                float dist = i - m_currentFloatIndex;
                while (dist < -count / 2f) dist += count;
                while (dist > count / 2f) dist -= count;

                float absDist = Mathf.Abs(dist);
                float xPos = dist * m_spacing;
                float scale = Mathf.Lerp(m_activeScale, m_inactiveScale, absDist); 
                float alpha = (absDist > 1.2f) ? 0f : Mathf.Lerp(1.0f, 0.4f, absDist);

                var rt = m_spawnedSlots[i];
                rt.anchoredPosition = new Vector2(xPos, 0);
                rt.localScale = Vector3.one * scale;

                if (m_spawnedImages[i]) 
                {
                    Sprite targetSprite = (absDist < 0.4f) ? m_elements[i].IconActive : m_elements[i].IconInactive;
                    
                    if (m_spawnedImages[i].sprite != targetSprite) 
                        m_spawnedImages[i].sprite = targetSprite;

                    var c = Color.white;
                    c.a = alpha;
                    m_spawnedImages[i].color = c;
                }
            }

            SortDepth();
        }

        private void SortDepth()
        {
            var indices = new List<int>();
            for(int i=0; i < m_spawnedSlots.Count; i++) indices.Add(i);

            indices.Sort((a, b) => 
            {
                float distA = GetWrappedDist(a);
                float distB = GetWrappedDist(b);
                return Mathf.Abs(distB).CompareTo(Mathf.Abs(distA));
            });

            for(int k=0; k < indices.Count; k++)
            {
                m_spawnedSlots[indices[k]].SetSiblingIndex(k);
            }
        }

        private float GetWrappedDist(int i)
        {
            float dist = i - m_currentFloatIndex;
            int count = m_elements.Count;
            while (dist < -count / 2f) dist += count;
            while (dist > count / 2f) dist -= count;
            return dist;
        }
    }
}
