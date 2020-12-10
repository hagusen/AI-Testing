using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NodeCollector
{
    static void Test() {

        TypeCache.GetTypesDerivedFrom<BaseNode>();

    }

}
