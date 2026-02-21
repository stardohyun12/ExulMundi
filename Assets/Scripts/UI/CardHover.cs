using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverOffset = 30f;
    public float hoverScale = 1.1f;
    public float hoverSpeed = 10f;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Vector3 targetPosition;
    private Vector3 targetScale;

    void Start()
    {
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
        targetPosition = originalPosition;
        targetScale = originalScale;
    }

    void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * hoverSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * hoverSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPosition = originalPosition + Vector3.up * hoverOffset;
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPosition = originalPosition;
        targetScale = originalScale;
    }
}
