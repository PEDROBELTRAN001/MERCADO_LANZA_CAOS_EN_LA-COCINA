using UnityEngine;

public class LocalPlayerView : MonoBehaviour
{
    private PlayerController owner;private Camera reference;private Camera view;
    private Vector3 focus;
    private float yaw, yawVelocity;
    public void Initialize(PlayerController player,Camera original)
    {
        owner=player;reference=original;view=GetComponent<Camera>();
        focus=owner.transform.position+Vector3.up*.6f;yaw=transform.eulerAngles.y;
    }
    private void LateUpdate()
    {
        if(owner==null || reference==null)return;
        var follow=reference.GetComponent<CameraFollow>();
        Vector3 motion=PlayerController.GameplayActive?owner.MovementDirection:Vector3.zero;
        if(motion.sqrMagnitude>.01f)
            yaw=Mathf.SmoothDampAngle(yaw,Mathf.Atan2(motion.x,motion.z)*Mathf.Rad2Deg,
                ref yawVelocity,follow!=null?follow.turnSmoothTime:.65f,100f,Time.deltaTime);
        focus=Vector3.Lerp(focus,owner.transform.position+Vector3.up*.6f+motion*.8f,1f-Mathf.Exp(-5f*Time.deltaTime));
        transform.rotation=Quaternion.Euler(reference.transform.eulerAngles.x,yaw,0f);
        float distance=follow!=null && follow.target!=null
            ?Vector3.Distance(reference.transform.position,follow.target.position+Vector3.up*follow.focusHeight):12f;
        transform.position=focus-transform.forward*distance;view.orthographicSize=reference.orthographicSize;
    }
}
