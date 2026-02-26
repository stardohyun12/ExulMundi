using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 전투 중 데미지 수치를 추적하여 화면에 표시.
/// EnemyUnit.OnDamageDealt 이벤트를 구독.
/// </summary>
public class DamageTrackerUI : MonoBehaviour
{
    public static DamageTrackerUI Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI lastHitText;     // 이번 타격 데미지
    public TextMeshProUGUI totalDamageText; // 이 세계에서 누적 총 데미지

    [Header("타격 강조 애니메이션")]
    public float flashDuration = 0.25f;     // 깜빡임 시간
    public Color flashColor = Color.red;    // 강조 색상
    public Color normalColor = Color.white; // 기본 색상

    private int _lastHit   = 0;
    private int _totalDamage = 0;
    private Coroutine _flashRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    void OnEnable()
    {
        EnemyUnit.OnDamageDealt += RegisterDamage;
    }

    void OnDisable()
    {
        EnemyUnit.OnDamageDealt -= RegisterDamage;
    }

    void Start()
    {
        UpdateUI();
    }

    /// <summary>
    /// EnemyUnit에서 데미지 발생 시 호출
    /// </summary>
    public void RegisterDamage(int damage)
    {
        _lastHit      = damage;
        _totalDamage += damage;
        UpdateUI();

        // 타격 강조 애니메이션
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashLastHit());
    }

    /// <summary>
    /// 세계 이동 시 누적 데미지 초기화
    /// </summary>
    public void ResetTotal()
    {
        _lastHit     = 0;
        _totalDamage = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (lastHitText != null)
            lastHitText.text = _lastHit > 0 ? $"-{_lastHit}" : "-";

        if (totalDamageText != null)
            totalDamageText.text = $"총 데미지: {_totalDamage}";
    }

    private IEnumerator FlashLastHit()
    {
        if (lastHitText == null) yield break;

        lastHitText.color = flashColor;
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            lastHitText.color = Color.Lerp(flashColor, normalColor, t);
            yield return null;
        }

        lastHitText.color = normalColor;
    }
}
