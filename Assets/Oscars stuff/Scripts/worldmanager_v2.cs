using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class worldmanager_v2 : MonoBehaviour
{
    public floorgenerator_V1 floorGenerator;
    private List<List<GameObject>> objectFloorLists = new List<List<GameObject>>();
    private List<List<Vector3>> positionFloorLists = new List<List<Vector3>>();


    void Start()
    {
        /*CHECK IF THERE IS SAVED WORLD IF SO LOAD I ELSE CREATE THE WORLD AND SAVE IT*/
        floorGenerator.PrepGeneration();
        floorGenerator.CreateFloor(1);
        
    }
}
