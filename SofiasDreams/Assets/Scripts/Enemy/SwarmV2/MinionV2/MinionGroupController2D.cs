using System.Collections.Generic;
using UnityEngine;

public class MinionGroupController2D : MonoBehaviour
{
     [Header("Refs")]
    public Transform player;

    [Header("Role policy")]
    [Min(0)] public int maxAttackers = 1;      // обычно 1
    public float[] supportYOffsets;            // офсеты по Y для саппортов, заполняются в инспекторе

    readonly List<MinionOrbitBrain2D> _minions = new();
    readonly HashSet<MinionOrbitBrain2D> _dead = new();

    // ===== API для пула =====
    public void Register(MinionOrbitBrain2D m)
    {
        if (!m || _dead.Contains(m) || _minions.Contains(m)) return;
        _minions.Add(m);
        /*m.onDied += OnMinionDied;
        m.onDespawned += OnMinionDespawned;
        m.SetPlayer(player);*/
        Rebalance();
    }

    public void Unregister(MinionOrbitBrain2D m)
    {
        if (!m) return;
        /*m.onDied -= OnMinionDied;
        m.onDespawned -= OnMinionDespawned;*/
        _minions.Remove(m);
        _dead.Remove(m);
        Rebalance();
    }

    void OnMinionDied(MinionOrbitBrain2D m)
    {
        if (!m) return;
        _dead.Add(m);
        // Отложенное снятие чтобы не ломать внешние подписки
        Invoke(nameof(Rebalance), 0f);
    }

    void OnMinionDespawned(MinionOrbitBrain2D m)
    {
        Unregister(m);
    }

    // ===== Распределение ролей =====
    void Rebalance()
    {
        // очистка умерших
        for (int i = _minions.Count - 1; i >= 0; --i)
        {
            var m = _minions[i];
            if (!m || _dead.Contains(m)) _minions.RemoveAt(i);
        }

        // сначала атакующие
        int attackers = 0;
        foreach (var m in _minions)
        {
            if (attackers < maxAttackers)
            {
                /*m.SetRole(MinionOrbitBrain2D.Role.Attacker);*/
                attackers++;
            }
            else
            {
                /*m.SetRole(MinionOrbitBrain2D.Role.Support);*/
            }
        }

        // назначаем офсеты саппортам по порядку
        int si = 0;
        foreach (var m in _minions)
        {
            /*if (m.CurrentRole != MinionOrbitBrain2D.Role.Support) continue;*/
            float off = supportYOffsets != null && supportYOffsets.Length > 0
                ? supportYOffsets[si % supportYOffsets.Length]
                : 0f;
            /*m.SetSupportYOffset(off);*/
            si++;
        }
    }
}
