using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu]
public class CollisionMatrixSO : SerializedScriptableObject
{
    public String[] CollisionLayerData = new string[32];
    public bool[,] CollisionMatrixData = new bool[32,32];

    
}
