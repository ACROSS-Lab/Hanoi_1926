using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PointOfInterest : MonoBehaviour 
{
    public GameObject canvas, sign, details;
    [SerializeField] float heightOffset = 0.001f;
    [HideInInspector] public int currentState = -1;

    #if UNITY_EDITOR
    void Reset()
    {
        SetUpCanvas();
    }

    void SetUpCanvas()
    {
        string prefabPath = "Assets/ASSETS/Prefabs/UI/POI_Canvas.prefab";
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabPath != null)
        {
            GameObject canvas = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            Transform canvasTransform = canvas.transform;
            canvasTransform.SetParent(transform);
            canvasTransform.localScale = Vector3.one;

            Bounds bounds = GetComponent<MeshRenderer>().bounds;
            Vector3 center = bounds.center;
            float height = bounds.extents.y * 2 + heightOffset;
            canvasTransform.position = new Vector3(center.x, height, center.z);

            this.canvas = canvas;
            sign = canvas.transform.Find("Sign").gameObject;
            details = canvas.transform.Find("Details").gameObject;

            sign.SetActive(false);
            details.SetActive(false);

            gameObject.AddComponent<LocalizedKey>().textComponent = details.GetComponentInChildren<TextMeshProUGUI>();

            Undo.RegisterCreatedObjectUndo(canvas, "Auto-add Prefab Component");
        }
    }
    #endif

    void OnEnable()
    {
        POIManager.Instance.Register(this);
    }

    void OnDisable()
    {   
        POIManager.Instance.Unregister(this);
    }
}
