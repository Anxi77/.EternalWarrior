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

    private InventoryPanel inventoryPanel;

    public void Initialize(Player player)
    {
        this.player = player;

        inventoryAction = playerInput.actionMaps[0].FindAction("Inventory");
        moveAction = playerInput.actionMaps[0].FindAction("Move");

        if (inventoryAction != null)
        {
            inventoryAction.performed += ToggleInventory;
        }

        if (moveAction != null)
        {
            moveAction.performed += GetMoveInput;
            moveAction.canceled += StopMovement;
        }
        playerInput.Enable();

        inventoryPanel = UIManager.Instance.GetPanel(PanelType.Inventory) as InventoryPanel;
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

    private void ToggleInventory(InputAction.CallbackContext context)
    {
        if (inventoryPanel.IsOpen)
        {
            inventoryPanel.Close();
        }
        else
        {
            inventoryPanel.Open();
        }
    }

    public void Cleanup()
    {
        inventoryAction.performed -= ToggleInventory;
        moveAction.performed -= GetMoveInput;
        moveAction.canceled -= StopMovement;
        playerInput.Disable();
    }
}
