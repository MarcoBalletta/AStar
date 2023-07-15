using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(TokenGenerator))]
[RequireComponent(typeof(LineRenderer))]
public class AStarCalculator : MonoBehaviour
{
    private Directions dir = new Directions();
    private List<TileData> openList = new List<TileData>();
    private List<TileData> closedList = new List<TileData>();
    private LineRenderer lineRenderer;
    private List<Vector3> pathPositions = new List<Vector3>();
    private bool pathSearching = false;
    private bool pathSearched = false;
    [SerializeField] private KeyCode startPathCalculationKeycode;
    [SerializeField] private KeyCode startMovePlayerKeycode;
    [SerializeField] private KeyCode startPrefabPositionKeycode;
    private GridManager gridManager;
    [SerializeField] private Vector2Int startCoordinates;
    [SerializeField] private Vector2Int endCoordinates;
    [SerializeField] private Vector2Int lastCoordinates;
    private TokenGenerator tokenGenerator;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        TryGetComponent(out gridManager);
        TryGetComponent(out tokenGenerator);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(startPathCalculationKeycode) && !pathSearching)
        {
            //SearchNextStep(mapTiles[lastCoordinates]);
            SearchNextStepWhile(gridManager.MapTiles[lastCoordinates]);
        }
        if (Input.GetKeyDown(startPrefabPositionKeycode))
        {
            tokenGenerator.ClearTokens();
            if (startCoordinates != null)
            {
                ReturnToNormalTiles(startCoordinates);
                ReturnToNormalTiles(endCoordinates);
            }
            SetStartEndPosition();
        }
        if (Input.GetKeyDown(startMovePlayerKeycode) && tokenGenerator.Tokens.Count > 0 && pathSearched)
        {
            StartCoroutine(MovePlayer());
        }
    }

    private void SearchNextStepWhile(TileData actualData)
    {
        pathSearching = true;
        while (actualData.ToVector() != endCoordinates)
        {
            foreach (Vector2Int direction in dir.directions)
            {
                Vector2Int neighbourCell = new Vector2Int(actualData.row + direction.x, actualData.column + direction.y);
                if (IsClosed(gridManager.MapTiles[neighbourCell])) continue;
                if (gridManager.CheckGridBounds(neighbourCell)) continue;
                if (!gridManager.CheckWalkable(neighbourCell)) continue;

                float g = actualData.aStarData.g + Vector2.Distance(gridManager.GetWorld2DPosition(actualData.ToVector()), gridManager.GetWorld2DPosition(neighbourCell));
                float h = Vector2.Distance(gridManager.GetWorld2DPosition(neighbourCell), gridManager.GetWorld2DPosition(endCoordinates));
                float f = g + h;

                //setup TextMeshPro
                gridManager.MapTiles[neighbourCell].tile.UiManager.SetFGHValues(f, g, h);

                //WIP
                if (!UpdateTileData(gridManager.MapTiles[neighbourCell], g, h, f, actualData))
                {
                    AddToOpenList(neighbourCell);
                    //openList.Add(mapTiles[neighbourCell]);
                    gridManager.MapTiles[neighbourCell].aStarData.SetupAStarData(g, h, f, actualData);
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
            AddToClosedList(firstData.ToVector());
            openList.RemoveAt(0);
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
            if (IsClosed(gridManager.MapTiles[neighbourCell])) continue;
            if (gridManager.CheckGridBounds(neighbourCell)) continue;
            if (!gridManager.CheckWalkable(neighbourCell)) continue;

            float g = actualData.aStarData.g + Vector2.Distance(gridManager.GetWorld2DPosition(actualData.ToVector()), gridManager.GetWorld2DPosition(neighbourCell));
            float h = Vector2.Distance(gridManager.GetWorld2DPosition(neighbourCell), gridManager.GetWorld2DPosition(endCoordinates));
            float f = g + h;

            //setup TextMeshPro
            gridManager.MapTiles[neighbourCell].tile.UiManager.SetFGHValues(f, g, h);

            //WIP
            if (!UpdateTileData(gridManager.MapTiles[neighbourCell], g, h, f, actualData))
            {
                AddToOpenList(neighbourCell);
                //openList.Add(mapTiles[neighbourCell]);
                gridManager.MapTiles[neighbourCell].aStarData.SetupAStarData(g, h, f, actualData);
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
        openList.Add(gridManager.MapTiles[coordinates]);
        gridManager.MapTiles[coordinates].ChangeMaterial(gridManager.MapTiles[coordinates].tile.open);
    }

    private void AddToClosedList(Vector2Int coordinates)
    {
        closedList.Add(gridManager.MapTiles[coordinates]);
        gridManager.MapTiles[coordinates].ChangeMaterial(gridManager.MapTiles[coordinates].tile.closed);
    }

    public void ReturnToNormalTiles(Vector2Int coordinates)
    {
        if (openList.Contains(gridManager.MapTiles[coordinates]))
        {
            openList.Remove(gridManager.MapTiles[coordinates]);
            gridManager.MapTiles[coordinates].tile.ChangeMaterial(gridManager.MapTiles[coordinates].tile.normal);
        }

        if (closedList.Contains(gridManager.MapTiles[coordinates]))
        {
            closedList.Remove(gridManager.MapTiles[coordinates]);
            gridManager.MapTiles[coordinates].tile.ChangeMaterial(gridManager.MapTiles[coordinates].tile.normal);
        }
    }

    private void TracePath()
    {
        pathPositions.Clear();
        var actualPosition = endCoordinates;
        lineRenderer.positionCount = 0;
        while (actualPosition != startCoordinates)
        {
            pathPositions.Add(gridManager.GetWorld3DPosition(gridManager.MapTiles[actualPosition].ToVector()));
            AddVertexToPath(actualPosition);
            actualPosition = gridManager.MapTiles[actualPosition].aStarData.parent.ToVector();
        }
        pathPositions.Reverse();
        lineRenderer.enabled = true;
        pathSearched = true;
    }

    private void AddVertexToPath(Vector2Int actualPosition)
    {
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, gridManager.GetWorld3DPosition(actualPosition) + Vector3.up);
    }

    private IEnumerator MovePlayer()
    {
        int index = 0;
        GameObject player = tokenGenerator.Tokens[0];
        while (index < pathPositions.Count)
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

    private void SetStartEndPosition()
    {
        FindRandomPosition(ref startCoordinates, tokenGenerator.StartPrefab);
        tokenGenerator.SpawnToken(tokenGenerator.StartPrefab, startCoordinates);
        SetupStartCoordinates();
        do
        {
            FindRandomPosition(ref endCoordinates, tokenGenerator.EndPrefab);
        } while (endCoordinates == startCoordinates);

        tokenGenerator.SpawnToken(tokenGenerator.EndPrefab, endCoordinates);
    }

    private void SetupStartCoordinates()
    {
        lastCoordinates = startCoordinates;
        AddToClosedList(startCoordinates);
    }

    private void FindRandomPosition(ref Vector2Int tilePositionSelected, GameObject prefab)
    {
        bool foundPosition = false;
        int numberOfTiles = gridManager.MaxColumn * gridManager.MaxRow;
        List<Vector2Int> coordinatesExamined = new List<Vector2Int>();

        while (numberOfTiles > 0 && !foundPosition)
        {
            gridManager.GenerateRowAndColumnRandom(out Vector2Int positionOnGrid);
            //GenerateRowAndColumnRandom(out int rowGrid, out int columnGrid);
            numberOfTiles--;
            if (coordinatesExamined.Contains(positionOnGrid)) continue;
            coordinatesExamined.Add(positionOnGrid);
            if (gridManager.CheckIfTileIsWalkable(positionOnGrid))
            {
                foundPosition = true;
                tilePositionSelected = positionOnGrid;
            }
        }
    }
}
