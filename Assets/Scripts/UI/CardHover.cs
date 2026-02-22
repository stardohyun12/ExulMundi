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

    private bool initialized;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        // 레이아웃 그룹이 카드를 배치한 뒤 위치를 캡처
        StartCoroutine(CapturePositionAfterLayout());
    }

    private System.Collections.IEnumerator CapturePositionAfterLayout()
    {
        yield return new WaitForEndOfFrame();
        originalPosition = transform.localPosition;
        targetPosition = originalPosition;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * hoverSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * hoverSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!initialized) return;
        targetPosition = originalPosition + Vector3.up * hoverOffset;
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!initialized) return;
        targetPosition = originalPosition;
        targetScale = originalScale;
    }
}
