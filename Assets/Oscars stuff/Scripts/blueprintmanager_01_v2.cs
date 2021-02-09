using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class blueprintmanager_01_v2 : MonoBehaviour
{

    public Image[] superBlueprints;
    public Image[] normalBlueprints;

    public Image[] treasureBlueprints;
    //ETC ETC




    public Image GetRandomSuperBlueprint(){

        return superBlueprints[Random.Range(0, superBlueprints.Length)];
    }

    public Image[] GetRoomsFromSuperBlueprint(Image img){
        Image[] imgArray = new Image[/*Storlek av img antal pixlar*/ 10];
        //Read img pixels and choose correct room
        //select image from corresponding array
        //rotate the image
        //add image to the new image array
        return  imgArray;
    }
    
}
