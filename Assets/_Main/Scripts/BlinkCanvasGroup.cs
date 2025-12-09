using UnityEngine;
using System.Collections;

public class BlinkCanvasGroup : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float blinkSpeed = 1f;  // how fast it fades
    [SerializeField] private float duration = 6f;    // total blink time

    private Coroutine blinkRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        StartBlink();
    }

    public void StartBlink()
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        blinkRoutine = StartCoroutine(Blink());
    }

    private IEnumerator Blink()
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // PingPong gives a smooth 0 → 1 → 0 → 1 pattern
            canvasGroup.alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);

            yield return null;
        }

        // Turn canvas fully off at the end
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);

        blinkRoutine = null;
    }
}
