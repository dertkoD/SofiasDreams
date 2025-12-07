using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class EnemyDeathAnimatorBridge : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] EnemyController controller;
    [SerializeField] Animator animator;

    [Header("Disable on death")]
    [SerializeField] MonoBehaviour[] disableOnDeath;     // AI/движение/стрельба и т.п.
    [SerializeField] bool disableRigidAndColliders = true;

    [Header("Auto watch")]
    [SerializeField] bool autoWatchHealth = true;        // автостарт при HP<=0

    [Header("Animator")]
    [SerializeField] string[] triggersToClear = { "Angry", "Idle" };
    [SerializeField] string defaultDeathTrigger = "Death";

    [Serializable] public enum MatchType { Tag, ClipNameContains, StateNameEquals, Any }
    [Serializable] public class DeathRule { public MatchType match = MatchType.Any; public string tokenOrTag = ""; public string deathTrigger = "Death"; }

    [Header("Ordered rules (first match wins)")]
    [SerializeField] DeathRule[] rules = { new DeathRule{ match=MatchType.Any, deathTrigger="Death" } };

    [Header("Finalize fallback")]
    [SerializeField] bool autoFinalizeIfNoEvent = true;
    [SerializeField, Min(0f)] float deathAutoFinalizeDelay = 2.0f;

    public UnityEngine.Events.UnityEvent onDeathStart;
    public UnityEngine.Events.UnityEvent onDeathEnd;

    readonly List<MonoBehaviour> _disabledBehaviours = new();
    Rigidbody2D _rb;
    Collider2D[] _cols;
    bool deathStarted;
    bool deathEndSignaled;

    void Awake()
    {
        if (!controller) controller = GetComponent<EnemyController>();
        if (!animator)   animator   = GetComponentInChildren<Animator>(true);
        _rb   = GetComponent<Rigidbody2D>();
        _cols = GetComponentsInChildren<Collider2D>(true);

        if (disableOnDeath == null || disableOnDeath.Length == 0)
            disableOnDeath = GetComponents<MonoBehaviour>()
                .Where(m => m != this && m.enabled && !(m is Animator)).ToArray();
    }

    void Update()
    {
        if (!autoWatchHealth || deathStarted || controller == null) return;
        if (controller.GetHealth() > 0) return;
        TriggerDeath();
    }

    public void TriggerDeath()
    {
        if (deathStarted) return;
        deathStarted = true;

        // 0) Сначала сообщаем слушателям (например, спавнеру минионов)
        onDeathStart?.Invoke();

        // 1) Отключаем поведение/физику
        _disabledBehaviours.Clear();
        if (disableOnDeath != null)
            foreach (var b in disableOnDeath)
                if (b && b.enabled) { b.enabled = false; _disabledBehaviours.Add(b); }

        if (disableRigidAndColliders)
        {
            if (_cols != null) foreach (var c in _cols) if (c) c.enabled = false;
            if (_rb)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.simulated = false;
            }
        }

        // 2) Сброс триггеров боя
        if (animator && triggersToClear != null)
            foreach (var t in triggersToClear)
                if (!string.IsNullOrEmpty(t)) animator.ResetTrigger(t);

        // 3) Выбор и запуск триггера смерти
        var trig = ChooseDeathTriggerByRules();
        if (string.IsNullOrEmpty(trig)) trig = defaultDeathTrigger;
        if (animator && !string.IsNullOrEmpty(trig)) animator.SetTrigger(trig);

        // 4) Фоллбек, если забыли Animation Event
        if (autoFinalizeIfNoEvent) StartCoroutine(CoAutoFinalize());
    }

    public void RestoreAfterRespawn(string idleStateName = "Idle", string idleTrigger = null)
    {
        deathStarted = false;
        deathEndSignaled = false;

        foreach (var b in _disabledBehaviours) if (b) b.enabled = true;
        _disabledBehaviours.Clear();

        if (_cols != null) foreach (var c in _cols) if (c) c.enabled = true;
        if (_rb) _rb.simulated = true;

        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
            if (!string.IsNullOrEmpty(idleTrigger)) animator.SetTrigger(idleTrigger);
            else if (!string.IsNullOrEmpty(idleStateName)) animator.Play(idleStateName, 0, 0f);
        }
    }

    string ChooseDeathTriggerByRules()
    {
        if (!animator) return defaultDeathTrigger;

        var state = animator.GetCurrentAnimatorStateInfo(0);
        var clips = animator.GetCurrentAnimatorClipInfo(0);

        foreach (var r in rules)
        {
            switch (r.match)
            {
                case MatchType.Any: return r.deathTrigger;
                case MatchType.Tag:
                    if (!string.IsNullOrEmpty(r.tokenOrTag) && state.IsTag(r.tokenOrTag)) return r.deathTrigger;
                    break;
                case MatchType.StateNameEquals:
                    if (!string.IsNullOrEmpty(r.tokenOrTag) && state.IsName(r.tokenOrTag)) return r.deathTrigger;
                    break;
                case MatchType.ClipNameContains:
                    if (!string.IsNullOrEmpty(r.tokenOrTag) &&
                        clips.Any(ci => ci.clip && ci.clip.name.IndexOf(r.tokenOrTag, StringComparison.OrdinalIgnoreCase) >= 0))
                        return r.deathTrigger;
                    break;
            }
        }
        return defaultDeathTrigger;
    }

    // Вызвать из Animation Event в конце клипа Death
    public void AnimationEvent_DeathEnd()
    {
        if (deathEndSignaled) return;
        deathEndSignaled = true;

        onDeathEnd?.Invoke();                 // опционально
        if (controller) controller.FinalizeDeath();
        else Destroy(gameObject);
    }

    System.Collections.IEnumerator CoAutoFinalize()
    {
        yield return new WaitForSeconds(deathAutoFinalizeDelay);
        if (deathEndSignaled) yield break;

        onDeathEnd?.Invoke();
        if (controller) controller.FinalizeDeath();
        else Destroy(gameObject);
    }
}
