using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Templates.MR;

public class GameManager : MonoBehaviour
{
    [Header("Home Screen")]
    [SerializeField] private GameObject[] m_HomePanels;
    [SerializeField] private Button[] m_HomeButtons;

    [Header("Main Screen")]
    [SerializeField] public GameObject[] m_MainPanel;
    [SerializeField] public Button[] m_MainButtons;

    [SerializeField]
    public event Action<int> Congratulation;

    [SerializeField]
    public Camera m_Camera;
    [SerializeField]
    public Material skyboxMaterial;
    [SerializeField]
    public ARFeatureController m_FeatureController;

    public bool passthroughActive = false;

    private void Awake()
    {
        m_FeatureController = GetComponent<ARFeatureController>();

        m_Camera = Camera.main;
    }

    private void Start()
    {
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
    }

    public void TogglePanels(int index, GameObject[] panels)
    {
        foreach(GameObject panel in panels)
        {
            panel.gameObject.SetActive(false);
        }
        panels[index].gameObject.SetActive(true);
    }

    public void CongratsUser(int id)
    {
        Congratulation?.Invoke(id);
    }

    public void InitializePassthrough()
    {
        if (m_FeatureController != null)
            m_FeatureController.TogglePassthrough(true);
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
