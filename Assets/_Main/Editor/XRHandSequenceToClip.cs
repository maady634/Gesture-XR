#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Capture; // your SOs live here (XRHandCaptureSequence, XRHandCaptureFrame)

/// <summary>
/// Bakes a XRHandCaptureSequence asset (no playback window needed)
/// straight into an AnimationClip that targets your scene hierarchy:
/// "Left Hand Tracking/..." and "Right Hand Tracking/...".
/// </summary>
public static class XRHandSequenceToClip
{
    // you said 30 is fine
    const float k_FrameRate = 30f;

    [MenuItem("XR Hands/Bake XRHandCaptureSequence (by asset)...")]
    public static void BakeMenu()
    {
        // pick the .asset you saved from the capture window
        var path = EditorUtility.OpenFilePanel("Select XRHandCaptureSequence", "Assets", "asset");
        if (string.IsNullOrEmpty(path))
            return;

        // make it project relative
        if (path.StartsWith(Application.dataPath))
            path = "Assets" + path.Substring(Application.dataPath.Length);

        var seq = AssetDatabase.LoadAssetAtPath<XRHandCaptureSequence>(path);
        if (seq == null)
        {
            Debug.LogError($"[XRHandSequenceToClip] Could not load XRHandCaptureSequence at {path}");
            return;
        }

        BakeSequence(seq, System.IO.Path.GetDirectoryName(path));
    }

    public static void BakeSequence(XRHandCaptureSequence sequence, string defaultFolder = "Assets")
    {
        var frames = sequence.frames;
        if (frames == null || frames.Count == 0)
        {
            Debug.LogError("[XRHandSequenceToClip] Sequence has no frames.");
            return;
        }

        // where to save
        var savePath = EditorUtility.SaveFilePanelInProject(
            "Save baked hand animation",
            sequence.name + "_Baked.anim",
            "anim",
            "Pick a place to save the baked clip",
            defaultFolder
        );
        if (string.IsNullOrEmpty(savePath))
            return;

        // build jointID -> transform path mappings from your screenshots
        var leftPaths  = BuildLeftPaths();
        var rightPaths = BuildRightPaths();

        // collect curves
        var curves = new Dictionary<string, TransformCurves>();

        // if the sequence knows the real duration, we can use timestamps
        bool useTimestamps = sequence.durationInSeconds > 0f;

        for (int frameIndex = 0; frameIndex < frames.Count; frameIndex++)
        {
            XRHandCaptureFrame frame = frames[frameIndex];
            float t = useTimestamps ? frame.timestamp : frameIndex / k_FrameRate;

            // LEFT
            if (frame.IsHandTracked(Handedness.Left))
            {
                for (int jointIdx = 0; jointIdx < XRHandJointID.EndMarker.ToIndex(); jointIdx++)
                {
                    var jointId = XRHandJointIDUtility.FromIndex(jointIdx);
                    if (!leftPaths.TryGetValue(jointId, out string path))
                        continue;

                    if (!frame.TryGetJoint(out XRHandJoint joint, Handedness.Left, jointId))
                        continue;

                    AddJointKey(curves, path, t, joint);
                }
            }

            // RIGHT
            if (frame.IsHandTracked(Handedness.Right))
            {
                for (int jointIdx = 0; jointIdx < XRHandJointID.EndMarker.ToIndex(); jointIdx++)
                {
                    var jointId = XRHandJointIDUtility.FromIndex(jointIdx);
                    if (!rightPaths.TryGetValue(jointId, out string path))
                        continue;

                    if (!frame.TryGetJoint(out XRHandJoint joint, Handedness.Right, jointId))
                        continue;

                    AddJointKey(curves, path, t, joint);
                }
            }
        }

        // build clip
        var clip = new AnimationClip();
        clip.frameRate = k_FrameRate;

        foreach (var kv in curves)
        {
            string path = kv.Key;
            var c = kv.Value;

            // position
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"),
                c.posX);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"),
                c.posY);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"),
                c.posZ);

            // rotation
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x"),
                c.rotX);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.y"),
                c.rotY);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.z"),
                c.rotZ);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.w"),
                c.rotW);
        }

        AssetDatabase.CreateAsset(clip, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[XRHandSequenceToClip] Baked {frames.Count} frames → {savePath}");
    }

    // ---------------- PATH MAPS (built from your screenshots) ----------------

    // Left hierarchy starts at "Left Hand Tracking"
    static Dictionary<XRHandJointID, string> BuildLeftPaths()
    {
        const string root = "Left Hand Tracking";

        var d = new Dictionary<XRHandJointID, string>
        {
            // root / palm
            { XRHandJointID.Wrist, $"{root}/L_Wrist" },
            { XRHandJointID.Palm,  $"{root}/L_Wrist/L_Palm" },

            // thumb
            { XRHandJointID.ThumbMetacarpal, $"{root}/L_Wrist/L_ThumbMetacarpal" },
            { XRHandJointID.ThumbProximal,   $"{root}/L_Wrist/L_ThumbMetacarpal/L_ThumbProximal" },
            { XRHandJointID.ThumbDistal,     $"{root}/L_Wrist/L_ThumbMetacarpal/L_ThumbProximal/L_ThumbDistal" },
            { XRHandJointID.ThumbTip,        $"{root}/L_Wrist/L_ThumbMetacarpal/L_ThumbProximal/L_ThumbDistal/L_ThumbTip" },

            // index
            { XRHandJointID.IndexMetacarpal,   $"{root}/L_Wrist/L_IndexMetacarpal" },
            { XRHandJointID.IndexProximal,     $"{root}/L_Wrist/L_IndexMetacarpal/L_IndexProximal" },
            { XRHandJointID.IndexIntermediate, $"{root}/L_Wrist/L_IndexMetacarpal/L_IndexProximal/L_IndexIntermediate" },
            { XRHandJointID.IndexDistal,       $"{root}/L_Wrist/L_IndexMetacarpal/L_IndexProximal/L_IndexIntermediate/L_IndexDistal" },
            { XRHandJointID.IndexTip,          $"{root}/L_Wrist/L_IndexMetacarpal/L_IndexProximal/L_IndexIntermediate/L_IndexDistal/L_IndexTip" },

            // middle
            { XRHandJointID.MiddleMetacarpal,   $"{root}/L_Wrist/L_MiddleMetacarpal" },
            { XRHandJointID.MiddleProximal,     $"{root}/L_Wrist/L_MiddleMetacarpal/L_MiddleProximal" },
            { XRHandJointID.MiddleIntermediate, $"{root}/L_Wrist/L_MiddleMetacarpal/L_MiddleProximal/L_MiddleIntermediate" },
            { XRHandJointID.MiddleDistal,       $"{root}/L_Wrist/L_MiddleMetacarpal/L_MiddleProximal/L_MiddleIntermediate/L_MiddleDistal" },
            { XRHandJointID.MiddleTip,          $"{root}/L_Wrist/L_MiddleMetacarpal/L_MiddleProximal/L_MiddleIntermediate/L_MiddleDistal/L_MiddleTip" },

            // ring
            { XRHandJointID.RingMetacarpal,   $"{root}/L_Wrist/L_RingMetacarpal" },
            { XRHandJointID.RingProximal,     $"{root}/L_Wrist/L_RingMetacarpal/L_RingProximal" },
            { XRHandJointID.RingIntermediate, $"{root}/L_Wrist/L_RingMetacarpal/L_RingProximal/L_RingIntermediate" },
            { XRHandJointID.RingDistal,       $"{root}/L_Wrist/L_RingMetacarpal/L_RingProximal/L_RingIntermediate/L_RingDistal" },
            { XRHandJointID.RingTip,          $"{root}/L_Wrist/L_RingMetacarpal/L_RingProximal/L_RingIntermediate/L_RingDistal/L_RingTip" },

            // little
            { XRHandJointID.LittleMetacarpal,   $"{root}/L_Wrist/L_LittleMetacarpal" },
            { XRHandJointID.LittleProximal,     $"{root}/L_Wrist/L_LittleMetacarpal/L_LittleProximal" },
            { XRHandJointID.LittleIntermediate, $"{root}/L_Wrist/L_LittleMetacarpal/L_LittleProximal/L_LittleIntermediate" },
            { XRHandJointID.LittleDistal,       $"{root}/L_Wrist/L_LittleMetacarpal/L_LittleProximal/L_LittleIntermediate/L_LittleDistal" },
            { XRHandJointID.LittleTip,          $"{root}/L_Wrist/L_LittleMetacarpal/L_LittleProximal/L_LittleIntermediate/L_LittleDistal/L_LittleTip" },
        };

        return d;
    }

    // Right hierarchy starts at "Right Hand Tracking"
    static Dictionary<XRHandJointID, string> BuildRightPaths()
    {
        const string root = "Right Hand Tracking";

        var d = new Dictionary<XRHandJointID, string>
        {
            { XRHandJointID.Wrist, $"{root}/R_Wrist" },
            { XRHandJointID.Palm,  $"{root}/R_Wrist/R_Palm" },

            // thumb
            { XRHandJointID.ThumbMetacarpal, $"{root}/R_Wrist/R_ThumbMetacarpal" },
            { XRHandJointID.ThumbProximal,   $"{root}/R_Wrist/R_ThumbMetacarpal/R_ThumbProximal" },
            { XRHandJointID.ThumbDistal,     $"{root}/R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal" },
            { XRHandJointID.ThumbTip,        $"{root}/R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip" },

            // index
            { XRHandJointID.IndexMetacarpal,   $"{root}/R_Wrist/R_IndexMetacarpal" },
            { XRHandJointID.IndexProximal,     $"{root}/R_Wrist/R_IndexMetacarpal/R_IndexProximal" },
            { XRHandJointID.IndexIntermediate, $"{root}/R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate" },
            { XRHandJointID.IndexDistal,       $"{root}/R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal" },
            { XRHandJointID.IndexTip,          $"{root}/R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip" },

            // middle
            { XRHandJointID.MiddleMetacarpal,   $"{root}/R_Wrist/R_MiddleMetacarpal" },
            { XRHandJointID.MiddleProximal,     $"{root}/R_Wrist/R_MiddleMetacarpal/R_MiddleProximal" },
            { XRHandJointID.MiddleIntermediate, $"{root}/R_Wrist/R_MiddleMetacarpal/R_MiddleProximal/R_MiddleIntermediate" },
            { XRHandJointID.MiddleDistal,       $"{root}/R_Wrist/R_MiddleMetacarpal/R_MiddleProximal/R_MiddleIntermediate/R_MiddleDistal" },
            { XRHandJointID.MiddleTip,          $"{root}/R_Wrist/R_MiddleMetacarpal/R_MiddleProximal/R_MiddleIntermediate/R_MiddleDistal/R_MiddleTip" },

            // ring
            { XRHandJointID.RingMetacarpal,   $"{root}/R_Wrist/R_RingMetacarpal" },
            { XRHandJointID.RingProximal,     $"{root}/R_Wrist/R_RingMetacarpal/R_RingProximal" },
            { XRHandJointID.RingIntermediate, $"{root}/R_Wrist/R_RingMetacarpal/R_RingProximal/R_RingIntermediate" },
            { XRHandJointID.RingDistal,       $"{root}/R_Wrist/R_RingMetacarpal/R_RingProximal/R_RingIntermediate/R_RingDistal" },
            { XRHandJointID.RingTip,          $"{root}/R_Wrist/R_RingMetacarpal/R_RingProximal/R_RingIntermediate/R_RingDistal/R_RingTip" },

            // little
            { XRHandJointID.LittleMetacarpal,   $"{root}/R_Wrist/R_LittleMetacarpal" },
            { XRHandJointID.LittleProximal,     $"{root}/R_Wrist/R_LittleMetacarpal/R_LittleProximal" },
            { XRHandJointID.LittleIntermediate, $"{root}/R_Wrist/R_LittleMetacarpal/R_LittleProximal/R_LittleIntermediate" },
            { XRHandJointID.LittleDistal,       $"{root}/R_Wrist/R_LittleMetacarpal/R_LittleProximal/R_LittleIntermediate/R_LittleDistal" },
            { XRHandJointID.LittleTip,          $"{root}/R_Wrist/R_LittleMetacarpal/R_LittleProximal/R_LittleIntermediate/R_LittleDistal/R_LittleTip" },
        };

        return d;
    }

    // ---------------- CURVE + KEY HELPERS ----------------

    class TransformCurves
    {
        public AnimationCurve posX = new AnimationCurve();
        public AnimationCurve posY = new AnimationCurve();
        public AnimationCurve posZ = new AnimationCurve();
        public AnimationCurve rotX = new AnimationCurve();
        public AnimationCurve rotY = new AnimationCurve();
        public AnimationCurve rotZ = new AnimationCurve();
        public AnimationCurve rotW = new AnimationCurve();
    }

    static void AddJointKey(Dictionary<string, TransformCurves> curves, string path, float time, XRHandJoint joint)
    {
        // XRHandJoint (runtime) uses TryGetPose(out Pose) → it's world space in XR Hands
        if (!joint.TryGetPose(out Pose pose))
            return;

        // ASSUMPTION: when you play this clip, "Left Hand Tracking" and "Right Hand Tracking"
        // will also be at world origin → so we can write position/rotation straight.
        Vector3 localPos = pose.position;
        Quaternion localRot = pose.rotation;

        if (!curves.TryGetValue(path, out var tc))
        {
            tc = new TransformCurves();
            curves[path] = tc;
        }

        tc.posX.AddKey(time, localPos.x);
        tc.posY.AddKey(time, localPos.y);
        tc.posZ.AddKey(time, localPos.z);

        tc.rotX.AddKey(time, localRot.x);
        tc.rotY.AddKey(time, localRot.y);
        tc.rotZ.AddKey(time, localRot.z);
        tc.rotW.AddKey(time, localRot.w);
    }
}
#endif
