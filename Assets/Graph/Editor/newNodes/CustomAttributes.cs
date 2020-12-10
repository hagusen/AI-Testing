using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;







/// <summary>
/// Tags a class to use it as a node
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class NodeMenuItemAttribute : Attribute
{
    public string name; // name in search menu

    public NodeMenuItemAttribute(string name) {

        this.name = name;
    }
}


/// <summary>
/// Tags a field/variable as input for ports
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class InputAttribute : Attribute
{
    public string name; // name of port 

    public InputAttribute(string name = "In") {

        this.name = name;

    }
}
