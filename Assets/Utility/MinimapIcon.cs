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
    
    private void Start()
    {
        CreateMinimapIcon();
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