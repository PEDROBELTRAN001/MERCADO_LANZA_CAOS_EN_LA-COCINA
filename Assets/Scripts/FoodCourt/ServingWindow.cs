using UnityEngine;

public class ServingWindow : Interactable
{
    [Header("Serving Window Settings")]
    public Transform servingSlot;
    public Transform cleanPlateSlot;

    public override void Interact(PlayerController player)
    {
        base.Interact(player);

        Transform hand = player.transform.Find("Hand");
        if (hand == null || hand.childCount == 0)
        {
            GiveCleanPlate(player);
            return;
        }

        GameObject item = hand.GetChild(0).gameObject;
        Plate plate = item.GetComponent<Plate>();

        if (plate != null)
        {
            if (plate.HasCompleteDish())
            {
                DeliverDish(item, player);
            }
            else if (plate.isDirty)
            {
                WashPlate(item, player);
            }
        }
    }

    private void GiveCleanPlate(PlayerController player)
    {
        Transform hand = player.transform.Find("Hand");
        if (hand == null)
        {
            GameObject handObj = new GameObject("Hand");
            handObj.transform.SetParent(player.transform);
            handObj.transform.localPosition = Vector3.up * 1.2f;
            hand = handObj.transform;
        }

        GameObject plateObj = new GameObject("Plate");
        plateObj.transform.SetParent(hand);
        plateObj.transform.localPosition = Vector3.zero;

        Plate plate = plateObj.AddComponent<Plate>();
        plate.isDirty = false;

        Collider col = plateObj.AddComponent<SphereCollider>();
        col.isTrigger = true;

        Renderer rend = plateObj.AddComponent<Renderer>();

        Debug.Log("Plato limpio recibido");
    }

    private void DeliverDish(GameObject dish, PlayerController player)
    {
        Debug.Log("Plato entregado al mostrador");
        GameManager.Instance.AddScore(50);
        Destroy(dish);
    }

    private void WashPlate(GameObject dirtyPlate, PlayerController player)
    {
        dirtyPlate.transform.SetParent(player.transform);
        dirtyPlate.transform.localPosition = Vector3.up * 1.5f;

        Plate plate = dirtyPlate.GetComponent<Plate>();
        if (plate != null)
        {
            plate.isDirty = false;
        }

        Renderer rend = dirtyPlate.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = Color.white;
        }

        Debug.Log("Plato lavado");
    }
}

public class Plate : MonoBehaviour
{
    [Header("Plate State")]
    public bool isDirty = false;

    [Header("Contents")]
    public System.Collections.Generic.List<Ingredient> ingredients = new System.Collections.Generic.List<Ingredient>();

    public bool HasCompleteDish()
    {
        return ingredients.Count > 0;
    }

    public void AddIngredient(Ingredient ingredient)
    {
        ingredients.Add(ingredient);
    }

    public void ClearPlate()
    {
        ingredients.Clear();
        isDirty = true;
    }
}
