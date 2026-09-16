using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UpgradeCardEntranceEffect : MonoBehaviour
{
    [Header("Entrance")]
    [SerializeField] private float delay;
    [SerializeField] private float duration = 0.32f;
    [SerializeField] private float slideDistance = 30f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 finalPosition;
    private Coroutine entranceRoutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        finalPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        entranceRoutine = StartCoroutine(PlayEntrance());
    }

    private IEnumerator PlayEntrance()
    {
        Vector2 startPosition =
            finalPosition + Vector2.down * slideDistance;

        rectTransform.anchoredPosition = startPosition;
        canvasGroup.alpha = 0f;

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress =
                1f - Mathf.Pow(1f - progress, 3f);

            rectTransform.anchoredPosition = Vector2.Lerp(
                startPosition,
                finalPosition,
                easedProgress
            );

            canvasGroup.alpha = easedProgress;

            yield return null;
        }

        rectTransform.anchoredPosition = finalPosition;
        canvasGroup.alpha = 1f;
        entranceRoutine = null;
    }

    private void OnDisable()
    {
        if (entranceRoutine != null)
            StopCoroutine(entranceRoutine);

        if (rectTransform != null)
            rectTransform.anchoredPosition = finalPosition;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        entranceRoutine = null;
    }
}