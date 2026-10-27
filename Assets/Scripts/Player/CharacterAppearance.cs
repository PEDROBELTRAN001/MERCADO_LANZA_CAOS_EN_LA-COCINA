using System.Collections.Generic;
using UnityEngine;

public class CharacterAppearance : MonoBehaviour
{
    private readonly List<GameObject> original = new List<GameObject>();
    private GameObject replacement;
    private PlayerController player;
    private bool sabotageMarked;
    public int SelectedSkin { get; private set; }
    public CharacterVisualRig ActiveRig { get; private set; }
    private void Awake()
    {
        player=GetComponent<PlayerController>();
        foreach(Transform child in transform)
            if(child.name!="Hand" && child.name!="Escarapela violeta")original.Add(child.gameObject);
    }
    public bool Apply(int index)
    {
        if(player.IsDetained)return false;
        var catalog=Resources.Load<CharacterSkinCatalog>("CharacterSkins");
        if(index<0 || index>=CharacterSkinCatalog.Names.Length || catalog==null || catalog.Model(index)==null)return false;
        if(replacement!=null){replacement.SetActive(false);Destroy(replacement);}
        foreach(var visual in original)if(visual!=null)visual.SetActive(false);
        Transform oldBadge=transform.Find("Escarapela violeta");if(oldBadge!=null)oldBadge.gameObject.SetActive(false);
        replacement=Instantiate(catalog.Model(index),transform);
        replacement.name="Apariencia seleccionada";
        replacement.transform.localPosition=Vector3.zero;
        replacement.transform.localRotation=Quaternion.identity;
        foreach(Collider collider in replacement.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        ActiveRig=replacement.GetComponent<CharacterVisualRig>();
        FitToBody(replacement,player.GetComponent<CapsuleCollider>(),true);
        if(ActiveRig!=null)
        {
            ActiveRig.Bind(player);
            ActiveRig.SetSabotageMarked(sabotageMarked);
            if(ActiveRig.carryPoint!=null)player.Hand.localPosition=transform.InverseTransformPoint(ActiveRig.carryPoint.position);
        }
        SelectedSkin=index;
        player.RefreshAnimator();
        return true;
    }
    public void MarkSabotage()
    {
        if(player==null || player.role!=PlayerController.PlayerRole.Impostor)return;
        sabotageMarked=true;ActiveRig?.SetSabotageMarked(true);
    }
    public void ResetVisualState()
    {
        sabotageMarked=false;ActiveRig?.ResetVisual();
    }
    private static void FitToBody(GameObject visual,CapsuleCollider body,bool quadruped)
    {
        if(body==null)return;
        var renderers=visual.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
        Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        float scale=body.height*.92f/Mathf.Max(.01f,bounds.size.y);
        if(quadruped)scale=Mathf.Min(scale,Mathf.Min(body.radius*2f/Mathf.Max(.01f,bounds.size.x),1.35f/Mathf.Max(.01f,bounds.size.z)));
        visual.transform.localScale*=scale;
        bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        Vector3 target=body.transform.TransformPoint(body.center-Vector3.up*body.height*.5f);
        visual.transform.position+=target-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
    }
}
