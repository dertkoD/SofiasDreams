using UnityEngine;

public interface IEnemyDamageFeedback : IHitStunState
{
    void OnDamage(Vector2 sourcePos);
}
