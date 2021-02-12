using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class AIAssetHandler
{
#if UNITY_EDITOR // used for all assets...
    // test actually works.... 
    [OnOpenAsset(1)]
    public static bool step1(int instanceID, int line) {
        var name = EditorUtility.InstanceIDToObject(instanceID);
        Debug.Log(AssetDatabase.GetAssetPath(instanceID));
        Debug.Log("Open Asset step: 1 (" + name + ")");
        return false; // we did not handle the open
    }

    // step2 has an attribute with index 2, so will be called after step1
    [OnOpenAsset(1)]
    public static bool step2(int instanceID, int line) {
        Debug.Log("Open Asset step: 2 (" + instanceID + ")");
        return false; // we did not handle the open
    }
    //

#endif
}
