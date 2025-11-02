#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands.Capture; // so we can locate the editor assembly that also has XRHandCapturePlayback

namespace EditorTools.XRHands
{
    public static class XRHandRecordingBaker
    {
        // you said 30 fps is fine
        const float k_FrameRate = 30f;

        [MenuItem("XR Hands/Bake Selected Recording To AnimationClip...")]
        public static void BakeSelectedRecording()
        {
            try
            {
                // 1. get the editor playback singleton
                var playback = GetPlaybackInstance();
                if (playback == null)
                {
                    Debug.LogError("[XRHandRecordingBaker] Could not find UnityEditor.XR.Hands.Capture.XRHandCapturePlayback.");
                    return;
                }

                // 2. make sure it has created hand GOs
                EnsureHandGameObjectsInitialized(playback);

                // 3. get selected recording
                var recording = GetSelectedRecording(playback);
                if (recording == null)
                {
                    Debug.LogError("[XRHandRecordingBaker] No recording selected in XRHandCapturePlayback window.");
                    return;
                }

                // 4. get frame count from recording (recording.frames.Count)
                int frameCount = GetRecordingFrameCount(recording);
                if (frameCount == 0)
                {
                    Debug.LogError("[XRHandRecordingBaker] Selected recording has 0 frames.");
                    return;
                }

                // 5. Ask where to save
                var _path = EditorUtility.SaveFilePanelInProject(
                    "Save Baked XR Hand Recording",
                    "BakedXRHands.anim",
                    "anim",
                    "Choose where to save the baked animation clip");
                if (string.IsNullOrEmpty(_path))
                    return;

                // 6. get common parent + hand roots
                var parentTransform = GetHandParentTransform(playback);                 // m_HandObjectParentTransform
                var leftHandRootGO  = GetHandRootGO(playback, true);                    // m_LeftHandGameObjects -> m_HandRoot
                var rightHandRootGO = GetHandRootGO(playback, false);                   // m_RightHandGameObjects -> m_HandRoot

                if (leftHandRootGO == null && rightHandRootGO == null)
                {
                    Debug.LogError("[XRHandRecordingBaker] Could not find left or right hand root GameObjects.");
                    return;
                }

                if (parentTransform == null)
                {
                    // fallback: use the scene root of the left hand
                    parentTransform = leftHandRootGO != null ? leftHandRootGO.transform.parent : rightHandRootGO.transform.parent;
                }

                // 7. We'll store curves per transform path
                var curvesPerPath = new Dictionary<string, TransformCurves>();

                float dt = 1f / k_FrameRate;

                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    float time = frameIndex * dt;

                    // very important: this is exactly how the editor preview does it
                    RenderFrame(playback, frameIndex);

                    // now the two hidden hands are in the right pose
                    if (leftHandRootGO != null)
                        RecordHierarchyForOneHand(parentTransform, leftHandRootGO.transform, time, curvesPerPath);

                    if (rightHandRootGO != null)
                        RecordHierarchyForOneHand(parentTransform, rightHandRootGO.transform, time, curvesPerPath);
                }

                // 8. build clip
                var clip = new AnimationClip();
                clip.frameRate = k_FrameRate;

                foreach (var kv in curvesPerPath)
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

                    // rotation (quaternion)
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

                // set clip settings (non-loop; change if you want)
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                AssetDatabase.CreateAsset(clip, _path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[XRHandRecordingBaker] Baked {frameCount} frames → {_path}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // ----------------- reflection helpers -----------------

        // your namespace + class name
        const string k_PlaybackFullName = "UnityEditor.XR.Hands.Capture.XRHandCapturePlayback";

        static object GetPlaybackInstance()
        {
            // the playback class lives in the *editor* assembly that also contains XRHandCaptureSequence
            var handsAsm = typeof(XRHandCaptureSequence).Assembly;
            var playbackType = handsAsm.GetType(k_PlaybackFullName);
            if (playbackType == null)
                return null;

            var getInstance = playbackType.GetMethod("GetInstance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (getInstance == null)
                return null;

            return getInstance.Invoke(null, null);
        }

        static void EnsureHandGameObjectsInitialized(object playback)
        {
            var m = playback.GetType().GetMethod("EnsureHandGameObjectsInitialized",
                BindingFlags.Instance | BindingFlags.NonPublic);
            m?.Invoke(playback, null);
        }

        static UnityEngine.Object GetSelectedRecording(object playback)
        {
            var p = playback.GetType().GetProperty("selectedRecording",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return p?.GetValue(playback, null) as UnityEngine.Object;
        }

        static int GetRecordingFrameCount(UnityEngine.Object recording)
        {
            if (recording == null) return 0;
            var t = recording.GetType();
            // XRHandCaptureSequence has "frames" : List<XRHandCaptureFrame>
            var f = t.GetField("frames", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return 0;
            var list = f.GetValue(recording) as System.Collections.IList;
            return list?.Count ?? 0;
        }

        static void RenderFrame(object playback, int frameId)
        {
            var m = playback.GetType().GetMethod("RenderFrame",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (m == null)
            {
                // fallback: older code used (Ensure -> SyncData -> SyncVisuals), but your file has RenderFrame
                return;
            }
            m.Invoke(playback, new object[] { frameId });
        }

        static Transform GetHandParentTransform(object playback)
        {
            var f = playback.GetType().GetField("m_HandObjectParentTransform",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return f?.GetValue(playback) as Transform;
        }

        static GameObject GetHandRootGO(object playback, bool left)
        {
            var f = playback.GetType().GetField(left ? "m_LeftHandGameObjects" : "m_RightHandGameObjects",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) return null;

            var hgo = f.GetValue(playback); // this is the HandGameObjects instance (the one you pasted)
            if (hgo == null) return null;

            // HandGameObjects is a private nested class, so we have to reflect its field:
            var hgoType = hgo.GetType(); // should be XRHandCapturePlayback+HandGameObjects
            var rootField = hgoType.GetField("m_HandRoot", BindingFlags.Instance | BindingFlags.NonPublic);
            if (rootField == null) return null;

            return rootField.GetValue(hgo) as GameObject;
        }

        // ------------- baking helpers -------------

        class TransformCurves
        {
            public AnimationCurve posX = new AnimationCurve();
            public AnimationCurve posY = new AnimationCurve();
            public AnimationCurve posZ = new AnimationCurve();
            public AnimationCurve rotX = new AnimationCurve();
            public AnimationCurve rotY = new AnimationCurve();
            public AnimationCurve rotZ = new AnimationCurve();
            public AnimationCurve rotW = new AnimationCurve();

            public void AddKey(float time, Transform t)
            {
                var lp = t.localPosition;
                var lr = t.localRotation;

                posX.AddKey(time, lp.x);
                posY.AddKey(time, lp.y);
                posZ.AddKey(time, lp.z);

                rotX.AddKey(time, lr.x);
                rotY.AddKey(time, lr.y);
                rotZ.AddKey(time, lr.z);
                rotW.AddKey(time, lr.w);
            }
        }

        static void RecordHierarchyForOneHand(Transform commonParent, Transform handRoot, float time,
            Dictionary<string, TransformCurves> curvesPerPath)
        {
            // record root
            AddTransformKey(commonParent, handRoot, time, curvesPerPath);

            // record all children
            RecordChildrenRecursive(commonParent, handRoot, time, curvesPerPath);
        }

        static void RecordChildrenRecursive(Transform commonParent, Transform current, float time,
            Dictionary<string, TransformCurves> curvesPerPath)
        {
            for (int i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);
                AddTransformKey(commonParent, child, time, curvesPerPath);
                RecordChildrenRecursive(commonParent, child, time, curvesPerPath);
            }
        }

        static void AddTransformKey(Transform commonParent, Transform tr, float time,
            Dictionary<string, TransformCurves> curvesPerPath)
        {
            string path = AnimationUtility.CalculateTransformPath(tr, commonParent);
            if (!curvesPerPath.TryGetValue(path, out var tc))
            {
                tc = new TransformCurves();
                curvesPerPath[path] = tc;
            }
            tc.AddKey(time, tr);
        }
    }
}
#endif
