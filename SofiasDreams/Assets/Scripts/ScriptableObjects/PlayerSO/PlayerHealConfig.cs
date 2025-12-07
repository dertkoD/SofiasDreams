using UnityEngine;

[CreateAssetMenu(menuName = "Configs/PlayerHealConfig")]
public class PlayerHealConfig : ScriptableObject
{
    public int healAmount = 1;
    public int maxCharges     = 3;
    public int killsPerCharge = 3;
}
