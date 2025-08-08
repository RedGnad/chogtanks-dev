using Fusion;

using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerNameDisplay : NetworkBehaviour
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
            nameText.fontSize = 50f;
        }
        
        UpdateTextPosition();
        
        if (!isSubscribedToPlayerProps)
        {
            // Fusion events handled differently
            isSubscribedToPlayerProps = true;
        }
        
        StartCoroutine(RefreshPlayerNamePeriodically());
    }

    private void SetPlayerName()
    {
        if (nameText != null && Object.InputAuthority != null)
        {
            string playerName = Object.InputAuthority.ToString();
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = $"Player {Object.InputAuthority.PlayerId}";
            }

            if (Object)
            {
                var nftManager = FindObjectOfType<ChogTanksNFTManager>();
                if (nftManager != null && nftManager.currentNFTState != null)
                {
                    int nftLevel = nftManager.currentNFTState.level;
                    
                    ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                    playerProps["level"] = nftLevel;
                    // Fusion player properties handled differently
                }
            }

            // PlayerRef doesn't have ContainsKey - using alternative approach
            if (Object.InputAuthority != null)
            {
                // int nftLevel = (int)Object.InputAuthority["level"]; // PlayerRef doesn't support indexing
                int nftLevel = 1; // Default level - TODO: implement proper level system
            }
            
            int playerLevel = 0;
            if (Object.InputAuthority.PlayerId == Object.InputAuthority.PlayerId)
            {
                playerLevel = 1; // Default level - TODO: implement proper level system
            }
            
            if (playerLevel > 0)
            {
                playerName += $" lvl {playerLevel}";
            }

            nameText.text = playerName;
            if (Object)
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
    
    // OnPlayerPropertiesUpdate removed for Fusion
    public void OnPlayerPropertiesUpdateFusion(PlayerRef targetPlayer)
    {
        if (Object.InputAuthority != null && targetPlayer.PlayerId == Object.InputAuthority.PlayerId)
        {
            // changedProps removed for Fusion
            // if (changedProps.ContainsKey("level")) // Removed for Fusion
            // {
                // SetPlayerName(); // TODO: Implement Fusion equivalent
            // }
        }
    }
    
    // private void OnPhotonEvent(ExitGames.Client.Photon.EventData photonEvent) // Removed for Fusion
    // {
        // if (photonEvent.Code == 226) // Removed for Fusion
        // {
            // SetPlayerName();
        // }
    // }
    
    private IEnumerator RefreshPlayerNamePeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f); 
            
            if (Object)
            {
                SetPlayerName();
            }
        }
    }
    
    void OnDestroy()
    {
        if (isSubscribedToPlayerProps)
        {
            // Fusion events handled differently
            isSubscribedToPlayerProps = false;
        }
        StopAllCoroutines();
    }

    public void SetLocalPlayerColor(Color color)
    {
        localPlayerColor = color;
        if (Object)
        {
            nameText.color = color;
        }
    }

    public void SetOtherPlayerColor(Color color)
    {
        otherPlayerColor = color;
        if (!Object)
        {
            nameText.color = color;
        }
    }
}