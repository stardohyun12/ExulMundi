using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 전투 UI: HP바, 스킬 탭, 전투 결과 표시
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("적 HP바")]
    public Transform enemyHPBarParent;
    public GameObject hpBarPrefab;

    [Header("동료 카드 (스킬 탭용)")]
    public Transform companionCardParent;
    public GameObject battleCardPrefab;

    [Header("결과 UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    private Dictionary<EnemyUnit, Slider> enemyHPBars = new Dictionary<EnemyUnit, Slider>();

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
        ClearHPBars();

        if (BattleManager.Instance == null) return;

        // 적 HP바 생성
        foreach (var enemy in BattleManager.Instance.enemyUnits)
        {
            CreateEnemyHPBar(enemy);
            enemy.OnHPChanged += UpdateEnemyHPBar;
        }

        // 동료 카드 — CardDisplay가 HP바 갱신 담당
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

    private void CreateEnemyHPBar(EnemyUnit enemy)
    {
        if (hpBarPrefab == null || enemyHPBarParent == null) return;

        GameObject bar = Instantiate(hpBarPrefab, enemyHPBarParent);
        Slider slider = bar.GetComponent<Slider>();
        if (slider != null)
        {
            slider.maxValue = enemy.MaxHP;
            slider.value = enemy.CurrentHP;
            enemyHPBars[enemy] = slider;
        }

        TextMeshProUGUI label = bar.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null && enemy.Data != null)
            label.text = enemy.Data.enemyName;
    }

    private void UpdateEnemyHPBar(EnemyUnit enemy)
    {
        if (enemyHPBars.TryGetValue(enemy, out Slider slider))
            slider.value = enemy.CurrentHP;
    }

    private void CreateBattleCards()
    {
        if (battleCardPrefab == null || companionCardParent == null) return;
        if (BattleManager.Instance == null) return;

        // 기존 카드 제거
        foreach (Transform child in companionCardParent)
            Destroy(child.gameObject);

        foreach (var companion in BattleManager.Instance.companionUnits)
        {
            GameObject card = Instantiate(battleCardPrefab, companionCardParent);

            // CardDisplay 세팅 + HP바 연결
            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.Setup(companion.Data);
                display.LinkUnit(companion);
            }

            // 탭 버튼 연결
            Button btn = card.GetComponent<Button>();
            if (btn != null)
            {
                CompanionUnit unit = companion; // 클로저 캡처
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

    private void ClearHPBars()
    {
        foreach (var kvp in enemyHPBars)
        {
            if (kvp.Key != null) kvp.Key.OnHPChanged -= UpdateEnemyHPBar;
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        enemyHPBars.Clear();
    }
}
