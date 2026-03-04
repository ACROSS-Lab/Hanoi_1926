using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class RenameToPrefabName : Editor
{
    // Modifiez le chemin pour utiliser "Tools/" au lieu de "GameObject/"
    private const string MENU_PATH = "Tools/Renommer en Nom du Prefab Parent";

    // Ajoute l'élément de menu à "Tools"
    [MenuItem(MENU_PATH, false, 0)]

    // Le 'validate'MenuItem permet de griser l'option si rien n'est sélectionné.
    [MenuItem(MENU_PATH, true)]
    static bool ValidateRenameSelected()
    {
        // Active si un ou plusieurs objets sont sélectionnés
        return Selection.gameObjects.Length > 0;
    }

    // Fonction principale appelée par l'élément de menu
    [MenuItem(MENU_PATH)]
    static void RenameSelected()
    {
        // Parcourt tous les objets sélectionnés
        foreach (GameObject go in Selection.gameObjects)
        {
            // Vérifie si l'objet est une instance de Prefab
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                // Obtient le Prefab source (l'Asset) à partir de l'instance
                Object prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(go);

                if (prefabAsset != null)
                {
                    // Enregistre l'action pour pouvoir l'annuler (Ctrl+Z)
                    Undo.RecordObject(go, "Renommer en nom de Prefab");

                    // Renomme l'objet sélectionné avec le nom de l'Asset Prefab
                    go.name = prefabAsset.name;

                    Debug.Log($"Renommage de '{go.name}' en '{prefabAsset.name}' (Nom du Prefab).");
                }
                else
                {
                    Debug.LogWarning($"L'objet '{go.name}' n'est pas lié à un Prefab Asset trouvable. Renommage ignoré.");
                }
            }
            else
            {
                Debug.LogWarning($"L'objet '{go.name}' n'est pas la racine d'une instance de Prefab. Renommage ignoré.");
            }
        }
    }
}