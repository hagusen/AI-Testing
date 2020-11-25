using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using Subtegral.DialogueSystem.DataContainers;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

public class AIGraphView : GraphView
{

    private readonly Vector2 defaultNodeSize = new Vector2(150, 200);


    public AIGraphView() {


        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        // Add node to the graphview
        AddElement(GenerateEntryPointNode());
    }//

    //Override since we have no data to pass around
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {

        var compatiblePorts = new List<Port>();

        //ports.ForEach()


    }



    private Port GeneratePort(AINode node, Direction portdirection, Port.Capacity capacity = Port.Capacity.Single) {

        return node.InstantiatePort(Orientation.Horizontal, portdirection, capacity, typeof(float));// temp float type = data to transmit
    }


    private AINode GenerateEntryPointNode() {

        var node = new AINode {

            title = "Start",
            GUID = Guid.NewGuid().ToString(),
            AIText = "Entry Point",
            EntryPoint = true

        };

        //Create and set name of port
        var generatedPort = GeneratePort(node, Direction.Output);
        generatedPort.portName = "Next";
        //Add port to node
        node.outputContainer.Add(generatedPort);


        // Mabye not needed?
        //Visual refresh
        node.RefreshExpandedState();
        node.RefreshPorts();



        node.SetPosition(new Rect(100, 200, 100, 150));
        return node;
    }

    public void CreateNode(string nodeName) {

        AddElement(CreateAINode(nodeName));
    }

    public AINode CreateAINode(string nodeName) {

        var aiNode = new AINode {
            title = nodeName,
            AIText = nodeName,
            GUID = Guid.NewGuid().ToString() 
        };

        var inputPort = GeneratePort(aiNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        aiNode.inputContainer.Add(inputPort);

        aiNode.RefreshExpandedState();
        aiNode.RefreshPorts();

        aiNode.SetPosition(new Rect(Vector2.zero, defaultNodeSize));

        return aiNode;
    }
}
