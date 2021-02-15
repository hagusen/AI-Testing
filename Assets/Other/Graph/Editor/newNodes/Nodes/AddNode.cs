using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[NodeMenuItem("Add")]
public class AddNode : BaseNode
{
    [Input("In A")]
    public int in_a;
    [Input]
    public int in_b;

    
    public int out_a;


        // testtt

    public override void Calculate (){

            out_a = in_a + in_b;

    }   

    public int Calculate (int in_a, int in_b){

            return in_a + in_b;

    }   



}
