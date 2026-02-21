using UnityEngine;

[CreateAssetMenu(fileName = "NewWorld", menuName = "Exul Mundi/World")]
public class WorldData : ScriptableObject
{
    public string worldName;
    public Sprite backgroundSprite;
    public EnemyData[] enemies;
    [Range(1, 10)] public int difficulty = 1;
}
