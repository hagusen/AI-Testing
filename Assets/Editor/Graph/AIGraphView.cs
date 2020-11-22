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
    public AIGraphView() {


        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        // Add node to the graphview
        AddElement(GenerateEntryPointNode());
    }

    private AINode GenerateEntryPointNode() {

        var node = new AINode {

            title = "Start",
            GUID = Guid.NewGuid().ToString(),
            AIText = "Entry Point",
            EntryPoint = true

        };

        node.SetPosition(new Rect(100, 200, 100, 150));

        return node;
    }
}
