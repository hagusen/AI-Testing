﻿using System;
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
    private String _fileName = "New AI";


    [MenuItem("Graph/AIGraph")]
    public static void OpenAIGraphWindow() {

        var window = GetWindow<AIGraph>();
        window.titleContent = new GUIContent("AI Graph");

    }



    private void RequestDataOperation(bool save) {
        if (string.IsNullOrEmpty(_fileName)) {
            EditorUtility.DisplayDialog("Invalid file name!", "Please enter a valid file name", "Ok");
        }

        var SaveUtility = GraphSaveUtility.GetInstance(_graphView);

        if (save) {
            SaveUtility.SaveGraph(_fileName);
        }
        else {
            SaveUtility.LoadGraph(_fileName);
        }

    }

    void OnEnable() {
        ConstructGraph();
        GenerateToolbar();
        GenerateMiniMap();
        GenerateBlackBoard();
        //
        //
        //NodeCollector.FindAllNodes();
    }
    private void ConstructGraph() {

        //Create 
        _graphView = new AIGraphView(this) {
            name = "AI Graph"

        };
        // Fill Window
        _graphView.StretchToParentSize();
        // Add the graphview to the window
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar() {

        var toolbar = new Toolbar();
        //

        var fileNameTextField = new TextField("File Name:");
        fileNameTextField.SetValueWithoutNotify(_fileName);
        fileNameTextField.MarkDirtyRepaint();
        //Put change into variable _fileName
        fileNameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue);
        toolbar.Add(fileNameTextField);

        toolbar.Add(new ToolbarButton(() => RequestDataOperation(true)) { text = "Save Data" });
        toolbar.Add(new ToolbarButton(() => RequestDataOperation(false)) { text = "Load Data" });
        /*
        var nodeCreateButton = new ToolbarButton(()=> { _graphView.CreateNode("AI Node");        }); // change
        nodeCreateButton.text = "Create Node";
        toolbar.Add(nodeCreateButton);
        */

        //
        rootVisualElement.Add(toolbar);

    }

    private void GenerateMiniMap() {
        var miniMap = new MiniMap { anchored = true };
        //Changed to position. width
        var cords = _graphView.contentViewContainer.WorldToLocal(new Vector2(this.position.width -10, 30));
        miniMap.SetPosition(new Rect(10 , 30, 200, 140));
        _graphView.Add(miniMap);
    }

    private void GenerateBlackBoard() {

        var blackboard = new Blackboard(_graphView);

        blackboard.Add(new BlackboardSection { title = "Variables" });

        blackboard.addItemRequested = _blackboard => {
            _graphView.AddPropertyToBlackBoard(new ExposedVariable());
        };

        blackboard.SetPosition(new Rect(10,180, 200, 300));
        blackboard.scrollable = true;


        //
        _graphView.Add(blackboard);
        _graphView.blackboard = blackboard;
    }


    void OnDisable() {

        rootVisualElement.Remove(_graphView);

    }



}
