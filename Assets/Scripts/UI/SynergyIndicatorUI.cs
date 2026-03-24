using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 활성 시너지 목록을 HUD에 표시합니다.
/// CardSynergySystem.OnSynergiesChanged를 구독해 실시간 갱신됩니다.
/// </summary>
public class SynergyIndicatorUI : MonoBehaviour
{
    [SerializeField] private Transform  entryContainer;
    [SerializeField] private GameObject entryPrefab;

    private readonly List<GameObject> _entries = new();

    private void OnEnable()
    {
        if (CardSynergySystem.Instance != null)
            CardSynergySystem.Instance.OnSynergiesChanged += Refresh;
    }

    private void OnDisable()
    {
        if (CardSynergySystem.Instance != null)
            CardSynergySystem.Instance.OnSynergiesChanged -= Refresh;
    }

    private void Start()
    {
        if (CardSynergySystem.Instance != null)
            Refresh(CardSynergySystem.Instance.ActiveSynergies);
    }

    /// <summary>활성 시너지 목록으로 UI를 갱신합니다.</summary>
    public void Refresh(IReadOnlyList<SynergyDefinition> synergies)
    {
        foreach (var entry in _entries)
            if (entry != null) Destroy(entry);
        _entries.Clear();

        if (synergies == null || synergies.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        foreach (var synergy in synergies)
        {
            var go  = entryPrefab != null
                      ? Instantiate(entryPrefab, entryContainer)
                      : CreateDefaultEntry(synergy.synergyName);

            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = $"<b>{synergy.synergyName}</b> — {synergy.effectDescription}";

            _entries.Add(go);
        }
    }

    private GameObject CreateDefaultEntry(string synergyName)
    {
        var go       = new GameObject($"SynergyEntry_{synergyName}");
        go.transform.SetParent(entryContainer, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280f, 30f);

        var bg    = go.AddComponent<Image>();
        bg.color  = new Color(0.8f, 0.65f, 0f, 0.85f);

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRt       = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(6f, 2f);
        textRt.offsetMax = new Vector2(-6f, -2f);

        var tmp       = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = 11f;
        tmp.color     = Color.black;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        return go;
    }
}
