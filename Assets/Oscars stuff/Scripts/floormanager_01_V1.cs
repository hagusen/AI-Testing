using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class floormanager_01_V1 : MonoBehaviour
{
    [SerializeField]
    private GameObject[] bunkerObjects;

    [SerializeField]
    private GameObject[] sewerObjects;

    [SerializeField]
    private GameObject[] facilityObjects;


    private List<List<GameObject>> objectFloorLists = new List<List<GameObject>>();
    private List<List<Vector3>> positionFloorLists = new List<List<Vector3>>();

    void Start() {
        for(int i = 0; i < 9; i++){
           objectFloorLists.Add(new List<GameObject>()); 
           positionFloorLists.Add(new List<Vector3>()); 
        }    
    }
    public void GenerateFloor(Image blueprint, Vector2 blueprintPos, int floorIndex){

        //CHECK IF LIST IS EMPTY IN THAT INDEX ELSE

        //loop each pixel and check position
        //Instantiate corresponding gameobject
        GameObject Tmp = new GameObject();
        objectFloorLists[floorIndex - 1].Add(Tmp);
        positionFloorLists[floorIndex - 1].Add(Tmp.transform.position);
    }

}
