using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class SpaceDeck : MonoBehaviour
{
    [SerializeField] GameObject deck;
    [SerializeField] Transform leftHand;
    [SerializeField] Vector3 offset; 
    [SerializeField] float YRotation;
    [SerializeField] float smoothSpeed;
    [SerializeField] float minViewDotProduct;

    [SerializeField] InputActionReference menuButton;

    void OnEnable()
    {
        menuButton.action.Enable();
    }

    void OnDisable()
    {
        menuButton.action.Disable();
    }

    void Start()
    {
        Vector3 targetPos = leftHand.position + leftHand.forward * offset.z + leftHand.right * offset.x + leftHand.up * offset.y;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        float yaw = leftHand.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0, yaw + YRotation, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        Vector3 handDirection = (leftHand.position - Camera.main.transform.position).normalized;
        float dot = Vector3.Dot(handDirection, Camera.main.transform.forward);

        if (dot < minViewDotProduct)
        {
            deck.SetActive(false);
        }
        else
        {
            deck.SetActive(true);
        }

        Vector3 targetPos = leftHand.position + leftHand.forward * offset.z + leftHand.right * offset.x + leftHand.up * offset.y;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        float yaw = leftHand.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0, yaw + YRotation, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }

    void Update()
    {
        if (menuButton.action.WasPressedThisFrame())
        {
            Debug.Log("Menu button pressed");
        }
    }
}
