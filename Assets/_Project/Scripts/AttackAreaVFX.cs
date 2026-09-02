using System.Collections;
using UnityEngine;

public class AttackAreaVFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private SpriteRenderer areaRenderer;

    [Header("Timing")]
    [SerializeField, Min(0.01f)]
    private float lifetime = 0.10f;

    private Color initialColor;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (areaRenderer == null)
        {
            areaRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (areaRenderer != null)
        {
            initialColor =
                areaRenderer.color;
        }
    }

    public void Play(
        Vector2 attackOrigin,
        Vector2 attackDirection,
        float attackReach,
        float attackWidth)
    {
        if (areaRenderer == null ||
            areaRenderer.sprite == null)
        {
            Destroy(gameObject);
            return;
        }

        if (attackDirection.sqrMagnitude <=
            0.0001f)
        {
            Destroy(gameObject);
            return;
        }

        attackDirection.Normalize();

        Vector2 center =
            attackOrigin +
            attackDirection *
            (attackReach * 0.5f);

        transform.position = center;

        float angle =
            Mathf.Atan2(
                attackDirection.y,
                attackDirection.x
            ) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        Vector2 spriteSize =
            areaRenderer.sprite.bounds.size;

        float scaleX =
            spriteSize.x > 0f
                ? attackReach / spriteSize.x
                : attackReach;

        float scaleY =
            spriteSize.y > 0f
                ? attackWidth / spriteSize.y
                : attackWidth;

        transform.localScale =
            new Vector3(
                scaleX,
                scaleY,
                1f
            );

        areaRenderer.color =
            initialColor;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine =
            StartCoroutine(
                FadeAndDestroy()
            );
    }

    private IEnumerator FadeAndDestroy()
    {
        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / lifetime
                );

            float alphaMultiplier =
                1f -
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            Color color =
                initialColor;

            color.a =
                initialColor.a *
                alphaMultiplier;

            areaRenderer.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }
}