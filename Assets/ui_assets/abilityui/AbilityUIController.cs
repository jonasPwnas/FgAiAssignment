using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace AbilityUI
{
    public class AbilityUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform[] m_abilitySlots;
        [SerializeField] private float m_cooldownDuration = 5.0f;
        [SerializeField] private float m_activeScale = 1.2f;
        [SerializeField] private Color m_activeColor = Color.white;
        [SerializeField] private Color m_interactionFlashColor = Color.white;

        [HideInInspector]public int m_idx = 0;
        private float[] m_timers;

        private void Start()
        {
            if (m_abilitySlots != null) m_timers = new float[m_abilitySlots.Length];
            UpdateVisuals();
        }

        private void OnValidate()
        {
            if (m_abilitySlots != null && m_abilitySlots.Length > 0) UpdateVisuals();
        }

        private void Update()
        {  
            UpdateCooldowns();
        }

        private void UpdateCooldowns()
        {
            if (m_timers == null) return;
            for (int i = 0; i < m_timers.Length; i++)
            {
                if (m_timers[i] > 0)
                {
                    m_timers[i] -= Time.deltaTime;
                    if (m_timers[i] < 0) m_timers[i] = 0;
                    Transform t = m_abilitySlots[i] ? m_abilitySlots[i].Find("CooldownOverlay") : null;
                    if (t)
                    {
                        Image cdImg = t.GetComponent<Image>();
                        if (cdImg) cdImg.fillAmount = m_timers[i] / m_cooldownDuration;
                    }
                }
            }
        }

        public void CycleAbilityLeft()
        {
            if (m_abilitySlots == null || m_abilitySlots.Length == 0) return;
            int oldComp = m_idx;
            m_idx = (m_idx + 1) % m_abilitySlots.Length;
            StartCoroutine(AnimateSwitch(oldComp, m_idx));
        }

        public void CycleAbilityRight()
        {
            if (m_abilitySlots == null || m_abilitySlots.Length == 0) return;
            int oldComp = m_idx;
            m_idx = (m_idx - 1 + m_abilitySlots.Length) % m_abilitySlots.Length;
            StartCoroutine(AnimateSwitch(oldComp, m_idx));
        }

        public void CycleToFire()
        {
            if (m_abilitySlots == null || m_abilitySlots.Length == 0) return;
            int oldComp = m_idx;
            m_idx = 0;
            StartCoroutine(AnimateSwitch(oldComp, m_idx));
        }

        public void CycleToWater()
        {
            if (m_abilitySlots == null || m_abilitySlots.Length == 0) return;
            int oldComp = m_idx;
            m_idx = 2 % m_abilitySlots.Length;
            StartCoroutine(AnimateSwitch(oldComp, m_idx));
        }

        public void CycleToAir()
        {
            if (m_abilitySlots == null || m_abilitySlots.Length == 0) return;
            int oldComp = m_idx;
            m_idx = 1 % m_abilitySlots.Length;
            StartCoroutine(AnimateSwitch(oldComp, m_idx));
        }

        public void UseAbility()
        {
            if (m_abilitySlots == null || m_abilitySlots.Length == 0 || m_timers == null || m_idx >= m_timers.Length) return;
            if (m_timers[m_idx] > 0)
            {
                StartCoroutine(Shake(m_abilitySlots[m_idx]));
                return;
            }

            m_timers[m_idx] = m_cooldownDuration;
            if (m_idx < m_abilitySlots.Length) StartCoroutine(AnimateUse(m_abilitySlots[m_idx]));
            UpdateCooldowns();
        }

        private IEnumerator AnimateSwitch(int oldIdx, int newIdx)
        {
            RectTransform oldS = m_abilitySlots[oldIdx];
            RectTransform newS = m_abilitySlots[newIdx];

            if (oldS) { oldS.gameObject.SetActive(true); oldS.anchoredPosition = Vector2.zero; }
            if (newS) { newS.gameObject.SetActive(true); newS.anchoredPosition = Vector2.zero; newS.localScale = Vector3.zero; }

            float dur = 0.25f;
            float t = 0f;
            Vector3 baseSc = Vector3.one * m_activeScale;

            while (t < dur)
            {
                t += Time.deltaTime;
                float n = t / dur;
                if (oldS) oldS.localScale = Vector3.Lerp(baseSc, Vector3.zero, n);
                if (newS) newS.localScale = Vector3.Lerp(Vector3.zero, baseSc, Mathf.Sin(n * Mathf.PI * 0.5f));
                yield return null;
            }

            if (oldS) { oldS.gameObject.SetActive(false); oldS.localScale = baseSc; oldS.anchoredPosition = Vector2.zero; }
            if (newS) { newS.localScale = baseSc; newS.anchoredPosition = Vector2.zero; 
                Image img = newS.GetComponent<Image>(); if (img) img.color = m_activeColor; }
        }

        private IEnumerator AnimateUse(RectTransform target)
        {
            Image img = target.GetComponent<Image>();
            Color defCol = m_activeColor;
            if (img) img.color = m_interactionFlashColor;

            float dur = 0.2f;
            float t = 0;
            Vector3 startSc = Vector3.one * m_activeScale;
            Vector3 punchSc = startSc * 1.4f;

            while (t < dur)
            {
                t += Time.deltaTime;
                float n = t / dur;
                target.localScale = Vector3.Lerp(startSc, punchSc, Mathf.Sin(n * Mathf.PI));
                if (t > dur * 0.5f && img) img.color = defCol;
                yield return null;
            }

            target.localScale = startSc;
            if (img) img.color = defCol;
        }

        private IEnumerator Shake(RectTransform target)
        {
            Vector2 c = Vector2.zero;
            float dur = 0.2f;
            float t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                target.anchoredPosition = c + new Vector2(Mathf.Sin(t * 50f) * 5f, 0);
                yield return null;
            }
            target.anchoredPosition = c;
        }

        private void UpdateVisuals()
        {
            for (int i = 0; i < m_abilitySlots.Length; i++)
            {
                bool active = (i == m_idx);
                if (m_abilitySlots[i])
                {
                    m_abilitySlots[i].gameObject.SetActive(active);
                    if (active) m_abilitySlots[i].localScale = Vector3.one * m_activeScale;
                }
            }
        }

        public void Setup(RectTransform[] slots)
        {
            m_abilitySlots = slots;
        }
    }
}
