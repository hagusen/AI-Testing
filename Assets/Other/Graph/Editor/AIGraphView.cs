﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using Subtegral.DialogueSystem.DataContainers;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
//using UnityEngine.UI;
using UnityEngine.UIElements;
//using Button = UnityEngine.UIElements.Button;

public class AIGraphView : GraphView
{

    public readonly Vector2 defaultNodeSize = new Vector2(150, 200);

    public List<ExposedVariable> exposedVariables = new List<ExposedVariable>();
    public Blackboard blackboard;

    private NodeSearchWindow _searchWindow;

    public AIGraphView(EditorWindow window) {
        styleSheets.Add(Resources.Load<StyleSheet>("AIGraph"));

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale *2);

        //this.selection

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        // Add node to the graphview
        AddElement(GenerateEntryPointNode());

        AddSearchWindow(window);
    }//

    private void AddSearchWindow(EditorWindow window) {

        _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        _searchWindow.Init(window, this);

        nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
    }

    //Override since we have no data to pass around
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {

        var compatiblePorts = new List<Port>();

        ports.ForEach(port => {

            if (startPort != port && startPort.node != port.node) {
                if ((startPort.direction == Direction.Input && port.direction == Direction.Output) || (startPort.direction == Direction.Output && port.direction == Direction.Input)) {
                    compatiblePorts.Add(port);
                }

            }
        });

        return compatiblePorts;
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
        var generatedPort = GeneratePort(node, Direction.Output); // change for multi 
        generatedPort.portName = "Next";
        //Add port to node
        node.outputContainer.Add(generatedPort);

        node.capabilities &= ~Capabilities.Movable;
        node.capabilities &= ~Capabilities.Deletable;

        // Mabye not needed?
        //Visual refresh
        node.RefreshExpandedState();
        node.RefreshPorts();


        //
        node.SetPosition(new Rect(500,300, 100, 150));
        return node;
    }

    public void AddPropertyToBlackBoard(ExposedVariable exposedVariable) {

        var localVariableName = exposedVariable.variableName;
        var localVariableValue = exposedVariable.variableValue;

        int iterator = 1;
        while (exposedVariables.Any(x => x.variableName == localVariableName)) {
            localVariableName = $"{localVariableName}({iterator})";
            iterator++;
            /* FIX Later
            if (localVariableName.Contains($"({iterator - 1})")) {
                localVariableName = $"{localVariableName.Substring(0, localVariableName.Length -3}" +"({iterator})";
            }
            */
        }


        var variable = new ExposedVariable();
        variable.variableName = localVariableName;
        variable.variableValue = localVariableValue;
        exposedVariables.Add(variable);


        var container = new VisualElement();
        var blackboardField = new BlackboardField { text = variable.variableName, typeText = "int variable" };
       // blackboardField.RegisterCallback<DragAndDrop>(evt => { });
        container.Add(blackboardField);


        var variableValueTextField = new TextField("Value:") {

            value = variable.variableValue
        };

        variableValueTextField.RegisterValueChangedCallback(evt => {

            var changingVariableIndex = exposedVariables.FindIndex(x => x.variableName == variable.variableName);
            exposedVariables[changingVariableIndex].variableValue = evt.newValue;
        
        });
        var blackboardValueRow = new BlackboardRow(blackboardField, variableValueTextField);
        container.Add(blackboardValueRow);


        blackboard.Add(container);
    }

    public void CreateNode(string nodeName, Vector2 position) {

        AddElement(CreateAINode(nodeName, position));
    }

    public AINode CreateAINode(string nodeName, Vector2 position) {

        var aiNode = new AINode {
            title = nodeName,
            AIText = nodeName,
            GUID = Guid.NewGuid().ToString()
        };

        var inputPort = GeneratePort(aiNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        aiNode.inputContainer.Add(inputPort);

        aiNode.styleSheets.Add(Resources.Load<StyleSheet>("Node"));

        var button = new ToolbarButton(() => { AddChoicePort(aiNode); });
        button.text = "";
        aiNode.titleContainer.Add(button);

        var textField = new TextField(string.Empty);
        textField.RegisterValueChangedCallback(evt => {

            aiNode.AIText = evt.newValue;
            aiNode.title = evt.newValue;
        });
        textField.SetValueWithoutNotify(aiNode.title);
        aiNode.mainContainer.Add(textField);

        aiNode.RefreshExpandedState();
        aiNode.RefreshPorts();
        aiNode.SetPosition(new Rect(position, defaultNodeSize));

        return aiNode;
    }

    // Add port to specific node
    public void AddChoicePort(AINode aiNode, string overriddenPortName = "") {
        var generatedPort = GeneratePort(aiNode, Direction.Output); // single

        // Remove old labels also creates a bug with the edge
        //var oldLabel = generatedPort.contentContainer.Q<Label>("type"); // Find the label
        //generatedPort.contentContainer.Remove(oldLabel);
        //


        var outputPortCount = aiNode.outputContainer.Query("connector").ToList().Count;
        var choicePortName = string.IsNullOrEmpty(overriddenPortName) ? $"Choice {outputPortCount}" : overriddenPortName;

        // text field as port
        var textField = new TextField {
            name = string.Empty,
            value = choicePortName
        };
        textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);
        generatedPort.contentContainer.Add(new Label("  "));
        generatedPort.contentContainer.Add(textField);
        //

        var deleteButton = new ToolbarButton(() => RemovePort(aiNode, generatedPort)) {
            text = "X"
        };
        generatedPort.contentContainer.Add(deleteButton);

        generatedPort.portName = choicePortName;
        aiNode.outputContainer.Add(generatedPort);
        aiNode.RefreshExpandedState();
        aiNode.RefreshPorts();
    }

    private void RemovePort(AINode aiNode, Port generatedPort) {
        var targetEdge = edges.ToList().Where(x => x.output.portName == generatedPort.portName && x.output.node == generatedPort.node);

        if (targetEdge.Any()) {
            //loop maybe?
            var edge = targetEdge.First();
            edge.input.Disconnect(edge);
            RemoveElement(targetEdge.First());
        }
        // Delete Port
        aiNode.outputContainer.Remove(generatedPort);
        aiNode.RefreshExpandedState();
        aiNode.RefreshPorts();
    }
}
