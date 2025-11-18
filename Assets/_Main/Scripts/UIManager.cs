using System;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Home Screen")]
    [SerializeField] private GameObject[] m_HomePanels;
    [SerializeField] private Button[] m_HomeButtons;

    [Header("Main Screen")]
    [SerializeField] public GameObject[] m_MainPanel;
    [SerializeField] public Button[] m_MainButtons;

    [Header("3D UI Sliders")]
    public UISlider3D[] m_Sliders;

    [SerializeField]
    private GameManager m_GameManager;

    [SerializeField]
    public Camera m_Camera;
    [SerializeField]
    public Material skyboxMaterial;
    [SerializeField]
    public bool passthroughActive = false;

    private void Start()
    {
        m_Camera = Camera.main;

        for (int btn = 0; btn < m_HomeButtons.Length; btn++)
        {
            int index = btn;
            if (m_HomePanels[index] != null)
            {
                m_HomeButtons[index].onClick.AddListener(() => TogglePanels(index, m_HomePanels));
            }
        }

        for (int btn = 0; btn < m_MainButtons.Length; btn++)
        {
            int index = btn;
            if (m_MainPanel[index] != null)
            {
                m_MainButtons[index].onClick.AddListener(() => TogglePanels(index, m_MainPanel));
            }
        }

        if(m_GameManager != null)
        {
            m_GameManager.NextChapterTrigger += UISliderUpdate;
        }
    }

    public void BackToHome()
    {
        TogglePanels(0, m_MainPanel);
    }

    public void UISliderUpdate(int index)
    {
        if(index <= 2)
        {
            Toggle3DSliders(0);
            m_Sliders[0].RightShift();
        }
        else if(index >= 3 && index <= 5)
        {
            Toggle3DSliders(1);
            m_Sliders[1].RightShift();
        }
        else
        {
            Toggle3DSliders(2);
            m_Sliders[2].RightShift();
        }
    }

    public void Toggle3DSliders(int index)
    {
        foreach (var item in m_Sliders)
        {
            item.gameObject.SetActive(false);
        }
        m_Sliders[index].gameObject.SetActive(true);
    }

    public void TogglePanels(int index, GameObject[] panels)
    {
        foreach (GameObject panel in panels)
        {
            panel.gameObject.SetActive(false);
        }
        panels[index].gameObject.SetActive(true);
    }

    public void TogglePassthroughMode()
    {
        if (!passthroughActive)
        {
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = Color.clear;
            RenderSettings.skybox = null;
            passthroughActive = true;
        }
        else
        {
            m_Camera.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.skybox = skyboxMaterial;
            passthroughActive = false;
        }
    }
}
