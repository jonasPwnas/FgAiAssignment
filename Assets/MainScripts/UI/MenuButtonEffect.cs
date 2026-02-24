using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Settings")]
    public float hoverScale = 1.2f;
    public float animSpeed = 10f;
    public Color normalColor = new Color(0.6f, 0.6f, 0.6f);
    public Color hoverColor = Color.white;

    [Header("Events")]
    public UnityEvent onClick;

    [Header("Internal Refs (Auto-assigned by Generator)")]
    public RectTransform leftLine;
    public RectTransform rightLine;
    public TextMeshProUGUI textComponent;

    private void Start()
    {
        // Init state
        if (textComponent != null) textComponent.color = normalColor;
        transform.localScale = Vector3.one;
        SetLineState(0, 0); // Lines invisible and width 0
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateButton(true));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateButton(false));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }

    private IEnumerator AnimateButton(bool isHover)
    {
        Vector3 targetScale = isHover ? Vector3.one * hoverScale : Vector3.one;
        Color targetColor = isHover ? hoverColor : normalColor;
        float targetLineWidth = isHover ? 40f : 0f;
        float targetAlpha = isHover ? 1f : 0f;

        while (true)
        {
            float step = Time.deltaTime * animSpeed;
            
            // Text Scale
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, step);
            
            // Text Color
            if (textComponent)
                textComponent.color = Color.Lerp(textComponent.color, targetColor, step);

            // Lines
            if (leftLine && rightLine)
            {
                SetLineState(
                    Mathf.Lerp(leftLine.sizeDelta.x, targetLineWidth, step),
                    Mathf.Lerp(leftLine.GetComponent<CanvasGroup>().alpha, targetAlpha, step)
                );
            }

            // Check if close enough
            if (Vector3.Distance(transform.localScale, targetScale) < 0.01f)
            {
                transform.localScale = targetScale;
                if (textComponent) textComponent.color = targetColor;
                if (leftLine) SetLineState(targetLineWidth, targetAlpha);
                break;
            }

            yield return null;
        }
    }

    private void SetLineState(float width, float alpha)
    {
        if (leftLine)
        {
            leftLine.sizeDelta = new Vector2(width, 1); // height 1px
            CanvasGroup cg = leftLine.GetComponent<CanvasGroup>();
            if (cg) cg.alpha = alpha;
        }
        if (rightLine)
        {
            rightLine.sizeDelta = new Vector2(width, 1);
            CanvasGroup cg = rightLine.GetComponent<CanvasGroup>();
            if (cg) cg.alpha = alpha;
        }
    }
}
