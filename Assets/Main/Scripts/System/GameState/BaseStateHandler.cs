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
        UI.CloseAllPanels();
    }

    public virtual void OnExit()
    {
        SavePlayerState();
    }

    public virtual void OnFixedUpdate() { }

    public virtual void OnUpdate() { }

    protected virtual void SavePlayerState()
    {
        if (Game.PlayerSystem.Player != null)
        {
            if (Game.PlayerSystem.Player.TryGetComponent<Inventory>(out var inventory))
            {
                var inventoryData = inventory.GetInventoryData();
                PlayerDataManager.Instance.SaveInventoryData(inventoryData);
            }
            if (PlayerSystem != null)
            {
                PlayerSystem.SaveGameState();
            }
        }
    }
}
