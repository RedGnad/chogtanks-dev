using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class FixTankPrefab : MonoBehaviour
{
    [MenuItem("Tools/Fix Tank Prefab")]
    public static void FixTankPrefabComponents()
    {
        // Charger le prefab TankPlayer
        GameObject prefab = Resources.Load<GameObject>("TankPlayer");
        if (prefab == null)
        {
            Debug.LogError("TankPlayer prefab not found in Resources!");
            return;
        }

        // Obtenir le chemin du prefab
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        Debug.Log($"Found TankPlayer prefab at: {prefabPath}");

        // Ouvrir le prefab pour modification
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
        
        // Ajouter les scripts NetworkBehaviour manquants
        if (prefabInstance.GetComponent<TankMovement2D>() == null)
        {
            prefabInstance.AddComponent<TankMovement2D>();
            Debug.Log("Added TankMovement2D component");
        }
        
        if (prefabInstance.GetComponent<TankShield>() == null)
        {
            prefabInstance.AddComponent<TankShield>();
            Debug.Log("Added TankShield component");
        }
        
        if (prefabInstance.GetComponent<TankShoot2D>() == null)
        {
            prefabInstance.AddComponent<TankShoot2D>();
            Debug.Log("Added TankShoot2D component");
        }
        
        if (prefabInstance.GetComponent<TankHealth2D>() == null)
        {
            prefabInstance.AddComponent<TankHealth2D>();
            Debug.Log("Added TankHealth2D component");
        }
        
        if (prefabInstance.GetComponent<SimpleTankRespawn>() == null)
        {
            prefabInstance.AddComponent<SimpleTankRespawn>();
            Debug.Log("Added SimpleTankRespawn component");
        }
        
        if (prefabInstance.GetComponent<TankAppearanceHandler>() == null)
        {
            prefabInstance.AddComponent<TankAppearanceHandler>();
            Debug.Log("Added TankAppearanceHandler component");
        }

        // Sauvegarder les modifications
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabInstance);
        
        Debug.Log("✅ TankPlayer prefab fixed successfully!");
        AssetDatabase.Refresh();
    }
}
#endif
