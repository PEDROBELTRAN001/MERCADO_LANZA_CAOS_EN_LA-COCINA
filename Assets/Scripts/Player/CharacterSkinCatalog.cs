using UnityEngine;

[CreateAssetMenu(menuName="Mercado/Catálogo de apariencias existentes")]
public class CharacterSkinCatalog : ScriptableObject
{
    public GameObject[] models=new GameObject[5];
    public static readonly string[] Names={"Tomás","El Rival","Doña Rosa","Cholita Paceña","Capibara"};
    public GameObject Model(int index)=>index>=0 && index<models.Length?models[index]:null;
}
