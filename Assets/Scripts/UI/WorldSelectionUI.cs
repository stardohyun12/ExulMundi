using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>게임 시작 시 1회 표시되는 세계 선택 패널.</summary>
public class WorldSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject          panel;
    [SerializeField] private WorldSelectionSlot[] slots;

    private Action<WorldDefinition> _onSelect;

    private void Awake() => panel?.SetActive(false);

    /// <summary>세계 선택 화면을 표시합니다.</summary>
    public void Show(WorldDefinition[] worldPool, Action<WorldDefinition> onSelect)
    {
        if (worldPool == null || worldPool.Length == 0)
        {
            onSelect?.Invoke(null);
            return;
        }

        _onSelect        = onSelect;
        var candidates   = ShuffledSubset(worldPool, slots.Length);

        for (int i = 0; i < slots.Length; i++)
        {
            bool active = i < candidates.Length;
            slots[i].gameObject.SetActive(active);
            if (active) slots[i].Setup(candidates[i], OnWorldSelected);
        }

        panel?.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        panel?.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnWorldSelected(WorldDefinition world)
    {
        Hide();
        _onSelect?.Invoke(world);
    }

    private static WorldDefinition[] ShuffledSubset(WorldDefinition[] src, int count)
    {
        var list = new List<WorldDefinition>(src);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        int take   = Mathf.Min(count, list.Count);
        var result = new WorldDefinition[take];
        for (int i = 0; i < take; i++) result[i] = list[i];
        return result;
    }
}
