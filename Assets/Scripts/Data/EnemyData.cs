using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Exul Mundi/Enemy")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public int maxHP;
    public int atk;
    public float atkSpeed = 1f;
    public Sprite sprite;
}
