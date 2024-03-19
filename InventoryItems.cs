using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryItems : MonoBehaviour
{
//Contains items stats. Attach to prefab gameobject with sprite.
    enum itemTypes
    {
        food,
        resource,
        tool
    }

    [SerializeField] itemTypes Type;

    //Info appropriate to each object type. Sorted into struts to make clearer in the Unity editor.
    [System.Serializable] struct foodInfo
    {
        //Food related stats
    }

    [System.Serializable] struct resourceInfo
    {
        //Resource related stats
    }

    [System.Serializable] struct toolInfo
    {
        //Tool related stats
    }

    [SerializeField] foodInfo FoodInfo;
    [SerializeField] resourceInfo ResourceInfo;
    [SerializeField] toolInfo ToolInfo;

}
