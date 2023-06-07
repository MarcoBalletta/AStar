using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TileUIManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI f;
    [SerializeField] TextMeshProUGUI g;
    [SerializeField] TextMeshProUGUI h;

    public void SetFGHValues(float f, float g, float h)
    {
        this.f.text = $"F: {f.ToString("0.00")}";
        this.g.text = $"G: {g.ToString("0.00")}";
        this.h.text = $"H: {h.ToString("0.00")}";
    }
}
