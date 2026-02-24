using Input;
using Player;
using UnityEngine;
using UnityEngine.UIElements;

public class InputSettings : MonoBehaviour
{
    PlayerInputHandler playerInputHandler;
    VcaController vcaController;
    private Slider sensitivitySlider;
    public ThirdPersonCamera ThirdPersonCamera;


    private void Awake()
    {
        vcaController = FindAnyObjectByType<VcaController>();
        ThirdPersonCamera = FindAnyObjectByType<ThirdPersonCamera>();
        playerInputHandler = FindAnyObjectByType<PlayerInputHandler>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sensitivitySlider = GetComponent<Slider>();
        ThirdPersonCamera = FindAnyObjectByType<ThirdPersonCamera>();

        // Load the saved sensitivity setting from PlayerPrefs and apply it to the slider and camera
        float savedSensitivity = PlayerPrefs.GetFloat("CameraSensitivity", ThirdPersonCamera.m_cameraRotSensitivity); // Default to 1.0f if not set
        sensitivitySlider.value = savedSensitivity;
        ThirdPersonCamera.m_cameraRotSensitivity = savedSensitivity;

        int savedAttackMode = PlayerPrefs.GetInt("AttackMode", 0); // Default to 0 (QueuedPress) if not set
        playerInputHandler.attackMode = (PlayerInputHandler.AttackInputMode)savedAttackMode;

        float savedVolume = PlayerPrefs.GetFloat(vcaController.vcaName + "Volume", 1.0f); // Default to 1.0f if no saved value
        vcaController.m_vcaController.setVolume(savedVolume); // Apply the saved volume setting to the VCA
    }

    public void SetSensitivity(float value)
    {
        ThirdPersonCamera.m_cameraRotSensitivity = value;
        PlayerPrefs.SetFloat("CameraSensitivity", value); // Save the sensitivity setting using PlayerPrefs
    }

    public void SetAttackMode()
    {
        if (playerInputHandler.attackMode == PlayerInputHandler.AttackInputMode.QueuedPress)
        {
            playerInputHandler.attackMode = PlayerInputHandler.AttackInputMode.Held;
            PlayerPrefs.SetInt("AttackMode", 1); // Save the attack mode setting using PlayerPrefs (1 for Held)
        }
        else
        {
            playerInputHandler.attackMode = PlayerInputHandler.AttackInputMode.QueuedPress;
            PlayerPrefs.SetInt("AttackMode", 0); // Save the attack mode setting using PlayerPrefs (0 for QueuedPress)
        }
    }
}
