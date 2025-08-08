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
    private NetworkRunner _runner;

    async void Start()
    {
        Debug.Log("[FUSION] Starting Fusion session...");
        
        // Créer le NetworkRunner comme avant
        _runner = GetComponent<NetworkRunner>();
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
        }
        
        _runner.ProvideInput = true;

        var sceneManager = GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
        {
            sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        // ❌ DÉSACTIVÉ : Ne plus démarrer Fusion automatiquement pour éviter "NetworkRunner should not be reused"
        // PhotonLauncher va gérer les sessions Fusion (création/join de rooms)
        Debug.Log("✅ [FUSION] NetworkRunner prepared, waiting for PhotonLauncher to start session...");
        
        // Activer l'UI immédiatement puisqu'on n'attend plus que Fusion démarre
        var lobbyUI = FindFirstObjectByType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnPhotonReady();
            Debug.Log("[FUSION] OnPhotonReady called - UI buttons activated");
            
            // 🎯 SOLUTION SIMPLE : Timer de 2 secondes pour cacher le loadingPanel
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
}
