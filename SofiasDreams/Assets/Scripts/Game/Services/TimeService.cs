using UnityEngine;

public class TimeService : ITimeService
{
    public float DeltaTime => Time.deltaTime;
}
