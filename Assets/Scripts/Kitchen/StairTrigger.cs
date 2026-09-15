using UnityEngine;
using UnityEngine.InputSystem;

public class StairTrigger : MonoBehaviour
{
    [Header("Teleport")]
    public Vector3 teleportDestination;
    public string stairName = "Escalera";

    private bool playerInRange = false;
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
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.yellow;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.yellow * 0.4f);
        rend.sharedMaterial = mat;

        Collider c = promptObj.GetComponent<Collider>();
        if (c != null) DestroyImmediate(c);

        promptObj.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (promptObj != null) promptObj.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (promptObj != null) promptObj.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange) return;

        bool pressed = false;
        if (Keyboard.current != null)
            pressed = Keyboard.current.eKey.wasPressedThisFrame;

        if (pressed)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.Teleport(teleportDestination);
                    Debug.Log(stairName + " -> Teletransportado!");
                }
            }
        }
    }
}
