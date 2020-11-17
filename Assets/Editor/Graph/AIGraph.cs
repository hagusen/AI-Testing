using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor;


public class AIGraph : EditorWindow
{
    private AIGraphView _graphView;
     
    [MenuItem("Graph/AIGraph")]
    public static void OpenAIGraphWindow() {

        var window = GetWindow<AIGraph>();
        window.titleContent = new GUIContent("AI Graph");




    }

    void OnEnable() {

    }


    void OnDisable() {

    }



}
