using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class GridManager : MonoBehaviour
{
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private float percentageWalkable;
    [SerializeField] private int maxRow;
    [SerializeField] private int maxColumn;
    [SerializeField] private GameObject startPrefab;
    [SerializeField] private GameObject endPrefab;
    [SerializeField] private Vector2Int startCoordinates;
    [SerializeField] private Vector2Int endCoordinates;
    [SerializeField] private Vector2Int lastCoordinates;
    [SerializeField] private KeyCode startPrefabPositionKeycode;
    [SerializeField] private KeyCode startPathCalculationKeycode;
    private Grid gridData;
    [SerializeField] private Transform wallParent;
    [SerializeField] private Vector3 offsetGrid;
    [SerializeField] private Dictionary<Vector2Int, TileData> mapTiles = new Dictionary<Vector2Int, TileData>();
    private List<GameObject> tokens = new List<GameObject>();
    private Directions dir = new Directions();
    private List<TileData> openList = new List<TileData>();
    private List<TileData> closedList = new List<TileData>();
    
    private void Awake()
    {
        gridData = GetComponent<Grid>();
    }

    private void Start()
    {
        GenerateGrid();
    }


    private void Update()
    {
        if (Input.GetKeyDown(startPrefabPositionKeycode))
        {
            ClearTokens();
            SetStartEndPosition();
        }
        if (Input.GetKeyDown(startPathCalculationKeycode))
        {
            SearchNextStep(mapTiles[lastCoordinates]);
        }
    }

    #region GenerationGrid
    private void GenerateGrid()
    {
        for (int row = 0; row < maxRow; row++)
        {
            for(int column = 0; column < maxColumn; column++)
            {
                var tile = Instantiate(tilePrefab, GetWorldPosition(new Vector2Int(row,column)), Quaternion.identity, transform);
                tile.transform.localScale = gridData.cellSize;
                bool walkable = ((row == 0 || row == maxRow-1) || (column == 0 || column == maxColumn -1)) ? false : Random.Range(0, 100) <= percentageWalkable;
                tile.Initialize(this, row, column, walkable, tile);
                tile.name = "Tile - (" + row.ToString() + " - " + column.ToString() + ")";
                mapTiles[new Vector2Int(row, column)] = tile.data;
            }
        }
        GenerateWalls();
        CenterCamera();
    }

    private void GenerateWalls()
    {
        foreach (var tile in mapTiles)
        {
            GameObject wall = (!tile.Value.walkable) ? Instantiate(wallPrefab, GetWorldPosition(tile.Key), Quaternion.identity, wallParent) : null;
            //if (!tile.Value.walkable) Instantiate(wallPrefab, GetWorldPosition(tile.Key), Quaternion.identity);
        }
    }

    #region GetWorldPosition
    private Vector3 GetWorldPosition(Vector2Int position)
    {
        return offsetGrid + new Vector3(position.x * (gridData.cellSize.x + gridData.cellGap.x), 0, position.y * (gridData.cellSize.z + gridData.cellGap.z));
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return offsetGrid + new Vector3(x * (gridData.cellSize.x + gridData.cellGap.x), 0, y * (gridData.cellSize.z + gridData.cellGap.z));
    }
    #endregion

    private bool CheckGridBounds(Vector2Int coordinates)
    {
        return (coordinates.x < 0 || coordinates.x >= maxRow || coordinates.y < 0 || coordinates.y >= maxColumn);
    }
    private bool CheckWalkable(Vector2Int coordinates)
    {
        return mapTiles[coordinates].walkable;
    }

    private void CenterCamera()
    {
        Vector3 startGrid = GetWorldPosition(0, 0);
        Vector3 endGrid = GetWorldPosition(maxRow -1, maxColumn - 1);

        Camera.main.transform.position = new Vector3((startGrid.x + endGrid.x) /2, HeightCamera(), (startGrid.z + endGrid.z) / 2);
    }

    private float HeightCamera()
    {
        return maxColumn * (gridData.cellGap.z + gridData.cellSize.z) + (gridData.cellGap.y + gridData.cellSize.y) +1;
    }
    #endregion
    #region GenerationTokens
    private void SetStartEndPosition()
    {
        FindRandomPosition(ref startCoordinates, startPrefab);
        SpawnToken(startPrefab, startCoordinates);
        lastCoordinates = startCoordinates;
        do
        {
            FindRandomPosition(ref endCoordinates, endPrefab);
        } while(endCoordinates == startCoordinates);
        
        SpawnToken(endPrefab, endCoordinates);
    }

    private void FindRandomPosition(ref Vector2Int tilePositionSelected, GameObject prefab)
    {
        bool foundPosition = false;
        int numberOfTiles = maxColumn * maxRow;
        List<Vector2Int> coordinatesExamined = new List<Vector2Int>();

        while (numberOfTiles > 0 && !foundPosition)
        {
            GenerateRowAndColumnRandom(out Vector2Int positionOnGrid);
            //GenerateRowAndColumnRandom(out int rowGrid, out int columnGrid);
            numberOfTiles--;
            if (coordinatesExamined.Contains(positionOnGrid)) continue;
            coordinatesExamined.Add(positionOnGrid);
            if (CheckIfTileIsWalkable(positionOnGrid))
            {
                foundPosition = true;
                tilePositionSelected = positionOnGrid;
            }
        }
    }
    
    private void SpawnToken(GameObject prefab, Vector2Int coordinate)
    {
        var token = Instantiate(prefab, GetWorldPosition(coordinate), Quaternion.identity);
        tokens.Add(token);
    }

    private void ClearTokens()
    {
        foreach(var token in tokens)
        {
            Destroy(token);
        }
    }

    #region GenerateRandomRowAndColumn
    private void GenerateRowAndColumnRandom(out Vector2Int position)
    {
        int randomRow = Random.Range(0, maxRow);
        int randomColumn = Random.Range(0, maxColumn);
        position = new Vector2Int(randomRow, randomColumn);
    }

    private void GenerateRowAndColumnRandom(out int randomRow, out int randomColumn)
    {
        randomRow = Random.Range(0, maxRow);
        randomColumn = Random.Range(0, maxColumn);
    }
    #endregion

    private bool CheckIfTileIsWalkable(Vector2Int positionGrid)
    {
        return mapTiles[positionGrid].walkable;
    }
    #endregion
    #region PathCalculation
    private void SearchNextStep(TileData actualData) 
    { 
        if(actualData.ToVector() == endCoordinates)
        {
            Debug.Log("End");
            return;
        }

        foreach(Vector2Int direction in dir.directions)
        {
            Vector2Int neighbourCell = new Vector2Int(actualData.row + direction.x, actualData.column + direction.y);
            if (CheckGridBounds(neighbourCell)) continue;
            if (!CheckWalkable(neighbourCell)) continue;
            if (IsClosed(mapTiles[neighbourCell])) continue;

            float g = actualData.aStarData.g + Vector2.Distance(GetWorldPosition(actualData.ToVector()), GetWorldPosition(neighbourCell));
            float h = Vector2.Distance(GetWorldPosition(neighbourCell), GetWorldPosition(endCoordinates));
            float f = g + h;

            //setup TextMeshPro

            if(!UpdateTileData(mapTiles[neighbourCell], g, h, f, actualData))
            {
                openList.Add(mapTiles[neighbourCell]);
                mapTiles[neighbourCell].aStarData.SetupAStarData(g, h, f, actualData);
            }
        }

        openList.OrderBy(tileData => tileData.aStarData.f).ToList();
        TileData firstData = openList.ElementAt(0);
        closedList.Add(firstData);
        openList.RemoveAt(0);
        firstData.ChangeMaterial(firstData.aStarData.closed);
        lastCoordinates = firstData.ToVector();
    }

    private bool UpdateTileData(TileData data, float g, float h, float f, TileData parent)
    {
        foreach(TileData tileData in openList)
        {
            if(tileData == data)
            {
                tileData.aStarData.g = g;
                tileData.aStarData.h = h;
                tileData.aStarData.f = f;
                tileData.aStarData.parent = parent;
                return true;
            }
        }
        return false;
    }



    private bool IsClosed(TileData data)
    {
        return closedList.Contains(data);
    }
    #endregion
}
