using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeCardHoverEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.04f;
    [SerializeField] private float pressedScale = 0.97f;
    [SerializeField] private float animationSpeed = 14f;

    private Button cardButton;
    private Vector3 normalScale;
    private Vector3 targetScale;
    private bool isHovered;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        normalScale = transform.localScale;
        targetScale = normalScale;
    }

    private void Update()
    {
        if (cardButton != null && !cardButton.interactable)
        {
            isHovered = false;
            targetScale = normalScale;
        }

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            animationSpeed * Time.unscaledDeltaTime
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanInteract())
            return;

        isHovered = true;
        targetScale = normalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = normalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanInteract())
            return;

        targetScale = normalScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanInteract())
            return;

        targetScale = isHovered
            ? normalScale * hoverScale
            : normalScale;
    }

    private bool CanInteract()
    {
        return cardButton == null || cardButton.interactable;
    }

    private void OnDisable()
    {
        transform.localScale = normalScale;
        targetScale = normalScale;
        isHovered = false;
    }
}