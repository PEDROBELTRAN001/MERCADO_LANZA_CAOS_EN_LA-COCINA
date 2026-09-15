using UnityEngine;

public class TrashStation : Interactable
{
    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        DiscardItem(player);
    }

    private void DiscardItem(PlayerController player)
    {
        Transform hand = player.transform.Find("Hand");
        if (hand == null || hand.childCount == 0)
        {
            Debug.Log("No tienes nada para tirar");
            return;
        }

        GameObject item = hand.GetChild(0).gameObject;
        Debug.Log("Tirando: " + item.name);
        Destroy(item);
    }
}
