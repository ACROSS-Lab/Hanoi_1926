using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneChunker : MonoBehaviour
{
    [Header("Grid Configurations")]
    [Tooltip("The side length of each square chunk in meters.")]
    public float chunkSize = 50f;
    
    [Tooltip("Toggle the visual wireframe grid preview in the Scene View.")]
    public bool showGridPreview = true;

    [Header("Group name")]
    public string groupName = "Chunks";

    // Draws the preview grid in the Scene View when the object is selected
    private void OnDrawGizmosSelected()
    {
        if (!showGridPreview || chunkSize <= 0.00001f) return;

        // Color coding for the preview lines
        Gizmos.color = Color.red;

        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;
        bool hasChildren = false;

        // Determine bounds based strictly on immediate child root pivots
        foreach (Transform child in transform)
        {
            hasChildren = true;
            Vector3 pos = child.position;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        if (!hasChildren) return;

        // Determine bounding grid ranges
        int minX = Mathf.FloorToInt(min.x / chunkSize) - 1;
        int maxX = Mathf.CeilToInt(max.x / chunkSize) + 1;
        int minZ = Mathf.FloorToInt(min.z / chunkSize) - 1;
        int maxZ = Mathf.CeilToInt(max.z / chunkSize) + 1;

        float targetY = transform.position.y;

        // Draw grid lines along X axis
        for (int x = minX; x <= maxX; x++)
        {
            Vector3 start = new Vector3(x * chunkSize, targetY, minZ * chunkSize);
            Vector3 end = new Vector3(x * chunkSize, targetY, maxZ * chunkSize);
            Gizmos.DrawLine(start, end);
        }

        // Draw grid lines along Z axis
        for (int z = minZ; z <= maxZ; z++)
        {
            Vector3 start = new Vector3(minX * chunkSize, targetY, z * chunkSize);
            Vector3 end = new Vector3(maxX * chunkSize, targetY, z * chunkSize);
            Gizmos.DrawLine(start, end);
        }
    }

    /// <summary>
    /// Loops through immediate children and groups them under structural grid nodes.
    /// </summary>
    public void SplitIntoChunks()
    {
        // 1. Gather all direct children into a list first so we don't break the loop sequence mid-execution
        List<Transform> directChildren = new List<Transform>();
        foreach (Transform child in transform)
        {
            // Skip any chunks if the user runs the script multiple times
            if (child.name.StartsWith(groupName)) continue;
            directChildren.Add(child);
        }

        if (directChildren.Count == 0)
        {
            Debug.LogWarning("No raw child objects found directly underneath this parent to organize.");
            return;
        }

        Dictionary<string, Transform> chunkCache = new Dictionary<string, Transform>();
        int objectsMoved = 0;

        #if UNITY_EDITOR
        // 2. Process each item
        foreach (Transform child in directChildren)
        {
            // Calculate coordinates
            int gridX = Mathf.FloorToInt(child.position.x / chunkSize);
            int gridZ = Mathf.FloorToInt(child.position.z / chunkSize);
            string chunkKey = groupName + $"_{gridX}_{gridZ}";

            Transform chunkParent;

            if (!chunkCache.TryGetValue(chunkKey, out chunkParent))
            {
                // Check if the chunk folder object already exists under this parent
                chunkParent = transform.Find(chunkKey);

                if (chunkParent == null)
                {
                    GameObject newChunkGo = new GameObject(chunkKey);
                    chunkParent = newChunkGo.transform;
                    chunkParent.SetParent(transform);
                    
                    // Position chunk pivot neatly at the center base of its respective grid sector
                    chunkParent.position = new Vector3(
                        (gridX * chunkSize) + (chunkSize / 2f),
                        transform.position.y,
                        (gridZ * chunkSize) + (chunkSize / 2f)
                    );

                    // Track via Undo system so you can revert if needed
                    Undo.RegisterCreatedObjectUndo(newChunkGo, "Create Chunk Folder");
                }
                chunkCache[chunkKey] = chunkParent;
            }

            // Reparent securely while tracking operations
            Undo.SetTransformParent(child, chunkParent, "Move asset to grid chunk");
            objectsMoved++;
        }

        Debug.Log($"Successfully sorted {objectsMoved} root assets into {chunkCache.Count} distinct grid chunks.");
        #endif
    }
}

// Custom inspector interface logic
#if UNITY_EDITOR
[CustomEditor(typeof(SceneChunker))]
public class SceneChunkerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default properties (Chunk Size, Toggle Show Grid)
        DrawDefaultInspector();

        SceneChunker chunker = (SceneChunker)target;

        GUILayout.Space(18);
        
        // Render Execution Interface Button
        if (GUILayout.Button("Organize Hierarchy Into Chunks", GUILayout.Height(36)))
        {
            if (EditorUtility.DisplayDialog(
                "Confirm Scene Partitioning", 
                $"Are you sure you want to group your assets into structural {chunker.chunkSize}m × {chunker.chunkSize}m chunk nodes? This will reorder the immediate hierarchy layout.", 
                "Proceed", 
                "Cancel"))
            {
                chunker.SplitIntoChunks();
            }
        }
    }
}
#endif