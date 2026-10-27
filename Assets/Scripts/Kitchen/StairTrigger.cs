using System.Collections.Generic;
using UnityEngine;

public class StairTrigger : MonoBehaviour
{
    public Vector3 teleportDestination;
    public string stairName = "Escalera";
    private GameObject promptObj;
    private void Start()
    {
        // Prompt flotante
        promptObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        promptObj.name = "Prompt_" + stairName;
        promptObj.transform.SetParent(transform);
        promptObj.transform.localPosition = new Vector3(0, 1.5f, 0);
        promptObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        Renderer rend = promptObj.GetComponent<Renderer>();
        // El proyecto usa URP: "Standard" se veria rosado
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = Color.yellow;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.yellow * 0.4f);
        rend.sharedMaterial = mat;

        Collider c = promptObj.GetComponent<Collider>();
        if (c != null) DestroyImmediate(c);

        promptObj.SetActive(false);
    }

    private readonly HashSet<PlayerController> nearby = new HashSet<PlayerController>();
    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player != null) { nearby.Add(player); if (promptObj != null) promptObj.SetActive(true); }
    }
    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player != null) { nearby.Remove(player); if (promptObj != null) promptObj.SetActive(nearby.Count > 0); }
    }
    public bool TryUseForAI(PlayerController player)
    {
        if (player == null || !player.usesAI || !PlayerController.GameplayActive || player.IsDetained || player.IsStunned) return false;
        Collider entrance = GetComponent<Collider>();
        if (entrance == null || Vector3.Distance(entrance.ClosestPoint(player.transform.position), player.transform.position) > 0.6f) return false;
        player.Teleport(teleportDestination);
        return true;
    }
    private void Update()
    {
        if (!PlayerController.GameplayActive) return;
        foreach (var player in nearby)
            if (player != null && !player.IsStunned && !player.IsDetained && player.InteractionPressed)
                player.Teleport(teleportDestination);
    }
}
