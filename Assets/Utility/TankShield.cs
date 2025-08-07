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
    public Sprite shieldSprite;
    
    [Header("Animation")]
    public float pulseIntensity = 0.3f; 
    public float rotationSpeed = 180f;
    
    private bool isShieldActive = false;
    private bool canUseShield = true;
    private GameObject currentShieldVisual;
    
    void Update()
    {
        if (!photonView.IsMine) return;
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"E pressed! canUseShield={canUseShield}, isShieldActive={isShieldActive}");
        }
        
        if (Input.GetKeyDown(KeyCode.E) && canUseShield && !isShieldActive)
        {
            ActivateShield();
        }
    }
    
    public void ActivateShield()
    {
        photonView.RPC("RPC_ActivateShield", RpcTarget.All);
    }
    
    [PunRPC]
    void RPC_ActivateShield()
    {
        if (isShieldActive) return;
        
        isShieldActive = true;
        canUseShield = false;
        
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayShieldActivation();
        }
        
        
        currentShieldVisual = new GameObject("Shield");
        
        Canvas canvas = currentShieldVisual.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 100;
        
        currentShieldVisual.transform.SetParent(transform);
        currentShieldVisual.transform.localPosition = Vector3.zero;
        currentShieldVisual.transform.localScale = Vector3.one * 0.01f; 
        
        GameObject imageObj = new GameObject("ShieldImage");
        imageObj.transform.SetParent(currentShieldVisual.transform);
        
        UnityEngine.UI.Image img = imageObj.AddComponent<UnityEngine.UI.Image>();
        
        if (shieldSprite != null)
        {
            img.sprite = shieldSprite;
            img.color = Color.white; 
        }
        else
        {
            img.color = new Color(0, 1, 1, 0.7f);
        }
        
        RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(7, 7); 
        rectTransform.localPosition = Vector3.zero;
        
        ShieldVisual shieldAnim = imageObj.AddComponent<ShieldVisual>();
        shieldAnim.pulseIntensity = pulseIntensity;
        shieldAnim.rotationSpeed = rotationSpeed;
        
        
        if (photonView.IsMine)
        {
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
        isShieldActive = false;
        
        if (currentShieldVisual != null)
        {
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
