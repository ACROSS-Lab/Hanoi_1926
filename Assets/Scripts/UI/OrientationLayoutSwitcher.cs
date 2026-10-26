using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bascule les éléments d'un menu entre un conteneur horizontal (paysage)
/// et un conteneur vertical (portrait) selon l'orientation de l'écran.
/// </summary>
public class OrientationLayoutSwitcher : MonoBehaviour
{
    [Header("Conteneurs")]
    [Tooltip("Objet avec un HorizontalLayoutGroup (affichage côte à côte)")]
    [SerializeField] private RectTransform landscapeContainer;

    [Tooltip("Objet avec un VerticalLayoutGroup (affichage l'un sur l'autre)")]
    [SerializeField] private RectTransform portraitContainer;

    [Header("Éléments du menu (dans l'ordre d'affichage)")]
    [SerializeField] private RectTransform[] items;

    private bool? isPortrait;

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        // Vérification peu coûteuse, ne fait rien tant que l'orientation ne change pas
        Refresh();
    }

    private void Refresh()
    {
        bool portrait = Screen.height > Screen.width;

        if (isPortrait.HasValue && isPortrait.Value == portrait)
            return;

        isPortrait = portrait;
        Apply(portrait);
    }

    private void Apply(bool portrait)
    {
        RectTransform target = portrait ? portraitContainer : landscapeContainer;
        RectTransform other = portrait ? landscapeContainer : portraitContainer;

        target.gameObject.SetActive(true);

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            items[i].SetParent(target, false);
            items[i].SetSiblingIndex(i);
        }

        other.gameObject.SetActive(false);

        // Force le recalcul immédiat de la mise en page
        LayoutRebuilder.ForceRebuildLayoutImmediate(target);
    }
}
