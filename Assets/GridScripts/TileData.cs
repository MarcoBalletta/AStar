using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileData
{
    public int row;
    public int column;
    public GridManager gm;
    public bool walkable;
    public AStarData aStarData = new AStarData();
    public Tile tile;

    public TileData(GridManager gridManager, int newRow, int newColumn, bool walkable, Tile tile)
    {
        row = newRow;
        column = newColumn;
        gm = gridManager;
        this.walkable = walkable;
        this.tile = tile;
    }

    public Vector2Int ToVector()
    {
        return new Vector2Int(row, column);
    }

    public void ChangeMaterial(Material material)
    {
        tile.ChangeMaterial(material);
    }
}
