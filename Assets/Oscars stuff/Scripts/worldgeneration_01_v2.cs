using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update

    private blueprintmanager_01_v2 blueprintManager;
    private floormanager_01_V1 floormanager;
    
    
    
    
    
    private Texture2D[] blueprints;
    private Texture2D superBlueprint;
    private Texture2D blueprint;
    void Start()
    {
        blueprintManager = this.GetComponent<blueprintmanager_01_v2>();
        superBlueprint = blueprintManager.GetRandomSuperBlueprint();
        blueprints = blueprintManager.GetRoomsFromSuperBlueprint(superBlueprint);
        floormanager.GenerateRoom(blueprints[1], new Vector2(1 , 1), 1);
    }
}
