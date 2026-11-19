using System;

[Serializable]
public class SaveData
{
    public int level = 1, round = 1, score, lives = 3, missiles = 2, weapon = 1, food;
    public int entryScore, entryMissiles = 2, entryWeapon = 1, entryFood;
    public SaveData Copy() => (SaveData)MemberwiseClone();
}
