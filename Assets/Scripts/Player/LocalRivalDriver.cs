using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Reutiliza los sabotajes de humo/aceite de presentacion_3; no introduce tacle ni evidencias.
public class LocalRivalDriver : MonoBehaviour
{
    public static event Action<PlayerController,Interactable> OnSabotageCommitted;
    private PlayerController owner;private float nextTurn,availableAt;private Vector3 direction;
    public float CooldownRemaining=>Mathf.Max(0,availableAt-Time.time);
    private void Awake(){owner=GetComponent<PlayerController>();}
    public void ResetRound(){nextTurn=availableAt=0;direction=Vector3.zero;}
    public void WaitAfterReturn(){nextTurn=0;availableAt=Mathf.Max(availableAt,Time.time+3);direction=Vector3.zero;}
    private void Update()
    {
        if(!PlayerController.GameplayActive || owner.IsDetained)return;
        if(owner.usesAI)
        {
            var local=LocalMultiplayer.Instance;if(local==null)return;
            Vector3 toward=local.Worker.transform.position-transform.position;toward.y=0;
            if(Time.time>=nextTurn)
            {
                direction=toward.magnitude>6?toward.normalized:Quaternion.Euler(0,UnityEngine.Random.Range(-110,110),0)*transform.forward;
                nextTurn=Time.time+UnityEngine.Random.Range(1f,2.4f);
            }
            Vector3 center=transform.position+Vector3.up*.5f;bool blocked=false;
            foreach(var hit in Physics.SphereCastAll(center,.32f,direction,1.1f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(transform) && hit.collider.GetComponentInParent<PlayerController>()!=owner){blocked=true;break;}
            if(blocked){direction=Quaternion.Euler(0,110,0)*direction;nextTurn=Time.time+.3f;}
            owner.SetAIMovement(direction*.45f);
        }
        else if(Keyboard.current!=null && Keyboard.current.uKey.wasPressedThisFrame)
        {
            if(!TrySabotage())GameHUD.Notify("Rival: acércate a un fogón o estación. U activa el sabotaje cuando esté disponible.");
        }
    }
    public bool TrySabotage()
    {
        var local=LocalMultiplayer.Instance;var target=owner.CurrentInteractable;
        if(!PlayerController.GameplayActive || owner.IsDetained || local==null || local.Rival!=owner || owner.usesAI || Time.time<availableAt || SabotageSystem.Instance==null)return false;
        if(target==null || !(target is CookingStation || target is ChoppingStation || target is AssemblyStation))return false;
        var collider=target.GetComponent<Collider>();Vector3 point=collider!=null?collider.ClosestPoint(owner.transform.position):target.transform.position;
        if(Vector3.Distance(point,owner.transform.position)>owner.interactionRange*1.5f)return false;
        SabotageSystem.Instance.Sabotage(target,owner);availableAt=Time.time+20;OnSabotageCommitted?.Invoke(owner,target);return true;
    }
}
