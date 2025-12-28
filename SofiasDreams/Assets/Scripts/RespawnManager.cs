using System.Collections;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform spawnPoint;

    [Header("Damage")]
    [SerializeField] private bool applyDamageOnRespawn = true;

    [Tooltip("If true, deals enough damage to drop HP to 0 (bypasses invuln if configured).")]
    [SerializeField] private bool lethalDamage = false;

    [Tooltip("Used only when Lethal Damage is false.")]
    [SerializeField] private int damageAmount = 1;

    [SerializeField] private bool bypassInvuln = true;

    [SerializeField] private DamageType damageType = DamageType.Melee;

    [Header("Respawn Feel")]
    [Tooltip("Optional tiny delay before teleport (useful for hit FX).")]
    [SerializeField] private float respawnDelay = 0f;

    [Tooltip("Prevents retriggering immediately after teleport.")]
    [SerializeField] private float triggerCooldown = 0.25f;

    bool _cooldown;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_cooldown) return;
        if (!other.CompareTag("Player")) return;

        if (spawnPoint == null)
        {
            Debug.LogWarning("[RespawnManager] No spawnPoint assigned.");
            return;
        }

        // Find player systems on the thing that entered the trigger (or its parents)
        var playerHealth = other.GetComponentInParent<Health>();
        var playerRb     = other.attachedRigidbody
                        ? other.attachedRigidbody
                        : other.GetComponentInParent<Rigidbody2D>();

        // Apply damage using DamageInfo pipeline
        if (applyDamageOnRespawn && playerHealth != null)
        {
            int dmg = lethalDamage ? Mathf.Max(1, playerHealth.CurrentHP) : Mathf.Max(0, damageAmount);

            // Direction/normal aren’t super important here
            var info = DamageInfo.FromHit(
                src: transform,
                dmg: dmg,
                point: other.bounds.ClosestPoint(transform.position),
                normal: Vector2.up,
                impulse: Vector2.zero,
                t: damageType,
                bypass: bypassInvuln,
                crit: false,
                stun: 0f
            );

            playerHealth.ApplyDamage(info);

            // If the player died from this damage, let the Death/Bonfire system handle it.
            // Do not teleport a dying/dissolving player.
            if (!playerHealth.IsAlive) return;
        }

        // Teleport + cleanup (optionally delayed)
        StartCoroutine(RespawnRoutine(other.transform, playerRb));
    }

    IEnumerator RespawnRoutine(Transform playerTransform, Rigidbody2D playerRb)
    {
        _cooldown = true;
        
        // Try to find the dissolve bridge on the player
        var dissolveBridge = playerTransform.GetComponentInChildren<PlayerDissolveVfxBridge>();
        
        if (dissolveBridge != null)
        {
            // Lock physics during dissolve
            if (playerRb != null) playerRb.simulated = false;

            // 1. Play dissolve OUT (death effect)
            bool dissolved = false;
            dissolveBridge.PlayDeathVfx(() => dissolved = true);

            // Wait until dissolve is finished
            while (!dissolved) yield return null;
        }

        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        // Reset physics so no retained horizontal velocity / momentum after teleport
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;

            // Prefer rb.position to avoid weirdness with interpolation
            playerRb.position = spawnPoint.position;
            
            // Re-enable physics simulation if we disabled it
            if (dissolveBridge != null) playerRb.simulated = true;
        }
        else
        {
            playerTransform.position = spawnPoint.position;
        }

        if (triggerCooldown > 0f)
            yield return new WaitForSeconds(triggerCooldown);

        // 2. Play dissolve IN (respawn effect)
        if (dissolveBridge != null)
        {
            dissolveBridge.PlayRespawnVfx();
        }

        _cooldown = false;
    }

    void OnDrawGizmos()
    {
        if (spawnPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
    }
}
