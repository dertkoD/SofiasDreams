using UnityEngine;

public class DashEventProxy : MonoBehaviour
{
    [SerializeField] Dasher2D dasher;

    public void FinishFromAnimation()
    {
        if (dasher != null)
            dasher.FinishFromAnimationDash();
    }
}
