using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>인카운터 완료 후 카드를 선택하는 보상 패널.</summary>
public class CardRewardUI : MonoBehaviour
{
    [SerializeField] private GameObject      panel;
    [SerializeField] private CardRewardSlot[] slots;
    [SerializeField] private Button          skipButton;

    private Action<CardData> _onChosen;

    private void Awake() => panel?.SetActive(false);

    private void Start() => skipButton?.onClick.AddListener(() => Choose(null));

    /// <summary>카드 보상 화면을 표시합니다.</summary>
    public void Show(CardData[] pool, Action<CardData> onChosen)
    {
        if (pool == null || pool.Length == 0)
        {
            onChosen?.Invoke(null);
            return;
        }

        _onChosen      = onChosen;
        var candidates = ShuffledSubset(pool, slots.Length);

        for (int i = 0; i < slots.Length; i++)
        {
            bool active = i < candidates.Length;
            slots[i].gameObject.SetActive(active);
            if (active) slots[i].Setup(candidates[i], Choose);
        }

        panel?.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        panel?.SetActive(false);
        Time.timeScale = 1f;
    }

    private void Choose(CardData card)
    {
        Hide();
        _onChosen?.Invoke(card);
    }

    private static CardData[] ShuffledSubset(CardData[] src, int count)
    {
        var list = new List<CardData>(src);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        int take   = Mathf.Min(count, list.Count);
        var result = new CardData[take];
        for (int i = 0; i < take; i++) result[i] = list[i];
        return result;
    }
}
