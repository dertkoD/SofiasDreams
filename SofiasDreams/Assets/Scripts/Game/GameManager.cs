using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private KeyCode keyCode = KeyCode.R;

    private void Update()
    {
        if (Input.GetKeyDown(keyCode))
            ResetScene();
    }
    private void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
