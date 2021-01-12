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
            var attr = nodeType.GetCustomAttributes(typeof(NodeMenuItemAttribute), false) as NodeMenuItemAttribute[];

            Debug.Log(attr[0].name);






            

            //          if (nodeType.IsAbstract)
            //return false; // skip node

            //            return nodeType.GetCustomAttributes<NodeMenuItemAttribute>().Count() > 0;

        }

    }

    public static void GetAllSpecialNodes(){


            foreach (var method in typeof(IUseAIGraph).GetMethods())
            {
                
            }



    }


    public static void Initialize() {

        FindAllNodes();
        Debug.Log("Constructor");

    }


    //Called the firsttime any method is called
    //Constructor
    static NodeCollector() {
        Initialize();
    }

    /*
    INTSTR
    POGDOG
    CYBERSPICE

    HUGBUG

    SUPER MEGA REGULAR
    RETRO BARK
    GAMEFLESH

    */

}
