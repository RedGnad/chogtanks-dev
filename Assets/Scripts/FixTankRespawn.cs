#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Fusion;

/// <summary>
/// Script utilitaire pour corriger le prefab TankPlayer en ajoutant le composant SimpleTankRespawn manquant
/// </summary>
public class FixTankRespawn : EditorWindow
{
    [MenuItem("Tools/Fix Tank Respawn Component")]
    public static void ShowWindow()
    {
        GetWindow<FixTankRespawn>("Fix Tank Respawn");
    }

    void OnGUI()
    {
        GUILayout.Label("Fix Tank Respawn Component", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("Ce script ajoute le composant SimpleTankRespawn manquant au prefab TankPlayer.");
        GUILayout.Space(10);
        
        if (GUILayout.Button("Fix TankPlayer Prefab"))
        {
            FixTankPlayerPrefab();
        }
    }

    static void FixTankPlayerPrefab()
    {
        // Charger le prefab TankPlayer
        string prefabPath = "Assets/Photon/PhotonUnityNetworking/Resources/TankPlayer.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"[FIX] Prefab TankPlayer non trouvé à : {prefabPath}");
            return;
        }

        Debug.Log($"[FIX] Prefab TankPlayer trouvé : {prefab.name}");

        // Vérifier si SimpleTankRespawn existe déjà
        SimpleTankRespawn existingRespawn = prefab.GetComponent<SimpleTankRespawn>();
        if (existingRespawn != null)
        {
            Debug.Log("[FIX] SimpleTankRespawn déjà présent sur le prefab");
            return;
        }

        // Ajouter le composant SimpleTankRespawn
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject prefabRoot = editingScope.prefabContentsRoot;
            
            // Ajouter SimpleTankRespawn
            SimpleTankRespawn respawnComponent = prefabRoot.AddComponent<SimpleTankRespawn>();
            Debug.Log("[FIX] Composant SimpleTankRespawn ajouté au prefab");

            // Vérifier si NetworkObject existe et enregistrer SimpleTankRespawn
            NetworkObject networkObject = prefabRoot.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                Debug.Log("[FIX] NetworkObject trouvé - SimpleTankRespawn sera automatiquement enregistré");
            }
            else
            {
                Debug.LogWarning("[FIX] NetworkObject non trouvé sur le prefab");
            }
        }

        // Sauvegarder les changements
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("✅ [FIX] Prefab TankPlayer corrigé avec succès !");
        Debug.Log("[FIX] Composants ajoutés : SimpleTankRespawn");
    }
}
#endif
