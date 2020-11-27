using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GraphSaveUtility
{
    private AIGraphView _targetGraphView;

    private List<Edge> Edges => _targetGraphView.edges.ToList();
    private List<AINode> Nodes => _targetGraphView.nodes.ToList().Cast<AINode>().ToList();

    public static GraphSaveUtility GetInstance(AIGraphView targetGraphView) {
        return new GraphSaveUtility {
            _targetGraphView = targetGraphView
        };
    }


    public void SaveGraph(string fileName) {

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


        AssetDatabase.CreateAsset(AIContainer, $"Assets/Graph/{fileName}.asset");
    }


    public void LoadGraph(string fileName) {

    }




}
