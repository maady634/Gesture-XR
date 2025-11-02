#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Capture;

public class XRHandCapturePlayerWindow : EditorWindow
{
    [SerializeField] private XRHandCaptureSequence _sequence;

    private const int JointCount = 26;
    [SerializeField] private List<Transform> _leftTargets = new List<Transform>(JointCount);
    [SerializeField] private List<Transform> _rightTargets = new List<Transform>(JointCount);

    private bool _isPlaying;
    private int _currentFrame;
    private double _lastEditorTime;
    private float _frameDelta = 1f / 30f;

    private Vector2 _scrollPos;

    [MenuItem("Tools/XR Hands/Capture Player")]
    public static void ShowWindow()
    {
        GetWindow<XRHandCapturePlayerWindow>("XR Hand Capture Player");
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        EnsureListSize(_leftTargets, JointCount);
        EnsureListSize(_rightTargets, JointCount);
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        EditorGUILayout.LabelField("XR Hand Capture Player", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        _sequence = (XRHandCaptureSequence)EditorGUILayout.ObjectField("Capture Sequence", _sequence, typeof(XRHandCaptureSequence), false);

        if (_sequence != null)
        {
            int frameCount = _sequence.frames != null ? _sequence.frames.Count : 0;
            EditorGUILayout.LabelField("Frames", frameCount.ToString());
            EditorGUILayout.LabelField("Duration (s)", _sequence.durationInSeconds.ToString("0.000"));
        }
        else
        {
            EditorGUILayout.HelpBox("Assign an XRHandCaptureSequence asset.", MessageType.Info);
        }

        EditorGUILayout.Space(10);
        DrawTargets("Left Hand Targets (26)", _leftTargets);
        EditorGUILayout.Space(5);
        DrawTargets("Right Hand Targets (26)", _rightTargets);
        EditorGUILayout.Space(10);

        DrawPlaybackControls();
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Debug: Print Frame[0] Structure to Console"))
        {
            PrintFirstFrameStructure();
        }

        EditorGUILayout.Space(10);

        // extra button to bake
        if (GUILayout.Button("Bake Frames To AnimationClip"))
        {
            BakeFramesToAnimation();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawTargets(string label, List<Transform> list)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EnsureListSize(list, JointCount);

        for (int i = 0; i < JointCount; i++)
        {
            list[i] = (Transform)EditorGUILayout.ObjectField($"{label.Substring(0, 1)} {i:00}", list[i], typeof(Transform), true);
        }
    }

    private void DrawPlaybackControls()
    {
        int frameCount = GetFrameCount();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("⏮", GUILayout.Width(40)))
        {
            _currentFrame = 0;
            ApplyFrame();
        }

        if (GUILayout.Button(_isPlaying ? "⏸" : "▶", GUILayout.Width(40)))
        {
            TogglePlay();
        }

        if (GUILayout.Button("⏭", GUILayout.Width(40)))
        {
            if (frameCount > 0)
            {
                _currentFrame = frameCount - 1;
                ApplyFrame();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (frameCount > 0)
        {
            int newFrame = EditorGUILayout.IntSlider("Frame", _currentFrame, 0, frameCount - 1);
            if (newFrame != _currentFrame)
            {
                _currentFrame = newFrame;
                ApplyFrame();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No frames in this sequence.", MessageType.Warning);
        }
    }

    private void TogglePlay()
    {
        _isPlaying = !_isPlaying;
        _lastEditorTime = EditorApplication.timeSinceStartup;

        if (_sequence != null && _sequence.frames != null && _sequence.frames.Count > 0)
        {
            if (_sequence.durationInSeconds > 0.0001f)
            {
                float fps = _sequence.frames.Count / _sequence.durationInSeconds;
                _frameDelta = fps > 0.01f ? 1f / fps : 1f / 30f;
            }
            else
            {
                _frameDelta = 1f / 30f;
            }
        }
        else
        {
            _frameDelta = 1f / 30f;
        }
    }

    private void OnEditorUpdate()
    {
        if (!_isPlaying) return;
        if (_sequence == null || _sequence.frames == null || _sequence.frames.Count == 0) return;

        double now = EditorApplication.timeSinceStartup;
        if (now - _lastEditorTime >= _frameDelta)
        {
            _lastEditorTime = now;
            _currentFrame++;
            if (_currentFrame >= _sequence.frames.Count)
                _currentFrame = 0;

            ApplyFrame();
        }
    }

    private int GetFrameCount()
    {
        if (_sequence == null || _sequence.frames == null)
            return 0;
        return _sequence.frames.Count;
    }

    private void ApplyFrame()
    {
        if (_sequence == null || _sequence.frames == null || _sequence.frames.Count == 0) return;
        if (_currentFrame < 0 || _currentFrame >= _sequence.frames.Count) return;

        XRHandCaptureFrame frame = _sequence.frames[_currentFrame];

        IList leftJoints = GetJointsArrayFromFrame(frame, true);
        IList rightJoints = GetJointsArrayFromFrame(frame, false);

        if (leftJoints != null)
        {
            for (int i = 0; i < JointCount && i < leftJoints.Count; i++)
            {
                Transform t = _leftTargets[i];
                if (t == null) continue;

                if (TryGetPoseFromJoint(leftJoints[i], out Vector3 pos, out Quaternion rot))
                {
                    t.position = pos;
                    t.rotation = rot;
                }
            }
        }

        if (rightJoints != null)
        {
            for (int i = 0; i < JointCount && i < rightJoints.Count; i++)
            {
                Transform t = _rightTargets[i];
                if (t == null) continue;

                if (TryGetPoseFromJoint(rightJoints[i], out Vector3 pos, out Quaternion rot))
                {
                    t.position = pos;
                    t.rotation = rot;
                }
            }
        }

        SceneView.RepaintAll();
    }

    private IList GetJointsArrayFromFrame(XRHandCaptureFrame frame, bool left)
    {
        object boxed = frame;
        Type t = boxed.GetType();
        string fieldName = left ? "m_LeftHandJoints" : "m_RightHandJoints";
        FieldInfo fi = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (fi == null)
            return null;
        return fi.GetValue(boxed) as IList;
    }

    private bool TryGetPoseFromJoint(object jointObj, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (jointObj == null) return false;

        Type jt = jointObj.GetType();

        object poseObj = null;
        var poseProp = jt.GetProperty("pose", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (poseProp != null)
            poseObj = poseProp.GetValue(jointObj);
        if (poseObj == null)
        {
            var poseField = jt.GetField("m_Pose", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (poseField != null)
                poseObj = poseField.GetValue(jointObj);
        }
        if (poseObj == null)
            return false;

        Type pt = poseObj.GetType();

        var posField = pt.GetField("position", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (posField != null)
            position = (Vector3)posField.GetValue(poseObj);
        else
        {
            var posProp = pt.GetProperty("position", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (posProp != null)
                position = (Vector3)posProp.GetValue(poseObj);
        }

        var rotField = pt.GetField("rotation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (rotField != null)
            rotation = (Quaternion)rotField.GetValue(poseObj);
        else
        {
            var rotProp = pt.GetProperty("rotation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (rotProp != null)
                rotation = (Quaternion)rotProp.GetValue(poseObj);
        }

        return true;
    }

    private void EnsureListSize(List<Transform> list, int size)
    {
        if (list == null) return;
        while (list.Count < size) list.Add(null);
        while (list.Count > size) list.RemoveAt(list.Count - 1);
    }

    private void PrintFirstFrameStructure()
    {
        if (_sequence == null || _sequence.frames == null || _sequence.frames.Count == 0)
        {
            Debug.LogWarning("No sequence / frames to inspect.");
            return;
        }

        XRHandCaptureFrame frame = _sequence.frames[0];
        object boxed = frame;
        Type t = boxed.GetType();

        Debug.Log("=== XRHandCaptureFrame (frame 0) fields ===");
        foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            object v = f.GetValue(boxed);
            Debug.Log($"{f.Name} : {f.FieldType} = {v}");
        }
        Debug.Log("==========================================");
    }

    // =====================================================================
    // BAKING (updated to fix stretching + record both sides)
    // =====================================================================
    private void BakeFramesToAnimation()
    {
        if (_sequence == null || _sequence.frames == null || _sequence.frames.Count == 0)
        {
            Debug.LogWarning("No sequence to bake.");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Hand Capture Animation",
            _sequence.name + "_Hands.anim",
            "anim",
            "Choose where to save the baked animation clip.");
        if (string.IsNullOrEmpty(path))
            return;

        int frameCount = _sequence.frames.Count;
        float fps = (_sequence.durationInSeconds > 0.0001f)
            ? frameCount / _sequence.durationInSeconds
            : 30f;

        var clip = new AnimationClip
        {
            frameRate = fps
        };

        // bake both, regardless of tracking flags
        BakeSideToClip(clip, true, _leftTargets, fps);
        BakeSideToClip(clip, false, _rightTargets, fps);

        // fix quaternion flipping
        clip.EnsureQuaternionContinuity();

        AssetDatabase.CreateAsset(clip, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"Baked hand capture to AnimationClip: {path}");
    }

    private void BakeSideToClip(AnimationClip clip, bool left, List<Transform> targets, float fps)
    {
        if (targets == null) return;
        int frameCount = _sequence.frames.Count;

        for (int jointIndex = 0; jointIndex < JointCount && jointIndex < targets.Count; jointIndex++)
        {
            Transform target = targets[jointIndex];
            if (target == null) continue;

            // path relative to scene root (when you add an Animator, make sure hierarchy matches)
            string path = AnimationUtility.CalculateTransformPath(target, null);

            var posX = new AnimationCurve();
            var posY = new AnimationCurve();
            var posZ = new AnimationCurve();
            var rotX = new AnimationCurve();
            var rotY = new AnimationCurve();
            var rotZ = new AnimationCurve();
            var rotW = new AnimationCurve();

            bool hasAnyKey = false;

            for (int f = 0; f < frameCount; f++)
            {
                XRHandCaptureFrame frame = _sequence.frames[f];

                // don't depend on isLeftHandTracked / isRightHandTracked here
                IList joints = GetJointsArrayFromFrame(frame, left);
                if (joints == null || jointIndex >= joints.Count)
                    continue;

                if (!TryGetPoseFromJoint(joints[jointIndex], out Vector3 worldPos, out Quaternion worldRot))
                    continue;

                // convert to local
                Vector3 localPos;
                Quaternion localRot;
                if (target.parent != null)
                {
                    localPos = target.parent.InverseTransformPoint(worldPos);
                    localRot = Quaternion.Inverse(target.parent.rotation) * worldRot;
                }
                else
                {
                    localPos = worldPos;
                    localRot = worldRot;
                }

                // normalize quaternion to avoid scaling artifacts
                localRot = Quaternion.Normalize(localRot);

                float time = f / fps;

                posX.AddKey(time, localPos.x);
                posY.AddKey(time, localPos.y);
                posZ.AddKey(time, localPos.z);
                rotX.AddKey(time, localRot.x);
                rotY.AddKey(time, localRot.y);
                rotZ.AddKey(time, localRot.z);
                rotW.AddKey(time, localRot.w);

                hasAnyKey = true;
            }

            if (!hasAnyKey)
                continue;

            clip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", posX);
            clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y", posY);
            clip.SetCurve(path, typeof(Transform), "m_LocalPosition.z", posZ);
            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.x", rotX);
            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.y", rotY);
            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.z", rotZ);
            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.w", rotW);
        }
    }
}
#endif
