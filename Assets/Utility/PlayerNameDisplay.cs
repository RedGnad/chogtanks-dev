using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerNameDisplay : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public Canvas nameCanvas;
    
    [Header("Position Settings")]
    public float heightOffset = 1.5f;
    
    [Header("Color Settings")]
    public Color localPlayerColor = Color.green; 
    public Color otherPlayerColor = Color.white;
    
    private bool isSubscribedToPlayerProps = false;
    
    private void Start()
    {
        SetPlayerName();
        
        if (nameCanvas != null)
        {
            nameCanvas.renderMode = RenderMode.WorldSpace;
            nameCanvas.worldCamera = Camera.main;
            nameCanvas.sortingOrder = 10;
            
            RectTransform canvasRect = nameCanvas.GetComponent<RectTransform>();
            canvasRect.localScale = Vector3.one * 0.02f; 
            canvasRect.sizeDelta = new Vector2(200, 50);
            
            nameCanvas.transform.localPosition = Vector3.zero;
        }
        
        if (nameText != null)
        {
            nameText.alignment = TextAlignmentOptions.Center;
        }
        
        UpdateTextPosition();
        
        if (!isSubscribedToPlayerProps)
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
            isSubscribedToPlayerProps = true;
        }
        
        StartCoroutine(RefreshPlayerNamePeriodically());
    }

    private void SetPlayerName()
    {
        if (nameText != null && photonView.Owner != null)
        {
            string playerName = photonView.Owner.NickName;
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = $"Player {photonView.Owner.ActorNumber}";
            }

            if (photonView.IsMine)
            {
                var nftManager = FindObjectOfType<ChogTanksNFTManager>();
                if (nftManager != null && nftManager.currentNFTState != null)
                {
                    int nftLevel = nftManager.currentNFTState.level;
                    
                    ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                    playerProps["level"] = nftLevel;
                    PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
                }
            }

            int playerLevel = 0;
            if (photonView.Owner.CustomProperties.ContainsKey("level"))
            {
                playerLevel = (int)photonView.Owner.CustomProperties["level"];
            }
            
            if (playerLevel > 0)
            {
                playerName += $" lvl {playerLevel}";
            }

            nameText.text = playerName;
            if (photonView.IsMine)
            {
                nameText.color = localPlayerColor;
            }
            else
            {
                nameText.color = otherPlayerColor;
            }
        }
    }

    private void UpdateTextPosition()
    {
        if (nameText != null)
        {
            nameText.transform.localPosition = new Vector3(0, heightOffset * 150, 0); 
        }
    }

    private void LateUpdate()
    {
        UpdateTextPosition();
        
        if (nameCanvas != null && Camera.main != null)
        {
            nameCanvas.transform.LookAt(Camera.main.transform);
            nameCanvas.transform.Rotate(0, 180, 0);
        }
    }
    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (photonView.Owner != null && targetPlayer.ActorNumber == photonView.Owner.ActorNumber)
        {
            if (changedProps.ContainsKey("level"))
            {
                SetPlayerName();
            }
        }
    }
    
    private void OnPhotonEvent(ExitGames.Client.Photon.EventData photonEvent)
    {
        if (photonEvent.Code == 226)
        {
            SetPlayerName();
        }
    }
    
    private IEnumerator RefreshPlayerNamePeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f); 
            SetPlayerName();
        }
    }
    
    void OnDestroy()
    {
        if (isSubscribedToPlayerProps)
        {
            PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
            isSubscribedToPlayerProps = false;
        }
        StopAllCoroutines();
    }

    public void SetLocalPlayerColor(Color color)
    {
        localPlayerColor = color;
        if (photonView.IsMine)
        {
            nameText.color = color;
        }
    }

    public void SetOtherPlayerColor(Color color)
    {
        otherPlayerColor = color;
        if (!photonView.IsMine)
        {
            nameText.color = color;
        }
    }
}