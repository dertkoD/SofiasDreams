using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [SerializeField] private GameObject spawnPoint;

    void OnTriggerEnter2D(Collider2D other)
    {
        // ���������, ��� ������ �����
        if (other.CompareTag("Player"))
        {
            PlayerHealth.Instance.TakeDamage(1);
            other.transform.position = spawnPoint.transform.position;
        }
    }

    // ������������ ����� ������ � ���������
    void OnDrawGizmos()
    {
        if (Camera.current != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPoint.transform.position, 0.5f);
        }
    }
}
