using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(Animator))]
public class SplineWalker : MonoBehaviour
{
    public SplineContainer splineContainer;
    public float speed = 1f;

    Animator animator;
    float distanceTravelled;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorMove()
    {
        distanceTravelled += speed * Time.deltaTime;

        Vector3 pos = splineContainer.EvaluatePosition(distanceTravelled);
        Vector3 tangent = splineContainer.EvaluateTangent(distanceTravelled);

        // Garde le rebond vertical naturel de la marche
        pos.y += animator.deltaPosition.y;

        transform.position = pos;
        if (tangent != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(tangent);
    }
}