using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class FloatingCriticalText : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float lifetime = 0.7f;

    [SerializeField, Min(0f)]
    private float riseDistance = 0.7f;

    [SerializeField, Min(1f)]
    private float popScale = 1.25f;

    private TextMeshPro textComponent;
    private Vector3 initialScale;
    private Color initialColor;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshPro>();
        initialScale = transform.localScale;
        initialColor = textComponent.color;
    }

    private void OnEnable()
    {
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition =
            startPosition + Vector3.up * riseDistance;

        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsed / lifetime);

            transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                progress
            );

            float scaleMultiplier;

            if (progress < 0.2f)
            {
                scaleMultiplier = Mathf.Lerp(
                    0.7f,
                    popScale,
                    progress / 0.2f
                );
            }
            else
            {
                scaleMultiplier = Mathf.Lerp(
                    popScale,
                    1f,
                    (progress - 0.2f) / 0.8f
                );
            }

            transform.localScale =
                initialScale * scaleMultiplier;

            Color currentColor = initialColor;

            if (progress > 0.5f)
            {
                currentColor.a = Mathf.Lerp(
                    initialColor.a,
                    0f,
                    (progress - 0.5f) / 0.5f
                );
            }

            textComponent.color = currentColor;

            yield return null;
        }

        Destroy(gameObject);
    }
}