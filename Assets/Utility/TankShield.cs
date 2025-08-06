using Photon.Pun;
using UnityEngine;
using System.Collections;

public class TankShield : MonoBehaviourPun
{
    [Header("Shield Settings")]
    public float shieldDuration = 1f;
    public float shieldCooldown = 5f;
    public KeyCode shieldKey = KeyCode.E;
    
    [Header("Visual")]
    public Sprite shieldSprite; // Sprite à assigner dans l'Inspector
    
    [Header("Animation")]
    public float pulseIntensity = 0.3f; // Intensité de la pulsation
    public float rotationSpeed = 180f; // Vitesse de rotation
    
    private bool isShieldActive = false;
    private bool canUseShield = true;
    private GameObject currentShieldVisual;
    
    void Update()
    {
        if (!photonView.IsMine) return;
        
        // Test si le script fonctionne
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"E pressed! canUseShield={canUseShield}, isShieldActive={isShieldActive}");
        }
        
        // Utiliser explicitement KeyCode.E pour éviter les conflits
        if (Input.GetKeyDown(KeyCode.E) && canUseShield && !isShieldActive)
        {
            Debug.Log("Shield activated with E key!");
            ActivateShield();
        }
    }
    
    void ActivateShield()
    {
        photonView.RPC("RPC_ActivateShield", RpcTarget.All);
    }
    
    [PunRPC]
    void RPC_ActivateShield()
    {
        if (isShieldActive) return;
        
        Debug.Log($"RPC_ActivateShield called for {photonView.Owner?.NickName}");
        
        isShieldActive = true;
        canUseShield = false;
        
        // Créer l'effet visuel - CANVAS qui fonctionne
        Debug.Log("Creating CANVAS shield visual");
        
        currentShieldVisual = new GameObject("Shield");
        
        // Canvas pour être SÛR de le voir
        Canvas canvas = currentShieldVisual.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 100;
        
        // Position du canvas - scale normal
        currentShieldVisual.transform.SetParent(transform);
        currentShieldVisual.transform.localPosition = Vector3.zero;
        currentShieldVisual.transform.localScale = Vector3.one * 0.01f; // Scale normal
        
        // Créer l'image dans le canvas
        GameObject imageObj = new GameObject("ShieldImage");
        imageObj.transform.SetParent(currentShieldVisual.transform);
        
        UnityEngine.UI.Image img = imageObj.AddComponent<UnityEngine.UI.Image>();
        
        // Utiliser le sprite personnalisé si assigné
        if (shieldSprite != null)
        {
            img.sprite = shieldSprite;
            img.color = Color.white; // Couleur neutre pour voir le sprite
            Debug.Log("Using custom shield sprite");
        }
        else
        {
            img.color = new Color(0, 1, 1, 0.7f); // Cyan translucide par défaut
            Debug.Log("Using default cyan color");
        }
        
        // Taille du bouclier autour du tank
        RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(7, 7); // ENCORE plus petit
        rectTransform.localPosition = Vector3.zero;
        
        // Ajouter rotation indépendante et pulsation
        ShieldVisual shieldAnim = imageObj.AddComponent<ShieldVisual>();
        shieldAnim.pulseIntensity = pulseIntensity;
        shieldAnim.rotationSpeed = rotationSpeed;
        
        Debug.Log($"Shield created at: {currentShieldVisual.transform.position}");
        
        // Démarrer les timers seulement pour le propriétaire
        if (photonView.IsMine)
        {
            Debug.Log("Starting shield timers");
            StartCoroutine(ShieldDurationCoroutine());
            StartCoroutine(ShieldCooldownCoroutine());
        }
    }
    
    IEnumerator ShieldDurationCoroutine()
    {
        yield return new WaitForSeconds(shieldDuration);
        photonView.RPC("RPC_DeactivateShield", RpcTarget.All);
    }
    
    IEnumerator ShieldCooldownCoroutine()
    {
        yield return new WaitForSeconds(shieldCooldown);
        canUseShield = true;
    }
    
    [PunRPC]
    void RPC_DeactivateShield()
    {
        Debug.Log($"Shield deactivated for {photonView.Owner?.NickName}");
        isShieldActive = false;
        
        if (currentShieldVisual != null)
        {
            Debug.Log("Destroying shield visual");
            Destroy(currentShieldVisual);
        }
        else
        {
            Debug.Log("No shield visual to destroy");
        }
    }
    
    public bool IsShieldActive()
    {
        return isShieldActive;
    }
    
    public bool CanUseShield()
    {
        return canUseShield;
    }
}
