using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Bump", menuName = "0ScriptableObjects/TerrainBumpInfo", order = 1)]
public class TerrainBumpInfo : ScriptableObject
{

    public int numberOfBumps;
    public MinMaxInt minMaxBumpsHeight;
    public int yDirection;
    public bool linkToSameTypeOfBump;
    public int maxLinkDistance;

    public TerrainBumpInfo(int _numberOfBumps, MinMaxInt _minMaxBumpsHeight, int _yDirection, bool _linkToSameTypeOfBump, int _maxLinkDistance)
    {
        numberOfBumps = _numberOfBumps;
        minMaxBumpsHeight = _minMaxBumpsHeight;
        yDirection = _yDirection;
        linkToSameTypeOfBump = _linkToSameTypeOfBump;
        maxLinkDistance = _maxLinkDistance;
    }

}
