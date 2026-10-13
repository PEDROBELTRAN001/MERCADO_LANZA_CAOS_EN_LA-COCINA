using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    public string interactionPrompt = "Presiona E";
    [Tooltip("Nombre que ve el jugador en el HUD (si esta vacio se usa el nombre del objeto)")]
    public string displayName;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name.Replace("_", " ") : displayName;
    public bool requiresItem = false;
    public string requiredItemTag = "";

    protected bool isInteracting = false;
    protected GameObject promptUI;

    public virtual void Interact(PlayerController player)
    {
        Debug.Log("Interactuando con: " + gameObject.name);
    }

    public virtual void ShowPrompt()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(true);
        }
    }

    public virtual void HidePrompt()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }

    public bool CanInteract(PlayerController player)
    {
        if (!requiresItem) return true;

        return player.CompareTag(requiredItemTag);
    }
}
