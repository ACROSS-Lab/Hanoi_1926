using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkyviewMovement : MonoBehaviour
{   
    [SerializeField] InputActionReference rightStick;
    [SerializeField] InputActionReference leftStick;
    [SerializeField] float speed = 2;

    Transform camTransform;
    
    void Start()
    {
        camTransform = Camera.main.transform;
    }

    void Update()
    {
       MoveBackAndForth(); 
       MoveUpAndDown();
    }

    void MoveBackAndForth()
    {
        Vector2 rightStickValue = rightStick.action.ReadValue<Vector2>();
        if (Mathf.Abs(rightStickValue.y) < 0.1f) return;
        Vector3 forward = camTransform.forward;
        forward.y = 0;
        forward.Normalize();
        transform.position += rightStickValue.y * speed * Time.deltaTime * forward;
    }

    void MoveUpAndDown()
    {
        Vector2 leftStickValue = leftStick.action.ReadValue<Vector2>();
        if (Mathf.Abs(leftStickValue.y) < 0.1f) return;
        transform.position += leftStickValue.y * speed * Time.deltaTime * Vector3.up;
    }
}
