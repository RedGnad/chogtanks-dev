using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Linq;

// Input data structure
public enum InputButtons
{
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8
}

public struct NetworkInputData : INetworkInput
{
    public InputButtons Buttons;
    public bool IsUp => (Buttons & InputButtons.Up) != 0;
    public bool IsDown => (Buttons & InputButtons.Down) != 0;
    public bool IsLeft => (Buttons & InputButtons.Left) != 0;
    public bool IsRight => (Buttons & InputButtons.Right) != 0;
}

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    // 🔧 Plus de référence locale au NetworkRunner - il sera géré par PhotonLauncher

    void Start()
    {
        Debug.Log("[FUSION] BasicSpawner initialized - waiting for PhotonLauncher to manage NetworkRunner");
        
        // 🔧 ARCHITECTURE FIXE : Ne plus créer de NetworkRunner ici
        // PhotonLauncher va créer le NetworkRunner ET attacher ce BasicSpawner comme callback
        // Cela évite les conflits de cycle de vie et les références cassées
        
        // Activer l'UI immédiatement puisque les composants Fusion sont prêts
        var lobbyUI = FindFirstObjectByType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnPhotonReady();
            Debug.Log("[FUSION] OnPhotonReady called - UI buttons activated");
            
            // 🎯 Timer pour cacher le loadingPanel
            StartCoroutine(HideLoadingPanelAfterDelay(lobbyUI, 2.6f));
        }
        else
        {
            Debug.LogError("[FUSION] LobbyUI not found!");
        }
        
        HideWaitPanel();
    }

    // 🎯 Coroutine pour cacher le loadingPanel après un délai
    private System.Collections.IEnumerator HideLoadingPanelAfterDelay(LobbyUI lobbyUI, float delay)
    {
        Debug.Log($"[FUSION] LoadingPanel timer started - will hide after {delay} seconds");
        yield return new WaitForSeconds(delay);
        
        if (lobbyUI != null && lobbyUI.loadingPanel != null)
        {
            Debug.Log($"[FUSION] Timer expired - hiding loadingPanel (was active: {lobbyUI.loadingPanel.activeInHierarchy})");
            lobbyUI.loadingPanel.SetActive(false);
            Debug.Log("✅ [FUSION] LoadingPanel hidden by timer!");
        }
        else
        {
            Debug.LogWarning("[FUSION] Cannot hide loadingPanel - LobbyUI or loadingPanel is null");
        }
    }

    private void HideWaitPanel()
    {
        Debug.Log("[FUSION] Attempting to hide Wait panel...");
        var waitPanel = GameObject.Find("Wait");
        Debug.Log($"[FUSION] Wait panel search result: {waitPanel}");
        
        if (waitPanel != null)
        {
            Debug.Log($"[FUSION] Wait panel found! Active state: {waitPanel.activeInHierarchy}");
            waitPanel.SetActive(false);
            Debug.Log("✅ [FUSION] Wait panel hidden!");
        }
        else
        {
            Debug.LogWarning("⚠️ [FUSION] Wait panel not found! Searching all GameObjects...");
            
            // Recherche alternative
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("Wait") || obj.name.Contains("wait"))
                {
                    Debug.Log($"[FUSION] Found potential wait object: {obj.name} (active: {obj.activeInHierarchy})");
                }
            }
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"✅ [FUSION] Player joined: {player}");
        
        // Activer l'UI (bouton GO → Brawl)
        Debug.Log("[FUSION] Searching for LobbyUI component...");
        var lobbyUI = FindFirstObjectByType<LobbyUI>();
        Debug.Log($"[FUSION] LobbyUI search result: {lobbyUI}");
        
        if (lobbyUI != null)
        {
            Debug.Log("[FUSION] Calling OnPhotonReady to activate UI buttons");
            lobbyUI.OnPhotonReady();
            Debug.Log("[FUSION] OnPhotonReady called successfully");
        }
        else
        {
            Debug.LogWarning("[FUSION] LobbyUI not found! Searching all objects...");
            
            // Recherche alternative
            var allLobbyUIs = Resources.FindObjectsOfTypeAll<LobbyUI>();
            Debug.Log($"[FUSION] Found {allLobbyUIs.Length} LobbyUI objects in Resources");
            
            foreach (var ui in allLobbyUIs)
            {
                Debug.Log($"[FUSION] LobbyUI found: {ui.name} (active: {ui.gameObject.activeInHierarchy})");
                if (ui.gameObject.activeInHierarchy)
                {
                    Debug.Log("[FUSION] Using active LobbyUI and calling OnPhotonReady");
                    ui.OnPhotonReady();
                    break;
                }
            }
        }
        
        // Cacher le panel Wait
        HideWaitPanel();
        
        // 🎯 SPAWNER LE TANK pour le joueur qui vient de rejoindre
        // En mode Shared, n'importe quel client peut spawner des objets
        Debug.Log($"[FUSION] Spawning tank for player {player}...");
        SpawnTank(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"❌ [FUSION] Player left: {player}");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        
        if (Input.GetKey(KeyCode.W))
            data.Buttons |= InputButtons.Up;
        if (Input.GetKey(KeyCode.S))
            data.Buttons |= InputButtons.Down;
        if (Input.GetKey(KeyCode.A))
            data.Buttons |= InputButtons.Left;
        if (Input.GetKey(KeyCode.D))
            data.Buttons |= InputButtons.Right;

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
{
    Debug.Log($"🔴 [FUSION] Shutdown: {shutdownReason}");
    
    // 🔧 FIX: Nettoyer les coroutines et références après shutdown pour éviter les interférences UI
    StopAllCoroutines();
    
    Debug.Log("[FUSION] BasicSpawner cleaned up after shutdown");
}

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("🟢 [FUSION] Connected to server!");
        
        // Hide the wait panel when connected
        var waitPanel = GameObject.Find("Wait");
        if (waitPanel != null)
        {
            waitPanel.SetActive(false);
            Debug.Log("✅ [FUSION] Wait panel hidden!");
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"🔴 [FUSION] Disconnected: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"❌ [FUSION] Connect failed: {reason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"📋 [FUSION] Session list updated: {sessionList.Count} sessions");
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("✅ [FUSION] Scene loaded!");
        
        // Also try to hide wait panel when scene loads
        var waitPanel = GameObject.Find("Wait");
        if (waitPanel != null)
        {
            waitPanel.SetActive(false);
            Debug.Log("✅ [FUSION] Wait panel hidden on scene load!");
        }
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("🔄 [FUSION] Loading scene...");
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    // 🎯 MÉTHODE DE SPAWN DE TANK
    private void SpawnTank(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[SPAWN] Attempting to spawn tank for player {player}...");
        
        // Trouver un point de spawn aléatoire
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Spawn");
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("[SPAWN] No spawn points found with tag 'Spawn'!");
            return;
        }
        
        Vector3 spawnPos = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform.position;
        
        // Ajouter un petit offset aléatoire pour éviter les collisions
        float offsetX = UnityEngine.Random.Range(-0.5f, 0.5f);
        float offsetY = UnityEngine.Random.Range(-0.5f, 0.5f);
        spawnPos += new Vector3(offsetX, offsetY, 0);
        
        // Charger le prefab tank depuis Resources
        GameObject tankPrefab = Resources.Load<GameObject>("TankPlayer");
        if (tankPrefab == null)
        {
            Debug.LogError("[SPAWN] TankPlayer prefab not found in Resources!");
            return;
        }
        
        // Spawner le tank via Fusion
        Debug.Log($"[SPAWN] Spawning tank at position {spawnPos} for player {player}");
        var tankNetworkObject = runner.Spawn(tankPrefab.GetComponent<NetworkObject>(), spawnPos, Quaternion.identity, player);
        
        if (tankNetworkObject != null)
        {
            Debug.Log($"✅ [SPAWN] Tank successfully spawned for player {player}!");
            
            // Configurer le nom du joueur si disponible
            var nameDisplay = tankNetworkObject.GetComponent<PlayerNameDisplay>();
            if (nameDisplay != null)
            {
                Debug.Log($"[SPAWN] PlayerNameDisplay configured for player {player}");
            }
            
            // 🌐 FUSION: Notifier NetworkUIManager si c'est le joueur local
            if (player == runner.LocalPlayer)
            {
                var networkUIManager = FindFirstObjectByType<NetworkUIManager>();
                if (networkUIManager != null)
                {
                    networkUIManager.NotifyLocalTankSpawned();
                    Debug.Log("[SPAWN] 🚗 Notification envoyée à NetworkUIManager pour tank local");
                    
                    // Mettre à jour la PlayerList après le spawn du tank
                    networkUIManager.UpdatePlayerList();
                    Debug.Log("[SPAWN] 📝 PlayerList mise à jour après spawn du tank");
                }
                else
                {
                    Debug.LogWarning("[SPAWN] ⚠️ NetworkUIManager non trouvé pour notification tank local");
                }
            }
        }
        else
        {
            Debug.LogError($"❌ [SPAWN] Failed to spawn tank for player {player}!");
        }
    }
}
