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
        if (WorldManager.Instance == null) return;

        // 전투 중이면 도주
        if (BattleManager.Instance != null && BattleManager.Instance.IsBattleActive)
        {
            Debug.Log("도주! 패널티 선택으로");
            WorldManager.Instance.FleeFromBattle();
        }
        else
        {
            // 전투 중이 아니면 다음 세계로
            Debug.Log("스와이프! 다음 세계로");
            WorldManager.Instance.TransitionToNextWorld();
        }
    }
}
