using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class floorgenerator_V1 : MonoBehaviour
{
    [SerializeField]
    private GameObject[] bunkerObjects;

    [SerializeField]
    private GameObject[] sewerObjects;

    [SerializeField]
    private GameObject[] facilityObjects;

    private Texture2D[] blueprints;
    private Texture2D superBlueprint;
    private Texture2D blueprint;

    public blueprintgenerator_v2 blueprintGenerator;

    private List<List<GameObject>> objectFloorLists = new List<List<GameObject>>();
    private List<List<Vector3>> positionFloorLists = new List<List<Vector3>>(); 





    public void PrepGeneration(){
        for(int i = 0; i < 9; i++){
           objectFloorLists.Add(new List<GameObject>()); 
           positionFloorLists.Add(new List<Vector3>());
        }
    }


    public void GenerateRoom(Texture2D blueprint, Vector2 blueprintPos, int floorIndex){

        Color[] roomTiles = new Color[blueprint.GetPixels().Length];
        Debug.Log(roomTiles[0]);
        //CHECK IF LIST IS EMPTY IN THAT INDEX ELSE
        //loop each pixel and check position
        //Instantiate corresponding gameobject
        GameObject Tmp = new GameObject();
        Debug.Log(Tmp.transform.position);
        objectFloorLists[floorIndex - 1].Add(Tmp);
        positionFloorLists[floorIndex - 1].Add(Tmp.transform.position);
    }


    public void CreateFloor(int floorIndex){
        superBlueprint = blueprintGenerator.GetRandomSuperBlueprint(floorIndex);
        blueprints = blueprintGenerator.GetRoomsFromSuperBlueprint(superBlueprint);
    }
    public void LoadFloor(int floorIndex){

    }

}
