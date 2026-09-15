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
        base.Interact(player);

        if (!isOccupied && availablePlates > 0)
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
        plate.transform.SetParent(player.transform);
        plate.transform.localPosition = Vector3.up * 1.5f;

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
