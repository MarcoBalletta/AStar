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
    [SerializeField] private KeyCode startMovePlayerKeycode;
    private Grid gridData;
    [SerializeField] private Transform wallParent;
    [SerializeField] private Vector3 offsetGrid;
    [SerializeField] private Dictionary<Vector2Int, TileData> mapTiles = new Dictionary<Vector2Int, TileData>();
    private List<GameObject> tokens = new List<GameObject>();
    private Directions dir = new Directions();
    private List<TileData> openList = new List<TileData>();
    private List<TileData> closedList = new List<TileData>();
    private LineRenderer lineRenderer;
    private List<Vector3> pathPositions = new List<Vector3>();
    private bool pathSearching = false;
    private bool pathSearched = false;

    private void Awake()
    {
        gridData = GetComponent<Grid>();
        lineRenderer = GetComponent<LineRenderer>();
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
            if (startCoordinates != null)
            {
                ReturnToNormalTiles(startCoordinates);
                ReturnToNormalTiles(endCoordinates);
            }
            SetStartEndPosition();
        }
        if (Input.GetKeyDown(startPathCalculationKeycode) && !pathSearching)
        {
            //SearchNextStep(mapTiles[lastCoordinates]);
            SearchNextStepWhile(mapTiles[lastCoordinates]);
        }
        if(Input.GetKeyDown(startMovePlayerKeycode) && tokens.Count > 0 && pathSearched)
        {
            StartCoroutine(MovePlayer());
        }
    }

    #region GenerationGrid
    private void GenerateGrid()
    {
        for (int row = 0; row < maxRow; row++)
        {
            for (int column = 0; column < maxColumn; column++)
            {
                var tile = Instantiate(tilePrefab, GetWorld3DPosition(new Vector2Int(row, column)), Quaternion.identity, transform);
                tile.transform.localScale = gridData.cellSize;
                bool walkable = ((row == 0 || row == maxRow - 1) || (column == 0 || column == maxColumn - 1)) ? false : Random.Range(0, 100) <= percentageWalkable;
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
            GameObject wall = (!tile.Value.walkable) ? Instantiate(wallPrefab, GetWorld3DPosition(tile.Key), Quaternion.identity, wallParent) : null;
            //if (!tile.Value.walkable) Instantiate(wallPrefab, GetWorld3DPosition(tile.Key), Quaternion.identity);
        }
    }

    #region GetWorld2DPosition
    private Vector2 GetWorld2DPosition(Vector2Int position)
    {
        return new Vector2(position.x * (gridData.cellSize.x + gridData.cellGap.x), position.y * (gridData.cellSize.z + gridData.cellGap.z));
    }

    private Vector2 GetWorld2DPosition(int x, int y)
    {
        return new Vector2(x * (gridData.cellSize.x + gridData.cellGap.x), y * (gridData.cellSize.z + gridData.cellGap.z));
    }
    #endregion

    #region GetWorld3DPosition
    private Vector3 GetWorld3DPosition(Vector2Int position)
    {
        return new Vector3(position.x * (gridData.cellSize.x + gridData.cellGap.x), 0, position.y * (gridData.cellSize.z + gridData.cellGap.z));
    }

    private Vector3 GetWorld3DPosition(int x, int y)
    {
        return new Vector3(x * (gridData.cellSize.x + gridData.cellGap.x), 0, y * (gridData.cellSize.z + gridData.cellGap.z));
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
        Vector3 startGrid = GetWorld3DPosition(0, 0);
        Vector3 endGrid = GetWorld3DPosition(maxRow - 1, maxColumn - 1);

        Camera.main.transform.position = new Vector3((startGrid.x + endGrid.x) / 2, HeightCamera(), (startGrid.z + endGrid.z) / 2);
    }

    private float HeightCamera()
    {
        return maxColumn * (gridData.cellGap.z + gridData.cellSize.z) + (gridData.cellGap.y + gridData.cellSize.y) + 1;
    }
    #endregion
    #region GenerationTokens
    private void SetStartEndPosition()
    {
        FindRandomPosition(ref startCoordinates, startPrefab);
        SpawnToken(startPrefab, startCoordinates);
        SetupStartCoordinates();
        do
        {
            FindRandomPosition(ref endCoordinates, endPrefab);
        } while (endCoordinates == startCoordinates);

        SpawnToken(endPrefab, endCoordinates);
    }

    private void SetupStartCoordinates()
    {
        lastCoordinates = startCoordinates;
        AddToClosedList(startCoordinates);
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
        var token = Instantiate(prefab, GetWorld3DPosition(coordinate), Quaternion.identity);
        tokens.Add(token);
    }

    private void ClearTokens()
    {
        while(tokens.Count > 0)
        {
            var item = tokens[0];
            tokens.RemoveAt(0);
            Destroy(item);
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
    private void SearchNextStepWhile(TileData actualData)
    {
        pathSearching = true;
        while (actualData.ToVector() != endCoordinates)
        {
            foreach (Vector2Int direction in dir.directions)
            {
                Vector2Int neighbourCell = new Vector2Int(actualData.row + direction.x, actualData.column + direction.y);
                if (IsClosed(mapTiles[neighbourCell])) continue;
                if (CheckGridBounds(neighbourCell)) continue;
                if (!CheckWalkable(neighbourCell)) continue;

                float g = actualData.aStarData.g + Vector2.Distance(GetWorld2DPosition(actualData.ToVector()), GetWorld2DPosition(neighbourCell));
                float h = Vector2.Distance(GetWorld2DPosition(neighbourCell), GetWorld2DPosition(endCoordinates));
                float f = g + h;

                //setup TextMeshPro
                mapTiles[neighbourCell].tile.UiManager.SetFGHValues(f, g, h);

                //WIP
                if (!UpdateTileData(mapTiles[neighbourCell], g, h, f, actualData))
                {
                    AddToOpenList(neighbourCell);
                    //openList.Add(mapTiles[neighbourCell]);
                    mapTiles[neighbourCell].aStarData.SetupAStarData(g, h, f, actualData);
                    //mapTiles[neighbourCell].ChangeMaterial(mapTiles[neighbourCell].tile.open);
                }
            }

            openList = openList.OrderBy(tileData => tileData.aStarData.f).ThenBy(tileData => tileData.aStarData.h).ToList();
            
            //se open list è vuota termina, vicolo cieco
            if (openList.Count == 0) 
            {
                Debug.Log("Dead end");
                return;
            } 
            TileData firstData = openList.ElementAt(0);
            //closedList.Add(firstData);
            AddToClosedList(firstData.ToVector());
            openList.RemoveAt(0);
            //firstData.ChangeMaterial(firstData.tile.closed);
            //lastCoordinates = firstData.ToVector();
            actualData = firstData;
        }
        TracePath();
    }

    private void SearchNextStep(TileData actualData)
    {
        if (actualData.ToVector() == endCoordinates)
        {
            TracePath();
            return;
        }
        
        foreach (Vector2Int direction in dir.directions)
        {
            Vector2Int neighbourCell = new Vector2Int(actualData.row + direction.x, actualData.column + direction.y);
            if (IsClosed(mapTiles[neighbourCell])) continue;
            if (CheckGridBounds(neighbourCell)) continue;
            if (!CheckWalkable(neighbourCell)) continue;

            float g = actualData.aStarData.g + Vector2.Distance(GetWorld2DPosition(actualData.ToVector()), GetWorld2DPosition(neighbourCell));
            float h = Vector2.Distance(GetWorld2DPosition(neighbourCell), GetWorld2DPosition(endCoordinates));
            float f = g + h;

            //setup TextMeshPro
            mapTiles[neighbourCell].tile.UiManager.SetFGHValues(f, g, h);

            //WIP
            if (!UpdateTileData(mapTiles[neighbourCell], g, h, f, actualData))
            {
                AddToOpenList(neighbourCell);
                //openList.Add(mapTiles[neighbourCell]);
                mapTiles[neighbourCell].aStarData.SetupAStarData(g, h, f, actualData);
                //mapTiles[neighbourCell].ChangeMaterial(mapTiles[neighbourCell].tile.open);
            }
        }

        openList = openList.OrderBy(tileData => tileData.aStarData.f).ThenBy(tileData => tileData.aStarData.h).ToList();
        TileData firstData = openList.ElementAt(0);
        //closedList.Add(firstData);
        AddToClosedList(firstData.ToVector());
        openList.RemoveAt(0);
        //firstData.ChangeMaterial(firstData.tile.closed);
        lastCoordinates = firstData.ToVector();
    }

    private bool UpdateTileData(TileData data, float g, float h, float f, TileData parent)
    {
        foreach (TileData tileData in openList)
        {
            if (tileData == data)
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

    private void AddToOpenList(Vector2Int coordinates)
    {
        openList.Add(mapTiles[coordinates]);
        mapTiles[coordinates].ChangeMaterial(mapTiles[coordinates].tile.open);
    }

    private void AddToClosedList(Vector2Int coordinates)
    {
        closedList.Add(mapTiles[coordinates]);
        mapTiles[coordinates].ChangeMaterial(mapTiles[coordinates].tile.closed);
    }

    private void ReturnToNormalTiles(Vector2Int coordinates)
    {
        if (openList.Contains(mapTiles[coordinates]))
        {
            openList.Remove(mapTiles[coordinates]);
            mapTiles[coordinates].tile.ChangeMaterial(mapTiles[coordinates].tile.normal);
        }

        if (closedList.Contains(mapTiles[coordinates]))
        {
            closedList.Remove(mapTiles[coordinates]);
            mapTiles[coordinates].tile.ChangeMaterial(mapTiles[coordinates].tile.normal);
        }
    }

    private void TracePath()
    {
        pathPositions.Clear();
        var actualPosition = endCoordinates;
        lineRenderer.positionCount = 0;
        while(actualPosition != startCoordinates)
        {
            pathPositions.Add(GetWorld3DPosition(mapTiles[actualPosition].ToVector()));
            AddVertexToPath(actualPosition);
            actualPosition = mapTiles[actualPosition].aStarData.parent.ToVector();
        }
        pathPositions.Reverse();
        lineRenderer.enabled = true;
        pathSearched = true;
    }

    private void AddVertexToPath(Vector2Int actualPosition)
    {
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount -1, GetWorld3DPosition(actualPosition) + Vector3.up);
    }

    private IEnumerator MovePlayer()
    {
        int index = 0;
        GameObject player = tokens[0];
        while(index < pathPositions.Count)
        {
            player.transform.Translate(GetDirection(player.transform.position, pathPositions[index]).normalized / 10f);
            if (Vector3.Distance(player.transform.position, pathPositions[index]) < 0.1) index++;
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("Reached end");
    }

    private Vector3 GetDirection(Vector3 from, Vector3 to)
    {
        return to - from;
    }
    #endregion
}
