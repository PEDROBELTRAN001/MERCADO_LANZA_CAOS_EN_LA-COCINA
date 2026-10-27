using System.Collections;
using UnityEngine;

public class InvestigationSystem : MonoBehaviour
{
    public static InvestigationSystem Instance { get; private set; }
    public InvestigationLedger Ledger { get; } = new InvestigationLedger();
    public CapibaraPolice Police { get; private set; }
    public MarketNavigation Navigation { get; private set; }
    public float falseReportPenalty = 5f;
    public float reportCooldown = 10f;
    public float exileSeconds = 8f;
    private float nextReportAt, returnAt;
    private bool returning;
    public float ReturnSeconds => returning ? Mathf.Max(0f, returnAt - Time.time) : 0f;

    private void Awake() { Instance = this; }
    private IEnumerator Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted += ResetRound;
        // Dejar que los personajes se apoyen en el suelo antes de elegir el puesto policial.
        yield return new WaitForSecondsRealtime(1f);
        Navigation = gameObject.AddComponent<MarketNavigation>();
        if (!Navigation.Build())
        {
            Debug.LogWarning("Capibara: no se pudo preparar una ruta transitable. Las denuncias válidas no se penalizarán.");
            yield break;
        }
        var local = LocalMultiplayer.Instance;
        if (local == null || local.Worker == null) yield break;
        // La introducción pausa la gravedad: buscar el suelo bajo Tomás aunque aún esté elevado.
        if (!FindHome(local.Worker, out Vector3 home))
        { Debug.LogWarning("Capibara: no se encontró un puesto conectado al suelo de Tomás."); yield break; }
        var root = new GameObject("Capibara_Policia_Local");
        root.transform.position = home;
        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = Vector3.up * 0.75f;
        collider.height = 1.5f;
        collider.radius = 0.45f;
        collider.isTrigger = true;
        GameObject prefab = Resources.Load<GameObject>("OficialCapibaraLocal");
        if (prefab != null)
        {
            GameObject visual = Instantiate(prefab, root.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("Falta el modelo Resources/OficialCapibaraLocal.");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up * 0.75f;
            visual.GetComponent<Collider>().enabled = false;
            Destroy(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().material.color = new Color(0.45f, 0.28f, 0.16f);
        }
        Police = root.AddComponent<CapibaraPolice>();
        Police.Initialize(this, home);
        root.GetComponentInChildren<CharacterVisualRig>()?.BindAgent(Police.GetComponent<UnityEngine.AI.NavMeshAgent>());
    }

    private bool FindHome(PlayerController worker, out Vector3 home)
    {
        home = worker.transform.position;
        var hits = Physics.RaycastAll(worker.transform.position + Vector3.up, Vector3.down, 30f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.normal.y < 0.7f || hit.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (!Navigation.Sample(hit.point + Vector3.up * 0.02f, 1f, out Vector3 floor)) continue;
            for (int ring = 1; ring <= 3; ring++)
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4f;
                    Vector3 candidate = floor + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring * 1.5f;
                    if (Navigation.CanReach(floor, candidate, out home) && Vector3.Distance(home, floor) > 1f) return true;
                }
            home = floor;
            return true;
        }
        return false;
    }

    public bool Report(PlayerController reporter)
    {
        var local = LocalMultiplayer.Instance;
        if (!PlayerController.GameplayActive || reporter == null || reporter.IsDetained ||
            reporter.role != PlayerController.PlayerRole.Worker || local == null || local.Rival == null) return false;
        if (Police == null || reporter.IsStunned || !reporter.CanInspect(Police) ||
            Vector3.Distance(reporter.transform.position, Police.transform.position + Vector3.up * 0.75f) > 2.5f)
            return false;
        if (Police.IsChasing || local.Rival.IsDetained) { GameHUD.Notify("El Capibara ya está atendiendo el caso."); return false; }
        if (Time.time < nextReportAt) { GameHUD.Notify("El Capibara pide que esperes antes de otra denuncia."); return false; }
        var proof = Ledger.AvailableProof(reporter.InvestigationId, local.Rival.InvestigationId);
        nextReportAt = Time.time + reportCooldown;
        if (proof == null)
        {
            GameHUD.Warn("Denuncia sin pruebas: pierdes " + falseReportPenalty + " segundos. Examina primero los restos con E.");
            GameManager.Instance.DeductTime(falseReportPenalty);
            return false;
        }
        if (!Police.CanPursue(local.Rival))
        {
            GameHUD.Notify("El Capibara no encuentra una ruta al sospechoso. Conservas la prueba y el tiempo.", 4f);
            return false;
        }
        if (!Ledger.Reserve(proof.Id, reporter.InvestigationId, local.Rival.InvestigationId)) return false;
        Police.Pursue(local.Rival, proof.Id);
        GameHUD.Notify("¡Piii! El Capibara acepta la prueba y busca al sospechoso.", 4f);
        return true;
    }

    public bool Arrest(PlayerController suspect, int proofId)
    {
        if (!PlayerController.GameplayActive || suspect == null || suspect.IsDetained || Police == null ||
            !Police.CanArrest(suspect, proofId) ||
            !Ledger.CompleteArrest(proofId, suspect.InvestigationId)) return false;
        suspect.SetDetained(true);
        int number = Ledger.ExpulsionsOf(suspect.InvestigationId);
        if (number >= 3)
        {
            returning = false;
            GameHUD.Warn("¡Tercera expulsión! El Rival queda fuera del mercado.", 5f);
            GameManager.Instance.WinByExpulsions();
        }
        else
        {
            returning = true;
            returnAt = Time.time + exileSeconds;
            GameHUD.Notify((number == 1 ? "Primera" : "Segunda") + " expulsión. El Rival regresará en " + exileSeconds + " segundos.", 4f);
        }
        return true;
    }
    private void Update()
    {
        if (!PlayerController.GameplayActive || !returning || Time.time < returnAt) return;
        returning = false;
        LocalMultiplayer.Instance.ReturnRival();
        GameHUD.Notify("El Rival volvió al mercado. Debe esperar antes de otro tacle.", 4f);
    }
    private void ResetRound()
    {
        Ledger.Reset();
        nextReportAt = returnAt = 0f;
        returning = false;
        if (Police != null) Police.ResetForRound();
    }
    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= ResetRound;
        if (Police != null) Destroy(Police.gameObject);
        if (Instance == this) Instance = null;
    }
}
