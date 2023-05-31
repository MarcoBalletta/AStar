using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    public TileData data;

    public void Initialize(GridManager gridM, int rowInit, int columnInit, bool walkable, Tile tile)
    {
        data = new TileData(gridM, rowInit, columnInit, walkable, tile);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log(gameObject.name);
    }

    public void ChangeMaterial(Material material)
    {
        GetComponent<Renderer>().material = material;
    }
}
