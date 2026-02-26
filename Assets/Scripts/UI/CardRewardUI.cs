using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 세계 클리어 후 카드 보상 선택 UI.
/// 카드 풀에서 랜덤 3장을 제시하고 플레이어가 1장을 획득.
/// </summary>
public class CardRewardUI : MonoBehaviour
{
    public static CardRewardUI Instance { get; private set; }

    [Header("패널")]
    public GameObject panel;

    [Header("카드 선택지 (3개)")]
    public CardRewardSlot[] rewardSlots = new CardRewardSlot[3];

    [Header("UI")]
    public TextMeshProUGUI titleText;
    public Button skipButton;

    private Action _onComplete;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    void Start()
    {
        panel?.SetActive(false);
        skipButton?.onClick.AddListener(OnSkip);
    }

    /// <summary>
    /// 카드 보상 UI 표시.
    /// cardPool 에서 랜덤 3장 제시 → 선택 후 onComplete 콜백.
    /// </summary>
    public void Show(CardData[] cardPool, Action onComplete)
    {
        if (cardPool == null || cardPool.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        _onComplete = onComplete;

        List<CardData> choices = PickRandom(cardPool, 3);

        for (int i = 0; i < rewardSlots.Length; i++)
        {
            if (rewardSlots[i] == null) continue;

            if (i < choices.Count)
                rewardSlots[i].Setup(choices[i], OnCardChosen);
            else
                rewardSlots[i].gameObject.SetActive(false);
        }

        if (titleText != null) titleText.text = "카드를 선택하세요";
        panel?.SetActive(true);
    }

    private void OnCardChosen(CardData card)
    {
        panel?.SetActive(false);

        if (PassiveCardManager.Instance != null)
            PassiveCardManager.Instance.AcquireCard(card);

        _onComplete?.Invoke();
    }

    private void OnSkip()
    {
        panel?.SetActive(false);
        _onComplete?.Invoke();
    }

    // 풀에서 중복 없이 count 장 랜덤 선택
    private static List<CardData> PickRandom(CardData[] pool, int count)
    {
        var list   = new List<CardData>(pool);
        var result = new List<CardData>();
        count = Mathf.Min(count, list.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = UnityEngine.Random.Range(0, list.Count);
            result.Add(list[idx]);
            list.RemoveAt(idx);
        }
        return result;
    }
}
