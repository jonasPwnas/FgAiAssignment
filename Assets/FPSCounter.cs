using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    private TMPro.TextMeshProUGUI fpsText;
    void Start()
    {
        fpsText = GetComponent<TMPro.TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        fpsText.text = $"FPS: {(int)(1f / Time.unscaledDeltaTime)}";
    }
}
