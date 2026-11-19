using UnityEngine;

public enum WeaponPattern { Single, Double, Spread }

[CreateAssetMenu(menuName = "Chicken Shooter/Weapon")]
public class WeaponDataSO : ScriptableObject
{
    public string weaponName = "Pulse Cannon";
    public Sprite icon;
    public Actor projectilePrefab;
    public WeaponPattern[] patterns = { WeaponPattern.Single, WeaponPattern.Double, WeaponPattern.Spread };
    public float interval = .18f, speed = 11, damage = 1, spreadAngle = .18f;
    public string projectileSprite = "laser";
}
