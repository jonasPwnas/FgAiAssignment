using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Options
{
    public class OptionsController : MonoBehaviour
    {
        /*[HideInInspector]public string currentResolution = "1920x1080";
        [HideInInspector]public string currentDisplayMode = "FULLSCREEN";
        [HideInInspector]public string currentVSync = "ENABLED";
        [HideInInspector]public string currentTargetFrameRate = "60";
        [HideInInspector]public string currentCameraShake = "ON";*/

        private class SettingData
        {
            public string key;
            public string[] options;
            public int currentIndex;
            public TMP_Text displayLabel;
        }

        private class SliderData
        {
            public string key;
            public float value; // 0 to 1
            public Slider uiSlider;
        }

        private Dictionary<string, SettingData> m_settings = new Dictionary<string, SettingData>();
        private Dictionary<string, SliderData> m_sliders = new Dictionary<string, SliderData>();
        
        // UI References
        private GameObject[] m_panels;
        //private Image[] m_tabHighlights; // I wanted to make it as a sprite, but that's fine 
        private GameObject[] m_tabButtons;
        
        private int currentTabIndex = 0;

        private void Start()
        {
            EnsureEventSystem();
            InitializeData();
            BindUI();
            SwitchTab(0);
        }

        private void EnsureEventSystem()
        {
            // Make sure we can click things
            if (!FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>())
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        //private void Update()
        //{
        //    HandleKeyboardInput();
        //}

        //private void HandleKeyboardInput()
        //{
        //    // Switch tabs with Q/E
        //    if (Input.GetKeyDown(KeyCode.Q))
        //    {
        //        int newIndex = currentTabIndex - 1;
        //        if (newIndex < 0) newIndex = 3;
        //        SwitchTab(newIndex);
        //    }
        //    if (Input.GetKeyDown(KeyCode.E))
        //    {
        //        int newIndex = currentTabIndex + 1;
        //        if (newIndex > 3) newIndex = 0;
        //        SwitchTab(newIndex);
        //    }

        //    if (Input.GetKeyDown(KeyCode.Escape))
        //    {
        //        gameObject.SetActive(false);
        //    }
        //}

        private void InitializeData()
        {
            // Define all our settings here
            AddSetting("Resolution", new[] { "1280x720", "1920x1080", "2560x1440", "3840x2160" }, PlayerPrefs.GetInt("Resolution", 1));
            AddSetting("Display Mode", new[] { "Windowed", "Fullscreen" }, PlayerPrefs.GetInt("Fullscreen", 1));
            AddSetting("Vertical Sync", new[] { "OFF", "ON" }, PlayerPrefs.GetInt("VSync", 1));
            AddSetting("TargetFrameRate", new[] { "30", "60", "120", "Unlimited" }, PlayerPrefs.GetInt("TargetFrameRate", 1));
            AddSetting("AttackMode", new[] { "CLICK", "HELD" }, PlayerPrefs.GetInt("AttackMode", 0));
            //AddSetting("Quality Preset", new[] { "LOW", "MEDIUM", "HIGH", "ULTRA" }, 3);
            //AddSetting("Mesh Quality", new[] { "LOW", "MEDIUM", "HIGH" }, 2);
            //AddSetting("Language", new[] { "ENGLISH", "SPANISH", "FRENCH", "GERMAN" }, 0);
            AddSetting("CameraShake", new[] { "OFF", "ON" }, PlayerPrefs.GetInt("CameraShake", 1));
            AddSetting("MotionBlur", new[] { "OFF", "ON" }, PlayerPrefs.GetInt("MotionBlur", 1));
            //AddSetting("Move Forward", new[] { "W", "UP ARROW" }, 0);
            //AddSetting("Interact", new[] { "F", "E" }, 0);

            // Sliders usually 0-1
            //AddSlider("Brightness", 0.7f);
            //AddSlider("Master Volume", 0.8f);
            //AddSlider("Music", 0.6f);
            //AddSlider("SFX", 0.8f);
            //AddSlider("Mouse Sensitivity", 0.5f);
        }

        private void AddSetting(string key, string[] opts, int defaultIdx)
        {
            m_settings[key] = new SettingData { key = key, options = opts, currentIndex = defaultIdx };
        }

        private void AddSlider(string key, float defaultVal)
        {
            m_sliders[key] = new SliderData { key = key, value = defaultVal };
        }

        private void BindUI()
        {
            // Setup Tabs
            m_panels = new GameObject[4];
            m_tabButtons = new GameObject[4];

            string[] tabNames = { "Tab_VIDEO", "Tab_GAMEPLAY", "Tab_AUDIO", "Tab_CONTROLS" };
            string[] panelNames = { "Panel_VIDEO", "Panel_GAMEPLAY", "Panel_AUDIO", "Panel_CONTROLS" };

            for (int i = 0; i < 4; i++)
            {
                m_panels[i] = FindGO(panelNames[i]);
                m_tabButtons[i] = FindGO(tabNames[i]);

                if (m_tabButtons[i])
                {
                    int idx = i; 
                    Button b = m_tabButtons[i].GetComponent<Button>();
                    if (!b) b = m_tabButtons[i].AddComponent<Button>();
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => SwitchTab(idx));
                }
            }

            // Setup Settings
            foreach (var kvp in m_settings)
            {
                BindSelector(kvp.Key);
            }

            foreach (var kvp in m_sliders)
            {
                BindSliderUI(kvp.Key);
            }

            // Details
            //BindButton("Btn_BACK", () => gameObject.SetActive(false));
            BindButton("Btn_RESET", OnReset);
            BindButton("Btn_APPLY", OnApply);
        }

        private void BindSelector(string key)
        {
            GameObject row = FindGO("Row_" + key);
            if (!row) return;

            Transform valueCont = RecursiveFind(row.transform, "Value");
            if (valueCont && valueCont.childCount > 0)
            {
                m_settings[key].displayLabel = valueCont.GetChild(0).GetComponent<TMP_Text>();
                UpdateSettingText(key);
            }

            // Hook up arrows
            var arrows = row.GetComponentsInChildren<Transform>()
                            .Where(t => t.name == "Arrow")
                            .Select(t => t.GetComponent<Button>())
                            .Where(b => b != null)
                            .ToArray();

            if (arrows.Length >= 2)
            {
                arrows[0].onClick.RemoveAllListeners();
                arrows[1].onClick.RemoveAllListeners();
                
                arrows[0].onClick.AddListener(() => CycleSetting(key, -1));
                arrows[1].onClick.AddListener(() => CycleSetting(key, 1));
            }
        }

        private void BindSliderUI(string key)
        {
            GameObject row = FindGO("Row_" + key);
            if (!row) return;

            Slider slider = row.GetComponentInChildren<Slider>();
            if (slider)
            {
                m_sliders[key].uiSlider = slider;
                slider.value = m_sliders[key].value;
                
                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener((val) => 
                {
                    m_sliders[key].value = val;
                });
            }
        }

        private void BindButton(string name, UnityEngine.Events.UnityAction action)
        {
            GameObject go = FindGO(name);
            if (go)
            {
                Button b = go.GetComponent<Button>();
                if(!b) b = go.AddComponent<Button>();
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(action);
            }
        }

        public void SwitchTab(int index)
        {
            currentTabIndex = index;

            for (int i = 0; i < m_panels.Length; i++)
            {
                if(m_panels[i]) m_panels[i].SetActive(i == index);
            }

            // Highlight the active tab
            for (int i = 0; i < m_tabButtons.Length; i++)
            {
                GameObject tab = m_tabButtons[i];
                if (!tab) continue;

                bool isActive = (i == index);
                
                TMP_Text t = tab.GetComponentInChildren<TMP_Text>();
                if (t) t.color = isActive ? Color.white : new Color(0.6f, 0.6f, 0.6f);

                Transform highlight = RecursiveFind(tab.transform, "Highlight");
                if (highlight) highlight.gameObject.SetActive(isActive);
            }
        }

        private void CycleSetting(string key, int dir)
        {
            var data = m_settings[key];
            data.currentIndex += dir;
            if (data.currentIndex < 0) data.currentIndex = data.options.Length - 1;
            if (data.currentIndex >= data.options.Length) data.currentIndex = 0;
            
            UpdateSettingText(key);
        }

        private void UpdateSettingText(string key)
        {
            var data = m_settings[key];
            if (data.displayLabel)
                data.displayLabel.text = data.options[data.currentIndex];
        }

        private void OnApply()
        {
            Debug.Log("Saving settings...");
            // Save logic here
            PlayerPrefs.Save();
        }

        private void OnReset()
        {
            Debug.Log("Resetting to defaults...");
            PlayerPrefs.DeleteAll();
            InitializeData(); 
            // Refresh visual state
            foreach(var k in m_settings.Keys) UpdateSettingText(k);
            foreach(var k in m_sliders.Keys) 
            {
                var s = m_sliders[k];
                if(s.uiSlider) s.uiSlider.value = s.value;
            }
        }

        private GameObject FindGO(string name)
        {
            Transform t = RecursiveFind(transform, name);
            return t ? t.gameObject : null;
        }

        private Transform RecursiveFind(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform res = RecursiveFind(child, name);
                if (res != null) return res;
            }
            return null;
        }
    }
}
