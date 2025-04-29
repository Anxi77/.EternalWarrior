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
            moveAction.performed += GetMoveInput;
            moveAction.canceled += StopMovement;
        }
        playerInput.Enable();
    }

    private void Update()
    {
        if (moveQueue.Count > 0)
        {
            player.SetMoveInput(moveQueue.Dequeue());
        }
    }

    private void GetMoveInput(InputAction.CallbackContext context)
    {
        var moveDirection = context.ReadValue<Vector2>();
        moveQueue.Enqueue(moveDirection);
    }

    private void StopMovement(InputAction.CallbackContext context)
    {
        moveQueue.Enqueue(Vector2.zero);
    }

    private void OpenInventory(InputAction.CallbackContext context)
    {
        UIManager.Instance.OpenPanel(PanelType.Inventory);
    }

    public void Cleanup()
    {
        inventoryAction.performed -= OpenInventory;
        moveAction.performed -= GetMoveInput;
        moveAction.canceled -= StopMovement;
        playerInput.Disable();
    }
}
