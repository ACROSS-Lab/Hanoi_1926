using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PointOfInterest : MonoBehaviour 
{
    [SerializeField] GameObject canvas, sign, details;
    [SerializeField] float heightOffset = 0.001f;
    [SerializeField] float range1 = 1f, range2 = 0.5f;

    Transform camTransform;
    int currentState = -1;

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green; 
        Gizmos.DrawWireSphere(transform.position, range1);

        Gizmos.color = Color.red; 
        Gizmos.DrawWireSphere(transform.position, range2);
    }

    void Start()
    {
        camTransform = Camera.main.transform;
        EvaluateDistance();
    }

    void Update()
    {
        EvaluateDistance();
    }

    void LateUpdate()
    {
        if (currentState != 0)
        {
            Vector3 directionToCamera = camTransform.position - canvas.transform.position;
            canvas.transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }

    void EvaluateDistance()
    {
        float distance = Vector3.Distance(transform.position, camTransform.position);
        if (distance <= range2)
        {
            ChangeState(2);
        }
        else if (distance <= range1)
        {
            ChangeState(1);
        }
        else
        {
            ChangeState(0);
        }
    }

    void ChangeState(int newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case 0: 
                if (sign.activeSelf) sign.SetActive(false);
                if (details.activeSelf) details.SetActive(false);
                break;

            case 1: 
                if (!sign.activeSelf) sign.SetActive(true);
                if (details.activeSelf) details.SetActive(false);
                break;

            case 2: 
                if (sign.activeSelf) sign.SetActive(false);
                if (!details.activeSelf) details.SetActive(true);
                break;
        }
    }

}
