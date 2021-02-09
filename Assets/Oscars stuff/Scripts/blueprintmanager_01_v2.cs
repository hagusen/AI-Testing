using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class blueprintmanager_01_v2 : MonoBehaviour
{

    public Texture2D[] superBlueprints;
    public Texture2D[] normalBlueprints;

    public Texture2D[] treasureBlueprints;
    //ETC ETC




    public Texture2D GetRandomSuperBlueprint(){

        return superBlueprints[Random.Range(0, superBlueprints.Length)];
    }

    public Texture2D[] GetRoomsFromSuperBlueprint(Texture2D img){
        Texture2D[] imgArray = new Texture2D[img.GetPixels().Length];
        //Read img pixels and choose correct room
        //select image from corresponding array
        //rotate the image
        //add image to the new image array
        return  imgArray;
    }
    
}
