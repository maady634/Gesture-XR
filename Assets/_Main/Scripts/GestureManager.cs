using UnityEngine;
using UnityEngine.XR.Hands.Capture;

public class GestureManager : MonoBehaviour
{
    [SerializeField]
    public GameManager gameManager;

    [SerializeField]
    private XRHandCaptureRuntimePlayer runtimePlayer;

    [SerializeField]
    public int CurrentGestureIndex = 0;

    [SerializeField]
    public SkinnedMeshRenderer leftHand;
    [SerializeField]
    public SkinnedMeshRenderer rightHand;

    public GestureData[] gestureDatas;

    private void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        runtimePlayer = GetComponent<XRHandCaptureRuntimePlayer>();

        if(gameManager != null )
        {
            gameManager.NextChapterTrigger += DisableHandPreview;
            gameManager.Congratulation += DisableHandPreview;
            gameManager.NextChapterTrigger += StartNextGesture;
        }
    }

    public void StartNextGesture(int index)
    {
        if(runtimePlayer == null) { return; }

        if (gestureDatas[index].showLeftHand)
        {
            leftHand.enabled = true;
        }
        if (gestureDatas[index].showRightHand)
        {
            rightHand.enabled = true;
        }

        CurrentGestureIndex++;

        runtimePlayer.sequence = gestureDatas[index].m_Sequence;
        runtimePlayer.startFrame = gestureDatas[index].m_StartFrame;
        runtimePlayer.endFrame = gestureDatas[index].m_EndFrame;

        runtimePlayer.PlayRange(runtimePlayer.startFrame, runtimePlayer.endFrame);
    }

    public void DisableHandPreview(int index)
    {
        leftHand.enabled = false;
        rightHand.enabled = false;
    }
}


