using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands.Capture;

public class XRHandCaptureRuntimePlayer : MonoBehaviour
{
    [Header("Capture")]
    public XRHandCaptureSequence sequence;

    [Header("Global offset (move this to reposition whole playback)")]
    public Transform playbackRoot;   // <— optional

    [Header("Joint Targets (26 each, in correct hierarchy like your screenshot)")]
    public List<Transform> leftJoints = new List<Transform>(26);
    public List<Transform> rightJoints = new List<Transform>(26);

    [Header("Playback")]
    public int startFrame = 0;
    public int endFrame = -1;     // -1 = last frame
    public bool loop = true;
    public bool playOnAwake = true;
    public float overrideFPS = 0f;   // 0 = use seq duration

    private float _timePerFrame = 1f / 30f;
    private float _timer;
    private int _currentFrame;
    private int _lastFrame;

    private const int JointCount = 26;

    void Awake()
    {
        EnsureListSize(leftJoints, JointCount);
        EnsureListSize(rightJoints, JointCount);
        SetupFrames();
    }

    void Start()
    {
        if (!playOnAwake)
            enabled = false;
    }

    public void PlayRange(int from, int to)
    {
        startFrame = Mathf.Max(0, from);
        endFrame = to;
        SetupFrames();
        _currentFrame = startFrame;
        enabled = true;
        ApplyFrame(_currentFrame);
    }

    void SetupFrames()
    {
        if (sequence == null || sequence.frames == null || sequence.frames.Count == 0)
            return;

        int total = sequence.frames.Count;
        _lastFrame = (endFrame < 0 || endFrame >= total) ? total - 1 : endFrame;
        startFrame = Mathf.Clamp(startFrame, 0, _lastFrame);

        if (overrideFPS > 0.01f)
        {
            _timePerFrame = 1f / overrideFPS;
        }
        else
        {
            if (sequence.durationInSeconds > 0.0001f)
            {
                float fps = total / sequence.durationInSeconds;
                _timePerFrame = 1f / Mathf.Max(1f, fps);
            }
            else
            {
                _timePerFrame = 1f / 30f;
            }
        }
    }

    void Update()
    {
        if (sequence == null || sequence.frames == null || sequence.frames.Count == 0)
            return;

        _timer += Time.deltaTime;
        if (_timer >= _timePerFrame)
        {
            _timer -= _timePerFrame;
            _currentFrame++;

            if (_currentFrame > _lastFrame)
            {
                if (loop)
                    _currentFrame = startFrame;
                else
                {
                    enabled = false;
                    return;
                }
            }

            ApplyFrame(_currentFrame);
        }
    }

    void ApplyFrame(int frameIndex)
    {
        if (sequence == null) return;
        if (frameIndex < 0 || frameIndex >= sequence.frames.Count) return;

        var frame = sequence.frames[frameIndex];

        var leftArray = GetJointsArrayFromFrame(frame, true);
        var rightArray = GetJointsArrayFromFrame(frame, false);

        // LEFT
        if (leftArray != null)
        {
            for (int i = 0; i < JointCount && i < leftArray.Count; i++)
            {
                var t = leftJoints[i];
                if (t == null) continue;

                if (TryGetPoseFromJoint(leftArray[i], out Vector3 posW, out Quaternion rotW))
                {
                    ApplyPoseToTransform(t, posW, rotW);
                }
            }
        }

        // RIGHT
        if (rightArray != null)
        {
            for (int i = 0; i < JointCount && i < rightArray.Count; i++)
            {
                var t = rightJoints[i];
                if (t == null) continue;

                if (TryGetPoseFromJoint(rightArray[i], out Vector3 posW, out Quaternion rotW))
                {
                    ApplyPoseToTransform(t, posW, rotW);
                }
            }
        }
    }

    // ★ THIS is the important part ★
    void ApplyPoseToTransform(Transform target, Vector3 capturedWorldPos, Quaternion capturedWorldRot)
    {
        // 1. apply global offset (move whole both-hands rig)
        if (playbackRoot != null)
        {
            capturedWorldPos = playbackRoot.TransformPoint(capturedWorldPos);
            capturedWorldRot = playbackRoot.rotation * capturedWorldRot;
        }

        // 2. now convert into *this joint's actual parent* space
        if (target.parent != null)
        {
            target.localPosition = target.parent.InverseTransformPoint(capturedWorldPos);
            target.localRotation = Quaternion.Inverse(target.parent.rotation) * capturedWorldRot;
        }
        else
        {
            // no parent, just drop in world
            target.position = capturedWorldPos;
            target.rotation = capturedWorldRot;
        }
    }

    System.Collections.IList GetJointsArrayFromFrame(XRHandCaptureFrame frame, bool left)
    {
        object boxed = frame;
        var t = boxed.GetType();
        var fieldName = left ? "m_LeftHandJoints" : "m_RightHandJoints";
        var fi = t.GetField(fieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        if (fi == null) return null;
        return fi.GetValue(boxed) as System.Collections.IList;
    }

    bool TryGetPoseFromJoint(object jointObj, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (jointObj == null) return false;

        var jt = jointObj.GetType();

        object poseObj = null;
        var poseProp = jt.GetProperty("pose",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (poseProp != null)
            poseObj = poseProp.GetValue(jointObj);
        if (poseObj == null)
        {
            var poseField = jt.GetField("m_Pose",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            if (poseField != null)
                poseObj = poseField.GetValue(jointObj);
        }
        if (poseObj == null)
            return false;

        var pt = poseObj.GetType();

        var posField = pt.GetField("position",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (posField != null)
            position = (Vector3)posField.GetValue(poseObj);
        else
        {
            var posProp = pt.GetProperty("position",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            if (posProp != null)
                position = (Vector3)posProp.GetValue(poseObj);
        }

        var rotField = pt.GetField("rotation",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (rotField != null)
            rotation = (Quaternion)rotField.GetValue(poseObj);
        else
        {
            var rotProp = pt.GetProperty("rotation",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            if (rotProp != null)
                rotation = (Quaternion)rotProp.GetValue(poseObj);
        }

        return true;
    }

    void EnsureListSize(List<Transform> list, int size)
    {
        while (list.Count < size) list.Add(null);
        while (list.Count > size) list.RemoveAt(list.Count - 1);
    }
}
