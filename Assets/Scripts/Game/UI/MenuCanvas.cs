using System.Collections;
using UnityEngine;

public class MenuCanvas : MonoBehaviour
{
    [Header("Tracking Targets")]
    public Transform leftHand;

    [Header("Stabilization Settings")]
    public float positionSmoothSpeed = 8f;
    public float rotationSmoothSpeed = 5f;

    [Header("Placement Offset")]
    public Vector3 positionalOffset = new Vector3(0f, 0.15f, 0f);

    [Header("Canvas Settings")]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] float fadeDuration = 0.5f;

    Transform mainCamera;

    void OnEnable()
    {
        mainCamera = Camera.main.transform;

        transform.position = leftHand.TransformPoint(positionalOffset);
        Vector3 directionToCamera = transform.position - mainCamera.position;
        directionToCamera.y = 0f; 
        transform.rotation = Quaternion.LookRotation(directionToCamera);

        StartCoroutine(ToggleCanvasOn());
    }

    void LateUpdate()
    {
        if (leftHand == null || mainCamera == null) return;

        UpdateSmoothedPosition();
        UpdateSmoothedRotation();
    }

    void UpdateSmoothedPosition()
    {
        Vector3 targetPosition = leftHand.TransformPoint(positionalOffset);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionSmoothSpeed);
    }

    void UpdateSmoothedRotation()
    {
        Vector3 directionToCamera = transform.position - mainCamera.position;
        directionToCamera.y = 0f; 

        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
        }
    }

    IEnumerator ToggleCanvasOn()
    {
        float timer = 0;
        while (timer <= fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1;
    }
}