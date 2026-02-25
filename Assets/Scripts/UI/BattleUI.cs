using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전투 UI 총괄.
/// 주인공 HP 바 / 결과 패널 / 에너지 바 표시.
/// 핸드 카드 UI는 HandManager가 직접 관리.
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("주인공 HP UI")]
    public Slider          heroHPSlider;
    public TextMeshProUGUI heroHPText;

    [Header("결과 UI")]
    public GameObject      resultPanel;
    public TextMeshProUGUI resultText;

    void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnBattleWon  += ShowVictory;
            BattleManager.Instance.OnBattleLost += ShowDefeat;

            // 주인공 HP 이벤트 구독
            if (BattleManager.Instance.HeroUnit != null)
                BattleManager.Instance.HeroUnit.OnHPChanged += RefreshHeroHP;
        }
    }

    void OnDestroy()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnBattleWon  -= ShowVictory;
            BattleManager.Instance.OnBattleLost -= ShowDefeat;

            if (BattleManager.Instance.HeroUnit != null)
                BattleManager.Instance.HeroUnit.OnHPChanged -= RefreshHeroHP;
        }
    }

    /// <summary>전투 시작 시 호출</summary>
    public void InitBattleUI()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        RefreshHeroHP(BattleManager.Instance?.HeroUnit);
    }

    private void RefreshHeroHP(HeroUnit hero)
    {
        if (hero == null) return;
        if (heroHPSlider != null)
        {
            heroHPSlider.maxValue = hero.MaxHP;
            heroHPSlider.value    = hero.CurrentHP;
        }
        if (heroHPText != null)
            heroHPText.text = $"{hero.CurrentHP} / {hero.MaxHP}";
    }

    private void ShowVictory()
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText  != null) resultText.text = "승리!";
    }

    private void ShowDefeat()
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText  != null) resultText.text = "패배...";
    }
}
