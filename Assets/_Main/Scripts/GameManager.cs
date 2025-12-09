using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Hands.Samples.GestureSample;
using UnityEngine.XR.Templates.MR;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    public StaticHandGesture[] staticHandGestures;

    private ARFeatureController m_FeatureController;

    [SerializeField]
    public event Action<int> NextChapterTrigger;

    [SerializeField]
    public event Action<int> Congratulation;

    private void Awake()
    {
        m_FeatureController = GetComponent<ARFeatureController>();
    }

    [SerializeField] private int skipCounter = 0;

    [Header("Timer UI")]
    [SerializeField] public Image skipBg;
    [SerializeField] public float initialTimer = 10f; // use this as the fixed starting value
    [SerializeField] public TextMeshProUGUI timer_ui;
    [SerializeField] public Button skipButton; // assign in inspector

    // runtime state
    private float remainingTimer = 0f;
    private Coroutine timerCoroutine = null;

    public UIManager uiManager;

    public void skipCurrent()
    {
        if(skipCounter == 3 || skipCounter == 6 || skipCounter == 12)
        {
            CongratsUser(0);
            uiManager.BackToHome();
            skipBg.gameObject.SetActive(false);
        }
        else
        {
            NextChapter(skipCounter);
        }
    }

    public void TurnOffGestures()
    {
        foreach(var f in staticHandGestures)
        {
            f.gameObject.SetActive(false);
        }
    }

    public void NextChapter(int nextChapter)
    {
        skipBg.gameObject.SetActive(true);
        skipCounter = nextChapter + 1;
        // reset gestures and start chapter completion
        timerCoroutine = StartTimer(initialTimer);
        timer_ = initialTimer; // keep for compatibility if other code uses timer_
        StartCoroutine(ChapterCompletion(nextChapter));
    }

    // Backwards-compatible public field you had (kept but prefer initialTimer)
    [SerializeField]
    public float timer_ = 10f;

    /// <summary>
    /// Starts the countdown coroutine, cancelling any existing one.
    /// </summary>
    private Coroutine StartTimer(float seconds)
    {
        // stop previous
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        remainingTimer = seconds;

        // reset UI
        if (skipBg != null) skipBg.fillAmount = 0f;
        if (timer_ui != null) timer_ui.text = Mathf.CeilToInt(remainingTimer).ToString();
        if (skipButton != null) skipButton.interactable = false;

        timerCoroutine = StartCoroutine(RunTimer());
        return timerCoroutine;
    }

    private IEnumerator RunTimer()
    {
        // Count down using Time.deltaTime — smooth fill
        while (remainingTimer > 0f)
        {
            remainingTimer -= Time.deltaTime;
            timer_ = remainingTimer; // keep compatibility

            // update timer UI (ceil so it shows 10..9..0)
            if (timer_ui != null) timer_ui.text = Mathf.CeilToInt(Mathf.Max(0f, remainingTimer)).ToString();

            // update loading image fill — change formula depending on desired direction:
            // fill from 0 -> 1 as time passes:
            if (skipBg != null) skipBg.fillAmount = (initialTimer - remainingTimer) / initialTimer;

            yield return null;
        }

        // reached zero: finalize UI
        remainingTimer = 0f;
        timer_ = 0f;
        if (timer_ui != null) timer_ui.text = "0";
        if (skipBg != null) skipBg.fillAmount = 1f;
        if (skipButton != null) skipButton.interactable = true;

        timerCoroutine = null;
    }

    public IEnumerator ChapterCompletion(int nextChapter)
    {
        // call tick mark of current chapter
        yield return new WaitForSeconds(1f);
        foreach (var item in staticHandGestures)
        {
            item.gameObject.SetActive(false);
        }
        staticHandGestures[nextChapter].gameObject.SetActive(true);
        NextChapterTrigger?.Invoke(nextChapter);
        Debug.Log("================== INVOKING NEXTCHAPTERTRIGGER ====================");
    }

    public void CongratsUser(int id)
    {
        Congratulation?.Invoke(id);
        Debug.Log("================== INVOKING CONGRATULATIONS ====================");
    }

    public void InitializePassthrough()
    {
        if (m_FeatureController != null)
            m_FeatureController.TogglePassthrough(true);
    }

#if UNITY_EDITOR
    public int TestChapter = 0;

    [ContextMenu("NextChapter chapter")]
    public void TestNextChapterEditor()
    {
        //StartCoroutine(ChapterCompletion(TestChapter));
        NextChapter(TestChapter);
        TestChapter++;
    }

    [ContextMenu("Congratulate User")]
    public void TestCongratulations()
    {
        CongratsUser(TestChapter);
    }
#endif

}
