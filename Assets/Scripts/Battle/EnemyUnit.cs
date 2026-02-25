using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 런타임 적 인스턴스. 자동 공격 + 행동 패턴 + DEF 지원.
/// </summary>
public class EnemyUnit : MonoBehaviour
{
    public EnemyData Data { get; private set; }

    public int  CurrentHP => _currentHP;
    public int  MaxHP     => Data != null ? Data.maxHP : 0;
    public bool IsAlive   => _currentHP > 0;

    [Header("UI 참조")]
    [SerializeField] private Image           enemyImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider          hpSlider;

    private int       _currentHP;
    private int       _currentDef;      // 런타임 DEF (Defender 패턴용 임시 버프 포함)
    private float     _attackTimer;
    private bool      _isAttacking;
    private HeroUnit  _target;

    // Enrager 패턴
    private bool _enraged;

    // Defender 패턴 — 주기적 방어막
    private float _defenderTimer;
    private const float DefenderCycleTime = 5f;
    private const int   DefenderBonusDef  = 10;
    private const float DefenderDuration  = 2f;
    private float _defenderBonusTimer;

    public void Initialize(EnemyData data)
    {
        Data        = data;
        _currentHP  = data.maxHP;
        _currentDef = data.def;
        _enraged    = false;
        _isAttacking = true;

        if (enemyImage != null) enemyImage.sprite = data.sprite;
        if (nameText   != null) nameText.text     = data.enemyName;
        if (hpSlider   != null)
        {
            hpSlider.maxValue = data.maxHP;
            hpSlider.value    = data.maxHP;
        }
    }

    public void SetTarget(HeroUnit hero) => _target = hero;

    void Update()
    {
        if (!_isAttacking || Data == null) return;

        TickBehavior();

        _attackTimer += Time.deltaTime;
        float interval = Data.atkSpeed > 0 ? 1f / Data.atkSpeed : 1f;
        if (_attackTimer >= interval)
        {
            _attackTimer = 0f;
            PerformAttack();
        }
    }

    private void TickBehavior()
    {
        // Enrager — HP 50% 이하 시 분노 (1회만)
        if (!_enraged && Data.behaviorType == EnemyBehaviorType.Enrager
            && _currentHP <= MaxHP * 0.5f)
        {
            _enraged = true;
            Debug.Log($"{Data.enemyName} 분노!");
        }

        // Defender — 주기적 방어막
        if (Data.behaviorType == EnemyBehaviorType.Defender)
        {
            _defenderTimer += Time.deltaTime;
            if (_defenderTimer >= DefenderCycleTime)
            {
                _defenderTimer   = 0f;
                _defenderBonusTimer = DefenderDuration;
                Debug.Log($"{Data.enemyName} 방어막 발동!");
            }

            if (_defenderBonusTimer > 0)
                _defenderBonusTimer -= Time.deltaTime;
        }
    }

    private void PerformAttack()
    {
        if (_target == null || !_target.IsAlive) return;

        int atk = Data.atk;

        // Enrager 분노 보정
        if (_enraged)
            atk = Mathf.RoundToInt(atk * 1.5f);

        _target.TakeDamage(atk);
    }

    /// <summary>일반 데미지 — DEF 적용</summary>
    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;

        int effectiveDef = _currentDef +
            (Data.behaviorType == EnemyBehaviorType.Defender && _defenderBonusTimer > 0
                ? DefenderBonusDef : 0);

        int dmg = Mathf.Max(0, damage - effectiveDef);
        _currentHP = Mathf.Max(0, _currentHP - dmg);
        UpdateHPUI();

        if (!IsAlive) OnDied();
    }

    /// <summary>관통 데미지 — DEF 무시</summary>
    public void TakeDamageIgnoreDef(int damage)
    {
        if (!IsAlive) return;
        _currentHP = Mathf.Max(0, _currentHP - damage);
        UpdateHPUI();
        if (!IsAlive) OnDied();
    }

    public void StopAttacking() => _isAttacking = false;

    private void OnDied()
    {
        StopAttacking();
        BattleManager.Instance?.OnEnemyDied(this);
    }

    private void UpdateHPUI()
    {
        if (hpSlider != null) hpSlider.value = _currentHP;
    }
}
