using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;
using System.Collections;
using System.Runtime.InteropServices;
using TMPro;

public class ScoreManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const float ROOM_LIFETIME = 180f; 
    private const float RESPAWN_TIME = 5f;
    private const float COIN_SPAWN_INTERVAL = 20f; 
    
    private const byte SCORE_UPDATE_EVENT = 1;
    private const byte MATCH_END_EVENT = 2;
    private const byte MATCH_START_TIME_EVENT = 5;
    private const byte SYNC_TIMER_EVENT = 6;
    
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool SubmitScoreJS(string score, string bonus, string walletAddress);
#endif
    
    private Dictionary<int, int> playerScores = new Dictionary<int, int>(); 
    private Dictionary<string, string> playerWallets = new Dictionary<string, string>();
    private float matchStartTime;
    private bool matchEnded = false;
    
    [Header("Coin System")]
    public GameObject coinPrefab; 
    public Transform[] coinSpawnPoints; 
    private float nextCoinSpawnTime;
    
    [Header("Power-up System")]
    public GameObject[] powerupPrefabs; 
    public float powerupSpawnInterval = 15f;
    private float nextPowerupSpawnTime;
    
    public static ScoreManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            StartMatch();
        }
    }
    
    public void ResetManager()
    {
        playerScores.Clear();
        playerWallets.Clear();
        matchStartTime = Time.time; 
        matchEnded = false;
        
        StopAllCoroutines();
    }
    
    public override void OnJoinedRoom()
    {
        
        ResetManager();
        
        StartMatch();
        
        if (!string.IsNullOrEmpty(PlayerSession.WalletAddress))
        {
            string walletAddress = PlayerSession.WalletAddress;
            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            
            object[] walletData = new object[] { actorNumber.ToString(), walletAddress };
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(3, walletData, options, SendOptions.SendReliable);
            
            playerWallets[actorNumber.ToString()] = walletAddress;
        }
    }
    
    private void StartMatch()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            matchStartTime = Time.time;
            nextCoinSpawnTime = Time.time + COIN_SPAWN_INTERVAL; // Premier coin dans 20s
            matchEnded = false;
            
            playerScores.Clear();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerScores[player.ActorNumber] = 0;
            }
            
            StartCoroutine(MatchTimer());
            
            SyncMatchTime(ROOM_LIFETIME);
            
            SyncScores();
        }
        else
        {            
            matchStartTime = Time.time - 1;
            
            StartCoroutine(MatchTimer());
        }
    }
    
    private IEnumerator MatchTimer()
    {
        float timeLeft = ROOM_LIFETIME;
        bool waitingForSync = !PhotonNetwork.IsMasterClient;
        int lastCountdownSecond = -1; // Pour éviter de jouer le son plusieurs fois par seconde
        
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdateRoomStatus("Ongoing Match                       ");
            
            if (waitingForSync)
            {
                LobbyUI.Instance.UpdateTimer((int)timeLeft);
            }
        }
        
        float nextSyncTime = 0f;
        
        while (timeLeft > 0 && !matchEnded)
        {
            timeLeft = ROOM_LIFETIME - (Time.time - matchStartTime);
            int currentSecond = Mathf.Max(0, (int)timeLeft);
            
            if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.UpdateTimer(currentSecond);
            }
            
            // Jouer le son de décompte pour les 5 dernières secondes
            if (currentSecond <= 5 && currentSecond >= 1 && currentSecond != lastCountdownSecond)
            {
                if (SFXManager.Instance != null)
                {
                    SFXManager.Instance.PlayCountdownBeep();
                }
                lastCountdownSecond = currentSecond;
            }
            
            // Spawn des coins (Master Client seulement) - avec protection contre les erreurs
            if (PhotonNetwork.IsMasterClient && Time.time >= nextCoinSpawnTime && !matchEnded)
            {
                try
                {
                    SpawnCoin();
                    nextCoinSpawnTime = Time.time + COIN_SPAWN_INTERVAL;
                }
                catch (System.Exception e)
                {
                    // Debug.LogError($"[COIN] ❌ Erreur lors du spawn de coin: {e.Message}");
                    // Réessayer dans 20 secondes même en cas d'erreur
                    nextCoinSpawnTime = Time.time + COIN_SPAWN_INTERVAL;
                }
            }
            
            // Spawn des power-ups (Master Client seulement)
            if (PhotonNetwork.IsMasterClient && Time.time >= nextPowerupSpawnTime && !matchEnded)
            {
                try
                {
                    SpawnPowerup();
                    nextPowerupSpawnTime = Time.time + powerupSpawnInterval;
                }
                catch (System.Exception e)
                {
                    // Debug.LogError($"[POWERUP] ❌ Erreur lors du spawn de power-up: {e.Message}");
                    nextPowerupSpawnTime = Time.time + powerupSpawnInterval;
                }
            }
            
            if (PhotonNetwork.IsMasterClient && Time.time > nextSyncTime)
            {
                SyncMatchTime(timeLeft);
                nextSyncTime = Time.time + 5f; 
            }
            
            yield return null;
            
            if (timeLeft <= 0 && PhotonNetwork.IsMasterClient)
            {
                EndMatch();
            }
        }
    }
    
    public void AddKill(int killerActorNumber)
    {
        AddScore(killerActorNumber, 1);
    }
    
    /// <summary>
    /// Ajoute des points au score d'un joueur (générique pour kills, coins, etc.)
    /// </summary>
    public void AddScore(int playerActorNumber, int points)
    {
        if (matchEnded) return;

        int scoreBefore = playerScores.ContainsKey(playerActorNumber) ? playerScores[playerActorNumber] : 0;
        if (playerScores.ContainsKey(playerActorNumber))
        {
            playerScores[playerActorNumber] += points;
        }
        else
        {
            playerScores[playerActorNumber] = points;
        }
        int scoreAfter = playerScores[playerActorNumber];
        
        // Debug.Log($"[SCOREMANAGER] ✅ {points} points ajoutés à ActorNumber {playerActorNumber} (Score: {scoreBefore} → {scoreAfter})");
        
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        object[] content = new object[] { playerActorNumber, playerScores[playerActorNumber] };
        PhotonNetwork.RaiseEvent(SCORE_UPDATE_EVENT, content, options, SendOptions.SendReliable);

        if (PhotonNetwork.IsMasterClient)
        {
            SyncScores();
        }

        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdatePlayerList();
        }
    }
    
    private void HandleScoreUpdate(int actorNumber, int score)
    {
        int before = playerScores.ContainsKey(actorNumber) ? playerScores[actorNumber] : -1;
        playerScores[actorNumber] = score;
        
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdatePlayerList();
        }
    }
    
    public void PlayerDied(int victimActorNumber, int killerActorNumber, int victimViewID)
    {
        Debug.Log($"[SCOREMANAGER] PlayerDied appelé - Victim: {victimActorNumber}, Killer: {killerActorNumber}, ViewID: {victimViewID}");
        Debug.Log($"[SCOREMANAGER] IsMasterClient: {PhotonNetwork.IsMasterClient}");
        
        if (!PhotonNetwork.IsMasterClient) return;

        if (killerActorNumber > 0 && killerActorNumber != victimActorNumber)
        {
            Debug.Log($"[SCOREMANAGER] Condition killer remplie - Ajout du kill et affichage killfeed");
            AddKill(killerActorNumber);

            string killerName = GetPlayerName(killerActorNumber);
            string victimName = GetPlayerName(victimActorNumber);
            if (LobbyUI.Instance != null && LobbyUI.Instance.killFeedText != null)
            {
                Debug.Log($"[KILLFEED] 📺 Affichage du killfeed: '{killerName} a tué {victimName} !'");
                LobbyUI.Instance.killFeedText.text = $"{killerName} a tué {victimName} !";
                LobbyUI.Instance.StartCoroutine(HideKillFeedAfterDelay(3f));
                
                // Jouer un son de killfeed aléatoire pour tous les joueurs via RPC
                Debug.Log("[KILLFEED] 🔊 Envoi du RPC PlayKillFeedSoundRPC à tous les joueurs...");
                photonView.RPC("PlayKillFeedSoundRPC", RpcTarget.All);
            }
        }
        else
        {
            Debug.Log($"[SCOREMANAGER] Condition killer NON remplie - killerActorNumber: {killerActorNumber}, victimActorNumber: {victimActorNumber}");
            if (killerActorNumber <= 0)
                Debug.Log("[SCOREMANAGER] Raison: killerActorNumber <= 0 (suicide ou dégâts environnementaux)");
            if (killerActorNumber == victimActorNumber)
                Debug.Log("[SCOREMANAGER] Raison: killerActorNumber == victimActorNumber (suicide)");
        }

        PhotonView victimView = PhotonView.Find(victimViewID);
        if (victimView != null)
        {
            PhotonNetwork.Destroy(victimView.gameObject);
            
            StartCoroutine(RespawnPlayer(victimActorNumber));
        }
        else
        {
        }
    }

    private string GetPlayerName(int actorNumber)
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == actorNumber)
                return string.IsNullOrEmpty(player.NickName) ? $"Player {actorNumber}" : player.NickName;
        }
        return $"Player {actorNumber}";
    }

    private IEnumerator HideKillFeedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (LobbyUI.Instance != null && LobbyUI.Instance.killFeedText != null)
            LobbyUI.Instance.killFeedText.text = "";
    }
    
    /// <summary>
    /// Spawn un power-up aléatoire à une position aléatoire (Master Client seulement)
    /// </summary>
    private void SpawnPowerup()
    {
        if (!PhotonNetwork.IsMasterClient) 
        {
            // Debug.LogWarning("[POWERUP] ⚠️ SpawnPowerup appelé mais ce client n'est pas Master Client");
            return;
        }
        
        if (powerupPrefabs == null || powerupPrefabs.Length == 0)
        {
            // Debug.LogWarning("[POWERUP] ⚠️ Aucun power-up prefab assigné dans l'inspecteur - spawn ignoré");
            return;
        }
        
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            // Debug.LogWarning("[POWERUP] ⚠️ Pas connecté à Photon ou pas dans une room - spawn ignoré");
            return;
        }
        
        // Choisir un power-up aléatoire
        int randomPowerupIndex = Random.Range(0, powerupPrefabs.Length);
        GameObject selectedPowerup = powerupPrefabs[randomPowerupIndex];
        
        if (selectedPowerup == null)
        {
            // Debug.LogWarning($"[POWERUP] ⚠️ Power-up prefab à l'index {randomPowerupIndex} est null");
            return;
        }
        
        Vector3 spawnPosition;
        
        // Utiliser les mêmes points de spawn que les coins
        if (coinSpawnPoints != null && coinSpawnPoints.Length > 0)
        {
            // Choisir un point de spawn aléatoire
            int randomIndex = Random.Range(0, coinSpawnPoints.Length);
            Transform spawnPoint = coinSpawnPoints[randomIndex];
            spawnPosition = spawnPoint.position;
            
            // Debug.Log($"[POWERUP] ⚡ Spawn {selectedPowerup.name} au point {randomIndex}: {spawnPosition}");
        }
        else
        {
            // Position aléatoire dans une zone définie (fallback)
            spawnPosition = new Vector3(
                Random.Range(-8f, 8f),  // X aléatoire
                Random.Range(-4f, 4f),  // Y aléatoire
                0f                      // Z fixe pour 2D
            );
            // Debug.Log($"[POWERUP] ⚡ Spawn {selectedPowerup.name} à position aléatoire: {spawnPosition}");
        }
        
        // Spawner le power-up via Photon pour synchronisation réseau
        try
        {
            GameObject powerup = PhotonNetwork.Instantiate(selectedPowerup.name, spawnPosition, Quaternion.identity);
            if (powerup != null)
            {
                // Debug.Log($"[POWERUP] ✅ Power-up {selectedPowerup.name} spawné avec succès ! NetworkID: {powerup.GetComponent<PhotonView>()?.ViewID}");
            }
        }
        catch (System.Exception e)
        {
            // Debug.LogError($"[POWERUP] ❌ Erreur lors de PhotonNetwork.Instantiate: {e.Message}");
        }
    }
    
    [PunRPC]
    void PlayKillFeedSoundRPC()
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayRandomKillFeedSoundLocal();
        }
        else
        {
            Debug.LogError("[KILLFEED] ❌ SFXManager.Instance est null sur ce client !");
        }
    }
    
    /// <summary>
    /// Spawn un coin à une position aléatoire (Master Client seulement)
    /// </summary>
    private void SpawnCoin()
    {
        if (!PhotonNetwork.IsMasterClient) 
        {
            // Debug.LogWarning("[COIN] ⚠️ SpawnCoin appelé mais ce client n'est pas Master Client");
            return;
        }
        
        if (coinPrefab == null)
        {
            // Debug.LogWarning("[COIN] ⚠️ Coin prefab non assigné dans l'inspecteur - spawn ignoré");
            return;
        }
        
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            // Debug.LogWarning("[COIN] ⚠️ Pas connecté à Photon ou pas dans une room - spawn ignoré");
            return;
        }
        
        Vector3 spawnPosition;
        
        // Utiliser les points de spawn définis ou une position aléatoire
        if (coinSpawnPoints != null && coinSpawnPoints.Length > 0)
        {
            // Choisir un point de spawn aléatoire
            int randomIndex = Random.Range(0, coinSpawnPoints.Length);
            Transform spawnPoint = coinSpawnPoints[randomIndex];
            spawnPosition = spawnPoint.position;
            
            // Debug.Log($"[COIN] 🪙 Spawn coin au point {randomIndex}:");
            // Debug.Log($"[COIN]   - Transform name: {spawnPoint.name}");
            // Debug.Log($"[COIN]   - Position: {spawnPosition}");
            // Debug.Log($"[COIN]   - Local position: {spawnPoint.localPosition}");
        }
        else
        {
            // Position aléatoire dans une zone définie (fallback)
            // ⚠️ Modifie ces valeurs selon la taille de ta map
            spawnPosition = new Vector3(
                Random.Range(-8f, 8f),  // X aléatoire (ajuste selon ta map)
                Random.Range(-4f, 4f),  // Y aléatoire (ajuste selon ta map)
                0f                      // Z fixe pour 2D
            );
            // Debug.Log($"[COIN] 🪙 Spawn coin à position aléatoire: {spawnPosition}");
        }
        
        // Spawner le coin via Photon pour synchronisation réseau
        try
        {
            GameObject coin = PhotonNetwork.Instantiate(coinPrefab.name, spawnPosition, Quaternion.identity);
            if (coin != null)
            {
                // Debug.Log($"[COIN] ✅ Coin spawné avec succès ! NetworkID: {coin.GetComponent<PhotonView>()?.ViewID}");
            }
        }
        catch (System.Exception e)
        {
            // Debug.LogError($"[COIN] ❌ Erreur lors de PhotonNetwork.Instantiate: {e.Message}");
        }
    }
    
    private IEnumerator RespawnPlayer(int actorNumber)
    {
        yield return new WaitForSeconds(RESPAWN_TIME);
        
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            
            foreach (var ui in GameObject.FindGameObjectsWithTag("GameOverUI"))
            {
                Destroy(ui);
            }
            
            var spawner = FindObjectOfType<PhotonTankSpawner>();
            if (spawner != null)
            {
                spawner.SpawnTank();        }
        else
        {
        }
    }
        else
        {
        }
    }
    
    public void EndMatch()
    {
        if (matchEnded) return;
        matchEnded = true;
        
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdateRoomStatus("Match ended!");
        }
        
        int highestScore = -1;
        int winnerActorNumber = -1;
        string winnerName = "Unknown Player";
        
        if (_playerNames == null)
        {
            _playerNames = new Dictionary<int, string>();
        }
        
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            string playerNickname = string.IsNullOrEmpty(player.NickName) ? 
                $"Player {player.ActorNumber}" : player.NickName;
            _playerNames[player.ActorNumber] = playerNickname;
        }
        
        foreach (var pair in playerScores)
        {
            if (pair.Value > highestScore)
            {
                highestScore = pair.Value;
            }
        }
        
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (playerScores.ContainsKey(player.ActorNumber) && playerScores[player.ActorNumber] == highestScore)
            {
                winnerActorNumber = player.ActorNumber;
                winnerName = _playerNames[player.ActorNumber];
                break;
            }
        }
        
        if (winnerActorNumber == -1)
        {
            foreach (var pair in playerScores)
            {
                if (pair.Value == highestScore)
                {
                    winnerActorNumber = pair.Key;
                    winnerName = _playerNames.ContainsKey(winnerActorNumber) ? 
                        _playerNames[winnerActorNumber] : $"Player {winnerActorNumber}";
                    break;
                }
            }
        }
        
        if (winnerActorNumber != -1)
        {
            playerScores[winnerActorNumber]++;
            highestScore++;
            
            if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.UpdatePlayerList();
            }
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            object[] content = new object[] { winnerActorNumber, winnerName, highestScore };
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(MATCH_END_EVENT, content, options, SendOptions.SendReliable);
            
            // Fallback: Also call ShowWinnerToAllRPC directly in case the event fails
            StartCoroutine(FallbackShowWinner(winnerActorNumber, winnerName, highestScore));
        }
        else
        {
            return;
        }
        
        ShowWinnerAndSubmitScores(winnerActorNumber, winnerName, highestScore);
    }
    
    private static Dictionary<int, string> _playerNames = new Dictionary<int, string>();
    
    public void ShowWinnerAndSubmitScores(int winnerActorNumber, string winnerName, int highestScore)
    {
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdateRoomStatus($"Victory: {winnerName} with {highestScore} points!");
        }
        
        GameObject[] gameOverUIs = GameObject.FindGameObjectsWithTag("GameOverUI");
        
        if (gameOverUIs.Length == 0)
        {
            Debug.Log("[WINNER-DEBUG] Recherche de PhotonLauncher...");
            PhotonLauncher launcher = FindObjectOfType<PhotonLauncher>();
            if (launcher != null)
            {
                Debug.Log($"[WINNER-DEBUG] PhotonLauncher trouvé, appel ShowWinnerToAllRPC pour {winnerName}");
                launcher.ShowWinnerToAllRPC(winnerName, winnerActorNumber);
            }
            else
            {
                Debug.LogError("[WINNER-DEBUG] PhotonLauncher NOT FOUND!");
            }
        }
        
        int localPlayerScore = 0;
        if (playerScores.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            localPlayerScore = playerScores[PhotonNetwork.LocalPlayer.ActorNumber];
        }
        
        int bonus = 0;
        
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            SubmitScoreToFirebase(localPlayerScore, bonus);
        }
        
        ChogTanksNFTManager nftManager = FindObjectOfType<ChogTanksNFTManager>();
        if (nftManager != null)
        {
            nftManager.ForceRefreshAfterMatch(localPlayerScore);
        }
    }
    
    private void SubmitScoreToFirebase(int score, int bonus)
    {
        string walletAddress = "";
        
        try
        {
            if (Reown.AppKit.Unity.AppKit.IsInitialized && 
                Reown.AppKit.Unity.AppKit.IsAccountConnected && 
                Reown.AppKit.Unity.AppKit.Account != null)
            {
                string appKitAddress = Reown.AppKit.Unity.AppKit.Account.Address;
                if (!string.IsNullOrEmpty(appKitAddress))
                {
                    walletAddress = appKitAddress;
                }
            }
        }
        catch (System.Exception ex)
        {
        }
        
        if (string.IsNullOrEmpty(walletAddress))
        {
            string prefsAddress = PlayerPrefs.GetString("walletAddress", "");
            if (!string.IsNullOrEmpty(prefsAddress))
            {
                walletAddress = prefsAddress;
            }
        }
        
        if (string.IsNullOrEmpty(walletAddress))
        {
            try
            {
                if (PlayerSession.IsConnected && !string.IsNullOrEmpty(PlayerSession.WalletAddress))
                {
                    walletAddress = PlayerSession.WalletAddress;
                }
            }
            catch (System.Exception ex)
            {
            }
        }
        
        if (string.IsNullOrEmpty(walletAddress))
        {
            walletAddress = "anonymous";
        }
        
#if UNITY_WEBGL && !UNITY_EDITOR
        SubmitScoreJS(score.ToString(), bonus.ToString(), walletAddress);
#else
#endif
    }
    
    
    private void SyncScores()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        List<object> scoreList = new List<object>();
        foreach (var pair in playerScores)
        {
            scoreList.Add(pair.Key);
            scoreList.Add(pair.Value);
        }
        
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(4, scoreList.ToArray(), options, SendOptions.SendReliable);
    }
    
    private void SyncMatchTime(float timeLeft)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(SYNC_TIMER_EVENT, timeLeft, options, SendOptions.SendReliable);
    }
    
    private IEnumerator FallbackShowWinner(int winnerActorNumber, string winnerName, int highestScore)
    {
        // Wait 2 seconds to see if the event was received
        yield return new WaitForSeconds(2f);
        
        // If no GameOverUI was created, the event probably failed
        GameObject[] gameOverUIs = GameObject.FindGameObjectsWithTag("GameOverUI");
        if (gameOverUIs.Length == 0)
        {
            Debug.LogWarning("[SCOREMANAGER] Photon event may have failed, calling ShowWinnerToAllRPC directly");
            PhotonLauncher launcher = FindObjectOfType<PhotonLauncher>();
            if (launcher != null && launcher.photonView != null)
            {
                launcher.photonView.RPC("ShowWinnerToAllRPC", RpcTarget.All, winnerName, winnerActorNumber);
            }
        }
    }
    
    public void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        
        if (eventCode == SCORE_UPDATE_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            int actorNumber = (int)data[0];
            int score = (int)data[1];
            
            HandleScoreUpdate(actorNumber, score);
        }
        else if (eventCode == MATCH_END_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            int winnerActorNumber = (int)data[0];
            string winnerName = (string)data[1];
            int highestScore = (int)data[2];
            
            ShowWinnerAndSubmitScores(winnerActorNumber, winnerName, highestScore);
        }
        else if (eventCode == 3) 
        {
            object[] data = (object[])photonEvent.CustomData;
            string actorIdStr = (string)data[0];
            string walletAddress = (string)data[1];
            
            playerWallets[actorIdStr] = walletAddress;
        }
        else if (eventCode == 4) 
        {
            object[] data = (object[])photonEvent.CustomData;
            
            playerScores.Clear();
            for (int i = 0; i < data.Length; i += 2)
            {
                int actorNumber = (int)data[i];
                int score = (int)data[i + 1];
                playerScores[actorNumber] = score;
            }
            
            if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.UpdatePlayerList();
            }
        }
        else if (eventCode == SYNC_TIMER_EVENT)
        {
            float timeRemaining = (float)photonEvent.CustomData;
            matchStartTime = Time.time - (ROOM_LIFETIME - timeRemaining);
            
            if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.UpdateTimer(Mathf.Max(0, (int)timeRemaining));
            }
        }
    }
    
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        
        if (newMasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            if (!matchEnded)
            {
                SyncMatchTime(ROOM_LIFETIME - (Time.time - matchStartTime));
            }
        }
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!playerScores.ContainsKey(newPlayer.ActorNumber))
        {
            playerScores[newPlayer.ActorNumber] = 0;
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            SyncScores();
            
            float timeLeft = ROOM_LIFETIME - (Time.time - matchStartTime);
            SyncMatchTime(timeLeft);
        }
    }
    
    public override void OnLeftRoom()
    {
        ResetManager();
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        ResetManager();
    }
    
    public Dictionary<int, int> GetPlayerScores()
    {
        return playerScores;
    }
    
    public bool IsMatchEnded()
    {
        return matchEnded || (Time.time - matchStartTime) >= ROOM_LIFETIME;
    }
}