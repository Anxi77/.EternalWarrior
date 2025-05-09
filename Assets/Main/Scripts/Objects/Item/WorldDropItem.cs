using System.Collections;
using UnityEngine;

public class WorldDropItem : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private CircleCollider2D pickupCollider;

    [SerializeField]
    private float pickupDelay = 0.5f;

    [SerializeField]
    private float magnetSpeed = 10f;

    private Item item;
    private bool canPickup = false;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = spriteRenderer ?? GetComponent<SpriteRenderer>();
        pickupCollider = pickupCollider ?? GetComponent<CircleCollider2D>();
    }

    private void Start()
    {
        StartCoroutine(EnablePickup());
    }

    private IEnumerator EnablePickup()
    {
        yield return new WaitForSeconds(pickupDelay);
        canPickup = true;
    }

    private void Update()
    {
        if (!canPickup)
            return;

        var player = GameManager.Instance.PlayerSystem.Player;
        if (player != null)
        {
            float pickupRange = player
                .GetComponent<StatSystem>()
                .GetStat(StatType.ExpCollectionRadius);
            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (distance <= pickupRange)
            {
                Vector2 direction = (player.transform.position - transform.position).normalized;
                rb.velocity = direction * magnetSpeed;
            }
        }
    }

    public void Initialize(Item item)
    {
        this.item = item;
        if (spriteRenderer != null)
        {
            if (item.GetItemData().Icon != null)
            {
                spriteRenderer.sprite = item.GetItemData().Icon;
            }
            else
            {
                Logger.LogWarning(
                    typeof(WorldDropItem),
                    $"No icon found for item: {item.GetItemData().ID}"
                );
            }
        }
        else
        {
            Logger.LogError(typeof(WorldDropItem), "SpriteRenderer is missing on WorldDropItem!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canPickup)
            return;
        if (!other.CompareTag("Player"))
            return;

        var inventory = other.GetComponent<Inventory>();
        if (inventory != null)
        {
            inventory.AddItem(item);
            PoolManager.Instance.Despawn<WorldDropItem>(this);
        }
    }
}
