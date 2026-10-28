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
        // Altura del modelo de Tomas, para que las otras apariencias midan lo mismo
        bool found=false;Bounds tomas=default;
        foreach(var original in originals)if(original.activeSelf)foreach(var renderer in original.GetComponentsInChildren<Renderer>())
        {if(!found){tomas=renderer.bounds;found=true;}else tomas.Encapsulate(renderer.bounds);}
        tomasHeight=found?tomas.size.y:0;
    }
    private float tomasHeight;
    public bool Apply(int index)
    {
        var catalog=Resources.Load<CharacterSkinCatalog>("MenuIntegration/CharacterSkins");
        if(player.IsDetained)return false;
        // El Rival (1) es Tomás con corbata: reutiliza su modelo, ropa y animaciones
        bool tomasBase=index==0 || index==1;
        // Modelos hechos en Blender (Resources/Characters): Doña Rosa (2) de cocinera y la Cholita Paceña (3)
        GameObject blenderModel=index==2?Resources.Load<GameObject>("Characters/DonaRosa"):index==3?Resources.Load<GameObject>("Characters/CholitaPacena"):null;
        if(index<0 || index>=5 || (!tomasBase && blenderModel==null && (catalog==null || catalog.Model(index)==null)))return false;
        if(replacement!=null){replacement.SetActive(false);Destroy(replacement);}
        for(int i=0;i<originals.Count;i++)if(originals[i]!=null)originals[i].SetActive(tomasBase && active[i]);
        var tomas=GetComponent<TomasAvatar>();if(tomas!=null){tomas.enabled=tomasBase;tomas.SetNecktie(index==1);}
        player.Hand.localPosition=handPosition;
        if(!tomasBase)
        {
            replacement=blenderModel!=null?WrapBlenderModel(blenderModel):Instantiate(catalog.Model(index),transform);replacement.name="Apariencia seleccionada";
            foreach(var collider in replacement.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            foreach(var rigidbody in replacement.GetComponentsInChildren<Rigidbody>(true)){rigidbody.isKinematic=true;rigidbody.useGravity=false;}
            var renderers=replacement.GetComponentsInChildren<Renderer>();
            if(renderers.Length>0 && TryGetComponent(out CapsuleCollider body))
            {
                Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                float scale=Mathf.Min(body.height*.92f/Mathf.Max(.01f,bounds.size.y),body.radius*2/Mathf.Max(.01f,bounds.size.x));
                // El modelo de Blender mide lo mismo que Tomas (no se encoge a la capsula)
                if(blenderModel!=null && tomasHeight>0)scale=tomasHeight/Mathf.Max(.01f,bounds.size.y);
                replacement.transform.localScale*=scale;
                bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                Vector3 feet=transform.TransformPoint(body.center-Vector3.up*body.height*.5f);
                replacement.transform.position+=feet-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            }
            var rig=replacement.GetComponent<CharacterVisualRig>();
            if(rig!=null){rig.Bind(player);if(rig.carryPoint!=null)player.Hand.localPosition=transform.InverseTransformPoint(rig.carryPoint.position);}
        }
        SelectedSkin=index;player.RefreshAnimator();return true;
    }
    // El modelo de Blender va dentro de una raiz con CharacterVisualRig, que lo hace rebotar al caminar.
    // Si al importarlo quedo de espaldas (ejes de Blender), se gira para que los ojos miren al frente.
    private GameObject WrapBlenderModel(GameObject model)
    {
        var root=new GameObject();root.transform.SetParent(transform,false);
        var body=Instantiate(model,root.transform);body.transform.localPosition=Vector3.zero;
        var eyes=body.transform.Find("Ojos");var renderers=body.GetComponentsInChildren<Renderer>();
        if(eyes!=null && renderers.Length>0)
        {
            Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            var eyesRenderer=eyes.GetComponent<Renderer>();Vector3 eyesCenter=eyesRenderer!=null?eyesRenderer.bounds.center:eyes.position;
            if(Vector3.Dot(eyesCenter-bounds.center,transform.forward)<0)body.transform.Rotate(0,180,0,Space.World);
        }
        var rig=root.AddComponent<CharacterVisualRig>();rig.designId="DonaRosaCholita";rig.torso=body.transform;
        return root;
    }
}
