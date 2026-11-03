using UnityEngine;

public class LocalPlayerView : MonoBehaviour
{
    private PlayerController owner;private Camera reference;private Camera view;
    public void Initialize(PlayerController player,Camera original){owner=player;reference=original;view=GetComponent<Camera>();}
    private void LateUpdate()
    {
        if(owner==null || reference==null)return;
        var follow=reference.GetComponent<CameraFollow>();Vector3 referenceFocus=follow!=null && follow.target!=null?follow.target.position:LocalMultiplayer.Instance.Worker.transform.position;
        if(follow!=null && follow.useBounds){referenceFocus.x=Mathf.Clamp(referenceFocus.x,follow.minXZ.x,follow.maxXZ.x);referenceFocus.z=Mathf.Clamp(referenceFocus.z,follow.minXZ.y,follow.maxXZ.y);}
        Vector3 offset=reference.transform.position-referenceFocus;
        Vector3 focus=owner.transform.position;
        if(follow!=null && follow.useBounds){focus.x=Mathf.Clamp(focus.x,follow.minXZ.x,follow.maxXZ.x);focus.z=Mathf.Clamp(focus.z,follow.minXZ.y,follow.maxXZ.y);}
        transform.position=Vector3.Lerp(transform.position,focus+offset,5*Time.deltaTime);transform.rotation=reference.transform.rotation;view.orthographicSize=reference.orthographicSize;
    }
}
