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
        //public List<PortDescription> portDescriptions = new List<PortDescription>();

    }

    public static void FindAllNodes() {

        NodeDescriptions localNodes = new NodeDescriptions();

        foreach (var nodeType in TypeCache.GetTypesDerivedFrom<BaseNode>()) {

            var attributes = nodeType.GetCustomAttributes(typeof(NodeMenuItemAttribute), false) as NodeMenuItemAttribute[];

            if (attributes != null)
            {
                
                foreach (var attr in attributes)
                {
                    localNodes.menutitleToNode.Add(attr.path, nodeType);
                }

                // Look for in and out fields 
                //







            }



            //          if (nodeType.IsAbstract)
            //return false; // skip node

            //            return nodeType.GetCustomAttributes<NodeMenuItemAttribute>().Count() > 0;

        }
        

    }


/*
		public static IEnumerable<(string path, Type type)>	GetNodeMenuEntries(BaseGraph graph = null)
		{
			foreach (var node in genericNodes.nodePerMenuTitle)
				yield return (node.Key, node.Value);

			if (graph != null && specificNodeDescriptions.TryGetValue(graph, out var specificNodes))
			{
				foreach (var node in specificNodes.nodePerMenuTitle)
					yield return (node.Key, node.Value);
			}
		}
*/


    public static void GetAllSpecialNodes(){


            foreach (var method in typeof(IUseAIGraph).GetMethods())
            {
                var x = method.GetParameters()[0].Name;

               //method.Invoke()

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



}
