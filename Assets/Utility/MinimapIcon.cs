using Photon.Pun;
using UnityEngine;

public class MinimapIcon : MonoBehaviourPunCallbacks
{
    [Header("Configuration")]
    [SerializeField] private Color localPlayerColor = Color.green;
    [SerializeField] private Color otherPlayerColor = Color.red;
    [SerializeField] private float iconSize = 3f; 
    
    private GameObject iconInstance;
    private SpriteRenderer iconRenderer;
    
    [Header("Cloak Power-up")]
    private bool isCloaked = false;
    private float cloakEndTime = 0f;
    
    private void Start()
    {
        CreateMinimapIcon();
    }
    
    private void Update()
    {
        if (isCloaked && Time.time >= cloakEndTime)
        {
            SetCloaked(false);
        }
    }
    
    private void CreateMinimapIcon()
    {
        iconInstance = new GameObject("MinimapIcon");
        iconInstance.transform.SetParent(transform);
        
        iconInstance.transform.localPosition = Vector3.zero;
        iconInstance.transform.localScale = Vector3.one * iconSize;
        
        iconInstance.layer = LayerMask.NameToLayer("Minimap");
        
        iconRenderer = iconInstance.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = CreateSimpleSquare(); 
        iconRenderer.sortingOrder = 100; 
        
        Color iconColor;
        if (photonView.IsMine)
        {
            iconColor = localPlayerColor;
            iconColor.a = 1f; 
        }
        else
        {
            iconColor = otherPlayerColor;
            iconColor.a = 1f; 
        }
        
        iconRenderer.color = iconColor;
        
    }
    
    public void ActivateCloak(float duration)
    {
        photonView.RPC("RPC_ActivateCloak", RpcTarget.All, duration);
    }
    
    [PunRPC]
    private void RPC_ActivateCloak(float duration)
    {
        isCloaked = true;
        cloakEndTime = Time.time + duration;
        SetCloaked(true);
    }
    
    private void SetCloaked(bool cloaked)
    {
        isCloaked = cloaked;
        if (iconRenderer != null)
        {
            if (photonView.IsMine)
            {
                iconRenderer.enabled = true;
                if (cloaked)
                {
                    Color dimmedColor = iconRenderer.color;
                    dimmedColor.a = 0.5f;
                    iconRenderer.color = dimmedColor;
                }
                else
                {
                    Color normalColor = iconRenderer.color;
                    normalColor.a = 1f;
                    iconRenderer.color = normalColor;
                }
            }
            else
            {
                iconRenderer.enabled = !cloaked;
            }
        }
        
        if (!cloaked)
        {
            //Debug.Log("[MinimapIcon] Cloak deactivated");
        }
    }
    
    private Sprite CreateSimpleSquare()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    private void OnDestroy()
    {
        if (iconInstance != null)
        {
            Destroy(iconInstance);
        }
    }
}