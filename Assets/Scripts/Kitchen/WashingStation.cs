using UnityEngine;

public class WashingStation : Interactable
{
    [Header("Washing Settings")]
    public float washTime = 2f;

    public override void Interact(PlayerController player)
    {
        base.Interact(player);
        WashPlate(player);
    }

    private void WashPlate(PlayerController player)
    {
        Transform hand = player.transform.Find("Hand");
        if (hand == null || hand.childCount == 0)
        {
            Debug.Log("No tienes un plato sucio");
            return;
        }

        GameObject item = hand.GetChild(0).gameObject;
        Plate plate = item.GetComponent<Plate>();

        if (plate != null && plate.isDirty)
        {
            plate.isDirty = false;
            Renderer rend = item.GetComponent<Renderer>();
            if (rend != null) rend.material.color = Color.white;
            Debug.Log("Plato lavado!");
        }
        else
        {
            Debug.Log("Este plato no necesita lavarse");
        }
    }
}
