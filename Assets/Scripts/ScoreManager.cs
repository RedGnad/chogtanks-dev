using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;
using System.Collections;
using System.Runtime.InteropServices;

public class ScoreManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const float ROOM_LIFETIME = 240f; // 4 minutes en secondes
    private const float RESPAWN_TIME = 5f; // 5 secondes pour respawn
    
    private const byte SCORE_UPDATE_EVENT = 1;
    private const byte MATCH_END_EVENT = 2;
    private const byte MATCH_START_TIME_EVENT = 5;
    private const byte SYNC_TIMER_EVENT = 6;
    
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool SubmitScoreJS(string score, string bonus, string walletAddress);
#endif
    
    private Dictionary<int, int> playerScores = new Dictionary<int, int>(); // ActorNumber -> Score
    private Dictionary<string, string> playerWallets = new Dictionary<string, string>(); // ActorNumber (string) -> Wallet Address
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
    
    // Méthode pour réinitialiser complètement l'état du manager
    public void ResetManager()
    {
        Debug.Log("[SCORE] Réinitialisation complète du ScoreManager");
        playerScores.Clear();
        playerWallets.Clear();
        matchStartTime = 0;
        matchEnded = false;
        
        // Arrêter toutes les coroutines en cours
        StopAllCoroutines();
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log("[SCORE] OnJoinedRoom - Réinitialisation et démarrage du match");
        
        // Réinitialiser l'état du manager quand on rejoint une nouvelle room
        ResetManager();
        
        // Démarrer le match
        StartMatch();
        
        // Enregistrer l'adresse wallet du joueur si disponible
        if (!string.IsNullOrEmpty(PlayerSession.WalletAddress))
        {
            string walletAddress = PlayerSession.WalletAddress;
            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            
            // Envoyer l'info wallet aux autres joueurs
            object[] walletData = new object[] { actorNumber.ToString(), walletAddress };
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(3, walletData, options, SendOptions.SendReliable);
            
            // Enregistrer localement
            playerWallets[actorNumber.ToString()] = walletAddress;
        }
    }
    
    private void StartMatch()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[SCORE] MasterClient démarre un nouveau match");
            matchStartTime = Time.time;
            matchEnded = false;
            
            // Initialiser les scores pour tous les joueurs actuels
            playerScores.Clear();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerScores[player.ActorNumber] = 0;
            }
            
            // Démarrer le timer
            StartCoroutine(MatchTimer());
            
            // Synchroniser le temps de départ avec tous les clients
            SyncMatchStartTime();
            
            // Synchroniser les scores avec tous les clients
            SyncScores();
        }
        else
        {            
            // Pour les clients non-master, démarrer aussi un timer local
            // qui sera synchronisé via les événements
            StartCoroutine(MatchTimer());
            Debug.Log("[SCORE] Client non-master attend la synchronisation des scores et du timer");
        }
    }
    
    private IEnumerator MatchTimer()
    {
        float timeLeft = ROOM_LIFETIME;
        
        // Mettre à jour le statut de la room
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdateRoomStatus("Match en cours!");
        }
        
        // Synchroniser le timer toutes les 5 secondes si on est MasterClient
        float nextSyncTime = 0f;
        
        while (timeLeft > 0 && !matchEnded)
        {
            // Si le matchStartTime n'est pas encore défini, attendre
            if (matchStartTime <= 0)
            {
                yield return null;
                continue;
            }
            
            timeLeft = ROOM_LIFETIME - (Time.time - matchStartTime);
            
            // Mettre à jour le timer UI
            if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.UpdateTimer(Mathf.Max(0, (int)timeLeft));
            }
            
            // Synchroniser périodiquement le timer si on est MasterClient
            if (PhotonNetwork.IsMasterClient && Time.time > nextSyncTime)
            {
                SyncMatchStartTime();
                nextSyncTime = Time.time + 5f;  // Sync toutes les 5 secondes
            }
            
            yield return null;
            
            // Vérifier si la partie est terminée
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
        Debug.Log($"[SCORE] AddKill: Joueur {killerActorNumber} passe de {scoreBefore} à {scoreAfter}");

        // Envoyer l'événement pour mettre à jour les scores
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        object[] content = new object[] { killerActorNumber, playerScores[killerActorNumber] };
        PhotonNetwork.RaiseEvent(SCORE_UPDATE_EVENT, content, options, SendOptions.SendReliable);

        // Forcer la resynchro globale après chaque kill (anti-doublon)
        if (PhotonNetwork.IsMasterClient)
        {
            SyncScores();
        }

        // Mettre à jour l'UI
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdatePlayerList();
        }
    }
    
    private void HandleScoreUpdate(int actorNumber, int score)
    {
        int before = playerScores.ContainsKey(actorNumber) ? playerScores[actorNumber] : -1;
        playerScores[actorNumber] = score;
        Debug.Log($"[SCORE] HandleScoreUpdate: Joueur {actorNumber} score {before} → {score}");

        // Mettre à jour l'UI si nécessaire
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdatePlayerList();
        }
    }
    
    public void PlayerDied(int victimActorNumber, int killerActorNumber, int victimViewID)
    {
        Debug.Log($"[SCORE] PlayerDied: Victime {victimActorNumber}, Tueur {killerActorNumber}, ViewID {victimViewID}");

        // Seul le MasterClient gère la logique de mort pour éviter les conflits.
        if (!PhotonNetwork.IsMasterClient) return;

        // Ajouter un point au tueur
        if (killerActorNumber > 0 && killerActorNumber != victimActorNumber)
        {
            AddKill(killerActorNumber);
        }

        // Détruire l'ancien tank de la victime
        PhotonView victimView = PhotonView.Find(victimViewID);
        if (victimView != null)
        {
            Debug.Log($"[SCORE] Destruction du tank de la victime (ViewID: {victimViewID})");
            PhotonNetwork.Destroy(victimView.gameObject);
            
            // Démarrer la coroutine de respawn pour le joueur mort
            Debug.Log($"[SCORE] Démarrage de la coroutine RespawnPlayer pour l'acteur {victimActorNumber}");
            StartCoroutine(RespawnPlayer(victimActorNumber));
        }
        else
        {
            Debug.LogError($"[ScoreManager] ERREUR: PhotonView non trouvé pour ViewID {victimViewID}. Impossible de démarrer le respawn.");
        }
    }
    
    private IEnumerator RespawnPlayer(int actorNumber)
    {
        Debug.Log($"[ScoreManager] Début du respawn pour le joueur {actorNumber} dans {RESPAWN_TIME} secondes");
        yield return new WaitForSeconds(RESPAWN_TIME);
        
        // Si c'est notre joueur local
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            Debug.Log($"[ScoreManager] Respawn pour joueur local {PhotonNetwork.LocalPlayer.NickName}");
            
            // Détruire l'UI GameOver si elle existe
            foreach (var ui in GameObject.FindGameObjectsWithTag("GameOverUI"))
            {
                Debug.Log("[ScoreManager] Destruction d'une UI GameOver");
                Destroy(ui);
            }
            
            // Respawner le tank
            var spawner = FindObjectOfType<PhotonTankSpawner>();
            if (spawner != null)
            {
                Debug.Log("[ScoreManager] PhotonTankSpawner trouvé, appel de SpawnTank()");
                spawner.SpawnTank();
            }
            else
            {
                Debug.LogError("[ScoreManager] PhotonTankSpawner introuvable! Impossible de respawn.");
            }
        }
        else
        {
            Debug.Log($"[ScoreManager] Pas de respawn pour ce client car actorNumber={actorNumber} != {PhotonNetwork.LocalPlayer.ActorNumber}");
        }
    }
    
    public void EndMatch()
    {
        if (matchEnded) return;
        matchEnded = true;
        
        // Mettre à jour le statut UI
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdateRoomStatus("Match terminé!");
        }
        
        // Trouver le joueur avec le score le plus élevé
        int highestScore = -1;
        int winnerActorNumber = -1;
        string winnerName = "Personne";
        
        foreach (var pair in playerScores)
        {
            if (pair.Value > highestScore)
            {
                highestScore = pair.Value;
                winnerActorNumber = pair.Key;
                
                // Trouver le nom du joueur gagnant
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    if (player.ActorNumber == winnerActorNumber)
                    {
                        winnerName = string.IsNullOrEmpty(player.NickName) ? 
                            $"Player {player.ActorNumber}" : player.NickName;
                        break;
                    }
                }
            }
        }
        
        Debug.Log($"[MATCH END] Gagnant: {winnerName} (Actor {winnerActorNumber}) avec {highestScore} points");
        
        // Ajouter un point bonus au gagnant
        if (winnerActorNumber != -1)
        {
            playerScores[winnerActorNumber]++;
            highestScore++; // Mettre à jour aussi le score pour l'affichage
            
            // Mettre à jour l'UI avec le score final
            if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.UpdatePlayerList();
            }
        }
        
        // Envoyer l'événement de fin de match à tous les joueurs
        if (PhotonNetwork.IsMasterClient)
        {
            object[] content = new object[] { winnerActorNumber, winnerName, highestScore };
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(MATCH_END_EVENT, content, options, SendOptions.SendReliable);
        }
        else
        {
            // Si ce n'est pas le MasterClient, il ne doit pas appeler ShowWinnerAndSubmitScores ici
            // car il recevra l'événement MATCH_END_EVENT qui appellera ShowWinnerAndSubmitScores
            return;
        }
        
        // Si c'est le MasterClient, afficher le gagnant et soumettre les scores
        ShowWinnerAndSubmitScores(winnerActorNumber, winnerName, highestScore);
    }
    
    public void ShowWinnerAndSubmitScores(int winnerActorNumber, string winnerName, int highestScore)
    {
        // Afficher le gagnant dans le statut de la room
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdateRoomStatus($"Victoire : {winnerName} avec {highestScore} points!");
        }
        
        // Soumettre les scores à Firebase
        int localPlayerScore = 0;
        if (playerScores.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            localPlayerScore = playerScores[PhotonNetwork.LocalPlayer.ActorNumber];
        }
        
        // Soumettre un score bonus si c'est le gagnant
        int bonus = (winnerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber) ? 1 : 0;
        
        // Soumettre le score à Firebase
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            SubmitScoreToFirebase(localPlayerScore, bonus);
        }
        
        // La room reste ouverte, on ne la quitte pas automatiquement
        // On pourrait ajouter un bouton pour quitter ou redémarrer le match
    }
    
    private void SubmitScoreToFirebase(int score, int bonus)
    {
        // 🔍 Recherche d'adresse wallet dans plusieurs sources
        string walletAddress = "";
        
        // 1. D'abord, essayer AppKit (source principale de vérité)
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
                    Debug.Log($"[SCORE] ✅ Wallet trouvé dans AppKit: {walletAddress}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SCORE] ⚠️ Erreur AppKit: {ex.Message}");
        }
        
        // 2. Si AppKit échoue, essayer PlayerPrefs
        if (string.IsNullOrEmpty(walletAddress))
        {
            string prefsAddress = PlayerPrefs.GetString("walletAddress", "");
            if (!string.IsNullOrEmpty(prefsAddress))
            {
                walletAddress = prefsAddress;
                Debug.Log($"[SCORE] ✅ Wallet trouvé dans PlayerPrefs: {walletAddress}");
            }
        }
        
        // 3. Enfin, essayer PlayerSession
        if (string.IsNullOrEmpty(walletAddress))
        {
            try
            {
                if (PlayerSession.IsConnected && !string.IsNullOrEmpty(PlayerSession.WalletAddress))
                {
                    walletAddress = PlayerSession.WalletAddress;
                    Debug.Log($"[SCORE] ✅ Wallet trouvé dans PlayerSession: {walletAddress}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SCORE] ⚠️ Erreur PlayerSession: {ex.Message}");
            }
        }
        
        // 4. Si toujours rien, utiliser "anonymous" comme fallback
        if (string.IsNullOrEmpty(walletAddress))
        {
            walletAddress = "anonymous";
            Debug.LogWarning($"[SCORE] ⚠️ Aucun wallet trouvé, utilisation de '{walletAddress}' par défaut");
        }
        
        // Appel JavaScript pour soumettre le score
        // Ceci est appelé via JSLib
#if UNITY_WEBGL && !UNITY_EDITOR
        SubmitScoreJS(score.ToString(), bonus.ToString(), walletAddress);
        Debug.Log($"[SCORE] 🚀 Score soumis à Firebase pour {walletAddress}: {score} (+{bonus})");
#else
        Debug.Log($"[SCORE] 🔧 [MOCK] Score soumis à Firebase: {score}, bonus: {bonus}, wallet: {walletAddress}");
#endif
    }
    
    // On ne quitte plus la room automatiquement
    // Si l'utilisateur veut quitter, il peut utiliser le bouton de retour
    
    private void SyncScores()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Créer le tableau d'objets pour synchroniser les scores
        List<object> scoreList = new List<object>();
        foreach (var pair in playerScores)
        {
            scoreList.Add(pair.Key);
            scoreList.Add(pair.Value);
        }
        
        // Envoyer l'événement pour synchroniser les scores
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(4, scoreList.ToArray(), options, SendOptions.SendReliable);
    }
    
    // Synchronise le temps de départ du match avec tous les clients
    private void SyncMatchStartTime()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Envoyer l'événement pour synchroniser le temps de départ
        Debug.Log($"[SCORE] Synchronisation du temps de départ: {matchStartTime}");
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(MATCH_START_TIME_EVENT, matchStartTime, options, SendOptions.SendReliable);
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
            
            // Afficher le gagnant et soumettre les scores
            ShowWinnerAndSubmitScores(winnerActorNumber, winnerName, highestScore);
            Debug.Log($"[MATCH END] Gagnant: {winnerName} (Actor {winnerActorNumber}) avec {highestScore} points");
        }
        else if (eventCode == 3) // Wallet info
        {
            object[] data = (object[])photonEvent.CustomData;
            string actorIdStr = (string)data[0];
            string walletAddress = (string)data[1];
            
            playerWallets[actorIdStr] = walletAddress;
        }
        else if (eventCode == 4) // Sync scores
        {
            object[] data = (object[])photonEvent.CustomData;
            
            // Reconstruire le dictionnaire des scores
            playerScores.Clear();
            for (int i = 0; i < data.Length; i += 2)
            {
                int actorNumber = (int)data[i];
                int score = (int)data[i + 1];
                playerScores[actorNumber] = score;
            }
            
            // Mettre à jour l'UI
            if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.UpdatePlayerList();
            }
        }
        else if (eventCode == MATCH_START_TIME_EVENT) // Sync match start time
        {
            float serverTime = (float)photonEvent.CustomData;
            float ping = PhotonNetwork.GetPing() / 1000f; // Convertir ping de ms à s
            matchStartTime = serverTime + ping/2; // Ajouter la moitié du ping pour compenser la latence
            
            Debug.Log($"[SCORE] Temps de début synchronisé: {matchStartTime}, ping: {ping}s");
        }
    }
    
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[SCORE] MasterClient switched to {newMasterClient.NickName}");
        
        // Si je deviens le nouveau MasterClient
        if (newMasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log("[SCORE] Je suis le nouveau MasterClient - Synchronisation du timer de match");
            // Continuer le timer existant, pas le réinitialiser
            if (!matchEnded)
            {
                // Re-synchroniser le temps pour tous les clients
                SyncMatchStartTime();
            }
        }
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[SCORE] Joueur {newPlayer.NickName} (ActorNumber: {newPlayer.ActorNumber}) entré dans la room");
        if (!playerScores.ContainsKey(newPlayer.ActorNumber))
        {
            playerScores[newPlayer.ActorNumber] = 0;
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            SyncScores();
        }
    }
    
    // Ajouter des callbacks pour les événements de changement de room
    public override void OnLeftRoom()
    {
        Debug.Log("[SCORE] Room quittée - Réinitialisation du ScoreManager");
        ResetManager();
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[SCORE] Déconnecté: {cause} - Réinitialisation du ScoreManager");
        ResetManager();
    }
    
    public Dictionary<int, int> GetPlayerScores()
    {
        return playerScores;
    }
}