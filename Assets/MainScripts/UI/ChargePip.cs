using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ChargePip : MonoBehaviour
{
    // Alex: НАСТРОЙКИ СПРАЙТОВ / SPRITE SETTINGS
    [Header("1. Спрайты (Sprites)")]
    [SerializeField] private Sprite m_activeSprite;
    [SerializeField] private Sprite m_emptySprite;

    // Alex: РАЗМЕР И ПОВОРОТ / SIZE AND ROTATION
    [Header("2. Вид (Appearance)")]
    [SerializeField] private float m_baseScale = 1.0f; // Увеличение спрайта внутри (зум)

    // Alex: ПОДСВЕТКА (GLOW) / GLOW
    [Header("3. Подсветка (Glow)")]
    [SerializeField] private bool m_useGlow = true;
    [Range(0, 1)][SerializeField] private float m_glowAlpha = 0.2f;
    [SerializeField] private float m_glowScale = 1.5f;

    // Alex: АНИМАЦИЯ / ANIMATION
    [Header("4. Анимация (Animation)")]
    [SerializeField] private float m_popScale = 3.0f;
    [SerializeField] private float m_popSpeed = 5f;
    [SerializeField] private float m_fadeOutSpeed = 8f;

    // Компоненты (находит сам) / Components (finds itself)
    private Image m_image;
    private RectTransform m_rect;
    private Image m_glowImage;
    private CanvasGroup m_fader;
    private bool m_isFilled = false;
    ChargeManager chargeManager;

    private void Awake()
    {
        m_image = GetComponent<Image>();
        m_rect = GetComponent<RectTransform>();
        chargeManager = FindAnyObjectByType<ChargeManager>();

        // Добавляем Fader если нет
        m_fader = GetComponent<CanvasGroup>();
        if (!m_fader) m_fader = gameObject.AddComponent<CanvasGroup>();

        // Создаем тень (glow) кодом
        if (m_useGlow) CreateGlow();

        // Фикс сплющивания
        if (m_image) m_image.preserveAspect = true;
    }

    private void Update()
    {
        m_activeSprite = chargeManager.CurrentActiveSprite;
    }

    private void CreateGlow()
    {
        GameObject go = new GameObject("Glow");
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling(); // На задний план / On the back
        go.transform.localScale = Vector3.one * m_glowScale;

        m_glowImage = go.AddComponent<Image>();
        m_glowImage.raycastTarget = false;
        m_glowImage.preserveAspect = true;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
    }

    // Главный метод обновления / Main update method
    public void UpdateState(bool filled, Sprite activeOverride, Color? glowColor = null)
    {
        if (!m_image) return;

        Sprite targetSprite = filled ? (activeOverride ? activeOverride : m_activeSprite) : m_emptySprite;
        bool wasFilled = m_isFilled;
        m_isFilled = filled;

        // Если исчезновение / If disappearing
        if (wasFilled && !filled)
        {
            StopAllCoroutines();
            StartCoroutine(AnimDissolve(targetSprite));
            return;
        }

        // Применяем картинку / Applying the image
        ApplyVisuals(filled, targetSprite, glowColor);

        // Если появление / If appearing
        if (!wasFilled && filled)
        {
            StopAllCoroutines();
            gameObject.SetActive(true); //maybe?
            StartCoroutine(AnimPop());
        }
        else if (filled)
        {
            // Сброс масштаба если просто обновили цвет / Reset scale if just updated color
            transform.localScale = Vector3.one * m_baseScale;
            m_fader.alpha = 1f;
        }
    }

    private void ApplyVisuals(bool filled, Sprite sprite, Color? glowColor)
    {
        m_fader.alpha = 1f;

        // Основная картинка / Main image
        if (sprite) { m_image.sprite = sprite; m_image.color = Color.white; }
        else m_image.color = Color.clear;

        // Свечение / Glow
        if (m_useGlow && m_glowImage)
        {
            if (filled && sprite)
            {
                m_glowImage.gameObject.SetActive(true);
                m_glowImage.sprite = sprite;
                Color c = glowColor ?? Color.white;
                c.a = m_glowAlpha;
                m_glowImage.color = c;
            }
            else m_glowImage.gameObject.SetActive(false);
        }
    }

    // Анимация удара (Pop) / Pop animation
    private IEnumerator AnimPop()
    {
        Vector3 baseSc = Vector3.one * m_baseScale;
        Vector3 bigSc = baseSc * m_popScale;

        transform.localScale = bigSc;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * m_popSpeed;
            transform.localScale = Vector3.Lerp(bigSc, baseSc, t);
            yield return null;
        }
        transform.localScale = baseSc;
    }

    // Анимация исчезновения / Disappearance animation
    private IEnumerator AnimDissolve(Sprite nextSprite)
    {
        Vector3 start = transform.localScale;
        Vector3 end = Vector3.one * 0.5f; // Сжимаемся / Compressing

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * m_fadeOutSpeed;
            transform.localScale = Vector3.Lerp(start, end, t);
            m_fader.alpha = 1f - t;
            yield return null;
        }

        // Финал / Final
        ApplyVisuals(false, nextSprite, null);
        transform.localScale = Vector3.one * m_baseScale;
    }
}
