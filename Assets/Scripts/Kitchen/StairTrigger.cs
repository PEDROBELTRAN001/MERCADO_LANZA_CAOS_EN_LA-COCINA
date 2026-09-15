using UnityEngine;

public class StairTrigger : Interactable
{
    [Header("Stair Settings")]
    public Vector3 teleportDestination;
    public string stairName = "Escalera";

    private GameObject promptObj;

    private void Start()
    {
        // Crear prompt visual
        promptObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        promptObj.name = "Prompt_" + stairName;
        promptObj.transform.SetParent(transform);
        promptObj.transform.localPosition = new Vector3(0, 2.5f, 0);
        promptObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        Renderer rend = promptObj.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Standard"));
        rend.material.color = Color.yellow;
        rend.material.SetColor("_EmissionColor", Color.yellow * 0.5f);
        rend.material.EnableKeyword("_EMISSION");

        // Quitar collider del prompt
        Collider col = promptObj.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        promptObj.SetActive(false);
    }

    public override void Interact(PlayerController player)
    {
        base.Interact(player);
        player.Teleport(teleportDestination);
        Debug.Log("Teletransportado a: " + teleportDestination);
    }

    public override void ShowPrompt()
    {
        if (promptObj != null) promptObj.SetActive(true);
    }

    public override void HidePrompt()
    {
        if (promptObj != null) promptObj.SetActive(false);
    }
}
