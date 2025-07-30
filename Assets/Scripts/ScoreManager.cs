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
        
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdateRoomStatus("Ongoing Match");
            
            if (waitingForSync)
            {
                LobbyUI.Instance.UpdateTimer((int)timeLeft);
            }
        }
        
        float nextSyncTime = 0f;
        
        while (timeLeft > 0 && !matchEnded)
        {
            timeLeft = ROOM_LIFETIME - (Time.time - matchStartTime);
            
            if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.UpdateTimer(Mathf.Max(0, (int)timeLeft));
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
        if (matchEnded) return;

        int scoreBefore = playerScores.ContainsKey(killerActorNumber) ? playerScores[killerActorNumber] : 0;
        if (playerScores.ContainsKey(killerActorNumber))
        {
            playerScores[killerActorNumber]++;
        }
        else
        {
            playerScores[killerActorNumber] = 1;
        }
        int scoreAfter = playerScores[killerActorNumber];
        
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        object[] content = new object[] { killerActorNumber, playerScores[killerActorNumber] };
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
        
        if (!PhotonNetwork.IsMasterClient) return;

        if (killerActorNumber > 0 && killerActorNumber != victimActorNumber)
        {
            AddKill(killerActorNumber);

            string killerName = GetPlayerName(killerActorNumber);
            string victimName = GetPlayerName(victimActorNumber);
            if (LobbyUI.Instance != null && LobbyUI.Instance.killFeedText != null)
            {
                LobbyUI.Instance.killFeedText.text = $"{killerName} a tué {victimName} !";
                LobbyUI.Instance.StartCoroutine(HideKillFeedAfterDelay(3f));
            }
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
            PhotonLauncher launcher = FindObjectOfType<PhotonLauncher>();
            if (launcher != null)
            {
                launcher.ShowWinnerToAllRPC(winnerName, winnerActorNumber);
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