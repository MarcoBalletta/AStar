using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TokenGenerator : MonoBehaviour
{
    [SerializeField] private GameObject startPrefab;
    [SerializeField] private GameObject endPrefab;
    private GridManager gridManager;
    private List<GameObject> tokens = new List<GameObject>();

    public List<GameObject> Tokens { get => tokens; }
    public GameObject StartPrefab { get => startPrefab; }
    public GameObject EndPrefab { get => endPrefab; }

    private void Start()
    {
        TryGetComponent(out gridManager);
    }

    public void SpawnToken(GameObject prefab, Vector2Int coordinate)
    {
        var token = Instantiate(prefab, gridManager.GetWorld3DPosition(coordinate), Quaternion.identity);
        tokens.Add(token);
    }

    public void ClearTokens()
    {
        while (tokens.Count > 0)
        {
            var item = tokens[0];
            tokens.RemoveAt(0);
            Destroy(item);
        }
    }
}
