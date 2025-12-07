using UnityEngine;

[CreateAssetMenu(fileName = "MinionBulletConfig", menuName = "Configs/MinionBullet")]
public class MinionBulletConfig : ScriptableObject
{
    public int damage = 1;
    public float speed = 10f;
    public float lifetime = 3f;
}
