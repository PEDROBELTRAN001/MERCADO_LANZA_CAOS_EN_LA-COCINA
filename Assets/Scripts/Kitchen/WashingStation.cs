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
        if (hand == null) return;
        if (hand.childCount == 0)
        {
            // Con la mano vacia el lavaplatos entrega un plato limpio para armar el pedido
            Plate clean = Plate.Create();
            clean.transform.SetParent(hand, false);
            clean.transform.localPosition = Vector3.zero;
            GameHUD.Notify("Plato limpio: llévalo a la mesa de armado");
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
            GameHUD.Notify("Para tomar un plato limpio, ven con la mano vacía");
        }
    }
}
