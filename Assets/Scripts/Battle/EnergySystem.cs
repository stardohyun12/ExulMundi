using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 에너지 시스템 — 시간이 지나면서 에너지가 찬다. 카드 사용 시 소모.
/// 최대 10, 초당 1.5 회복 (기본값)
/// </summary>
public class EnergySystem : MonoBehaviour
{
    public static EnergySystem Instance { get; private set; }

    [Header("에너지 설정")]
    public float maxEnergy      = 10f;
    public float regenPerSecond = 1.5f;

    [Header("UI")]
    [SerializeField] private Slider          energySlider;
    [SerializeField] private TextMeshProUGUI energyText;

    private float _currentEnergy;
    private bool  _isActive;
    private float _regenMult  = 1f;
    private float _regenTimer;

    public float CurrentEnergy    => _currentEnergy;
    public int   CurrentEnergyInt => Mathf.FloorToInt(_currentEnergy);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>전투 시작 시 호출</summary>
    public void StartEnergyRegen()
    {
        _currentEnergy = 0f;
        _regenMult     = 1f;
        _regenTimer    = 0f;
        _isActive      = true;
        UpdateUI();
    }

    /// <summary>전투 종료 시 호출</summary>
    public void StopEnergyRegen()
    {
        _isActive = false;
    }

    void Update()
    {
        if (!_isActive) return;

        // 회복 배율 타이머
        if (_regenTimer > 0)
        {
            _regenTimer -= Time.deltaTime;
            if (_regenTimer <= 0) _regenMult = 1f;
        }

        _currentEnergy = Mathf.Min(maxEnergy,
            _currentEnergy + regenPerSecond * _regenMult * Time.deltaTime);
        UpdateUI();
    }

    public bool CanAfford(int cost) => _currentEnergy >= cost;

    /// <summary>에너지 소모 시도. 성공 시 true 반환.</summary>
    public bool TrySpend(int cost)
    {
        if (!CanAfford(cost)) return false;
        _currentEnergy -= cost;
        UpdateUI();
        return true;
    }

    /// <summary>에너지 폭발 카드 — 전부 소모하고 소모량 반환</summary>
    public int DrainAll()
    {
        int amount = CurrentEnergyInt;
        _currentEnergy = 0f;
        UpdateUI();
        return amount;
    }

    /// <summary>회복 속도 배율 버프 적용</summary>
    public void ApplyRegenBoost(float multiplier, float duration)
    {
        _regenMult  = Mathf.Max(_regenMult, multiplier);
        _regenTimer = Mathf.Max(_regenTimer, duration);
    }

    private void UpdateUI()
    {
        if (energySlider != null)
            energySlider.value = _currentEnergy / maxEnergy;
        if (energyText != null)
            energyText.text = $"{CurrentEnergyInt} / {(int)maxEnergy}";
    }
}
