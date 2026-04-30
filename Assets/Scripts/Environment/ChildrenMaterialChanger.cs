using UnityEngine;

/// <summary>
/// Changes the material on all children of this object.
/// Public methods are compatible with Unity Events (they appear in the Inspector).
/// </summary>
public class ChildrenMaterialChanger : MonoBehaviour
{
    [Header("Default Material (optional)")]
    [Tooltip("Material applied when calling ApplyDefaultMaterial()")]
    [SerializeField] private Material defaultMaterial;

    [Header("Options")]
    [Tooltip("If enabled, also includes this object's own Renderer, not just its children")]
    [SerializeField] private bool includeSelf = false;

    // -------------------------------------------------------------------------
    // PUBLIC METHODS — usable via Unity Events
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies a material to all children (and optionally this object).
    /// Can be called from a Unity Event by passing a Material as a parameter.
    /// </summary>
    /// <param name="newMaterial">The new material to apply.</param>
    public void ApplyMaterialToChildren(Material newMaterial)
    {
        if (newMaterial == null)
        {
            Debug.LogWarning($"[ChildrenMaterialChanger] No material provided on {gameObject.name}.");
            return;
        }

        Renderer[] renderers = GetTargetRenderers();

        foreach (Renderer rend in renderers)
        {
            rend.material = newMaterial;
        }

        Debug.Log($"[ChildrenMaterialChanger] Material '{newMaterial.name}' applied to {renderers.Length} renderer(s) on {gameObject.name}.");
    }

    /// <summary>
    /// Applies the default material assigned in the Inspector.
    /// Can be called from a Unity Event with no parameter.
    /// </summary>
    public void ApplyDefaultMaterial()
    {
        if (defaultMaterial == null)
        {
            Debug.LogWarning($"[ChildrenMaterialChanger] No default material assigned on {gameObject.name}.");
            return;
        }

        ApplyMaterialToChildren(defaultMaterial);
    }

    /// <summary>
    /// Hides all children by disabling their Renderers.
    /// </summary>
    public void HideAllChildren()
    {
        SetRenderersEnabled(false);
    }

    /// <summary>
    /// Shows all children by enabling their Renderers.
    /// </summary>
    public void ShowAllChildren()
    {
        SetRenderersEnabled(true);
    }

    // -------------------------------------------------------------------------
    // PRIVATE METHODS
    // -------------------------------------------------------------------------

    /// <summary>
    /// Retrieves all targeted Renderers based on the current options.
    /// </summary>
    private Renderer[] GetTargetRenderers()
    {
        if (includeSelf)
        {
            return GetComponentsInChildren<Renderer>(includeInactive: true);
        }
        else
        {
            // Exclude this object's own Renderer, only target children
            Renderer[] all = GetComponentsInChildren<Renderer>(includeInactive: true);
            Renderer self = GetComponent<Renderer>();

            if (self == null) return all;

            System.Collections.Generic.List<Renderer> children = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in all)
            {
                if (r != self) children.Add(r);
            }
            return children.ToArray();
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        Renderer[] renderers = GetTargetRenderers();
        foreach (Renderer rend in renderers)
        {
            rend.enabled = enabled;
        }
    }
}
