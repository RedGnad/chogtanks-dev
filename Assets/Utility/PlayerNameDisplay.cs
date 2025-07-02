using Photon.Pun;
using UnityEngine;
using TMPro;

public class PlayerNameDisplay : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public Canvas nameCanvas;
    
    [Header("Position Settings")]
    public float heightOffset = 1.5f;
    
    [Header("Color Settings")]
    public Color localPlayerColor = Color.green;  // Couleur pour le joueur local
    public Color otherPlayerColor = Color.white;  // Couleur pour les autres joueurs
    
    private void Start()
    {
        SetPlayerName();
        
        if (nameCanvas != null)
        {
            nameCanvas.renderMode = RenderMode.WorldSpace;
            nameCanvas.worldCamera = Camera.main;
            nameCanvas.sortingOrder = 10;
            
            RectTransform canvasRect = nameCanvas.GetComponent<RectTransform>();
            canvasRect.localScale = Vector3.one * 0.02f; // PLUS GROS
            canvasRect.sizeDelta = new Vector2(200, 50);
            
            nameCanvas.transform.localPosition = Vector3.zero;
        }
        
        // AJOUT : Centrer le texte
        if (nameText != null)
        {
            nameText.alignment = TextAlignmentOptions.Center;
        }
        
        UpdateTextPosition();
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
            nameText.text = playerName;
            
            // Utiliser les couleurs configurables
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
            nameText.transform.localPosition = new Vector3(0, heightOffset * 150, 0); // PLUS HAUT
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

    // Méthodes publiques pour changer les couleurs depuis un autre script
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