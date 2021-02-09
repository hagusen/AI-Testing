using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField] 
    private GameObject roof;
    [SerializeField] 
    private GameObject wall;
    [SerializeField] 
    private GameObject wallRare;
    [SerializeField] 
    private GameObject floor;
    [SerializeField] 
    private GameObject floorRare;

    private blueprintmanager_01_v2 blueprintManager;
    private Image[] blueprints;




    private Image superBlueprint;

    void Start()
    {
        blueprintManager = this.GetComponent<blueprintmanager_01_v2>();
        superBlueprint = blueprintManager.GetRandomSuperBlueprint();
        blueprints = blueprintManager.GetRoomsFromSuperBlueprint(superBlueprint);
        
    }
}
