using UnityEngine;

public enum FormationShape { Grid, V, Diamond }

[CreateAssetMenu(menuName = "Chicken Shooter/Wave")]
public class WaveDataSO : ScriptableObject
{
    public Actor enemyPrefab;
    public int count = 8;
    public bool asteroids, boss;
    public FormationShape formation;
    public float health = 2, fireInterval = 3, enemySpeed = 3, formationSpacing = 1.35f, entryCurve = 1.1f;
}
