using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapSaveUtility
{
    private AIGraphView _targetGraphView;

    public static GrapSaveUtility GetInstance(AIGraphView targetGraphView) {
        return new GrapSaveUtility {
            _targetGraphView = targetGraphView
        };
    }


    public void SaveGraph(string fileName) {



    }


    public void LoadGraph(string fileName) {

    }




}
