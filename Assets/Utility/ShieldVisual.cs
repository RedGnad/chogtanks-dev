using UnityEngine;

public class ShieldVisual : MonoBehaviour
{
    [Header("Animation")]
    public float rotationSpeed = 180f;
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.3f;
    
    private Vector3 baseScale;
    private RectTransform rectTransform;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            baseScale = rectTransform.localScale;
        }
        else
        {
            baseScale = transform.localScale;
        }
    }
    
    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && !renderer.isVisible) return;
        
        Vector3 currentEuler = transform.eulerAngles;
        currentEuler.z += rotationSpeed * Time.deltaTime;
        transform.eulerAngles = currentEuler;
        
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
        Vector3 newScale = baseScale * pulse;
        
        if (rectTransform != null)
        {
            rectTransform.localScale = newScale;
        }
        else
        {
            transform.localScale = newScale;
        }
    }
}
