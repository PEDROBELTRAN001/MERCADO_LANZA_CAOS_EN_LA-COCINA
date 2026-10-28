using System.Collections.Generic;
using UnityEngine;

// Reutiliza las piezas de los personajes de presentacion_3. Tomás conserva TomasAvatar.
public class CharacterAppearance : MonoBehaviour
{
    private readonly List<GameObject> originals=new List<GameObject>();
    private readonly List<bool> active=new List<bool>();
    private GameObject replacement;private PlayerController player;private Vector3 handPosition;
    public int SelectedSkin {get;private set;}
    private void Awake()
    {
        player=GetComponent<PlayerController>();var avatar=GetComponent<TomasAvatar>();handPosition=avatar!=null?avatar.handPosition:player.Hand.localPosition;player.Hand.localPosition=handPosition;
        foreach(Transform child in transform)if(child.name!="Hand" && child.GetComponent<Camera>()==null){originals.Add(child.gameObject);active.Add(child.gameObject.activeSelf);}
    }
    public bool Apply(int index)
    {
        var catalog=Resources.Load<CharacterSkinCatalog>("MenuIntegration/CharacterSkins");
        if(player.IsDetained)return false;
        if(index<0 || index>=5 || (index!=0 && (catalog==null || catalog.Model(index)==null)))return false;
        if(replacement!=null){replacement.SetActive(false);Destroy(replacement);}
        for(int i=0;i<originals.Count;i++)if(originals[i]!=null)originals[i].SetActive(index==0 && active[i]);
        var tomas=GetComponent<TomasAvatar>();if(tomas!=null)tomas.enabled=index==0;
        player.Hand.localPosition=handPosition;
        if(index!=0)
        {
            replacement=Instantiate(catalog.Model(index),transform);replacement.name="Apariencia seleccionada";
            foreach(var collider in replacement.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            foreach(var rigidbody in replacement.GetComponentsInChildren<Rigidbody>(true)){rigidbody.isKinematic=true;rigidbody.useGravity=false;}
            var renderers=replacement.GetComponentsInChildren<Renderer>();
            if(renderers.Length>0 && TryGetComponent(out CapsuleCollider body))
            {
                Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                float scale=Mathf.Min(body.height*.92f/Mathf.Max(.01f,bounds.size.y),body.radius*2/Mathf.Max(.01f,bounds.size.x));replacement.transform.localScale*=scale;
                bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                Vector3 feet=transform.TransformPoint(body.center-Vector3.up*body.height*.5f);
                replacement.transform.position+=feet-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            }
            var rig=replacement.GetComponent<CharacterVisualRig>();
            if(rig!=null){rig.Bind(player);if(rig.carryPoint!=null)player.Hand.localPosition=transform.InverseTransformPoint(rig.carryPoint.position);}
        }
        SelectedSkin=index;player.RefreshAnimator();return true;
    }
}
