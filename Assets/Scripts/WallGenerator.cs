using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    [SerializeField] private Transform wallParent;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private float percentageWalkable;
    private GridManager gridManager;

    private void Start()
    {
        TryGetComponent<GridManager>(out gridManager);
        gridManager.onGridGenerated += GenerateWalls;
    }

    private void GenerateWalls(Dictionary<Vector2Int, TileData> mapTiles)
    {
        foreach (var tile in mapTiles)
        {
            tile.Value.walkable = ((tile.Key.x == 0 || tile.Key.x == gridManager.MaxRow - 1) || (tile.Key.y == 0 || tile.Key.y == gridManager.MaxColumn - 1)) ? false : Random.Range(0, 100) <= percentageWalkable;
            GameObject wall = (!tile.Value.walkable) ? Instantiate(wallPrefab, gridManager.GetWorld3DPosition(tile.Key), Quaternion.identity, wallParent) : null;
            //if (!tile.Value.walkable) Instantiate(wallPrefab, GetWorld3DPosition(tile.Key), Quaternion.identity);
        }
    }
}
