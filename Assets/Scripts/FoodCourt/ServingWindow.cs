using UnityEngine;

public class ServingWindow : Interactable
{
    [Header("Serving Window Settings")]
    public Transform servingSlot;
    public Transform cleanPlateSlot;

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        // El ciclo actual entrega directo en la mesa del pedido (FoodCourtTable).
        // Antes este mostrador creaba un "Plate" con AddComponent<Renderer>(), que Unity no permite (es abstracto).
        GameHUD.Notify("Lleva el plato directamente a la mesa que lo pidió");
    }
}
