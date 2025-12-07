using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GuardTargetBinder2D : MonoBehaviour
{
    public Transform Target { get; private set; }

    [Tooltip("Автоперепривязка при смене родителя / повторной активации.")]
    public bool autoRebind = true;

    void OnEnable()
    {
        if (autoRebind) RebindNow();
    }

    void OnTransformParentChanged()
    {
        if (autoRebind) RebindNow();
    }

    /// Найти ближайший вверх по иерархии SwarmMinionSpawner и принять его Transform как цель.
    public void RebindNow()
    {
        // На префаб-ассете не биндимся
        if (!gameObject.scene.IsValid()) { Target = null; return; }

        var spawner = GetComponentInParent<SwarmMinionSpawner>(true);
        if (spawner && spawner.gameObject.scene.IsValid())
        {
            Target = spawner.transform;
        }
        else
        {
            // Фоллбек: если по какой-то причине спавнера нет, охраняем непосредственного родителя
            Target = transform.parent;
        }
    }
}
