using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

static class NodeCollector
{
    public static void FindAllNodes() {



        foreach (var nodeType in TypeCache.GetTypesDerivedFrom<BaseNode>()) {

            Debug.Log(nodeType.FullName);


  //          if (nodeType.IsAbstract)
                //return false; // skip node

//            return nodeType.GetCustomAttributes<NodeMenuItemAttribute>().Count() > 0;

        }

    }

    //Called the firsttime any method is called
    //Constructor
    static NodeCollector() {

        FindAllNodes();
        

    }

}
