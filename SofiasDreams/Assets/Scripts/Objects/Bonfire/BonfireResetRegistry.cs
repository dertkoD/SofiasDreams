using System.Collections.Generic;
using Zenject;

public class BonfireResetRegistry : IInitializable
{
    readonly List<IBonfireResettable> _items = new(128);

    public void Initialize() { }

    public void Register(IBonfireResettable item)
    {
        if (item != null && !_items.Contains(item))
            _items.Add(item);
    }

    public void Unregister(IBonfireResettable item)
    {
        if (item != null)
            _items.Remove(item);
    }

    public void ResetAll()
    {
        var snapshot = _items.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            snapshot[i]?.OnBonfireReset();
    }
}