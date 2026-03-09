#if false // DEPRECATED — 삭제 예정
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 테스트용 간단한 전투 시작 스크립트.
/// Hero가 Dummy를 자동으로 공격하는 것을 시뮬레이션.
/// </summary>
public class TestBattleStarter : MonoBehaviour
{
    [Header("유닛 참조")]
    public HeroUnit hero;
    public EnemyUnit dummy;

    [Header("데이터 참조")]
    public EnemyData dummyData;  // Inspector에서 직접 연결

    [Header("UI 참조")]
    public Text damageText;

    [Header("테스트 설정")]
    public float attackInterval = 2f;
    public int dummyMaxHP = 1000;

    private float _timer = 0f;
    private int _totalDamage = 0;
    private int _dummyCurrentHP;

    void Start()
    {
        // SimpleWorldManager가 있으면 전투 초기화는 위임 — BattleManager 등록 생략
        if (SimpleWorldManager.Instance != null)
        {
            if (hero != null) SetupTestHero();
            return;
        }

        // ── SimpleWorldManager 없는 단독 테스트용 ──
        if (BattleManager.Instance != null)
        {
            var prop = typeof(BattleManager).GetProperty("IsBattleActive");
            if (prop != null && prop.CanWrite)
                prop.SetValue(BattleManager.Instance, true);
            else
                SetPrivateField(BattleManager.Instance, "<IsBattleActive>k__BackingField", true);

            if (hero != null)
                BattleManager.Instance.HeroUnit = hero;
        }

        if (hero != null && dummy != null)
        {
            SetupTestHero();
            SetupTestDummy();

            if (BattleManager.Instance != null &&
                !BattleManager.Instance.enemyUnits.Contains(dummy))
            {
                BattleManager.Instance.enemyUnits.Add(dummy);
                Debug.Log($"BattleManager에 Dummy 등록 완료! CurrentEnemy: {BattleManager.Instance.CurrentEnemy?.name}");
            }
        }
        else
        {
            Debug.LogError("Hero 또는 Dummy가 연결되지 않았습니다!");
        }
    }

    void Update()
    {
        if (hero == null || dummy == null) return;

        // UI 실시간 업데이트 (매 프레임)
        UpdateDamageUI();

        // 자동 공격 타이머
        _timer += Time.deltaTime;
        if (_timer >= attackInterval)
        {
            _timer = 0f;
            AutoAttack();
        }
    }

    private void UpdateDamageUI()
    {
        // 현재 HP 확인
        int currentHP = dummy.IsAlive ? dummy.CurrentHP : _dummyCurrentHP;
        int maxHP = dummy.Data != null ? dummy.Data.maxHP : dummyMaxHP;
        float heroATK = hero.EffectiveATK;

        // UI 업데이트
        if (damageText != null)
        {
            damageText.text = $"주인공 공격력: {heroATK:F1}\n총 데미지: {_totalDamage}\n허수아비 HP: {currentHP}/{maxHP}\n\n패시브 카드가 자동으로 공격력을 올려줍니다!\n카드를 클릭하면 액티브 효과 발동!";
        }
    }

    private void SetupTestHero()
    {
        // HeroUnit은 ScriptableObject 없이 직접 스탯 설정
        // 리플렉션으로 private 필드 설정
        SetPrivateField(hero, "_baseHP", 100);
        SetPrivateField(hero, "_currentHP", 100);
        SetPrivateField(hero, "_baseATK", 20);
        SetPrivateField(hero, "_baseDEF", 5);

        Debug.Log("Hero 설정 완료: HP 100, ATK 20, DEF 5");
    }

    private void SetupTestDummy()
    {
        // Inspector에서 연결된 EnemyData 사용
        if (dummyData != null)
        {
            dummy.Initialize(dummyData);
            dummy.SetTarget(hero);
            
            // 수동으로 HP 설정 (테스트용)
            SetPrivateField(dummy, "_currentHP", dummyMaxHP);
            
            Debug.Log($"Dummy 설정 완료: {dummyData.enemyName}, HP {dummyMaxHP}");
        }
        else
        {
            Debug.LogError("dummyData가 null입니다! Inspector에서 EnemyData를 연결하세요!");
        }
    }

    private void AutoAttack()
    {
        // Dummy HP 체크
        int currentHP = dummy.IsAlive ? dummy.CurrentHP : _dummyCurrentHP;
        
        if (currentHP <= 0)
        {
            if (damageText != null)
            {
                damageText.text = $"허수아비 처치! 총 데미지: {_totalDamage}\n\n새로고침하여 다시 시작하세요.";
            }
            return;
        }

        // 주인공이 허수아비를 공격
        float heroATK = hero.EffectiveATK;
        int damage = Mathf.Max(1, Mathf.FloorToInt(heroATK));

        // Dummy에게 데미지
        if (dummy.IsAlive)
        {
            dummy.TakeDamage(damage);
        }
        else
        {
            _dummyCurrentHP -= damage;
            _dummyCurrentHP = Mathf.Max(0, _dummyCurrentHP);
        }

        _totalDamage += damage;

        Debug.Log($"자동 공격! Hero ATK: {heroATK:F1}, 데미지: {damage}, 총 데미지: {_totalDamage}");
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"필드 '{fieldName}'를 찾을 수 없습니다.");
        }
    }
}
#endif

