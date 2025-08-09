using UnityEngine;
using UnityEditor;
using Fusion;

#if UNITY_EDITOR
public class FixShellPrefab : MonoBehaviour
{
    [MenuItem("Tools/Fix Shell Prefab")]
    public static void FixShellPrefabComponents()
    {
        // Charger le prefab Shell
        GameObject prefab = Resources.Load<GameObject>("Shell");
        if (prefab == null)
        {
            Debug.LogError("Shell prefab not found in Resources!");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

        bool modified = false;

        // Ajouter NetworkObject si manquant
        if (prefabInstance.GetComponent<NetworkObject>() == null)
        {
            var networkObject = prefabInstance.AddComponent<NetworkObject>();
            Debug.Log("Added NetworkObject component to Shell");
            modified = true;
        }

        // Vérifier et ajouter les NetworkBehaviour à la liste NetworkedBehaviours
        var networkObj = prefabInstance.GetComponent<NetworkObject>();
        if (networkObj != null)
        {
            var shellHandler = prefabInstance.GetComponent<ShellCollisionHandler>();
            
            if (shellHandler != null)
            {
                // Ajouter ShellCollisionHandler à la liste NetworkedBehaviours
                var networkedBehaviours = new System.Collections.Generic.List<NetworkBehaviour>();
                
                if (networkObj.NetworkedBehaviours != null)
                {
                    networkedBehaviours.AddRange(networkObj.NetworkedBehaviours);
                }
                
                if (!networkedBehaviours.Contains(shellHandler))
                {
                    networkedBehaviours.Add(shellHandler);
                    Debug.Log("Added ShellCollisionHandler to NetworkedBehaviours list");
                    modified = true;
                }
                
                // Mettre à jour la liste
                networkObj.NetworkedBehaviours = networkedBehaviours.ToArray();
            }
        }

        if (modified)
        {
            // Sauvegarder le prefab
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            Debug.Log("✅ Shell prefab fixed and saved!");
        }
        else
        {
            Debug.Log("Shell prefab already correctly configured");
        }

        PrefabUtility.UnloadPrefabContents(prefabInstance);
        AssetDatabase.Refresh();
    }
}
#endif
