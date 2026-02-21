using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 도주 시 패널티 선택지 UI. 선택 후 세계 전환 진행.
/// </summary>
public class PenaltyManager : MonoBehaviour
{
    public static PenaltyManager Instance { get; private set; }

    [Header("패널티 UI")]
    public GameObject penaltyPanel;
    public Button penaltyButton1; // HP 감소
    public Button penaltyButton2; // 동료 이탈
    public Button penaltyButton3; // 난이도 증가

    public TextMeshProUGUI penaltyText1;
    public TextMeshProUGUI penaltyText2;
    public TextMeshProUGUI penaltyText3;

    [Header("패널티 설정")]
    public int hpPenaltyAmount = 20;
    public int difficultyIncreaseAmount = 2;

    private Action onPenaltySelected;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (penaltyPanel != null)
            penaltyPanel.SetActive(false);
    }

    void Start()
    {
        if (penaltyButton1 != null) penaltyButton1.onClick.AddListener(SelectHPPenalty);
        if (penaltyButton2 != null) penaltyButton2.onClick.AddListener(SelectCompanionLoss);
        if (penaltyButton3 != null) penaltyButton3.onClick.AddListener(SelectDifficultyIncrease);

        UpdateButtonTexts();
    }

    /// <summary>
    /// 패널티 선택 UI 표시
    /// </summary>
    public void ShowPenaltySelection(Action onSelected)
    {
        onPenaltySelected = onSelected;

        if (penaltyPanel != null)
            penaltyPanel.SetActive(true);

        // 동료가 없으면 이탈 버튼 비활성화
        if (penaltyButton2 != null)
            penaltyButton2.interactable = PartyManager.Instance != null
                                          && PartyManager.Instance.slots.Count > 1;
    }

    private void SelectHPPenalty()
    {
        Debug.Log($"패널티 선택: 전체 HP {hpPenaltyAmount} 감소");

        if (BattleManager.Instance != null)
        {
            foreach (var companion in BattleManager.Instance.companionUnits)
                companion.ReduceHP(hpPenaltyAmount);
        }

        ClosePenaltyUI();
    }

    private void SelectCompanionLoss()
    {
        Debug.Log("패널티 선택: 동료 1명 이탈");

        if (PartyManager.Instance != null)
            PartyManager.Instance.RemoveRandomFromParty();

        ClosePenaltyUI();
    }

    private void SelectDifficultyIncrease()
    {
        Debug.Log($"패널티 선택: 난이도 +{difficultyIncreaseAmount}");
        // 난이도 증가는 WorldManager에서 다음 세계 선택 시 반영할 수 있음
        // 현재는 로그만 출력
        ClosePenaltyUI();
    }

    private void ClosePenaltyUI()
    {
        if (penaltyPanel != null)
            penaltyPanel.SetActive(false);

        onPenaltySelected?.Invoke();
        onPenaltySelected = null;
    }

    private void UpdateButtonTexts()
    {
        if (penaltyText1 != null) penaltyText1.text = $"HP -{hpPenaltyAmount}";
        if (penaltyText2 != null) penaltyText2.text = "동료 1명 이탈";
        if (penaltyText3 != null) penaltyText3.text = $"난이도 +{difficultyIncreaseAmount}";
    }
}
