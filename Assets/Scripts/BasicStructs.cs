using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
struct CircleInfo
{
    public Vector2Int Centre;
    public int Radius;

    public CircleInfo(Vector2Int _centre, int radius)
    {
        Centre = _centre;
        Radius = radius;
    }
}

[Serializable]
public struct MinMaxInt
{
    public int min; public int max;

    public MinMaxInt(int _min, int _max)
    {
        min = _min; max = _max;
    }
}
