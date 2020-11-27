using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private AIGraphView _targetGraphView;
    private AIContainer _containerCache;

    private List<Edge> Edges => _targetGraphView.edges.ToList();
    private List<AINode> Nodes => _targetGraphView.nodes.ToList().Cast<AINode>().ToList();

    public static GraphSaveUtility GetInstance(AIGraphView targetGraphView) {
        return new GraphSaveUtility {
            _targetGraphView = targetGraphView
        };
    }


    public void SaveGraph(string fileName) { // Ports are saved by order now!!

        if (!Edges.Any())
            return;
        
        // Save all edges
        var AIContainer = ScriptableObject.CreateInstance<AIContainer>();
        // Expects every node to have single input
        // we only save output edges 
        var connectedPorts = Edges.Where(x => x.input.node != null).ToArray();
        for (int i = 0; i < connectedPorts.Length; i++) {

            var outputNode = connectedPorts[i].output.node as AINode;
            var inputNode = connectedPorts[i].input.node as AINode;

            AIContainer.NodeLinks.Add(new NodeLinkData {
                BaseNodeGUID = outputNode.GUID,
                // Input ports always have the same name
                // 
                PortName = connectedPorts[i].output.portName,
                TargetNodeGUID = inputNode.GUID
            });
        }


        foreach (var aiNode in Nodes.Where(node => !node.EntryPoint)) {
            AIContainer.AINodeData.Add(new AINodeData {
                Guid = aiNode.GUID,
                AItext = aiNode.AIText,
                Position = aiNode.GetPosition().position
            
            });
        }

        if (!AssetDatabase.IsValidFolder("Assets/Graph")) {
            AssetDatabase.CreateFolder("Assets", "Graph");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Graph/Resources")) {
            AssetDatabase.CreateFolder("Assets/Graph", "Resources");
        }

        AssetDatabase.CreateAsset(AIContainer, $"Assets/Graph/Resources/{fileName}.asset");
    }


    public void LoadGraph(string fileName) {

        _containerCache = Resources.Load<AIContainer>(fileName);
        if (_containerCache == null) {

            EditorUtility.DisplayDialog("File Not Found", "Target AI graph does not exist!", "Ok");
            return;
        }


        ClearGraph();
        CreateNodes();
        ConnectNodes();






    }

    private void ConnectNodes() {
        for (int i = 0; i < Nodes.Count; i++) {

            var connections = _containerCache.NodeLinks.Where(x => x.BaseNodeGUID == Nodes[i].GUID).ToList();
            for (int j = 0; j < connections.Count; j++) {
                var targetNodeGuid = connections[j].TargetNodeGUID;
                //find the first one with the same Guid
                var targetNode = Nodes.First(x => x.GUID == targetNodeGuid);
                LinkNodes(Nodes[i].outputContainer[j].Q<Port>(), (Port)targetNode.inputContainer[0]); // Always 1 input..


                // Why? Move to create Nodes
                targetNode.SetPosition(new Rect(_containerCache.AINodeData.First(x => x.Guid == targetNodeGuid).Position, _targetGraphView.defaultNodeSize));
            }
        }
    }

    private void LinkNodes(Port output, Port input) {
        var tempEdge = new Edge {
            output = output,
            input = input
        };
        //? null checker
        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);
        _targetGraphView.Add(tempEdge);
    }

    private void CreateNodes() {

        foreach (var nodeData in _containerCache.AINodeData) {
            //Instead use enum?
            var tempNode = _targetGraphView.CreateAINode(nodeData.AItext);
            tempNode.GUID = nodeData.Guid;
            _targetGraphView.AddElement(tempNode);

            var nodePorts = _containerCache.NodeLinks.Where(x => x.BaseNodeGUID == nodeData.Guid).ToList();
            nodePorts.ForEach(x => _targetGraphView.AddChoicePort(tempNode, x.PortName));
        }

    }

    private void ClearGraph() {

        //Set Entry point GUID
        Nodes.Find(x => x.EntryPoint).GUID = _containerCache.NodeLinks[0].BaseNodeGUID;

        foreach (var node in Nodes) {
            if (node.EntryPoint) {
                continue;
            }
            //get all output connections from this node
            Edges.Where(x => x.input.node == node).ToList()
                //Remove all connections
                .ForEach(edge => _targetGraphView.RemoveElement(edge));
            // Remove the node
            _targetGraphView.RemoveElement(node);
        }


    }
}
