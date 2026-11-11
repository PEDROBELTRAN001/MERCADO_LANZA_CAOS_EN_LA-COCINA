using UnityEngine;

// Plato terminado (lo crea la mesa de armado). La mesa del comedor lo acepta si es el plato que pidio.
public class DishItem : MonoBehaviour
{
    public RecipeData recipe;
    public bool IsContaminated { get; private set; }
    private Transform saltLabel;
    private void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted += ClearForNewRound;
    }
    private void ClearForNewRound() => Destroy(gameObject);
    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= ClearForNewRound;
    }
    private void LateUpdate()
    {
        if (saltLabel != null && Camera.main != null) saltLabel.rotation = Camera.main.transform.rotation;
    }
    public bool TrySalt()
    {
        if (IsContaminated) return false;
        IsContaminated = true;
        var label = new GameObject("Pista de sal");
        saltLabel = label.transform;
        label.transform.SetParent(transform, false);
        label.transform.localPosition = Vector3.up * 0.65f;
        var text = label.AddComponent<TextMesh>();
        text.text = "¡SAL!";
        text.characterSize = 0.12f;
        text.fontSize = 48;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = Color.yellow;
        return true;
    }
}
