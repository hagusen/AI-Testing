using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

static class NodeCollector
{

    public struct PortDescription{

        public Type nodeType; // ??
        public Type portType;

        public bool isInput;
        public string portName;

    }
    public class NodeDescriptions{

        public Dictionary<string, Type> menutitleToNode = new Dictionary<string, Type>();
        public List<NodeDescriptions> portDescriptions = new List<NodeDescriptions>();

    }

    public static void FindAllNodes() {


        foreach (var nodeType in TypeCache.GetTypesDerivedFrom<BaseNode>()) {

            Debug.Log(nodeType.FullName);
            var attr = nodeType.GetCustomAttributes(typeof(NodeMenuItemAttribute), false) as NodeMenuItemAttribute[];
//
            Debug.Log(attr[0].name);


            //          if (nodeType.IsAbstract)
            //return false; // skip node

            //            return nodeType.GetCustomAttributes<NodeMenuItemAttribute>().Count() > 0;

        }

    }

    public static void GetAllSpecialNodes(){


            foreach (var method in typeof(IUseAIGraph).GetMethods())
            {
                var x = method.GetParameters()[0].Name;

               // method.Invoke()

                //method.Name;
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
    CYBERSPICE
    POGDOG
    

    Pedal star
    RETRO BARK
    GAMEFLESH

    HUGBUG

    SUPER MEGA REGULAR
    INTSTR

    */

}
