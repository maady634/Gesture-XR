using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UISlider3D : MonoBehaviour
{
    public List<RectTransform> uiElements; // List of world space UI Canvases (all elements)
    public RectTransform[] placeholders; // Array of three placeholders (also world space UI)

    public float transitionDuration = 0.5f; // Duration of the transition

    private int currentIndex = 0;

    void Start()
    {
        StartCoroutine(UpdateSlider());
    }

    public void RightShift()
    {
        currentIndex = (currentIndex + 1) % uiElements.Count;
        StartCoroutine(UpdateSlider());
    }

    public void LeftShift()
    {
        currentIndex = (currentIndex - 1 + uiElements.Count) % uiElements.Count;
        StartCoroutine(UpdateSlider());
    }

    IEnumerator UpdateSlider()
    {
        Vector3[] startPositions = new Vector3[placeholders.Length];
        Vector3[] targetPositions = new Vector3[placeholders.Length];
        Vector3[] startScales = new Vector3[placeholders.Length];
        Vector3[] targetScales = new Vector3[placeholders.Length];
        Quaternion[] startRotations = new Quaternion[placeholders.Length];
        Quaternion[] targetRotations = new Quaternion[placeholders.Length];

        // Store start positions, scales, and rotations
        for (int i = 0; i < placeholders.Length; i++)
        {
            int index = (currentIndex + i) % uiElements.Count;

            startPositions[i] = uiElements[index].position;
            targetPositions[i] = placeholders[i].position;

            startScales[i] = uiElements[index].localScale;
            targetScales[i] = placeholders[i].localScale;

            startRotations[i] = uiElements[index].rotation;
            targetRotations[i] = placeholders[i].rotation;

            // Activate the three main elements
            uiElements[index].gameObject.SetActive(true);
        }

        // Deactivate all other elements
        for (int i = 0; i < uiElements.Count; i++)
        {
            if (!IsElementInVisibleRange(i))
            {
                uiElements[i].gameObject.SetActive(false);
            }
        }

        float elapsedTime = 0f;

        // Transition the active elements to the placeholder positions, scales, and rotations
        while (elapsedTime < transitionDuration)
        {
            for (int i = 0; i < placeholders.Length; i++)
            {
                int index = (currentIndex + i) % uiElements.Count;

                uiElements[index].position = Vector3.Lerp(startPositions[i], targetPositions[i], elapsedTime / transitionDuration);
                uiElements[index].localScale = Vector3.Lerp(startScales[i], targetScales[i], elapsedTime / transitionDuration);
                uiElements[index].rotation = Quaternion.Lerp(startRotations[i], targetRotations[i], elapsedTime / transitionDuration);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure all elements reach their final positions, scales, and rotations
        for (int i = 0; i < placeholders.Length; i++)
        {
            int index = (currentIndex + i) % uiElements.Count;

            uiElements[index].position = targetPositions[i];
            uiElements[index].localScale = targetScales[i];
            uiElements[index].rotation = targetRotations[i];
        }
    }

    private bool IsElementInVisibleRange(int elementIndex)
    {
        for (int i = 0; i < placeholders.Length; i++)
        {
            int index = (currentIndex + i) % uiElements.Count;
            if (elementIndex == index)
            {
                return true;
            }
        }
        return false;
    }
}
