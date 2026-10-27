using UnityEngine;

public class FoodVisual : MonoBehaviour
{
    private GameObject visual;
    private Ingredient.IngredientType currentType;
    private FoodModelFactory.Preparation currentState;
    public void Refresh(Ingredient.IngredientType type, bool chopped, bool cooked)
    {
        var state=cooked ? (chopped ? FoodModelFactory.Preparation.ChoppedCooked : FoodModelFactory.Preparation.Cooked) :
            chopped ? FoodModelFactory.Preparation.Chopped : FoodModelFactory.Preparation.Raw;
        if(visual!=null && currentType==type && currentState==state) return;
        if(visual!=null){visual.SetActive(false); Destroy(visual);}
        currentType=type; currentState=state;
        visual=FoodModelLibrary.CreateIngredient(type,state);
        visual.transform.SetParent(transform,false);
        visual.transform.localPosition=Vector3.zero;
        visual.transform.localRotation=Quaternion.identity;
    }
}
