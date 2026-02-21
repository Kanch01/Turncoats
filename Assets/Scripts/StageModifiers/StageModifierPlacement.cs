using UnityEngine;
using UnityEngine.Tilemaps;

public enum PlacementMode
{
    Free,       // Can be anywhere
    Grid,       // Snap to tilemap/grid
}

public class StageModifierPlacement : MonoBehaviour
{
    public PlacementMode placementMode = PlacementMode.Free;
    public bool ignoreCollisions = false; // Some stage mods ignore collisions
    public bool mustBeOnTile = false;      // Only valid if on tilemap
    public Tilemap groundTilemap;          // Reference to tilemap if needed
}