using DG.Tweening;
using UnityEngine;

public class Narrator : MonoBehaviour 
{
    [SerializeField] Animator animator;
    [SerializeField] LocalizedKey localizedKey;
    [SerializeField] AudioSource audioSource;
    [SerializeField] GameObject dialogueBox;
    [SerializeField] float smoothTurn = 10f;

    public Tween Move(Vector3 targetPosition, Vector3 offsetAtCenter, float targetScale, float flyDuration)
    {
        Vector3 midPoint = Vector3.Lerp(transform.position, targetPosition, 0.5f) + offsetAtCenter;
        Vector3[] path = {transform.position, midPoint, targetPosition};

        Sequence action = DOTween.Sequence();

        action.Join(transform.DOPath(path, flyDuration, PathType.CatmullRom).SetEase(Ease.InOutSine));
        action.Join(transform.DOScale(targetScale, flyDuration).SetEase(Ease.InOutSine));

        return action;
    }

    public float StartTalking(string key, int bodyState, int eyesState, int mouthStartState)
    {
        dialogueBox.SetActive(true);

        localizedKey.localizationKey = key;
        localizedKey.UpdateText();
        localizedKey.UpdateAudioClip();

        animator.SetInteger("BodyState", bodyState);
        animator.SetInteger("EyesState", eyesState);
        animator.SetInteger("MouthState", mouthStartState);

        audioSource.Play();

        float talkingTime = audioSource.clip.length;
        return talkingTime;
    }

    public void FinishDialogue(int mouthEndState)
    {
        animator.SetInteger("MouthState", mouthEndState);
    }

    public void DisableDialogueBox()
    {
        dialogueBox.SetActive(false);
    }

    void LateUpdate()
    {
        Vector3 direction = Camera.main.transform.position - transform.position;
        direction.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * smoothTurn);
    }
}