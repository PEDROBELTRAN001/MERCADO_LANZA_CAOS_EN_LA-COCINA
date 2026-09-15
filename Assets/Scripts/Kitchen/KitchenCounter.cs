using UnityEngine;

public class KitchenCounter : Interactable
{
    [Header("Counter Settings")]
    public Transform itemSlot;
    public bool isOccupied = false;

    protected GameObject storedItem;

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        if (isOccupied)
        {
            PickupItem(player);
        }
        else
        {
            TryPlaceItem(player);
        }
    }

    protected virtual void PickupItem(PlayerController player)
    {
        if (storedItem != null)
        {
            storedItem.transform.SetParent(player.transform);
            storedItem.transform.localPosition = Vector3.up * 1.5f;
            isOccupied = false;
            Debug.Log("Recogido: " + storedItem.name);
        }
    }

    protected virtual void TryPlaceItem(PlayerController player)
    {
        Transform playerHand = player.transform.Find("Hand");
        if (playerHand != null && playerHand.childCount > 0)
        {
            Transform item = playerHand.GetChild(0);
            item.SetParent(itemSlot != null ? itemSlot : transform);
            item.localPosition = Vector3.zero;
            storedItem = item.gameObject;
            isOccupied = true;
            Debug.Log("Colocado: " + item.name);
        }
    }

    public GameObject GetStoredItem()
    {
        return storedItem;
    }

    public void ClearSlot()
    {
        storedItem = null;
        isOccupied = false;
    }
}
