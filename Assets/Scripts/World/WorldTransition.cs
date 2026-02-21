using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 쇼츠 스타일 세로 슬라이드 전환 (코루틴 기반)
/// 현재 화면이 위로 밀려나가고 새 화면이 아래에서 올라옴
/// </summary>
public class WorldTransition : MonoBehaviour
{
    [Header("전환 설정")]
    public RectTransform currentPanel;
    public RectTransform nextPanel;
    public float transitionDuration = 0.5f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isTransitioning;

    public bool IsTransitioning => isTransitioning;

    /// <summary>
    /// 슬라이드 전환 재생. onMidpoint는 화면이 완전히 덮인 시점에 호출.
    /// </summary>
    public void PlayTransition(Action onMidpoint)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionCoroutine(onMidpoint));
    }

    private IEnumerator TransitionCoroutine(Action onMidpoint)
    {
        isTransitioning = true;
        float screenHeight = GetScreenHeight();

        // nextPanel을 화면 아래에 배치
        if (nextPanel != null)
        {
            nextPanel.gameObject.SetActive(true);
            nextPanel.anchoredPosition = new Vector2(0, -screenHeight);
        }

        Vector2 currentStart = Vector2.zero;
        Vector2 currentEnd = new Vector2(0, screenHeight);
        Vector2 nextStart = new Vector2(0, -screenHeight);
        Vector2 nextEnd = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));

            if (currentPanel != null)
                currentPanel.anchoredPosition = Vector2.Lerp(currentStart, currentEnd, t);
            if (nextPanel != null)
                nextPanel.anchoredPosition = Vector2.Lerp(nextStart, nextEnd, t);

            yield return null;
        }

        // 최종 위치 보정
        if (currentPanel != null)
            currentPanel.anchoredPosition = currentEnd;
        if (nextPanel != null)
            nextPanel.anchoredPosition = nextEnd;

        // 세계 세팅 콜백
        onMidpoint?.Invoke();

        // 패널 스왑: next가 이제 current
        SwapPanels();

        isTransitioning = false;
    }

    private void SwapPanels()
    {
        // currentPanel을 화면 아래로 보내고 역할 교환
        if (currentPanel != null)
            currentPanel.gameObject.SetActive(false);

        (currentPanel, nextPanel) = (nextPanel, currentPanel);
    }

    private float GetScreenHeight()
    {
        if (currentPanel != null)
        {
            Canvas canvas = currentPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                return canvasRect.rect.height;
            }
        }
        return Screen.height;
    }
}
