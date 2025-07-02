using Photon.Pun;
using UnityEngine;

public class MinimapIcon : MonoBehaviourPunCallbacks
{
    [Header("Configuration")]
    [SerializeField] private Color localPlayerColor = Color.green;
    [SerializeField] private Color otherPlayerColor = Color.red;
    [SerializeField] private float iconSize = 3f; // PLUS GROS pour debug
    
    private GameObject iconInstance;
    private SpriteRenderer iconRenderer;
    
    private void Start()
    {
        CreateMinimapIcon();
    }
    
    private void CreateMinimapIcon()
    {
        // Crée un GameObject pour l'icône
        iconInstance = new GameObject("MinimapIcon");
        iconInstance.transform.SetParent(transform);
        
        // Position IDENTIQUE au tank
        iconInstance.transform.localPosition = Vector3.zero;
        iconInstance.transform.localScale = Vector3.one * iconSize;
        
        // AJOUT : Mettre sur le layer Minimap pour qu'il ne soit visible que sur la minimap
        iconInstance.layer = LayerMask.NameToLayer("Minimap");
        
        // Ajoute un SpriteRenderer
        iconRenderer = iconInstance.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = CreateSimpleSquare(); // Carré simple pour debug
        iconRenderer.sortingOrder = 100; // TRÈS au-dessus
        
        // Couleur TRÈS visible
        Color iconColor;
        if (photonView.IsMine)
        {
            iconColor = localPlayerColor;
            iconColor.a = 1f; // Opaque
        }
        else
        {
            iconColor = otherPlayerColor;
            iconColor.a = 1f; // Opaque
        }
        
        iconRenderer.color = iconColor;
        
        Debug.Log($"[MinimapIcon] Icône créée - Position: {transform.position}, Couleur: {iconColor}, IsMine: {photonView.IsMine}");
    }
    
    private Sprite CreateSimpleSquare()
    {
        // Carré simple 32x32 PLEIN
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        // TOUT en blanc
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    private void Update()
    {
        // DEBUG : Affiche les infos toutes les 2 secondes
        if (Time.time % 2f < 0.1f)
        {
            if (iconInstance != null)
            {
                Debug.Log($"[MinimapIcon] Tank: {transform.position}, Icône: {iconInstance.transform.position}, Visible: {iconRenderer.enabled}");
            }
        }
    }
    
    private void OnDestroy()
    {
        if (iconInstance != null)
        {
            Destroy(iconInstance);
        }
    }
}