using UnityEngine;
using System.Collections;

public class CookingStation : KitchenCounter
{
    [Header("Cooking Settings")]
    public float cookingTime = 3f;
    public ParticleSystem steamEffect;

    private bool isCooking = false;
    private float cookingProgress = 0f;

    public override void Interact(PlayerController player)
    {
        if (isCooking)
        {
            Debug.Log("Cocinando... " + Mathf.RoundToInt(cookingProgress * 100) + "%");
            return;
        }

        base.Interact(player);

        if (isOccupied && !isCooking)
        {
            StartCoroutine(Cook());
        }
    }

    private IEnumerator Cook()
    {
        isCooking = true;
        cookingProgress = 0f;

        if (steamEffect != null)
        {
            steamEffect.Play();
        }

        while (cookingProgress < 1f)
        {
            cookingProgress += Time.deltaTime / cookingTime;
            yield return null;
        }

        cookingProgress = 1f;
        isCooking = false;

        if (steamEffect != null)
        {
            steamEffect.Stop();
        }

        if (storedItem != null)
        {
            Ingredient ingredient = storedItem.GetComponent<Ingredient>();
            if (ingredient != null)
            {
                ingredient.MarkCooked();
                GameHUD.Notify("Listo: " + ingredient.DisplayName + " (recógelo con E)");
                Debug.Log("Coccion completa: " + storedItem.name);
            }
        }
    }

    public float GetCookingProgress()
    {
        return cookingProgress;
    }

    public bool IsCooking()
    {
        return isCooking;
    }
}
