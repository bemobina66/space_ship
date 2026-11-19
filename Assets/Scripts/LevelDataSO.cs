using UnityEngine;

[CreateAssetMenu(menuName = "Chicken Shooter/Level")]
public class LevelDataSO : ScriptableObject
{
    public int levelIndex = 1;
    public string sceneName, title;
    public WaveDataSO[] waves;
    public LevelDataSO nextLevel;
    public string backgroundSprite, enemySprite, bossSprite, musicClip;
}
