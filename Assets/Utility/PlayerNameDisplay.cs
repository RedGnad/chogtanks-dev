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
    public UnityEngine.UI.Image monadBadgeImage; 
    [Tooltip("Optionnel: assignez un TMP (UGUI ou 3D) distinct pour afficher uniquement le niveau (avec votre Text Effect). Laissez vide pour garder le comportement d'origine (nom + lvl ensemble).")]
    public TMP_Text levelText;
    [Tooltip("Préfixe ajouté avant la valeur du niveau dans levelText.")]
    public string levelPrefix = " lvl ";
    
    [Header("Position Settings")]
    public float heightOffset = 1.5f;
    
    [Header("Color Settings")]
    public Color localPlayerColor = Color.green; 
    public Color otherPlayerColor = Color.white;
    [Tooltip("Quand activé, ce script applique la couleur (local/autre). Désactive-le pour laisser les Text Effects / Matériaux TMP gérer la couleur.")]
    public bool overrideTextColor = true;

    // Rich Text annulé: nous utilisons désormais un TMP séparé (levelText) pour appliquer un Text Effect uniquement au niveau.
    
    private bool isSubscribedToPlayerProps = false;
    private Coroutine badgeRotateCo;
    
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
            nameText.richText = true;
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

            // Get player level
            int playerLevel = 0;
            if (photonView.Owner.CustomProperties.ContainsKey("level"))
            {
                playerLevel = (int)photonView.Owner.CustomProperties["level"];
            }
            
            // Rendu: si levelText est assigné, séparer nom et niveau pour appliquer un Text Effect seulement au niveau
            if (levelText != null)
            {
                nameText.text = playerName;
                if (playerLevel > 0)
                {
                    levelText.text = $"{levelPrefix}{playerLevel}";
                    levelText.gameObject.SetActive(true);
                }
                else
                {
                    levelText.text = string.Empty;
                    levelText.gameObject.SetActive(false);
                }
            }
            else
            {
                // Fallback: concaténer le niveau au nom si aucun TMP séparé n'est fourni
                if (playerLevel > 0)
                {
                    playerName += $" lvl {playerLevel}";
                }
                nameText.text = playerName;
            }

            // Show/hide Monad ID verified badge image
            bool isMonadVerified = IsPlayerMonadVerified(photonView.Owner);
            
            if (monadBadgeImage != null)
            {
                monadBadgeImage.gameObject.SetActive(isMonadVerified);
                
                // Ajouter rotation au badge si visible
                if (isMonadVerified)
                {
                    if (badgeRotateCo == null)
                    {
                        badgeRotateCo = StartCoroutine(RotateBadge());
                    }
                }
                else
                {
                    if (badgeRotateCo != null)
                    {
                        StopCoroutine(badgeRotateCo);
                        badgeRotateCo = null;
                    }
                }
                
            }
            else
            {
                Debug.LogWarning($"[BADGE-DEBUG] monadBadgeImage is NULL for {photonView.Owner.NickName}");
            }

            // Set text color (optionnel): laisse les Text Effects gérer la couleur si overrideTextColor est faux
            if (overrideTextColor)
            {
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
            yield return new WaitForSeconds(60f); 
            
            if (photonView.IsMine)
            {
                SetPlayerName();
            }
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
    badgeRotateCo = null;
    }

    public void SetLocalPlayerColor(Color color)
    {
        localPlayerColor = color;
    if (photonView.IsMine && overrideTextColor)
        {
            nameText.color = color;
        }
    }

    public void SetOtherPlayerColor(Color color)
    {
        otherPlayerColor = color;
    if (!photonView.IsMine && overrideTextColor)
        {
            nameText.color = color;
        }
    }
    
    /// <summary>
    /// Vérifie si un joueur a un Monad ID verified
    /// </summary>
    private bool IsPlayerMonadVerified(Player player)
    {
        if (player == null) return false;
        
        // 1) Vérifier l'état runtime reçu via RPC
        if (MonadBadgeState.TryGet(player.ActorNumber, out bool cached))
        {
            return cached;
        }
        
        // 2) Pour le joueur local, fallback PlayerPrefs (au cas où le RPC n'a pas encore été reçu)
        if (player == PhotonNetwork.LocalPlayer)
        {
            return PlayerPrefs.GetInt("MonadVerified", 0) == 1;
        }
        
        return false;
    }
    
    /// <summary>
    /// Coroutine pour faire tourner le badge Monad ID
    /// </summary>
    private System.Collections.IEnumerator RotateBadge()
    {
        if (monadBadgeImage == null) yield break;
        
        float rotationSpeed = 90f; // Degrés par seconde
        
        while (monadBadgeImage.gameObject.activeInHierarchy)
        {
            monadBadgeImage.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // RPC pour synchroniser explicitement l'état du badge sur tous les clients (alternative aux Custom Properties)

    // Permet à d'autres composants (RPC côté root) de piloter le badge sans dupliquer la logique
    public void SetBadgeState(bool isVerified)
    {
        if (monadBadgeImage == null)
        {
            Debug.LogWarning("[BADGE] monadBadgeImage is null; cannot set badge state");
            return;
        }

        monadBadgeImage.gameObject.SetActive(isVerified);

        if (isVerified)
        {
            if (badgeRotateCo == null)
            {
                badgeRotateCo = StartCoroutine(RotateBadge());
            }
        }
        else
        {
            if (badgeRotateCo != null)
            {
                StopCoroutine(badgeRotateCo);
                badgeRotateCo = null;
            }
        }
    }

}