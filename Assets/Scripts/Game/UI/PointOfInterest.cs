using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;




#if UNITY_EDITOR
using UnityEditor;
#endif

public class PointOfInterest : MonoBehaviour 
{
    [Header("Visual Settings")]
    public GameObject canvas, sign, details;
    [SerializeField] float heightOffset = 0.001f;
    [HideInInspector] public int currentState = -1;
    public Sprite displayTexture;

    [Header("Audio Settings")]
    [SerializeField][Range(0f, 1f)] float volume = 0.1f;
    [SerializeField][Range(0.8f, 1.2f)] float pitch = 0.8f;
    [SerializeField] bool randomizePitch = true;
    [SerializeField][Range(0f, 0.2f)] float pitchVariance = 0.2f;
    
    Vector3 originalScale;
    Tween scaleTween, contentTween;
    CanvasGroup signGroup, detailsGroup;
    Transform canvasTransform, camTransform;
    AudioSource audioSource;
    XRSimpleInteractable interactable;
    MeshRenderer[] meshRenderers;

    static readonly int propertyID = Shader.PropertyToID("_Highlight");

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

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
    }

    void OnEnable()
    {
        POIManager.Register(this);        

        if (interactable != null)
        {
            interactable.selectEntered.AddListener(SelectEnter);
        }
    }

    void OnDisable()
    {   
        POIManager.Unregister(this);

        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(SelectEnter);
        }
    }

    void Start()
    {
        canvasTransform = canvas.transform;
        originalScale = canvasTransform.localScale;
        signGroup = sign.GetComponent<CanvasGroup>();
        detailsGroup = details.GetComponent<CanvasGroup>();
        camTransform = Camera.main.transform;
        audioSource = canvas.GetComponent<AudioSource>();
    }

    void Update()
    {
        if (currentState != 0)
        {
            Vector3 directionToCamera = camTransform.position - canvasTransform.position;
            canvasTransform.rotation = Quaternion.LookRotation(directionToCamera);
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

    public void SetHighlight(MaterialPropertyBlock propertyBlock, float value)
    {
        foreach (var renderer in meshRenderers)
        {
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(propertyID, value);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    void SetState0(float popDuration)
    {
        TriggerPopOut(popDuration);
    }

    void SetState1(float popDuration, float fadeDuration)
    {
        if (currentState == 0)
        {
            TriggerPopIn(popDuration);
            PlayPopSound();
        } 
        ShowSign(fadeDuration);
    }

    void SetState2(float popDuration, float fadeDuration)
    {
        if (currentState == 0)
        {
            TriggerPopIn(popDuration);
            PlayPopSound();
        } 
        ShowDetails(fadeDuration);
    }

    void TriggerPopIn(float duration)
    {
        scaleTween?.Kill();
        float ratio = canvasTransform.localScale.x / originalScale.x;
        float dynamicDuration = duration * Mathf.Clamp01(1 - ratio);
        scaleTween = canvasTransform.DOScale(originalScale, dynamicDuration)
                            .SetEase(Ease.OutBack)
                            .SetAutoKill(true);
    }

    void TriggerPopOut(float duration)
    {
        scaleTween?.Kill();
        float ratio = canvasTransform.localScale.x / originalScale.x;
        float dynamicDuration = duration * Mathf.Clamp01(ratio);
        scaleTween = canvasTransform.DOScale(Vector3.zero, dynamicDuration)
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

    void PlayPopSound()
    {
        if (audioSource == null || audioSource.clip == null) return;

        audioSource.pitch = randomizePitch
            ? pitch + Random.Range(-pitchVariance, pitchVariance)
            : pitch;

        audioSource.PlayOneShot(audioSource.clip, volume);
    }

    void SelectEnter(SelectEnterEventArgs args)
    {
        if (currentState == 0) return;
        POIManager.Instance.SetDisplayPOI(this);
    }
}
