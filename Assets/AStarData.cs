using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class AStarData
{
    public float f, g, h;
    public Material open;
    public Material closed;
    public Material normal;
    public TileData parent;

    public void SetupAStarData(float g, float h, float f, TileData parent)
    {
        this.g = g;
        this.h = h;
        this.f = f;
        this.parent = parent;
        closed = Resources.Load<Material>("/Materials/Closed");
        open = Resources.Load<Material>("/Materials/Open");
        normal = Resources.Load<Material>("/Materials/Tiles");
    }
}

