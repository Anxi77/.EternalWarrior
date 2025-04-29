using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public InputActionAsset playerInput;

    private InputAction inventoryAction;
    private InputAction moveAction;

    private Player player;

    private Queue<Vector2> moveQueue = new Queue<Vector2>();

    public void Initialize(Player player)
    {
        this.player = player;

        inventoryAction = playerInput.actionMaps[0].FindAction("Inventory");
        moveAction = playerInput.actionMaps[0].FindAction("Move");

        if (inventoryAction != null)
        {
            inventoryAction.performed += OpenInventory;
        }

        if (moveAction != null)
        {
            moveAction.performed += Move;
        }
    }

    private void Update()
    {
        if (moveQueue.Count > 0)
        {
            player.SetMoveInput(moveQueue.Dequeue());
        }
    }

    private void Move(InputAction.CallbackContext context)
    {
        if (context.ReadValue<Vector2>() != Vector2.zero)
        {
            var moveDirection = context.ReadValue<Vector2>();
            Logger.Log(typeof(PlayerInput), $"Move: {moveDirection}");
            moveQueue.Enqueue(moveDirection);
        }
    }

    private void OpenInventory(InputAction.CallbackContext context)
    {
        UIManager.Instance.OpenPanel(PanelType.Inventory);
    }

    private void OnDisable()
    {
        inventoryAction.performed -= OpenInventory;
    }
}
