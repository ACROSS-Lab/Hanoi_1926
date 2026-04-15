using System.Linq;
using UnityEngine;

public class Face_Controller : MonoBehaviour
{
    [Header("____________Mouth____________")]
    [Space(20)]
    public int MouthmaterialIndex = 1;
    private Material mouth_material;

    [Space(20)]
    public Vector3 Mouth_restLocalPos = new Vector3(0.25f, 0.5f, 0f);

    [Header("____________Eyes____________")]
    [Space(20)]
    public int EyesmaterialIndex = 1;
    private Material eyes_material;

    [Space(20)]
    public Vector3 Eyes_restLocalPos = new Vector3(0.25f, 0.5f, 0f);

    private SkinnedMeshRenderer skinnedMesh;
    private Transform mouthBone;
    private Transform eyesBone;

    void Start()
    {
        skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();

        mouthBone = GetComponentsInChildren<Transform>()
            .FirstOrDefault(t => t.name == "CTRL_Mouth");

        eyesBone = GetComponentsInChildren<Transform>()
            .FirstOrDefault(t => t.name == "CTRL_Eyes");

        if (mouthBone == null)
            Debug.LogError("CTRL_Mouth bone not found!");
        if (eyesBone == null)
            Debug.LogError("CTRL_Eyes bone not found!");

        if (skinnedMesh == null)
        {
            Debug.LogWarning("No SkinnedMeshRenderer found");
            return;
        }
        else
        {
            // Debug.Log("SkinnedMeshRenderer trouv� : " + skinnedMesh.name);
        }

        var mats = skinnedMesh.materials;

        // --- Mouth material ---
        if (MouthmaterialIndex < mats.Length)
        {
            mouth_material = mats[MouthmaterialIndex];
            // Debug.Log("Mouthmaterial trouv�");
        }
        else
        {
            Debug.LogError("MouthmaterialIndex hors limites");
        }

        // --- Eyes material ---
        if (EyesmaterialIndex < mats.Length)
        {
            eyes_material = mats[EyesmaterialIndex];
            // Debug.Log("Eyesmaterial trouv�");
        }
        else
        {
            Debug.LogError("EyesmaterialIndex hors limites");
        }
    }

    void Update()
    {
       if (mouth_material == null || eyes_material == null) return;

        Vector3 mouth_delta = mouthBone.localPosition - Mouth_restLocalPos;
        Vector3 eyes_delta = eyesBone.localPosition - Eyes_restLocalPos;

        //print(mouthBone.localPosition);
      

        // Mouth Calculation
        float mouth_rawX = Mathf.Clamp01(mouth_delta.x * 2f);
        float mouth_rawY = Mathf.Clamp01(mouth_delta.z * 2f);

        float mouth_stepX = Mathf.Floor(mouth_rawX * 4f) / 4f;
        float mouth_stepY = Mathf.Floor(mouth_rawY * 4f) / 4f;

        mouth_material.SetFloat("_OffsetX", mouth_stepX);
        mouth_material.SetFloat("_OffsetY", mouth_stepY);

        // Eyes Calculation
        float eyes_rawX = Mathf.Clamp01(eyes_delta.x * 2f);
        float eyes_rawY = Mathf.Clamp01(eyes_delta.z * 2f);

        float eyes_stepX = Mathf.Floor(eyes_rawX * 4f) / 4f;
        float eyes_stepY = Mathf.Floor(eyes_rawY * 4f) / 4f;

        eyes_material.SetFloat("_OffsetX", eyes_stepX);
        eyes_material.SetFloat("_OffsetY", eyes_stepY);
    }
}
