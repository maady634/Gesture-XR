/*#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Capture;

public static class XRHandCaptureDebug
{
    // Menu: Tools/Hand Recording/Print First Frame Joints
    [MenuItem("Tools/Hand Recording/Print First Frame Joints")]
    public static void PrintFirstFrame()
    {
        // get the selected asset in Project
        var seq = Selection.activeObject as XRHandCaptureSequence;
        if (seq == null)
        {
            Debug.LogWarning("Select an XRHandCaptureSequence asset in the Project window first.");
            return;
        }

        var frames = seq.frames;
        if (frames == null || frames.Count == 0)
        {
            Debug.LogWarning("Sequence has no frames.");
            return;
        }

        var frame = frames[0];
        Debug.Log($"=== XRHandCaptureSequence: {seq.name} | Frame 0 | time={frame.timestamp:0.000}s ===");

        // LEFT
        if (true)
        {
            Debug.Log("LEFT HAND:");
            foreach (XRHandJointID id in System.Enum.GetValues(typeof(XRHandJointID)))
            {
                if (id == XRHandJointID.Invalid) continue;

                if (frame.TryGetJoint(out var joint, Handedness.Left, id))
                {
                    // the array index is id.ToIndex()
                    int idx = id.ToIndex();
                    Debug.Log($"L[{idx:00}] -> {id}  pos={joint.pose.position} rot={joint.pose.rotation}");
                }
                else
                {
                    Debug.Log($"L[??] -> {id}  (not valid in this frame)");
                }
            }
        }
        else
        {
            Debug.Log("LEFT HAND: not tracked in frame 0");
        }

        // RIGHT
        if (true)
        {
            Debug.Log("RIGHT HAND:");
            foreach (XRHandJointID id in System.Enum.GetValues(typeof(XRHandJointID)))
            {
                if (id == XRHandJointID.Invalid) continue;

                if (frame.TryGetJoint(out var joint, Handedness.Right, id))
                {
                    int idx = id.ToIndex();
                    Debug.Log($"R[{idx:00}] -> {id}  pos={joint.} rot={joint.pose.rotation}");
                }
                else
                {
                    Debug.Log($"R[??] -> {id}  (not valid in this frame)");
                }
            }
        }
        else
        {
            Debug.Log("RIGHT HAND: not tracked in frame 0");
        }

        Debug.Log("=== Done. Use the indices (L[00]..L[25], R[00]..R[25]) to fill the editor window slots in the same order. ===");
    }
}
#endif
*/