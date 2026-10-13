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
            Transform hand = player.transform.Find("Hand");
            if (hand == null) return;
            if (hand.childCount > 0)
            {
                // Cada estacion sostiene una sola cosa: avisar en vez de no hacer nada
                GameHUD.Notify(DisplayName + " ya tiene: " + storedItem.name + ". Recógelo primero con la mano vacía", 3.5f);
                return;
            }
            storedItem.transform.SetParent(hand, false);
            storedItem.transform.localPosition = Vector3.zero;
            isOccupied = false;
            Debug.Log("Recogido: " + storedItem.name);
            storedItem = null;
        }
    }

    protected virtual void TryPlaceItem(PlayerController player)
    {
        Transform playerHand = player.transform.Find("Hand");
        if (playerHand != null && playerHand.childCount > 0)
        {
            Transform item = playerHand.GetChild(0);
            if (itemSlot != null)
            {
                item.SetParent(itemSlot);
                item.localPosition = Vector3.zero;
            }
            else
            {
                // Sin punto de colocacion: se apoya encima del mueble (antes quedaba escondido en su centro
                // y deformado por la escala del cubo)
                item.SetParent(null);
                Collider body = GetComponent<Collider>();
                float top = body != null ? body.bounds.max.y : transform.position.y;
                item.position = new Vector3(transform.position.x, top + 0.15f, transform.position.z);
            }
            storedItem = item.gameObject;
            isOccupied = true;
            Debug.Log("Colocado: " + item.name);
        }
        else
        {
            GameHUD.Notify(DisplayName + " está vacía: trae un ingrediente para dejarlo aquí");
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
