using System.Collections;
using UnityEngine;

public abstract class BaseStateHandler : IGameState
{
    protected readonly GameManager Game;
    protected readonly PlayerSystem PlayerSystem;
    protected readonly UIManager UI;

    protected BaseStateHandler()
    {
        UI = UIManager.Instance;
        Game = GameManager.Instance;
        PlayerSystem = GameManager.Instance.PlayerSystem;
    }

    public virtual void OnEnter()
    {
        if (UI != null)
        {
            //UI.CleanupUI();
        }
    }

    public virtual void OnExit()
    {
        SavePlayerState();
    }

    public virtual void OnFixedUpdate() { }

    public virtual void OnUpdate() { }

    protected virtual void SavePlayerState()
    {
        if (Game != null && Game.Player != null)
        {
            if (Game.Player.TryGetComponent<Inventory>(out var inventory))
            {
                var inventoryData = inventory.GetInventoryData();
                DataSystem.PlayerDataSystem.SaveInventoryData(inventoryData);
            }
            if (PlayerSystem != null)
            {
                PlayerSystem.SaveGameState();
            }
        }
    }

    protected Coroutine StartCoroutine(IEnumerator routine)
    {
        return GameManager.Instance.StartCoroutine(routine);
    }

    protected void StopCoroutine(IEnumerator routine)
    {
        GameManager.Instance.StopCoroutine(routine);
    }
}
