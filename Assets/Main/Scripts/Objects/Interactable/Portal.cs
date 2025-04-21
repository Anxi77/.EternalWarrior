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
        if (other.CompareTag("Player"))
        {
            LoadingManager.Instance.OnPortalEnter(destinationType);
        }
    }
}
