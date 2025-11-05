using UnityEngine.XR.Hands.Capture;
using UnityEngine;

[CreateAssetMenu(fileName = "GestureData", menuName = "RecordedHands/GestureData")]
public class GestureData: ScriptableObject
{
    public int m_Index;
    public XRHandCaptureSequence m_Sequence;
    public int m_StartFrame;
    public int m_EndFrame;
    public bool showLeftHand;
    public bool showRightHand;
}