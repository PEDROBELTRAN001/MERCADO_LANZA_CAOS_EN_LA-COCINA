using UnityEngine;
using UnityEngine.AI;

// Articulación visual: no cambia la física ni las reglas del personaje.
public class CharacterVisualRig : MonoBehaviour
{
    public string designId;
    public bool quadruped;
    public Transform torso, head, leftArm, rightArm, leftLeg, rightLeg, backLeftLeg, backRightLeg, carryPoint;
    public GameObject sabotageStains;
    // Equipo decorativo de la referencia: deja espacio para el ingrediente real al cargarlo.
    public GameObject idleEquipment;
    public float carryingArmAngle = -43f;
    public bool SabotageMarked => sabotageStains != null && sabotageStains.activeSelf;
    private PlayerController player;
    private NavMeshAgent agent;
    private Transform[] joints;
    private Quaternion[] rest;
    private Vector3 torsoPosition, headPosition;
    private float phase;

    private void Awake() { CachePose(); }
    private void CachePose()
    {
        joints = new[] { leftArm, rightArm, leftLeg, rightLeg, backLeftLeg, backRightLeg };
        rest = new Quaternion[joints.Length];
        for (int i = 0; i < joints.Length; i++) rest[i] = joints[i] != null ? joints[i].localRotation : Quaternion.identity;
        if (torso != null) torsoPosition = torso.localPosition;
        if (head != null) headPosition = head.localPosition;
    }
    public void Bind(PlayerController owner) { player = owner; ResetVisual(); }
    public void BindAgent(NavMeshAgent owner) { agent = owner; ResetVisual(); }
    public void SetSabotageMarked(bool value) { if (sabotageStains != null) sabotageStains.SetActive(value); }
    public void ResetVisual()
    {
        if (joints == null) CachePose();
        phase = 0f;
        for (int i = 0; i < joints.Length; i++) if (joints[i] != null) joints[i].localRotation = rest[i];
        if (torso != null) torso.localPosition = torsoPosition;
        if (head != null) head.localPosition = headPosition;
        SetSabotageMarked(false);
        if(idleEquipment!=null)idleEquipment.SetActive(true);
    }
    private void LateUpdate()
    {
        if (!Application.isPlaying || !PlayerController.GameplayActive || (player == null && agent == null)) return;
        if (player != null && player.IsDetained) return;
        Vector3 velocity = player != null ? player.GetComponent<Rigidbody>().linearVelocity : agent.velocity;
        velocity.y = 0f;
        float moving = Mathf.Clamp01(velocity.magnitude / 3f);
        phase += Time.deltaTime * Mathf.Lerp(3f, 10f, moving);
        float swing = Mathf.Sin(phase) * moving;
        bool carrying = player != null && player.HeldItem != null;
        if(idleEquipment!=null)idleEquipment.SetActive(!carrying);
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null) continue;
            float angle = (i % 2 == 0 ? 1f : -1f) * swing * (quadruped ? 24f : 25f);
            if (!quadruped && i < 2) angle = carrying ? carryingArmAngle : idleEquipment!=null ? 0f : -angle * 0.55f;
            if (quadruped && i >= 4) angle = -angle;
            joints[i].localRotation = rest[i] * Quaternion.Euler(angle, 0f, 0f);
        }
        if (torso != null) torso.localPosition = torsoPosition + Vector3.up * Mathf.Abs(Mathf.Sin(phase)) * moving * 0.018f;
        if (head != null) head.localPosition = headPosition + Vector3.up * Mathf.Sin(phase * .5f) * moving * 0.008f;
    }
}
