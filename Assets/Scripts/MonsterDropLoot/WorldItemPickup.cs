using UnityEngine;

public class WorldItemPickup : MonoBehaviour
{
    private int itemId;
    private int amount;

    private bool collected;

    public void Initialize(int id, int quantity)
    {
        itemId = id;
        amount = quantity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        // Get inventory
        
        //InventoryManager inventory =
        //    other.GetComponentInParent<InventoryManager>();

        //if (inventory == null)
        //    return;

        //inventory.AddItem(itemId, amount);

        collected = true;
        Destroy(gameObject);
    }
}