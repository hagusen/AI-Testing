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
using UnityEditor.UIElements;



public class AIGraph : EditorWindow
{
    private AIGraphView _graphView;

    [MenuItem("Graph/AIGraph")]
    public static void OpenAIGraphWindow() {

        var window = GetWindow<AIGraph>();
        window.titleContent = new GUIContent("AI Graph");




    }

    private void ConstructGraph() {

        //Create 
        _graphView = new AIGraphView {
            name = "AI Graph"

        };
        // Fill Window
        _graphView.StretchToParentSize();
        // Add the graphview to the window
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar() {

        var toolbar = new Toolbar();

        var nodeCreateButton = new ToolbarButton(()=> { _graphView.CreateNode("AI Node");        }); // change
        nodeCreateButton.text = "Create Node";
        toolbar.Add(nodeCreateButton);

        rootVisualElement.Add(toolbar);

    }



    void OnEnable() {
        ConstructGraph();
        GenerateToolbar();
    }


    void OnDisable() {

        rootVisualElement.Remove(_graphView);

    }



}
