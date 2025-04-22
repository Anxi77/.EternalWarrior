using UnityEngine;

public class Portal : MonoBehaviour
{
    private SceneType destinationType;

    public void Initialize(SceneType destType)
    {
        destinationType = destType;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        switch (destinationType)
        {
            case SceneType.Main_Town:
                GameManager.Instance.PlayerSystem.SaveGameState();
                LoadingManager.Instance.LoadScene(SceneType.Main_Town.ToString());
                break;
            case SceneType.Main_Stage:
                GameManager.Instance.PlayerSystem.SaveGameState();
                LoadingManager.Instance.LoadScene(SceneType.Main_Stage.ToString());
                break;
        }
    }
}
