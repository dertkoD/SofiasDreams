using UnityEngine;

[CreateAssetMenu(menuName = "VFX/Player Dissolve Bundle", fileName = "PlayerDissolveBundle")]
public class PlayerDissolveBundleSO : ScriptableObject
{
    public DissolveVfxSettingsSO death;
    public DissolveVfxSettingsSO respawn;
}
