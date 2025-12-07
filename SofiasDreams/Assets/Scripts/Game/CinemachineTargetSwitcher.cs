using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CinemachineTargetSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class CameraSwitchPoint
    {
        public Transform switchTarget;  // Новый таргет камеры
        public Transform point;         // Центр радиуса
        public float radius = 2f;       // Радиус срабатывания
        [HideInInspector] public bool hasSwitched = false;
    }

    [SerializeField] private CinemachineCamera virtualCam;
    [SerializeField] private Transform player;
    [SerializeField] private List<CameraSwitchPoint> switchPoints;

    void Update()
    {
        foreach (var sp in switchPoints)
        {
            if (sp.hasSwitched)
                continue;

            if (Vector3.Distance(player.position, sp.point.position) <= sp.radius)
            {
                virtualCam.Follow = sp.switchTarget;
                virtualCam.LookAt = sp.switchTarget;
                sp.hasSwitched = true;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (switchPoints == null)
            return;

        foreach (var sp in switchPoints)
        {
            if (sp.point == null)
                continue;

            Gizmos.color = sp.hasSwitched ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(sp.point.position, sp.radius);

            // Нарисовать линию до switchTarget
            if (sp.switchTarget != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(sp.point.position, sp.switchTarget.position);
            }
        }
    }
}
