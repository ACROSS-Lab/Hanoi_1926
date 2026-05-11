using TMPro;
using UnityEngine;

public class PointOfInterest : MonoBehaviour 
{
    [SerializeField] string locationName;
    [SerializeField] Transform canvasTransform;
    [SerializeField] TextMeshProUGUI nameText;

    void Awake()
    {
        SetPostion();
    }

    void SetPostion()
    {
        Bounds bounds = GetComponent<MeshRenderer>().bounds;
        Vector3 center = bounds.center;
        float height = bounds.extents.y * 2;
        canvasTransform.position = new Vector3(center.x, height, center.z);
    }
}