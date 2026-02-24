using Options;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class VideoSettings : MonoBehaviour
{

    OptionsController optionsController;


    private int m_targetFrameRateIndex = 1;
    private int m_resolutionIndex = 1;
    private bool m_isVSyncOn = true;
    private bool m_isFullscreenOn = true;
    private bool m_isMotionBlurOn = true;

    private void Awake()
    {
    
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {


        int savedFrameRate = PlayerPrefs.GetInt("TargetFrameRate", 1);
        SetTargetFrameRate(savedFrameRate);
        m_targetFrameRateIndex = savedFrameRate;

        int savedResolution = PlayerPrefs.GetInt("Resolution", 1);
        SetResolution(savedResolution);
        m_resolutionIndex = savedResolution;

        int savedVSync = PlayerPrefs.GetInt("VSync", 1);
        QualitySettings.vSyncCount = savedVSync;
        m_isVSyncOn = savedVSync == 1;

        int savedFullscreen = PlayerPrefs.GetInt("Fullscreen", 1);
        Screen.fullScreenMode = savedFullscreen == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        m_isFullscreenOn = savedFullscreen == 1;
        
    }

    public void SetVSync()
    {
        if (m_isVSyncOn)
        {
            QualitySettings.vSyncCount = 0; // Disable VSync
            PlayerPrefs.SetInt("VSync", 0);
            m_isVSyncOn = false;
        }
        else
        {
            QualitySettings.vSyncCount = 1; // Enable VSync
            PlayerPrefs.SetInt("VSync", 1);
            m_isVSyncOn = true;
        }
    }

    private void SetTargetFrameRate(int index)
    {
        switch (index)
        {
            case 0:
                Application.targetFrameRate = 30;
                PlayerPrefs.SetInt("TargetFrameRate", 0);
                break;
            case 1:
                Application.targetFrameRate = 60;
                PlayerPrefs.SetInt("TargetFrameRate", 1);
                break;
            case 2:
                Application.targetFrameRate = 120;
                PlayerPrefs.SetInt("TargetFrameRate", 2);
                break;
            case 3:
                Application.targetFrameRate = -1; // Unlimited
                PlayerPrefs.SetInt("TargetFrameRate", 3);
                break;
        }
    }

    public void TargetFrameRateGoRight() 
    {   
        m_targetFrameRateIndex = m_targetFrameRateIndex + 1;
        if (m_targetFrameRateIndex > 3) 
        {
            m_targetFrameRateIndex = 0;
        }
        SetTargetFrameRate(m_targetFrameRateIndex);;
    }

    public void TargetFrameRateGoLeft() 
    {   
        m_targetFrameRateIndex = m_targetFrameRateIndex - 1;
        if (m_targetFrameRateIndex < 0) 
        {
            m_targetFrameRateIndex = 3;
        }
        SetTargetFrameRate(m_targetFrameRateIndex);
    }

    public void SetFullscreen()
    {
        if (!m_isFullscreenOn)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerPrefs.SetInt("Fullscreen", 1);
            m_isFullscreenOn = true;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            PlayerPrefs.SetInt("Fullscreen", 0);
            m_isFullscreenOn = false;
        }
    }

    private void SetResolution(int index)
    {
        switch (index)
        {
            case 0:
                Screen.SetResolution(1280, 720, true);
                PlayerPrefs.SetInt("Resolution", 0);
                break;
            case 1:
                Screen.SetResolution(1920, 1080, true);
                PlayerPrefs.SetInt("Resolution", 1);
                break;
            case 2:
                Screen.SetResolution(2560, 1440, true);
                PlayerPrefs.SetInt("Resolution", 2);
                break;
            case 3:
                Screen.SetResolution(3840, 2160, true);
                PlayerPrefs.SetInt("Resolution", 3);;
                break;
        }
    }

    public void ResolutionGoRight() 
    {   
        m_resolutionIndex = m_resolutionIndex + 1;
        if (m_resolutionIndex > 3) 
        {
            m_resolutionIndex = 0;
        }
        SetResolution(m_resolutionIndex);
    }

    public void ResolutionGoLeft() 
    {   
        m_resolutionIndex = m_resolutionIndex - 1;
        if (m_resolutionIndex < 0) 
        {
            m_resolutionIndex = 3;
        }
        SetResolution(m_resolutionIndex);
    }

    public void CameraShake()
    {
        
    }

    public void MotionBlur()
    {
        
    }
}
