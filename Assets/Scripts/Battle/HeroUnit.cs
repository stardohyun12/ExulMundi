using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 주인공 런타임 인스턴스.
/// 스탯 3계층 구조: 1계층(영구) / 2계층(전투 지속 버프) / 3계층(즉발 효과)
/// </summary>
public class HeroUnit : MonoBehaviour
{
    // ───────────────────────────────────────
    // 데이터
    // ───────────────────────────────────────
    public HeroData Data { get; private set; }

    // ───────────────────────────────────────
    // 1계층: 영구 성장 (런 전체 유지)
    // ───────────────────────────────────────
    private int _baseHP;
    private int _baseATK;
    private int _baseDEF;

    // ───────────────────────────────────────
    // 2계층: 전투 지속 버프 (전투 종료 시 초기화)
    // ───────────────────────────────────────
    private int   _atkBuff;
    private int   _defBuff;
    private int   _maxHPBuff;
    private bool  _blockNextHit;
    private bool  _weaknessExposed;     // 다음 카드 효과 2배
    private float _atkSpeedMult = 1f;
    private float _atkSpeedTimer;
    private float _defBuffTimer;
    private bool  _thornsActive;
    private float _thornsTimer;
    private bool  _invincibleActive;
    private float _invincibleTimer;

    // ───────────────────────────────────────
    // HP
    // ───────────────────────────────────────
    private int _currentHP;
    public int  CurrentHP => _currentHP;
    public int  MaxHP     => _baseHP + _maxHPBuff;
    public bool IsAlive   => _currentHP > 0;

    // ───────────────────────────────────────
    // 전투
    // ───────────────────────────────────────
    private float    _attackTimer;
    private EnemyUnit _target;

    // ───────────────────────────────────────
    // 스탯 프로퍼티
    // ───────────────────────────────────────
    public int EffectiveATK => _baseATK + _atkBuff;
    public int EffectiveDEF => _baseDEF + _defBuff;

    // ───────────────────────────────────────
    // UI
    // ───────────────────────────────────────
    [Header("UI")]
    [SerializeField] private Image             heroImage;
    [SerializeField] private Slider            hpSlider;
    [SerializeField] private TextMeshProUGUI   hpText;

    // ───────────────────────────────────────
    // 이벤트
    // ───────────────────────────────────────
    public event Action<HeroUnit> OnHPChanged;
    public event Action<HeroUnit> OnHeroDied;

    // ═══════════════════════════════════════
    // 초기화
    // ═══════════════════════════════════════

    public void Initialize(HeroData data)
    {
        Data     = data;
        _baseHP  = data.baseHP;
        _baseATK = data.baseATK;
        _baseDEF = data.baseDEF;

        ClearCombatBuffs();
        _currentHP = MaxHP;
        UpdateUI();
    }

    /// <summary>전투 시작 시 HP 및 버프 리셋</summary>
    public void ResetForBattle()
    {
        _currentHP   = MaxHP;
        _attackTimer = 0f;
        ClearCombatBuffs();
        UpdateUI();
    }

    public void SetTarget(EnemyUnit enemy) => _target = enemy;

    // ═══════════════════════════════════════
    // Update — 자동 공격 + 버프 타이머
    // ═══════════════════════════════════════

    void Update()
    {
        if (!IsAlive || Data == null) return;

        TickBuffTimers();

        if (_target == null || !_target.IsAlive) return;

        float interval = Data.attackInterval / _atkSpeedMult;
        _attackTimer += Time.deltaTime;
        if (_attackTimer >= interval)
        {
            _attackTimer = 0f;
            PerformAttack();
        }
    }

    private void TickBuffTimers()
    {
        if (_atkSpeedTimer > 0)
        {
            _atkSpeedTimer -= Time.deltaTime;
            if (_atkSpeedTimer <= 0) _atkSpeedMult = 1f;
        }
        if (_defBuffTimer > 0)
        {
            _defBuffTimer -= Time.deltaTime;
            if (_defBuffTimer <= 0) _defBuff = 0;
        }
        if (_thornsTimer > 0)
        {
            _thornsTimer -= Time.deltaTime;
            if (_thornsTimer <= 0) _thornsActive = false;
        }
        if (_invincibleTimer > 0)
        {
            _invincibleTimer -= Time.deltaTime;
            if (_invincibleTimer <= 0) _invincibleActive = false;
        }
    }

    private void PerformAttack()
    {
        if (_target == null || !_target.IsAlive) return;
        _target.TakeDamage(EffectiveATK);
    }

    // ═══════════════════════════════════════
    // 피해 / 회복
    // ═══════════════════════════════════════

    public void TakeDamage(int rawDamage)
    {
        if (!IsAlive) return;

        // 불사 — HP 1 이하 보장
        if (_invincibleActive)
        {
            _currentHP = Mathf.Max(1, _currentHP);
            return;
        }

        // 방어막 — 1회 무효화
        if (_blockNextHit)
        {
            _blockNextHit = false;
            return;
        }

        int dmg = Mathf.Max(0, rawDamage - EffectiveDEF);

        // 가시 갑옷 — 받은 피해 50% 반사
        if (_thornsActive && _target != null)
            _target.TakeDamage(Mathf.RoundToInt(dmg * 0.5f));

        _currentHP = Mathf.Max(0, _currentHP - dmg);
        UpdateUI();
        OnHPChanged?.Invoke(this);

        if (_currentHP <= 0)
            OnHeroDied?.Invoke(this);
    }

    public void Heal(int amount)
    {
        if (!IsAlive) return;
        _currentHP = Mathf.Min(MaxHP, _currentHP + amount);
        UpdateUI();
        OnHPChanged?.Invoke(this);
    }

    public void StopAttacking() { _target = null; }

    // ═══════════════════════════════════════
    // 카드 효과 적용 메서드 (2계층 버프)
    // ═══════════════════════════════════════

    public void ApplyATKBuff(int amount) => _atkBuff += amount;

    public void ApplyDEFBuff(int amount, float duration)
    {
        _defBuff     += amount;
        _defBuffTimer = Mathf.Max(_defBuffTimer, duration);
    }

    public void ApplyBlockNextHit() => _blockNextHit = true;

    public void ApplyAtkSpeedBuff(float multiplier, float duration)
    {
        _atkSpeedMult  = Mathf.Max(_atkSpeedMult, multiplier);
        _atkSpeedTimer = Mathf.Max(_atkSpeedTimer, duration);
    }

    public void ApplyThorns(float duration)
    {
        _thornsActive = true;
        _thornsTimer  = Mathf.Max(_thornsTimer, duration);
    }

    public void ApplyInvincible(float duration)
    {
        _invincibleActive = true;
        _invincibleTimer  = Mathf.Max(_invincibleTimer, duration);
    }

    public void SetWeaknessExposed() => _weaknessExposed = true;

    /// <summary>약점 노출 소비 (카드 효과 2배 처리 후 플래그 해제)</summary>
    public bool ConsumeWeaknessExposed()
    {
        if (!_weaknessExposed) return false;
        _weaknessExposed = false;
        return true;
    }

    // ═══════════════════════════════════════
    // 영구 스탯 변경 (1계층)
    // ═══════════════════════════════════════

    /// <summary>스테이지 클리어 보상으로 영구 성장</summary>
    public void GrowPermanent(int hp = 0, int atk = 0, int def = 0)
    {
        _baseHP  += hp;
        _baseATK += atk;
        _baseDEF += def;
        _currentHP = Mathf.Min(_currentHP + hp, MaxHP);
        UpdateUI();
    }

    /// <summary>피의 계약 — ATK 영구 증가</summary>
    public void ApplyPermanentATKBuff(int amount) => _baseATK += amount;

    /// <summary>피의 계약 — 최대HP 영구 감소</summary>
    public void ApplyPermanentHPPenalty(int amount)
    {
        _baseHP    = Mathf.Max(1, _baseHP - amount);
        _currentHP = Mathf.Min(_currentHP, MaxHP);
        UpdateUI();
    }

    /// <summary>도주 패널티 — HP 직접 감소</summary>
    public void ApplyHPPenalty(int amount)
    {
        _currentHP = Mathf.Max(1, _currentHP - amount);
        UpdateUI();
    }

    public void ClearCombatBuffs()
    {
        _atkBuff          = 0;
        _defBuff          = 0;
        _maxHPBuff        = 0;
        _blockNextHit     = false;
        _weaknessExposed  = false;
        _atkSpeedMult     = 1f;
        _atkSpeedTimer    = 0f;
        _defBuffTimer     = 0f;
        _thornsActive     = false;
        _thornsTimer      = 0f;
        _invincibleActive = false;
        _invincibleTimer  = 0f;
    }

    // ═══════════════════════════════════════
    // UI
    // ═══════════════════════════════════════

    private void UpdateUI()
    {
        if (hpSlider != null)
            hpSlider.value = MaxHP > 0 ? (float)_currentHP / MaxHP : 0f;
        if (hpText != null)
            hpText.text = $"{_currentHP} / {MaxHP}";
        if (heroImage != null && Data?.heroSprite != null)
            heroImage.sprite = Data.heroSprite;
    }
}
