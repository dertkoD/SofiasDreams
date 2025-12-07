using UnityEngine;

public class EnemyPatrolController : MonoBehaviour
{
    [SerializeField] EnemyPatrolPath _path;
    [SerializeField] EnemyMovement _movement;
    [SerializeField] bool _loop = true;

    EnemyConfigSO _config;

    int _currentIndex;
    int _direction = 1;
    float _pauseTimer;
    bool _movingToPoint;

    public void Configure(EnemyConfigSO config)
    {
        _config = config;
    }

    public void BeginPatrol()
    {
        _pauseTimer = 0f;
        _movingToPoint = false;
        _currentIndex = 0;
        _direction = 1;

        if (_path != null && _path.Count > 0)
            MoveToCurrentPoint();
    }

    public void StopPatrol()
    {
        _movement?.Stop();
        _movingToPoint = false;
    }

    public void Tick()
    {
        if (_config == null || _path == null || _movement == null || _path.Count == 0)
            return;

        if (!_movingToPoint)
        {
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                return;
            }

            MoveToCurrentPoint();
            return;
        }

        if (_movement.IsAtDestination(_config.destinationTolerance))
        {
            _movingToPoint = false;
            _pauseTimer = Random.Range(_config.minPauseAtPoint, _config.maxPauseAtPoint);
            AdvanceIndex();
        }
    }

    void MoveToCurrentPoint()
    {
        Vector3 target = _path.GetPoint(_currentIndex);
        _movement.MoveTo(target);
        _movingToPoint = true;
    }

    void AdvanceIndex()
    {
        if (_path.Count <= 1)
            return;

        if (_loop)
        {
            _currentIndex = (_currentIndex + 1) % _path.Count;
        }
        else
        {
            int next = _currentIndex + _direction;

            if (next >= _path.Count || next < 0)
            {
                _direction *= -1;
                next = Mathf.Clamp(_currentIndex + _direction, 0, _path.Count - 1);
            }

            _currentIndex = next;
        }
    }
    
    public void SetPath(EnemyPatrolPath path)
    {
        _path = path;
    }
}
