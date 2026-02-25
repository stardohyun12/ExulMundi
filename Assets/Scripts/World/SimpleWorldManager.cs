using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 간단한 세계 관리자 - 배경색으로 세계 구분
/// </summary>
public class SimpleWorldManager : MonoBehaviour
{
    public static SimpleWorldManager Instance { get; private set; }

    [Header("세계 배경색")]
    public Color[] worldColors = new Color[]
    {
        new Color(0.2f, 0.8f, 0.3f), // 숲 - 녹색
        new Color(0.9f, 0.3f, 0.1f), // 화산 - 빨강
        new Color(0.3f, 0.7f, 0.9f), // 얼음 - 파랑
        new Color(0.6f, 0.4f, 0.8f), // 유적 - 보라
        new Color(0.2f, 0.2f, 0.3f), // 어둠 - 검정
        new Color(1.0f, 0.9f, 0.5f)  // 천상 - 노랑
    };

    [Header("참조")]
    public Camera mainCamera;
    public BattleManager battleManager;
    public TestBattleStarter testBattleStarter;

    [Header("UI")]
    public TextMeshProUGUI worldInfoText;
    public Button skipWorldButton;
    public GameObject cardSelectPanel;  // 카드 선택 패널
    public TextMeshProUGUI selectPromptText;  // 안내 텍스트

    private int _currentWorldIndex = 0;
    private int _worldLevel = 1;
    private bool _isSelectingCard = false;
    private bool _payWithCard = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (skipWorldButton != null)
        {
            skipWorldButton.onClick.AddListener(SkipWorld);
        }

        if (cardSelectPanel != null)
        {
            cardSelectPanel.SetActive(false);
        }

        LoadNextWorld();
    }

    public void LoadNextWorld()
    {
        _currentWorldIndex = Random.Range(0, worldColors.Length);
        
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = worldColors[_currentWorldIndex];
        }

        if (worldInfoText != null)
        {
            worldInfoText.text = $"{GetWorldName(_currentWorldIndex)} Lv.{_worldLevel}";
        }

        RespawnEnemy();

        Debug.Log($"세계 이동: {GetWorldName(_currentWorldIndex)} Lv.{_worldLevel}");
    }

    private void RespawnEnemy()
    {
        if (testBattleStarter != null && testBattleStarter.dummy != null)
        {
            var dummyData = testBattleStarter.dummyData;
            if (dummyData != null)
            {
                int scaledHP = Mathf.RoundToInt(testBattleStarter.dummyMaxHP * Mathf.Pow(1.5f, _worldLevel - 1));
                testBattleStarter.dummy.Initialize(dummyData);
                
                System.Reflection.FieldInfo hpField = typeof(EnemyUnit).GetField("_currentHP", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (hpField != null)
                {
                    hpField.SetValue(testBattleStarter.dummy, scaledHP);
                }
                
                Debug.Log($"적 재생성: HP {scaledHP}");
            }
        }
    }

    public void OnEnemyDefeated()
    {
        Debug.Log("적 처치! 다음 세계로...");
        _worldLevel++;
        Invoke(nameof(LoadNextWorld), 1.5f);
    }

    public void SkipWorld()
    {
        _payWithCard = Random.value > 0.5f;

        if (_payWithCard)
        {
            if (PassiveCardManager.Instance != null)
            {
                var hand = PassiveCardManager.Instance.GetCurrentHand();
                if (hand.Count > 0)
                {
                    // 카드 선택 UI 표시
                    ShowCardSelectPanel();
                    return;
                }
                else
                {
                    PayWithHP();
                }
            }
        }
        else
        {
            PayWithHP();
        }

        LoadNextWorld();
    }

    private void ShowCardSelectPanel()
    {
        _isSelectingCard = true;

        if (cardSelectPanel != null)
        {
            cardSelectPanel.SetActive(true);
        }

        if (selectPromptText != null)
        {
            selectPromptText.text = "버릴 카드를 선택하세요";
        }

        Debug.Log("카드 선택 모드 활성화");
    }

    /// <summary>
    /// CardSlot에서 호출 - 선택된 카드 버리기
    /// </summary>
    public void OnCardSelected(CardSlot slot)
    {
        if (!_isSelectingCard) return;
        if (slot == null || slot.OccupantCard == null) return;

        CardData cardToRemove = slot.OccupantCard;
        
        // 카드 제거
        slot.Clear();
        
        // PassiveCardManager의 ownedCards에서도 제거
        if (PassiveCardManager.Instance != null)
        {
            PassiveCardManager.Instance.ownedCards.Remove(cardToRemove);
            PassiveCardManager.Instance.OnCardUsed(cardToRemove);
        }
        
        Debug.Log($"카드 버림: {cardToRemove.cardName} (보유 카드에서 제거됨)");

        // 선택 모드 종료
        _isSelectingCard = false;
        if (cardSelectPanel != null)
        {
            cardSelectPanel.SetActive(false);
        }

        // 다음 세계로
        LoadNextWorld();
    }

    /// <summary>
    /// 카드 선택 취소 - HP로 대신 지불
    /// </summary>
    public void CancelCardSelect()
    {
        _isSelectingCard = false;
        if (cardSelectPanel != null)
        {
            cardSelectPanel.SetActive(false);
        }

        PayWithHP();
        LoadNextWorld();
    }

    public bool IsSelectingCard => _isSelectingCard;

    private void PayWithHP()
    {
        if (battleManager != null && battleManager.HeroUnit != null)
        {
            int damage = Mathf.RoundToInt(battleManager.HeroUnit.MaxHP * 0.2f);
            battleManager.HeroUnit.TakeDamage(damage);
            Debug.Log($"HP 지불: {damage} 데미지");
        }
    }

    private string GetWorldName(int index)
    {
        string[] names = { "숲의 세계", "화산 지대", "얼음 산맥", "고대 유적", "어둠의 성채", "천상의 정원" };
        return names[index];
    }
}
