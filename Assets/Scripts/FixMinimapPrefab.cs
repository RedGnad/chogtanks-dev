#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Script utilitaire pour ajouter automatiquement le composant MinimapIcon au prefab TankPlayer
/// Exécuter via: Tools > Fix Tank Prefab > Add Minimap Icon
/// </summary>
public class FixMinimapPrefab : MonoBehaviour
{
    [MenuItem("Tools/Fix Tank Prefab/Add Minimap Icon")]
    public static void AddMinimapIconToPrefab()
    {
        string prefabPath = "Assets/Photon/PhotonUnityNetworking/Resources/TankPlayer.prefab";
        
        // Charger le prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[MINIMAP_FIX] Prefab TankPlayer introuvable à: {prefabPath}");
            return;
        }
        
        // Vérifier si MinimapIcon existe déjà
        MinimapIcon existingIcon = prefab.GetComponent<MinimapIcon>();
        if (existingIcon != null)
        {
            Debug.Log("[MINIMAP_FIX] ✅ MinimapIcon déjà présent sur TankPlayer");
            return;
        }
        
        // Ajouter le composant MinimapIcon
        try
        {
            // Créer une instance temporaire pour modification
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            
            // Ajouter MinimapIcon
            MinimapIcon minimapIcon = instance.AddComponent<MinimapIcon>();
            
            Debug.Log("[MINIMAP_FIX] ✅ MinimapIcon ajouté au prefab TankPlayer");
            
            // Sauvegarder les modifications dans le prefab
            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            
            // Nettoyer l'instance temporaire
            DestroyImmediate(instance);
            
            // Forcer la sauvegarde
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[MINIMAP_FIX] 🎯 Prefab TankPlayer mis à jour avec succès !");
            Debug.Log("[MINIMAP_FIX] 📍 La minimap devrait maintenant afficher les icônes des tanks");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MINIMAP_FIX] ❌ Erreur lors de l'ajout de MinimapIcon: {e.Message}");
        }
    }
    
    [MenuItem("Tools/Fix Tank Prefab/Validate Minimap Setup")]
    public static void ValidateMinimapSetup()
    {
        Debug.Log("[MINIMAP_VALIDATION] 🔍 Validation de la configuration minimap...");
        
        // Vérifier le prefab TankPlayer
        string prefabPath = "Assets/Photon/PhotonUnityNetworking/Resources/TankPlayer.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError("[MINIMAP_VALIDATION] ❌ Prefab TankPlayer introuvable");
            return;
        }
        
        // Vérifier MinimapIcon
        MinimapIcon minimapIcon = prefab.GetComponent<MinimapIcon>();
        if (minimapIcon != null)
        {
            Debug.Log("[MINIMAP_VALIDATION] ✅ MinimapIcon présent sur TankPlayer");
        }
        else
        {
            Debug.LogWarning("[MINIMAP_VALIDATION] ⚠️ MinimapIcon manquant sur TankPlayer");
        }
        
        // Vérifier MinimapCamera dans la scène
        MinimapCamera minimapCamera = FindFirstObjectByType<MinimapCamera>();
        if (minimapCamera != null)
        {
            Debug.Log("[MINIMAP_VALIDATION] ✅ MinimapCamera trouvée dans la scène");
            
            Camera cam = minimapCamera.GetComponent<Camera>();
            if (cam != null)
            {
                Debug.Log($"[MINIMAP_VALIDATION] 📹 Caméra minimap: enabled={cam.enabled}, rect={cam.rect}");
            }
        }
        else
        {
            Debug.LogWarning("[MINIMAP_VALIDATION] ⚠️ MinimapCamera introuvable dans la scène");
        }
        
        // Vérifier le layer Minimap
        int minimapLayer = LayerMask.NameToLayer("Minimap");
        if (minimapLayer == -1)
        {
            Debug.LogWarning("[MINIMAP_VALIDATION] ⚠️ Layer 'Minimap' n'existe pas - créez-le dans Project Settings > Tags and Layers");
        }
        else
        {
            Debug.Log($"[MINIMAP_VALIDATION] ✅ Layer 'Minimap' trouvé (index: {minimapLayer})");
        }
        
        Debug.Log("[MINIMAP_VALIDATION] 🔍 Validation terminée");
    }
}
#endif
