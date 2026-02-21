using UnityEngine;

[CreateAssetMenu(fileName = "NewCompanion", menuName = "Exul Mundi/Companion")]
public class CompanionData : ScriptableObject
{
    public string companionName;
    public int maxHP;
    public Sprite cardImage;

    // 전투 스탯
    public int atk;
    public float atkSpeed = 1f; // 초당 공격 횟수

    // 스킬 정보
    public string skillName;
    [TextArea] public string skillDescription;
    public int skillDamage;
    public float skillCooldown = 5f; // 스킬 쿨다운 (초)
}
