using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Data/Cards.csv 파일이 변경될 때마다 자동으로 CardData ScriptableObject를 생성/갱신합니다.
/// CSV 헤더의 열 이름이 CardData 필드와 대응됩니다.
/// 배열 필드(synergyTags, compatibleCategories)는 파이프(|)로 구분합니다.
/// </summary>
public class CardDataImporter : AssetPostprocessor
{
    private const string CsvPath    = "Assets/Data/Cards.csv";
    private const string OutputPath = "Assets/ScriptableObjects/Card";

    // ── 자동 임포트 ────────────────────────────────────────────────────────

    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets,    string[] movedFromPaths)
    {
        foreach (var path in importedAssets)
        {
            if (path == CsvPath)
            {
                RunImport();
                return;
            }
        }
    }

    // ── 메뉴 수동 실행 ─────────────────────────────────────────────────────

    [MenuItem("Exul Mundi/Import Cards CSV")]
    public static void RunImport()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogError($"[CardDataImporter] CSV를 찾을 수 없습니다: {CsvPath}");
            return;
        }

        var lines = File.ReadAllLines(CsvPath, Encoding.UTF8);
        if (lines.Length < 2)
        {
            Debug.LogWarning("[CardDataImporter] 데이터 행이 없습니다.");
            return;
        }

        var headers = ParseLine(lines[0]);
        int created = 0, updated = 0, skipped = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var values = ParseLine(lines[i]);
            var row    = BuildDict(headers, values);

            if (!row.TryGetValue("cardName", out var name) || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            string safeName = name.Trim().Replace(" ", "_");
            string assetPath = $"{OutputPath}/{safeName}.asset";

            var card  = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            bool isNew = card == null;
            if (isNew) card = ScriptableObject.CreateInstance<CardData>();

            ApplyRow(card, row);

            if (isNew)
            {
                AssetDatabase.CreateAsset(card, assetPath);
                created++;
            }
            else
            {
                EditorUtility.SetDirty(card);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CardDataImporter] 완료 — 생성: {created}, 갱신: {updated}, 건너뜀: {skipped}");
    }

    // ── CardData 필드 적용 ─────────────────────────────────────────────────

    private static void ApplyRow(CardData card, Dictionary<string, string> row)
    {
        card.cardName    = Get(row, "cardName");
        card.description = Get(row, "description");
        card.rarity      = Enum<CardRarity>(row,  "rarity",   CardRarity.Common);
        card.cardType    = Enum<CardType>(row,     "cardType", CardType.Accessory);
        card.weaponType  = Enum<WeaponType>(row,   "weaponType", WeaponType.Sword);

        card.baseDamage          = Int(row,   "baseDamage");
        card.baseAttackInterval  = Float(row, "baseAttackInterval");
        card.baseRange           = Float(row, "baseRange");
        card.baseProjectileSpeed = Float(row, "baseProjectileSpeed");
        card.baseAttackSize      = Float(row, "baseAttackSize");
        card.baseAttackCount     = Int(row,   "baseAttackCount");

        card.effectComponentType = Get(row,   "effectComponentType");
        card.effectValue         = Float(row, "effectValue");
        card.effectValue2        = Float(row, "effectValue2");

        card.synergyTags          = StringArray(Get(row, "synergyTags"));
        card.compatibleCategories = EnumArray<WeaponCategory>(Get(row, "compatibleCategories"));
    }

    // ── CSV 파싱 유틸리티 ──────────────────────────────────────────────────

    /// <summary>RFC 4180 준수 — 큰따옴표로 감싼 필드 내 쉼표와 줄바꿈 허용.</summary>
    private static string[] ParseLine(string line)
    {
        var result  = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                { current.Append('"'); i++; }
                else
                { inQuotes = !inQuotes; }
            }
            else if (c == ',' && !inQuotes)
            { result.Add(current.ToString()); current.Clear(); }
            else
            { current.Append(c); }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }

    private static Dictionary<string, string> BuildDict(string[] headers, string[] values)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
            dict[headers[i].Trim()] = i < values.Length ? values[i].Trim() : string.Empty;
        return dict;
    }

    // ── 타입 변환 헬퍼 ────────────────────────────────────────────────────

    private static string Get(Dictionary<string, string> d, string key)
        => d.TryGetValue(key, out var v) ? v : string.Empty;

    private static int Int(Dictionary<string, string> d, string key)
        => int.TryParse(Get(d, key), out var r) ? r : 0;

    private static float Float(Dictionary<string, string> d, string key)
        => float.TryParse(Get(d, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var r) ? r : 0f;

    private static T Enum<T>(Dictionary<string, string> d, string key, T defaultVal) where T : struct, System.Enum
        => System.Enum.TryParse<T>(Get(d, key), true, out var r) ? r : defaultVal;

    private static string[] StringArray(string value)
        => string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split('|', StringSplitOptions.RemoveEmptyEntries);

    private static T[] EnumArray<T>(string value) where T : struct, System.Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return Array.Empty<T>();
        var list = new List<T>();
        foreach (var part in value.Split('|', StringSplitOptions.RemoveEmptyEntries))
            if (System.Enum.TryParse<T>(part.Trim(), true, out var e)) list.Add(e);
        return list.ToArray();
    }
}
