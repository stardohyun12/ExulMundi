using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전투 UI: 동료 카드 탭, 전투 결과 표시
/// 적 HP바는 EnemyUnit 프리팹이 자체적으로 보유
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("동료 카드 (스킬 탭용)")]
    public Transform companionCardParent;
    public GameObject battleCardPrefab;

    [Header("결과 UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnBattleWon += ShowVictory;
            BattleManager.Instance.OnBattleLost += ShowDefeat;
        }
    }

    void OnDestroy()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnBattleWon -= ShowVictory;
            BattleManager.Instance.OnBattleLost -= ShowDefeat;
        }
    }

    /// <summary>
    /// 전투 시작 시 UI 초기화
    /// </summary>
    public void InitBattleUI()
    {
        CreateBattleCards();

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    /// <summary>
    /// 동료 카드 탭 시 스킬 발동
    /// </summary>
    public void OnCompanionCardTapped(CompanionUnit unit)
    {
        if (unit == null || !unit.IsAlive) return;
        bool used = unit.TryUseSkill();
        if (!used)
            Debug.Log("스킬 쿨다운 중...");
    }

    private void CreateBattleCards()
    {
        if (battleCardPrefab == null || companionCardParent == null) return;
        if (BattleManager.Instance == null) return;

        foreach (Transform child in companionCardParent)
            Destroy(child.gameObject);

        foreach (var companion in BattleManager.Instance.companionUnits)
        {
            GameObject card = Instantiate(battleCardPrefab, companionCardParent);

            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.Setup(companion.Data);
                display.LinkUnit(companion);
            }

            Button btn = card.GetComponent<Button>();
            if (btn != null)
            {
                CompanionUnit unit = companion;
                btn.onClick.AddListener(() => OnCompanionCardTapped(unit));
            }
        }
    }

    private void ShowVictory()
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = "승리!";
    }

    private void ShowDefeat()
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = "패배...";
    }
}
