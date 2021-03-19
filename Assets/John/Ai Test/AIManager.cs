using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{

    BaseEnemy prefab;





    // Update is called once per frame
    void Update()
    {
        
    }


    void SpawnEnemies(){


        for (int i = 0; i < 1; i++)
        {
            Instantiate(prefab);
        }
    }



}
