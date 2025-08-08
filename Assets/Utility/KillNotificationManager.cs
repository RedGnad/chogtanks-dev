using UnityEngine;
using TMPro;
using Fusion;
using System.Collections;
using System.Collections.Generic;

public class KillNotificationManager : NetworkBehaviour
{
    [SerializeField] private TMP_Text killNotificationText;
    [SerializeField] private float notificationDuration = 3f; 
    
    private static KillNotificationManager _instance;
    public static KillNotificationManager Instance 
    { 
        get 
        {
            if (_instance == null)
                _instance = FindObjectOfType<KillNotificationManager>();
            return _instance;
        }
    }
    
    private Queue<string> notificationQueue = new Queue<string>();
    private bool isShowingNotification = false;
    private LobbyUI cachedLobbyUI;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(this.gameObject);

        CacheLobbyUIReference();

        if (killNotificationText != null)
            killNotificationText.gameObject.SetActive(false);
    }

    private void CacheLobbyUIReference()
    {
        if (cachedLobbyUI == null)
        {
            cachedLobbyUI = FindObjectOfType<LobbyUI>();
        }
        
        if (killNotificationText == null && cachedLobbyUI != null)
        {
            killNotificationText = cachedLobbyUI.killFeedText;
        }
    }    private void Start()
    {
        CacheLobbyUIReference();
    }
    
    public void SetKillNotificationText(TMP_Text text)
    {
        killNotificationText = text;
    }
    
    public void ShowKillNotification(int killerActorNumber, int killedActorNumber)
    {
        if (Runner != null && Runner.IsServer)
        {
            ShowKillNotificationRPC(killerActorNumber, killedActorNumber);
        }
        else
        {
            ShowKillNotificationLocal(killerActorNumber, killedActorNumber);
        }
    }
    
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void ShowKillNotificationRPC(int killerActorNumber, int killedActorNumber)
    {
        ShowKillNotificationLocal(killerActorNumber, killedActorNumber);
    }
    
    private void ShowKillNotificationLocal(int killerActorNumber, int killedActorNumber)
    {
        string killerName = "Unknown";
        string killedName = "Unknown";
        
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (player.PlayerId == killerActorNumber)
            {
                killerName = string.IsNullOrEmpty(player.ToString()) ? $"Player {killerActorNumber}" : player.ToString();
            }
            if (player.PlayerId == killedActorNumber)
            {
                killedName = string.IsNullOrEmpty(player.ToString()) ? $"Player {killedActorNumber}" : player.ToString();
            }
        }
        
        string notificationText = $"{killerName} shot {killedName}";
        
        notificationQueue.Enqueue(notificationText);
        if (!isShowingNotification)
        {
            StartCoroutine(ProcessNotificationQueue());
        }
    }
    
    private IEnumerator ProcessNotificationQueue()
    {
        isShowingNotification = true;
        
        if (killNotificationText == null)
        {
            CacheLobbyUIReference();
        }
        
        while (notificationQueue.Count > 0)
        {
            string currentNotification = notificationQueue.Dequeue();
            
            if (killNotificationText != null)
            {
                killNotificationText.gameObject.SetActive(true);
                killNotificationText.text = currentNotification;
                
                yield return new WaitForSeconds(notificationDuration);
                
                killNotificationText.gameObject.SetActive(false);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        isShowingNotification = false;
    }
}
