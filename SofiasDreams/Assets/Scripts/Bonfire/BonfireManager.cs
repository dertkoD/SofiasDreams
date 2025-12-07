using UnityEngine;
using UnityEngine.UI;

public class BonfireManager : MonoBehaviour
{
    public static BonfireManager Instance;
    
    public PlayerController playerController;
    [SerializeField] private Image reminder;

    public bool IsAtBonfire { get; private set; }
    
    private void Awake()
    {
        reminder.enabled = false;
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ActivateBonfire(Vector3 bonfirePosition)
    {
        if (IsAtBonfire) return;

        IsAtBonfire = true;
        playerController.enabled = false;
        reminder.enabled = true;
        Time.timeScale = 0;
        
        PlayerHealth.Instance.RestoreAtBonfire();
        
        Debug.Log("Entered bonfire");
    }

    public void ExitBonfire()
    {
        if (!IsAtBonfire) return;

        playerController.enabled = true;
        reminder.enabled = false;
        Time.timeScale = 1;

        IsAtBonfire = false;
        
        Debug.Log("Exited bonfire");
    }
}
