using System;
using System.Linq;
using UnityEngine;
using Fusion;

/// <summary>
/// NetworkUIManager - Objet temporaire NetworkBehaviour qui gère la synchronisation UI
/// Créé/détruit à chaque session Fusion, communique avec LobbyUI persistant via events
/// Gère aussi le timer de match synchronisé entre tous les clients
/// </summary>
public class NetworkUIManager : NetworkBehaviour
{
    #region Networked Properties - Timer synchronisé
    
    /// <summary>Temps restant du match en secondes (synchronisé réseau)</summary>
    [Networked] public float MatchTimeRemaining { get; set; } = 300f; // 5 minutes par défaut
    
    /// <summary>Indique si le timer du match est actif</summary>
    [Networked] public bool IsMatchTimerActive { get; set; } = false;
    
    /// <summary>Timestamp de démarrage du match (pour calculs précis)</summary>
    [Networked] public float MatchStartTime { get; set; } = 0f;
    
    #endregion
    
    #region Events - Communication avec UI persistante
    
    /// <summary>Déclenché quand un joueur rejoint la room</summary>
    public static event Action<string> OnRoomJoined;
    
    /// <summary>Déclenché quand le joueur local quitte la room</summary>
    public static event Action OnRoomLeft;
    
    /// <summary>Déclenché quand le tank local est spawné</summary>
    public static event Action OnLocalTankSpawned;
    
    /// <summary>Déclenché pour mettre à jour la liste des joueurs</summary>
    public static event Action<string> OnPlayerListUpdated;
    
    /// <summary>Déclenché pour mettre à jour le timer de match</summary>
    public static event Action<float> OnMatchTimerUpdated;
    
    /// <summary>Déclenché pour afficher un message dans le killfeed</summary>
    public static event Action<string> OnKillFeedMessage;
    
    #endregion

    #region NetworkBehaviour Lifecycle
    
    public override void Spawned()
    {
        Debug.Log("[NETWORK_UI] 🌐 NetworkUIManager spawned - prêt pour la synchronisation UI");
        
        // Notifier LobbyUI que la room est rejointe
        string roomName = Runner.SessionInfo.Name;
        OnRoomJoined?.Invoke(roomName);
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Debug.Log("[NETWORK_UI] 💥 NetworkUIManager despawned");
        
        // Notifier LobbyUI que la room est quittée
        OnRoomLeft?.Invoke();
    }
    
    #endregion

    #region RPCs - Synchronisation UI entre clients
    
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void UpdatePlayerListRpc(string playerList)
    {
        Debug.Log($"[NETWORK_UI] 📝 UpdatePlayerListRpc reçu: '{playerList}'");
        Debug.Log($"[NETWORK_UI] 📝 OnPlayerListUpdated subscribers: {OnPlayerListUpdated?.GetInvocationList().Length ?? 0}");
        OnPlayerListUpdated?.Invoke(playerList);
        Debug.Log($"[NETWORK_UI] ✅ Event OnPlayerListUpdated déclenché");
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void UpdateMatchTimerRpc(float timeRemaining)
    {
        OnMatchTimerUpdated?.Invoke(timeRemaining);
    }
    
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void ShowKillFeedMessageRpc(string message)
    {
        Debug.Log($"[NETWORK_UI] 💀 KillFeed: {message}");
        OnKillFeedMessage?.Invoke(message);
    }
    
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RestartMatchSoftRPC()
    {
        Debug.Log("[NETWORK_UI] 🔄 RestartMatchSoft RPC called");
        
        // Nettoyer les UI GameOver
        foreach (var ui in GameObject.FindGameObjectsWithTag("GameOverUI"))
        {
            Destroy(ui);
        }

        // Reset minimap
        var minimapCam = FindFirstObjectByType<MinimapCamera>();
        if (minimapCam != null)
        {
            minimapCam.ForceReset();
        }

        // Despawn tank actuel
        TankHealth2D myTank = null;
        foreach (var t in FindObjectsByType<TankHealth2D>(FindObjectsSortMode.None))
        {
            if (t.Object && t.Object.HasInputAuthority)
            {
                myTank = t;
                break;
            }
        }
        if (myTank != null)
        {
            Runner.Despawn(myTank.Object);
        }
    }
    
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void ShowWinnerToAllRPC(string winnerName, int winnerActorNumber)
    {
        Debug.Log($"[NETWORK_UI] 🏆 ShowWinner RPC: {winnerName} (ID: {winnerActorNumber})");
        
        bool isWinner = Runner.LocalPlayer.PlayerId == winnerActorNumber;
        
        // Trouver le prefab GameOverUI
        GameObject prefabToUse = null;
        var tankHealth = FindFirstObjectByType<TankHealth2D>();
        if (tankHealth != null)
        {
            var field = typeof(TankHealth2D).GetField("gameOverUIPrefab", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                prefabToUse = field.GetValue(tankHealth) as GameObject;
            }
        }
        
        // Créer l'UI GameOver
        Camera mainCam = Camera.main;
        if (mainCam != null && prefabToUse != null)
        {
            GameObject uiInstance = Instantiate(prefabToUse, mainCam.transform);
            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localPosition = new Vector3(0f, 0f, 1f);
                rt.localRotation = Quaternion.identity;
                float baseScale = 1f;
                float dist = Vector3.Distance(mainCam.transform.position, rt.position);
                float scaleFactor = baseScale * (dist / mainCam.orthographicSize) * 0.1f;
                rt.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            }
            
            var controller = uiInstance.GetComponent<GameOverUIController>();
            if (controller != null)
            {
                if (isWinner)
                {
                    controller.ShowWinner(winnerName);
                }
                else
                {
                    controller.ShowWinner(winnerName); // Même méthode pour winner/loser
                }
            }
        }
    }
    
    #endregion

    #region Public Methods - Interface pour autres scripts
    
    /// <summary>Notifier que le tank local a été spawné</summary>
    public void NotifyLocalTankSpawned()
    {
        Debug.Log("[NETWORK_UI] 🚗 Tank local spawné - notification UI");
        OnLocalTankSpawned?.Invoke();
    }
    
    /// <summary>Mettre à jour la liste des joueurs connectés avec scores</summary>
    public void UpdatePlayerList()
    {
        Debug.Log($"[NETWORK] 📝 UpdatePlayerList appelé - HasStateAuthority: {Object.HasStateAuthority}");
        
        if (!Object.HasStateAuthority) 
        {
            Debug.Log("[NETWORK] ⚠️ Pas d'autorité - UpdatePlayerList ignoré");
            return;
        }
        
        // Récupérer les scores depuis ScoreManager
        var scoreManager = FindFirstObjectByType<ScoreManager>();
        var playerScores = scoreManager?.GetPlayerScores() ?? new System.Collections.Generic.Dictionary<int, int>();
        
        Debug.Log($"[NETWORK] 📊 ScoreManager trouvé: {scoreManager != null}, Scores: {playerScores.Count}");
        
        var activePlayers = Runner.ActivePlayers.ToList();
        Debug.Log($"[NETWORK] 👥 Joueurs actifs: {activePlayers.Count}");
        
        // Créer une liste triée par score (descendant)
        var sortedPlayers = new System.Collections.Generic.List<(int playerId, int score)>();
        
        foreach (var player in activePlayers)
        {
            int score = playerScores.ContainsKey(player.PlayerId) ? playerScores[player.PlayerId] : 0;
            sortedPlayers.Add((player.PlayerId, score));
            Debug.Log($"[NETWORK] 👤 Player {player.PlayerId}: {score} pts");
        }
        
        // Trier par score décroissant
        sortedPlayers.Sort((a, b) => b.score.CompareTo(a.score));
        
        // Construire la chaîne de caractères
        var playerList = new System.Text.StringBuilder();
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            if (i > 0) playerList.Append("\n");
            var (playerId, score) = sortedPlayers[i];
            playerList.Append($"{i + 1}. Player {playerId}: {score} pts");
        }
        
        string finalList = playerList.ToString();
        Debug.Log($"[NETWORK] 📋 Liste finale générée: '{finalList}'");
        
        UpdatePlayerListRpc(finalList);
    }
    
    /// <summary>Afficher un message de kill dans le feed</summary>
    public void ShowKillMessage(string killerName, string victimName)
    {
        string message = $"{killerName} eliminated {victimName}";
        ShowKillFeedMessageRpc(message);
    }
    
    /// <summary>Test manuel des events (pour FusionArchitectureValidator)</summary>
    public void TestEvents()
    {
        Debug.Log("[NETWORK_UI] 🧪 Testing events manually...");
        OnKillFeedMessage?.Invoke("TEST: Architecture validation kill message");
        OnPlayerListUpdated?.Invoke("TEST: Player1, Player2");
        OnMatchTimerUpdated?.Invoke(123.45f);
    }
    
    #endregion
    
    #region Timer Management - Synchronisé réseau
    
    /// <summary>
    /// Démarre le timer de match (seulement le Host/Server)
    /// </summary>
    public void StartMatchTimer(float durationSeconds = 300f)
    {
        if (!Object.HasStateAuthority) return;
        
        MatchTimeRemaining = durationSeconds;
        MatchStartTime = (float)Runner.SimulationTime;
        IsMatchTimerActive = true;
        
        Debug.Log($"[NETWORK_UI] ⏰ Timer de match démarré: {durationSeconds}s");
    }
    
    /// <summary>
    /// Arrête le timer de match (seulement le Host/Server)
    /// </summary>
    public void StopMatchTimer()
    {
        if (!Object.HasStateAuthority) return;
        
        IsMatchTimerActive = false;
        Debug.Log("[NETWORK_UI] ⏰ Timer de match arrêté");
    }
    
    /// <summary>
    /// Update appelé chaque frame pour gérer le timer
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // Seulement le Host/Server met à jour le timer
        if (!Object.HasStateAuthority || !IsMatchTimerActive) return;
        
        // Calculer le temps restant basé sur le temps réseau
        float elapsedTime = (float)Runner.SimulationTime - MatchStartTime;
        MatchTimeRemaining = Mathf.Max(0f, 300f - elapsedTime); // 300s = durée initiale
        
        // Timer expiré
        if (MatchTimeRemaining <= 0f)
        {
            IsMatchTimerActive = false;
            OnMatchTimeExpired();
        }
    }
    
    /// <summary>
    /// Appelé quand le timer expire
    /// </summary>
    private void OnMatchTimeExpired()
    {
        Debug.Log("[NETWORK_UI] ⏰ Timer de match expiré!");
        // Notifier tous les clients via RPC
        ShowMatchEndRPC("Time's up!");
    }
    
    /// <summary>
    /// RPC pour notifier la fin de match
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void ShowMatchEndRPC(string reason)
    {
        Debug.Log($"[NETWORK_UI] 🏁 Fin de match: {reason}");
        // Déclencher l'event pour l'UI
        OnKillFeedMessage?.Invoke($"MATCH ENDED: {reason}");
    }
    
    /// <summary>
    /// Update local pour synchroniser l'UI avec le timer réseau
    /// </summary>
    void Update()
    {
        // Tous les clients mettent à jour leur UI locale
        if (IsMatchTimerActive)
        {
            float remainingSeconds = MatchTimeRemaining;
            OnMatchTimerUpdated?.Invoke(remainingSeconds);
        }
    }
    
    #endregion
}
