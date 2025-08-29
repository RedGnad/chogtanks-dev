using UnityEngine;
using System.Collections;

public class MenuCameraController : MonoBehaviour
{
    public static MenuCameraController Instance { get; private set; }
    
    [Header("Menu Camera Movement")]
    public Camera targetCamera;
    public float moveSpeed = 1f;
    public float moveRange = 8f;
    public float smoothness = 0.5f;
    
    [Header("Menu Music")]
    public AudioSource musicSource;
    public AudioClip menuMusic;
    public AudioClip[] gameMusicPlaylist; 
    [Range(0f, 1f)]
    public float musicVolume = 0.3f;
    
    private Vector3 originalCameraPosition;
    private Vector3 currentVelocity;
    private bool isMenuModeActive = false;
    private Coroutine movementCoroutine;
    private float noiseOffsetX;
    private float noiseOffsetY;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
            
        if (targetCamera != null)
            originalCameraPosition = targetCamera.transform.position;
        
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();
        }
        
        musicSource.clip = menuMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        
        StartCoroutine(TryAutoStartMusic());
    }
    
    private System.Collections.IEnumerator TryAutoStartMusic()
    {
        for (int i = 0; i < 10; i++)
        {
            yield return null;
        }
        
        if (menuMusic != null && musicSource != null)
        {
            musicSource.Play();
            
            yield return new WaitForSeconds(0.1f);
            if (!musicSource.isPlaying)
            {
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
                musicSource.Play();
            }
        }
    }
    
    public void EnableMenuMode()
    {
        if (isMenuModeActive) return;
        
        isMenuModeActive = true;
        
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(MenuCameraMovement());
        
        if (musicSource != null && menuMusic != null)
        {
            musicSource.clip = menuMusic;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }
    
    public void DisableMenuMode()
    {
        if (!isMenuModeActive) return;
        
        isMenuModeActive = false;
        
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        
    }
    
    public void FreezeCameraImmediately()
    {
        if (isMenuModeActive)
        {
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
            
            isMenuModeActive = false;
            
        }
    }
    
    private IEnumerator MenuCameraMovement()
    {
        if (targetCamera == null) yield break;
        
        noiseOffsetX = Random.Range(0f, 100f);
        noiseOffsetY = Random.Range(0f, 100f);
        
        while (isMenuModeActive)
        {
            float noiseX = (Mathf.PerlinNoise(noiseOffsetX, 0f) - 0.5f) * 2f; // -1 to 1
            float noiseY = (Mathf.PerlinNoise(0f, noiseOffsetY) - 0.5f) * 2f; // -1 to 1
            
            Vector3 targetPos = originalCameraPosition + new Vector3(
                noiseX * moveRange,
                noiseY * moveRange, 
                0f 
            );
            
            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                targetPos,
                smoothness * Time.deltaTime
            );
            
            noiseOffsetX += moveSpeed * Time.deltaTime * 0.1f;
            noiseOffsetY += moveSpeed * Time.deltaTime * 0.08f; // Slightly different speed for more organic movement
            
            yield return null;
        }
    }
    
    
    private IEnumerator ResetCameraPosition()
    {
        if (targetCamera == null) yield break;
        
        Vector3 startPosition = targetCamera.transform.position;
        float resetDuration = 2f;
        float elapsed = 0f;
        
        while (elapsed < resetDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / resetDuration;
            
            targetCamera.transform.position = Vector3.Lerp(startPosition, originalCameraPosition, t);
            
            yield return null;
        }
        
        targetCamera.transform.position = originalCameraPosition;
    }
    
    public bool IsMenuModeActive()
    {
        return isMenuModeActive;
    }
    
    public void StartGameMusic()
    {
        if (musicSource != null && gameMusicPlaylist != null && gameMusicPlaylist.Length > 0)
        {
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            int randomIndex = Random.Range(0, gameMusicPlaylist.Length);
            AudioClip clipToPlay = gameMusicPlaylist[randomIndex];

            if (clipToPlay != null)
            {
                musicSource.clip = clipToPlay;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
            else
            {
                Debug.LogWarning("[MenuCameraController] Selected audio clip is null.");
            }
        }
        else
        {
            Debug.LogWarning("[MenuCameraController] Game music playlist is empty or not assigned.");
        }
    }
    
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }
}
