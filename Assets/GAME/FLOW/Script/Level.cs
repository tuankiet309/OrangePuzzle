using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Scriptable Objects/Level")]
public class Level : ScriptableObject
{
    [Header("Dimension Size")]
    public int dimension;

    [Header("Positions in this Dimension")]
    public List<Vector2Int> positions = new List<Vector2Int>();

}
