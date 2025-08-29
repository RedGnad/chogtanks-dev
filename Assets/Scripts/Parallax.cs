using UnityEngine;

public class ParallaxVariety : MonoBehaviour
{
    [Header("Parallax Settings")]
    public GameObject cam;
    public float parallaxSpeedX = 0.5f;
    public float parallaxSpeedY = 0.3f;
    
    [Header("Element Generation")]
    public int elementCount = 8;
    public Vector2 spawnAreaSize = new Vector2(40f, 20f);
    public Vector2 sizeRange = new Vector2(0.5f, 2f);
    public Vector2 rotationRange = new Vector2(-30f, 30f);
    public float minDistance = 3f;
    
    [Header("Visual Settings")]
    public Vector2 alphaRange = new Vector2(0.4f, 1f); 
    public Color[] colorPalette = { Color.white, new Color(1f, 0.8f, 0.6f), new Color(0.8f, 1f, 0.9f) };
    public bool useRandomColors = true;
    
    [Header("Blur Effect")]
    public bool enableBlur = false;
    [Range(0f, 10f)]
    public float blurIntensity = 2f;
    
    private Vector3 lastCamPos;
    private bool isInitialized = false;
    private GameObject[] elements;
    private Vector3[] originalPositions;
    
    void Start()
    {
        if (elementCount > 0)
        {
            CreateVariedElements();
        }
    }
    
    void CreateVariedElements()
    {
        elements = new GameObject[elementCount];
        originalPositions = new Vector3[elementCount];
        
        SpriteRenderer originalSR = GetComponent<SpriteRenderer>();
        if (originalSR == null) return;
        
        originalSR.enabled = false;
        
        for (int i = 0; i < elementCount; i++)
        {
            elements[i] = new GameObject($"ParallaxElement_{i}");
            elements[i].transform.parent = transform;
            
            SpriteRenderer sr = elements[i].AddComponent<SpriteRenderer>();
            sr.sprite = originalSR.sprite;
            sr.sortingOrder = originalSR.sortingOrder;
            
            
            Vector3 position = GetRandomPosition(i);
            elements[i].transform.position = position;
            originalPositions[i] = position;
            
            float scale = Random.Range(sizeRange.x, sizeRange.y);
            elements[i].transform.localScale = Vector3.one * scale;
            
            float rotation = Random.Range(rotationRange.x, rotationRange.y);
            elements[i].transform.rotation = Quaternion.Euler(0, 0, rotation);
            
            if (enableBlur && blurIntensity > 0)
            {
                ApplyBlurEffect(sr);
            }
            
            Color color = useRandomColors ? GetRandomColor() : Color.white;
            color.a = Random.Range(alphaRange.x, alphaRange.y);
            sr.color = color;
        }
    }
    
    Vector3 GetRandomPosition(int index)
    {
        Vector3 position;
        int attempts = 0;
        
        do
        {
            position = new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
                transform.position.z
            );
            attempts++;
        }
        while (IsTooClose(position, index) && attempts < 20);
        
        return position + transform.position;
    }
    
    bool IsTooClose(Vector3 newPos, int currentIndex)
    {
        for (int i = 0; i < currentIndex; i++)
        {
            if (Vector3.Distance(newPos, originalPositions[i] - transform.position) < minDistance)
                return true;
        }
        return false;
    }
    
    Color GetRandomColor()
    {
        if (colorPalette.Length == 0) return Color.white;
        
        Color baseColor = colorPalette[Random.Range(0, colorPalette.Length)];
        
        float variation = 0.2f;
        baseColor.r += Random.Range(-variation, variation);
        baseColor.g += Random.Range(-variation, variation);
        baseColor.b += Random.Range(-variation, variation);
        
        baseColor.r = Mathf.Clamp01(baseColor.r);
        baseColor.g = Mathf.Clamp01(baseColor.g);
        baseColor.b = Mathf.Clamp01(baseColor.b);
        
        return baseColor;
    }
    
    void ApplyBlurEffect(SpriteRenderer sr)
    {
        if (blurIntensity <= 0) return;
        
        Material blurMaterial = new Material(Shader.Find("UI/Default"));
        blurMaterial.mainTexture = sr.sprite.texture;
        
        Color blurColor = sr.color;
        blurColor.a *= (1f - (blurIntensity * 0.1f));
        sr.color = blurColor;
        
        sr.material = blurMaterial;
    }
    
    
    void Update()
    {
        if (!isInitialized)
        {
            lastCamPos = cam.transform.position;
            isInitialized = true;
            return;
        }
        
        Vector3 deltaMovement = cam.transform.position - lastCamPos;
        
        if (elements != null)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] != null)
                {
                    elements[i].transform.position += new Vector3(
                        deltaMovement.x * parallaxSpeedX,
                        deltaMovement.y * parallaxSpeedY,
                        0f
                    );
                }
            }
        }
        
        lastCamPos = cam.transform.position;
    }
    
    
    public void ResetParallax()
    {
        if (cam != null)
        {
            lastCamPos = cam.transform.position;
            isInitialized = false;
        }
    }
    
    void OnDestroy()
    {
        if (elements != null)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] != null)
                {
                    DestroyImmediate(elements[i]);
                }
            }
        }
    }
}
