using UnityEngine;
using UnityEngine.InputSystem;

public class CardSwipe : MonoBehaviour
{
    private Vector2 startTouchPosition;
    private bool isDragging;
    public float swipeThreshold = 100f;

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            startTouchPosition = Mouse.current.position.ReadValue();
            isDragging = true;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;

            // 카드 드래그 중에는 스와이프 무시
            if (PassiveCardManager.Instance != null && PassiveCardManager.Instance.IsDragging) return;

            Vector2 endPosition = Mouse.current.position.ReadValue();
            float swipeDistance = startTouchPosition.y - endPosition.y;

            if (swipeDistance > swipeThreshold)
            {
                OnSwipeDown();
            }
        }
    }

    void OnSwipeDown()
    {
        // 구버전 WorldManager - 사용 안함
        // 스와이프 기능은 SimpleWorldManager에서 버튼으로 대체
        Debug.Log("스와이프 기능 비활성화 (SimpleWorldManager 사용 중)");
    }
}
