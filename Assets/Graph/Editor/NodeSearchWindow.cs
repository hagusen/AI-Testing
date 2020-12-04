using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

// The basic data provider for the search window
public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
{

    private AIGraphView _graphView;


    public void Init(AIGraphView graphView) {
        _graphView = graphView;
    }


    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context) {

        var tree = new List<SearchTreeEntry> {
            //First group entries are the header of it
            new SearchTreeGroupEntry(new GUIContent("Create Stuff"), 0),
            //level = Row
            //Logic Group
            new SearchTreeGroupEntry(new GUIContent("Logic"), 1),
            // AINode
            new SearchTreeEntry(new GUIContent("AINode")) {
                userData = new AINode(),
                level = 2,
            },
            //
            new SearchTreeEntry(new GUIContent("Hello world")) {
                level = 2
            }

        };
        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context) {

        switch (SearchTreeEntry.userData) {

            case AINode aiNode:

                Debug.Log("AINode Created");
                _graphView.CreateNode("s");
                return true;

            default:
                return false;
        }

        //instead null check and just call the function with userdata as a argument
        //CreateNode(SearchTreeEntry.userData)

    }
}
