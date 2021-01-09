using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine.UIElements;

// The basic data provider for the search window
public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
{

    // https://www.google.com/search?q=get+list+of+every+inherited+classes&oq=get+list+of+every+inher&aqs=chrome.1.69i57j33i22i29i30.53035j0j7&sourceid=chrome&ie=UTF-8
    // test this
    //

    private AIGraphView _graphView;
    private EditorWindow _window;

    private Texture2D _iconFix;


    public void Init(EditorWindow window, AIGraphView graphView)
    {
        _graphView = graphView;
        _window = window;

        _iconFix = new Texture2D(1, 1);
        _iconFix.SetPixel(0, 0, new Color(0, 0, 0, 0));
        _iconFix.Apply();
    }


    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {

        var tree = new List<SearchTreeEntry> {
            //First group entries are the header of it
            new SearchTreeGroupEntry(new GUIContent("Create Stuff"), 0),
            //level = Row
            //Logic Group
            new SearchTreeGroupEntry(new GUIContent("Logic"), 1),
            // AINode
            new SearchTreeEntry(new GUIContent("AINode", _iconFix)) {
                userData = new AINode(),
                level = 2,
            },
            //
            new SearchTreeEntry(new GUIContent("Hello world", _iconFix)) {
                level = 2
            }

        };
        for (int i = 0; i < 30; i++)
        {

            tree.Add(new SearchTreeEntry(new GUIContent("AINode", _iconFix))
            {
                userData = new AINode(),
                level = 2,
            });
        }


        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {

        var worldMousePosition = _window.rootVisualElement.ChangeCoordinatesTo(_window.rootVisualElement.parent, context.screenMousePosition - _window.position.position);
        var localMousePosition = _graphView.contentViewContainer.WorldToLocal(worldMousePosition);

        switch (SearchTreeEntry.userData)
        {

            case AINode aiNode:

                Debug.Log("AINode Created");
                _graphView.CreateNode("s", localMousePosition);
                return true;

            default:
                return false;
        }

        //instead null check and just call the function with userdata as a argument
        //CreateNode(SearchTreeEntry.userData)

    }
}
