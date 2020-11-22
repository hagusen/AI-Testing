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
public class AIGraph : EditorWindow
{
    private AIGraphView _graphView;
     
    [MenuItem("Graph/AIGraph")]
    public static void OpenAIGraphWindow() {

        var window = GetWindow<AIGraph>();
        window.titleContent = new GUIContent("AI Graph");




    }

    void OnEnable() {

        //Create 
        _graphView = new AIGraphView {
            name = "AI Graph"

        };
        // Fill Window
        _graphView.StretchToParentSize();
        // Add the graphview to the window
        rootVisualElement.Add(_graphView);
    }


    void OnDisable() {

        rootVisualElement.Remove(_graphView);

    }



}
