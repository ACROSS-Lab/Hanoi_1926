using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class PointOfInterest : MonoBehaviour 
{
    public GameObject canvas, sign, details;
    [SerializeField] float heightOffset = 0.001f;
    [HideInInspector] public int currentState = -1;
    
    Vector3 originalScale;
    Tween scaleTween, contentTween;
    CanvasGroup signGroup, detailsGroup;
    Transform camTransform;

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

            MeshRenderer fisrtMesh = GetComponentsInChildren<MeshRenderer>()[0];
            Bounds bounds = fisrtMesh.bounds;
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
        POIManager.Register(this);

        originalScale = transform.localScale;
        signGroup = sign.GetComponent<CanvasGroup>();
        detailsGroup = details.GetComponent<CanvasGroup>();

        camTransform = Camera.main.transform;
    }

    void OnDisable()
    {   
        POIManager.Unregister(this);
    }

    void Update()
    {
        if (currentState != 0)
        {
            Vector3 directionToCamera = camTransform.position - canvas.transform.position;
            canvas.transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }

    public void ChangeState(int newState, float popDuration, float fadeDuration)
    {
        if (currentState == newState) return;

        switch(newState)
        {
            case 0:
                SetState0(popDuration);
                break;
            case 1:
                SetState1(popDuration, fadeDuration);
                break;
            case 2:
                SetState2(popDuration, fadeDuration);
                break;
        }
        
        currentState = newState;
    }

    void SetState0(float popDuration)
    {
        TriggerPopOut(popDuration);
    }

    void SetState1(float popDuration, float fadeDuration)
    {
        if (currentState == 0) TriggerPopIn(popDuration);
        ShowSign(fadeDuration);
    }

    void SetState2(float popDuration, float fadeDuration)
    {
        if (currentState == 0) TriggerPopIn(popDuration);
        ShowDetails(fadeDuration);
    }

    void TriggerPopIn(float duration)
    {
        scaleTween?.Kill();
        float ratio = transform.localScale.x / originalScale.x;
        float dynamicDuration = duration * Mathf.Clamp01(1 - ratio);
        scaleTween = transform.DOScale(originalScale, dynamicDuration)
                            .SetEase(Ease.OutBack)
                            .SetAutoKill(true);
    }

    void TriggerPopOut(float duration)
    {
        scaleTween?.Kill();
        float ratio = transform.localScale.x / originalScale.x;
        float dynamicDuration = duration * Mathf.Clamp01(ratio);
        scaleTween = transform.DOScale(Vector3.zero, dynamicDuration)
                            .SetEase(Ease.InBack)
                            .SetAutoKill(true)
                            .OnComplete(() => {sign.SetActive(false); details.SetActive(false);});
    }

    void ShowDetails(float duration)
    {
        contentTween?.Kill();
        details.SetActive(true);
        contentTween = DOTween.Sequence()
            .Join(signGroup.DOFade(0f, duration))
            .Join(detailsGroup.DOFade(1f, duration))
            .OnStart(() => {detailsGroup.alpha = 0f;})
            .OnComplete(() => {sign.SetActive(false);});
    }

    void ShowSign(float duration)
    {
        contentTween?.Kill();
        sign.SetActive(true);
        contentTween = DOTween.Sequence()
            .Join(detailsGroup.DOFade(0f, duration))
            .Join(signGroup.DOFade(1f, duration))
            .OnStart(() => {signGroup.alpha = 0f;})
            .OnComplete(() => {details.SetActive(false);});
    }
}
