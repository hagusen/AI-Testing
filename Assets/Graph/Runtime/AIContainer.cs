using System.Collections.Generic;
using System;
using UnityEngine;



[Serializable]
public class AIContainer : ScriptableObject
{
    public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
    public List<AINodeData> AINodeData = new List<AINodeData>();



}
