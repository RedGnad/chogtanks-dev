using UnityEngine;
using UnityEditor;
using Fusion;

#if UNITY_EDITOR
public class FixTankShield : MonoBehaviour
{
    [MenuItem("Tools/Fix Tank Shield")]
    public static void AddTankShieldToPrefab()
    {
        // Charger le prefab TankPlayer
        GameObject prefab = Resources.Load<GameObject>("TankPlayer");
        if (prefab == null)
        {
            Debug.LogError("TankPlayer prefab not found in Resources!");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

        bool modified = false;

        // Ajouter TankShield si manquant
        if (prefabInstance.GetComponent<TankShield>() == null)
        {
            var tankShield = prefabInstance.AddComponent<TankShield>();
            Debug.Log("Added TankShield component to TankPlayer");
            modified = true;
        }

        // Vérifier et ajouter TankShield à la liste NetworkedBehaviours
        var networkObj = prefabInstance.GetComponent<NetworkObject>();
        if (networkObj != null)
        {
            var tankShield = prefabInstance.GetComponent<TankShield>();
            
            if (tankShield != null)
            {
                // Ajouter TankShield à la liste NetworkedBehaviours
                var networkedBehaviours = new System.Collections.Generic.List<NetworkBehaviour>();
                
                if (networkObj.NetworkedBehaviours != null)
                {
                    networkedBehaviours.AddRange(networkObj.NetworkedBehaviours);
                }
                
                if (!networkedBehaviours.Contains(tankShield))
                {
                    networkedBehaviours.Add(tankShield);
                    Debug.Log("Added TankShield to NetworkedBehaviours list");
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
            Debug.Log("✅ TankPlayer prefab fixed with TankShield!");
        }
        else
        {
            Debug.Log("TankPlayer prefab already has TankShield configured");
        }

        PrefabUtility.UnloadPrefabContents(prefabInstance);
        AssetDatabase.Refresh();
    }
}
#endif
