using System;
using System.Collections;
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

    public void NextChapter(int nextChapter)
    {
        StartCoroutine(ChapterCompletion(nextChapter));
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
        StartCoroutine(ChapterCompletion(TestChapter));
        TestChapter++;
    }

    [ContextMenu("Congratulate User")]
    public void TestCongratulations()
    {
        CongratsUser(TestChapter);
    }
#endif

}
