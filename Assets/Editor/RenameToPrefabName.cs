using UnityEngine;
using UnityEditor;

public class RenameToPrefabName
{
    private const string MENU_PATH = "Tools/Renommer en Nom du Prefab Parent";

    [MenuItem(MENU_PATH)]
    static void RenameSelected()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(go);

                if (prefabAsset != null)
                {
                    Undo.RegisterCompleteObjectUndo(go, "Renommer en nom de Prefab");

                    string oldName = go.name;
                    go.name = prefabAsset.name;

                    Debug.Log($"Renommage de '{oldName}' en '{prefabAsset.name}'.");
                }
                else
                {
                    Debug.LogWarning($"L'objet '{go.name}' n'est pas lié à un Prefab Asset trouvable.");
                }
            }
            else
            {
                Debug.LogWarning($"L'objet '{go.name}' n'est pas la racine d'une instance de Prefab.");
            }
        }
    }

    [MenuItem(MENU_PATH, true)]
    static bool ValidateRenameSelected()
    {
        return Selection.gameObjects.Length > 0;
    }
}