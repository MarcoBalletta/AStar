using System.Collections.Generic;
using UnityEngine;

public class Directions
{
    public List<Vector2Int> directions = new List<Vector2Int>()
    {
        new Vector2Int(0,1),
        new Vector2Int(-1,0),
        new Vector2Int(1,0),
        new Vector2Int(0,-1)
    };
}
