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
    public AudioClip[] gameMusicPlaylist; // Changed to an array for the playlist
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
        // Setup camera
        if (targetCamera == null)
            targetCamera = Camera.main;
            
        if (targetCamera != null)
            originalCameraPosition = targetCamera.transform.position;
        
        // Setup audio
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();
        }
        
        musicSource.clip = menuMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        
        // Essayer de lancer la musique après quelques frames
        StartCoroutine(TryAutoStartMusic());
    }
    
    private System.Collections.IEnumerator TryAutoStartMusic()
    {
        // Attendre 10 frames comme suggéré
        for (int i = 0; i < 10; i++)
        {
            yield return null;
        }
        
        if (menuMusic != null && musicSource != null)
        {
            musicSource.Play();
            Debug.Log("[MenuCameraController] Tentative de lancement automatique de la musique");
            
            // Si ça ne marche pas, simuler une interaction utilisateur
            yield return new WaitForSeconds(0.1f);
            if (!musicSource.isPlaying)
            {
                // Simuler un clic en déclenchant l'événement d'interaction
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
                musicSource.Play();
                Debug.Log("[MenuCameraController] Simulation d'interaction pour la musique");
            }
        }
    }
    
    // Called directly by LobbyUI when joinPanel becomes active
    public void EnableMenuMode()
    {
        if (isMenuModeActive) return;
        
        isMenuModeActive = true;
        Debug.Log("[MenuCameraController] Menu mode enabled");
        
        // Start camera movement
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(MenuCameraMovement());
        
        // Start menu music
        if (musicSource != null && menuMusic != null)
        {
            musicSource.clip = menuMusic;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }
    
    // Called directly by LobbyUI when joinPanel becomes inactive
    public void DisableMenuMode()
    {
        if (!isMenuModeActive) return;
        
        isMenuModeActive = false;
        Debug.Log("[MenuCameraController] Menu mode disabled");
        
        // Stop camera movement IMMEDIATELY
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        
        // Stop music
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        
        // DON'T reset camera position - let the game camera take over immediately
        // This prevents the conflict/trembling
    }
    
    // Method to freeze camera immediately (called before any button action)
    public void FreezeCameraImmediately()
    {
        if (isMenuModeActive)
        {
            // Stop movement coroutine instantly
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
            
            // Mark as inactive to prevent any further movement
            isMenuModeActive = false;
            
            Debug.Log("[MenuCameraController] Camera frozen immediately");
        }
    }
    
    private IEnumerator MenuCameraMovement()
    {
        if (targetCamera == null) yield break;
        
        // Initialize noise offsets for smooth random movement
        noiseOffsetX = Random.Range(0f, 100f);
        noiseOffsetY = Random.Range(0f, 100f);
        
        while (isMenuModeActive)
        {
            // Generate smooth continuous movement using Perlin noise
            float noiseX = (Mathf.PerlinNoise(noiseOffsetX, 0f) - 0.5f) * 2f; // -1 to 1
            float noiseY = (Mathf.PerlinNoise(0f, noiseOffsetY) - 0.5f) * 2f; // -1 to 1
            
            // Calculate target position on 2D plane (X and Y, keeping Z constant)
            Vector3 targetPos = originalCameraPosition + new Vector3(
                noiseX * moveRange,
                noiseY * moveRange, // Equal vertical and horizontal movement
                0f // Keep Z constant for 2D plane
            );
            
            // Smooth movement towards target
            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                targetPos,
                smoothness * Time.deltaTime
            );
            
            // Increment noise offsets for continuous movement
            noiseOffsetX += moveSpeed * Time.deltaTime * 0.1f;
            noiseOffsetY += moveSpeed * Time.deltaTime * 0.08f; // Slightly different speed for more organic movement
            
            yield return null;
        }
    }
    
    // Removed GenerateNewTarget - using continuous Perlin noise instead
    
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
    
    // Method to start game music when entering waitingPanel
    public void StartGameMusic()
    {
        if (musicSource != null && gameMusicPlaylist != null && gameMusicPlaylist.Length > 0)
        {
            // Stop any currently playing music first
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            // Select a random clip from the playlist
            int randomIndex = Random.Range(0, gameMusicPlaylist.Length);
            AudioClip clipToPlay = gameMusicPlaylist[randomIndex];

            if (clipToPlay != null)
            {
                musicSource.clip = clipToPlay;
                musicSource.volume = musicVolume;
                musicSource.Play();
                Debug.Log($"[MenuCameraController] Game music started with clip: {clipToPlay.name}");
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
    
    // Method to stop any music
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            Debug.Log("[MenuCameraController] Music stopped");
        }
    }
}
