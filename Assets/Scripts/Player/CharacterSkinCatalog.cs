using UnityEngine;

[CreateAssetMenu(menuName = "Mercado/Apariencias")]
public class CharacterSkinCatalog : ScriptableObject
{
    public GameObject tomas;
    public GameObject rival;
    public GameObject donaRosa;
    public GameObject cholita;
    public GameObject capibara;
    public RuntimeAnimatorController tomasAnimator;
    public static readonly string[] Names = { "Tomás", "El Rival", "Doña Rosa", "Cholita Paceña", "Capibara" };
    public GameObject Model(int index)
    {
        switch (index)
        {
            case 1: return rival;
            case 2: return donaRosa;
            case 3: return cholita;
            case 4: return capibara;
            default: return tomas;
        }
    }
}
