#if false // DEPRECATED — 삭제 예정
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 도주 시 패널티 선택 UI.
/// 동료 시스템 제거 — 주인공 HP 감소 / 난이도 증가 2종.
/// </summary>
public class PenaltyManager : MonoBehaviour
{
    public static PenaltyManager Instance { get; private set; }

    [Header("패널티 UI")]
    public GameObject      penaltyPanel;
    public Button          penaltyButton1; // HP 감소
    public Button          penaltyButton2; // 난이도 증가
    public TextMeshProUGUI penaltyText1;
    public TextMeshProUGUI penaltyText2;

    [Header("패널티 설정")]
    public int hpPenaltyAmount         = 20;
    public int difficultyIncreaseAmount = 2;

    private Action _onPenaltySelected;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (penaltyPanel != null) penaltyPanel.SetActive(false);
    }

    void Start()
    {
        if (penaltyButton1 != null) penaltyButton1.onClick.AddListener(SelectHPPenalty);
        if (penaltyButton2 != null) penaltyButton2.onClick.AddListener(SelectDifficultyIncrease);
        UpdateButtonTexts();
    }

    /// <summary>도주 패널티 UI 표시</summary>
    public void ShowPenaltySelection(Action onSelected)
    {
        _onPenaltySelected = onSelected;
        if (penaltyPanel != null) penaltyPanel.SetActive(true);
    }

    private void SelectHPPenalty()
    {
        Debug.Log($"패널티: HP -{hpPenaltyAmount}");
        BattleManager.Instance?.ApplyHPPenalty(hpPenaltyAmount);
        ClosePenaltyUI();
    }

    private void SelectDifficultyIncrease()
    {
        Debug.Log($"패널티: 난이도 +{difficultyIncreaseAmount}");
        BattleManager.Instance?.ApplyDifficultyPenalty(difficultyIncreaseAmount);
        ClosePenaltyUI();
    }

    private void ClosePenaltyUI()
    {
        if (penaltyPanel != null) penaltyPanel.SetActive(false);
        _onPenaltySelected?.Invoke();
        _onPenaltySelected = null;
    }

    private void UpdateButtonTexts()
    {
        if (penaltyText1 != null) penaltyText1.text = $"HP -{hpPenaltyAmount}";
        if (penaltyText2 != null) penaltyText2.text = $"난이도 +{difficultyIncreaseAmount}";
    }
}
#endif

