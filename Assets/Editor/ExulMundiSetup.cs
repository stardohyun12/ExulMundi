#if false // DEPRECATED — 삭제 예정
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Exul Mundi 초기 에셋 자동 생성 도구.
/// Unity 메뉴: Exul Mundi > Setup > Create Default Assets
/// </summary>
public static class ExulMundiSetup
{
    private const string SO_PATH = "Assets/ScriptableObjects";

    [MenuItem("Exul Mundi/Setup/Create Default Assets")]
    public static void CreateAllDefaultAssets()
    {
        CreateHeroData();
        CreateSampleCards();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ Exul Mundi 기본 에셋 생성 완료!");
        EditorUtility.DisplayDialog("완료", "HeroData + 샘플 카드 6장 생성 완료!\n" +
            "Assets/ScriptableObjects 폴더를 확인하세요.", "OK");
    }

    // ─────────────────────────────────────
    // HeroData
    // ─────────────────────────────────────

    static void CreateHeroData()
    {
        string path = $"{SO_PATH}/Hero_Default.asset";
        if (File.Exists(Application.dataPath + path.Replace("Assets", "")))
        {
            Debug.Log("Hero_Default.asset 이미 존재 — 스킵");
            return;
        }

        var hero = ScriptableObject.CreateInstance<HeroData>();
        hero.heroName       = "주인공";
        hero.baseHP         = 100;
        hero.baseATK        = 10;
        hero.baseDEF        = 2;
        hero.attackInterval = 1.5f;

        AssetDatabase.CreateAsset(hero, path);
        Debug.Log($"✅ Hero_Default.asset 생성: {path}");
    }

    // ─────────────────────────────────────
    // 샘플 CardData 6장
    // ─────────────────────────────────────

    static void CreateSampleCards()
    {
        CreateCard("Card_QuickStrike",
            name:        "빠른 일격",
            desc:        "적에게 ATK의 150% 즉시 데미지.",
            category:    CardCategory.Offense,
            rarity:      CardRarity.Common,
            cost:        1,
            effect:      CardEffectType.InstantDamage,
            val:         150f);

        CreateCard("Card_PowerBlow",
            name:        "폭발 강타",
            desc:        "적에게 ATK의 250% 데미지.",
            category:    CardCategory.Offense,
            rarity:      CardRarity.Common,
            cost:        2,
            effect:      CardEffectType.InstantDamage,
            val:         250f);

        CreateCard("Card_EmergencyHeal",
            name:        "응급 치료",
            desc:        "즉시 HP 20 회복.",
            category:    CardCategory.Defense,
            rarity:      CardRarity.Common,
            cost:        1,
            effect:      CardEffectType.InstantHeal,
            val:         20f);

        CreateCard("Card_IronWall",
            name:        "철벽 방어",
            desc:        "다음 공격 1회를 완전히 무효화한다.",
            category:    CardCategory.Defense,
            rarity:      CardRarity.Rare,
            cost:        2,
            effect:      CardEffectType.BlockNextHit,
            val:         0f);

        CreateCard("Card_DrawCard",
            name:        "집중",
            desc:        "카드 1장을 추가로 드로우한다.",
            category:    CardCategory.Utility,
            rarity:      CardRarity.Common,
            cost:        1,
            effect:      CardEffectType.DrawCard,
            val:         1f);

        CreateCard("Card_BloodPact",
            name:        "피의 계약",
            desc:        "ATK 영구 +10. 대신 최대 HP 영구 -15.",
            category:    CardCategory.Special,
            rarity:      CardRarity.Rare,
            cost:        0,
            effect:      CardEffectType.BloodPact,
            val:         10f,
            val2:        15f);
    }

    static void CreateCard(string fileName, string name, string desc,
        CardCategory category, CardRarity rarity, int cost,
        CardEffectType effect, float val, float val2 = 0f, int hitCount = 1)
    {
        string path = $"{SO_PATH}/{fileName}.asset";
        if (File.Exists(Application.dataPath + path.Replace("Assets", "")))
        {
            Debug.Log($"{fileName}.asset 이미 존재 — 스킵");
            return;
        }

        var card          = ScriptableObject.CreateInstance<CardData>();
        card.cardName     = name;
        card.description  = desc;
        card.category     = category;
        card.rarity       = rarity;
        // energyCost 제거됨
        card.effectType   = effect;
        card.effectValue  = val;
        card.effectValue2 = val2;
        card.hitCount     = hitCount;

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"✅ {fileName}.asset 생성");
    }
}
#endif

