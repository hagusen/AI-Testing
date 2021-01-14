using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUseAIGraph
{
    //Input?
    //[GraphInput(typeof(float))]
    float PlayerDistance();


    //[GraphOutput(typeof(void))]
    void FireWeapon();


    //[GraphOutput(typeof(int), typeof(bool))]
    void MoveTowardPlayer(int speed, bool IsFast);

    ///// Hur ska jag skicka in dem???
    

}