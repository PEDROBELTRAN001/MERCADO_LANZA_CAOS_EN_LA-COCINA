using UnityEngine;

public class PlateStation : KitchenCounter
{
    [Header("Plate Settings")]
    public GameObject platePrefab;
    public int maxPlates = 10;

    private int availablePlates;

    private void Start()
    {
        availablePlates = maxPlates;
    }

    public override void Interact(PlayerController player)
    {
        if (player.HeldItem != null) { GameHUD.Notify("Necesitas la mano vacía para tomar un plato."); return; }

        if (availablePlates > 0)
        {
            SpawnPlate(player);
        }
    }

    private void SpawnPlate(PlayerController player)
    {
        if (platePrefab == null)
        {
            Debug.LogWarning("No hay prefab de plato asignado");
            return;
        }

        GameObject plate = Instantiate(platePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        if (!player.TryHoldItem(plate)) { Destroy(plate); return; }

        availablePlates--;
        Debug.Log("Plato dispensado. Platos restantes: " + availablePlates);
    }

    public int GetAvailablePlates()
    {
        return availablePlates;
    }

    public void RestockPlates(int amount)
    {
        availablePlates = Mathf.Min(availablePlates + amount, maxPlates);
    }
}
