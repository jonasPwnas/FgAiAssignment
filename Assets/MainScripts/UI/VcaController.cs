using UnityEngine;
using UnityEngine.UI;

public class VcaController : MonoBehaviour
{
    public FMOD.Studio.VCA m_vcaController;

    public string vcaName; // The name of the VCA to control, set this in the Unity Inspector

    private Slider slider; // Reference to the UI Slider component

    private void Awake()
    {
        m_vcaController = FMODUnity.RuntimeManager.GetVCA("vca:/" + vcaName);
    }

    private void Start()
    {
        slider = GetComponent<Slider>();
        // Load the saved volume setting from PlayerPrefs and set the slider value
        float savedVolume = PlayerPrefs.GetFloat(vcaName + "Volume", 1.0f); // Default to 1.0f if no saved value
        slider.value = savedVolume;
        m_vcaController.setVolume(savedVolume); // Apply the saved volume setting to the VCA
    }

    public void SetVolume(float volume)
    {
        m_vcaController.setVolume(volume);
        PlayerPrefs.SetFloat(vcaName + "Volume", volume); // Save the volume setting using PlayerPrefs
    }

}
