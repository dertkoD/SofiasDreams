using UnityEngine;

public class Knockback2D : MonoBehaviour, IKnockback
{
    [SerializeField] Rigidbody2D _rb;
    KnockbackSettings _s; bool _inStun; float _stun;

    public void Configure(KnockbackSettings s) => _s = s;
    public bool IsInHitStun => _inStun;

    public void Apply(DamageInfo info)
    {
        if (_rb == null)
        {
            Debug.LogWarning("[Knockback2D] No Rigidbody2D");
            return;
        }

        Debug.Log($"[Knockback2D] AddForce {info.impulse}, stun={info.stunSeconds}");
        _rb.AddForce(info.impulse, ForceMode2D.Impulse);        
        _stun = Mathf.Max(_stun, info.stunSeconds>0?info.stunSeconds:_s.defaultHitStop);
        _inStun = _stun > 0;
    }
    public void ApplyImpulse(Vector2 impulse, Vector2 _) { if (_rb) _rb.AddForce(impulse, ForceMode2D.Impulse); }

    void Update()
    {
        if(!_inStun) return;
        _stun -= Time.deltaTime;
        if (_stun <= 0)
        {
            _stun=0; 
            _inStun=false;
        }
    }
}
