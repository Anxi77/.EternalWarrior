using System;
using UnityEngine;

public class Portal : MonoBehaviour
{
    private SceneType destinationType;
    private Action<SceneType> onEnter;

    public void Initialize(SceneType destType, Action<SceneType> onEnter)
    {
        destinationType = destType;
        this.onEnter = onEnter;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            onEnter?.Invoke(destinationType);
        }
    }
}
