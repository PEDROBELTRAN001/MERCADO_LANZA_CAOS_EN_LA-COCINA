using UnityEngine;
using System.Collections;

public class ChoppingStation : KitchenCounter
{
    [Header("Chopping Settings")]
    public float choppingTime = 1.5f;

    private bool isChopping = false;
    private float choppingProgress = 0f;

    public override void Interact(PlayerController player)
    {
        if (isChopping)
        {
            Debug.Log("Picando... " + Mathf.RoundToInt(choppingProgress * 100) + "%");
            return;
        }

        base.Interact(player);

        if (isOccupied && !isChopping)
        {
            StartCoroutine(Chop());
        }
    }

    private IEnumerator Chop()
    {
        isChopping = true;
        choppingProgress = 0f;

        while (choppingProgress < 1f)
        {
            choppingProgress += Time.deltaTime / choppingTime;
            yield return null;
        }

        choppingProgress = 1f;
        isChopping = false;

        if (storedItem != null)
        {
            Ingredient ingredient = storedItem.GetComponent<Ingredient>();
            if (ingredient != null)
            {
                ingredient.MarkChopped();
                GameHUD.Notify("Listo: " + ingredient.DisplayName + " (recógelo con E)");
                Debug.Log("Picado completo: " + storedItem.name);
            }
        }
    }

    public float GetChoppingProgress()
    {
        return choppingProgress;
    }

    public bool IsChopping()
    {
        return isChopping;
    }
}
