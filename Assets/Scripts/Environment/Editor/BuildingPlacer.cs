using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class BuildingPlacer : EditorWindow
{
    public string csvPath = "Assets/buildings.csv";
    public BuildingCategory[] categories;
    public float globalScale = 1f;
    [Range(0f, 0.5f)]
    public float overlapThreshold = 0.1f;

    private SerializedObject serializedObject;
    private SerializedProperty categoriesProperty;
    private List<Bounds> placedBounds = new List<Bounds>();

    [MenuItem("Tools/Place Buildings")]
    public static void ShowWindow()
    {
        var window = GetWindow<BuildingPlacer>("Building Placer");
        window.serializedObject = new SerializedObject(window);
        window.categoriesProperty = window.serializedObject.FindProperty("categories");
    }

    void OnGUI()
    {
        if (serializedObject == null)
        {
            serializedObject = new SerializedObject(this);
            categoriesProperty = serializedObject.FindProperty("categories");
        }

        GUILayout.Label("Building Placer", EditorStyles.boldLabel);
        GUILayout.Space(5);

        GUILayout.Label("CSV Path");
        csvPath = EditorGUILayout.TextField(csvPath);
        GUILayout.Space(5);

        globalScale = EditorGUILayout.FloatField("Global Scale", globalScale);
        GUILayout.Space(5);

        overlapThreshold = EditorGUILayout.Slider(
            $"Overlap Threshold ({Mathf.RoundToInt(overlapThreshold * 100)}%)",
            overlapThreshold, 0f, 0.5f);
        GUILayout.Space(10);

        serializedObject.Update();
        EditorGUILayout.PropertyField(categoriesProperty, new GUIContent("Categories"), true);
        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(10);

        if (GUILayout.Button("Place Buildings"))
            PlaceBuildings();

        GUILayout.Space(5);

        if (GUILayout.Button("Clear All Buildings"))
            ClearBuildings();
    }

    void PlaceBuildings()
    {
        if (categories == null || categories.Length == 0)
        {
            Debug.LogError("Aucune catégorie définie !");
            return;
        }

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSV introuvable : {csvPath}");
            return;
        }

        var parentGO = GameObject.Find("Buildings") ?? new GameObject("Buildings");
        parentGO.transform.localScale = Vector3.one;
        var parent = parentGO.transform;

        placedBounds.Clear();

        string[] lines;
        using (var fs = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var sr = new StreamReader(fs, System.Text.Encoding.UTF8))
        {
            lines = sr.ReadToEnd().Split('\n');
        }

        int placed = 0;
        int skipped = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var col = lines[i].Split(',');
            if (col.Length < 7) continue;

            string name = col[0].Trim();
            float cx = float.Parse(col[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            float cy = float.Parse(col[2].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            float width = float.Parse(col[4].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            float depth = float.Parse(col[5].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            float angle = float.Parse(col[6].Trim(), System.Globalization.CultureInfo.InvariantCulture);


            if (i == 1) // log uniquement la première ligne
            {
                Debug.Log($"name={name} cx={cx} cy={cy} angle={angle}");
                Debug.Log($"position Unity = ({-cx}, 0, {-cy})");
                Debug.Log($"rotation Unity = {Quaternion.Euler(0f, angle, 0f).eulerAngles}");
            }

            var prefab = PickPrefab(width, depth);
            if (prefab == null)
            {
                Debug.LogWarning($"Aucun prefab pour {name}");
                skipped++;
                continue;
            }

            // Pivot directement sur le bord du bloc, rotation perpendiculaire
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(-cx, 0f, -cy);
            go.transform.localRotation = Quaternion.Euler(0f, angle + 90f, 0f);

            var prefabBounds = GetPrefabBounds(prefab);
            float prefabW = prefabBounds.size.x * globalScale;
            float prefabD = prefabBounds.size.z * globalScale;

            int cols = Mathf.FloorToInt(width / prefabW);
            int rows = Mathf.FloorToInt(depth / prefabD);

            if (cols < 1 || rows < 1)
            {
                // Prefab trop grand — un seul centré sur le pivot
                Vector3 worldPos = go.transform.TransformPoint(Vector3.zero);
                if (TryPlace(prefab, worldPos, go.transform, prefabBounds, Vector3.zero, name + "_0"))
                    placed++;
                else
                {
                    DestroyImmediate(go);
                    skipped++;
                }
                continue;
            }

            // Grille qui part du bord (Z=0 local = façade sur le bord)
            // Les bâtiments s'étendent vers l'intérieur du bloc (Z positif local)
            float gridW = cols * prefabW;
            float offsetX = -(gridW - prefabW) / 2f;

            bool anyPlaced = false;
            for (int row = 0; row < rows; row++)
            {
                for (int col2 = 0; col2 < cols; col2++)
                {
                    float localX = offsetX + col2 * prefabW;
                    float localZ = row * prefabD; // part du bord, s'étend vers l'intérieur

                    Vector3 worldPos = go.transform.TransformPoint(new Vector3(localX, 0f, localZ));
                    var slotPrefab = PickPrefab(width, depth);
                    var slotBounds = GetPrefabBounds(slotPrefab);

                    if (TryPlace(slotPrefab, worldPos, go.transform, slotBounds,
                                 new Vector3(localX, 0f, localZ), $"{name}_{row}_{col2}"))
                        anyPlaced = true;
                }
            }

            if (!anyPlaced)
            {
                DestroyImmediate(go);
                skipped++;
            }
            else
            {
                placed++;
            }
        }

        Debug.Log($"✓ {placed} empreintes traitées, {skipped} ignorées.");
    }

    bool TryPlace(GameObject prefab, Vector3 worldPos, Transform parent,
                  Bounds prefabBounds, Vector3 localPos, string instanceName)
    {
        Vector3 scaledSize = new Vector3(
            prefabBounds.size.x * globalScale,
            prefabBounds.size.y * globalScale,
            prefabBounds.size.z * globalScale
        );

        Bounds futureBounds = new Bounds(worldPos, scaledSize);

        foreach (var existing in placedBounds)
        {
            if (!existing.Intersects(futureBounds)) continue;

            float overlapX = Mathf.Min(existing.max.x, futureBounds.max.x)
                           - Mathf.Max(existing.min.x, futureBounds.min.x);
            float overlapZ = Mathf.Min(existing.max.z, futureBounds.max.z)
                           - Mathf.Max(existing.min.z, futureBounds.min.z);

            if (overlapX <= 0 || overlapZ <= 0) continue;

            float overlapArea = overlapX * overlapZ;
            float futureArea = scaledSize.x * scaledSize.z;
            float overlapRatio = overlapArea / futureArea;

            if (overlapRatio > overlapThreshold)
                return false;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * globalScale;
        instance.name = instanceName;

        placedBounds.Add(futureBounds);
        return true;
    }

    Bounds GetPrefabBounds(GameObject prefab)
    {
        var renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);

        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    GameObject PickPrefab(float width, float depth)
    {
        float area = width * depth;

        BuildingCategory picked = null;
        float smallestValidMax = float.MaxValue;

        foreach (var cat in categories)
        {
            if (area <= cat.maxArea && cat.maxArea < smallestValidMax)
            {
                if (cat.prefabs != null && cat.prefabs.Length > 0)
                {
                    picked = cat;
                    smallestValidMax = cat.maxArea;
                }
            }
        }

        if (picked == null)
        {
            float largestMax = float.MinValue;
            foreach (var cat in categories)
            {
                if (cat.maxArea > largestMax && cat.prefabs != null && cat.prefabs.Length > 0)
                {
                    picked = cat;
                    largestMax = cat.maxArea;
                }
            }
        }

        if (picked == null) return null;

        return picked.prefabs[Random.Range(0, picked.prefabs.Length)];
    }

    void ClearBuildings()
    {
        var parent = GameObject.Find("Buildings");
        if (parent != null)
        {
            DestroyImmediate(parent);
            Debug.Log("Buildings supprimés.");
        }
        placedBounds.Clear();
    }
}