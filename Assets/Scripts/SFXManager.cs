using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class SFXClip
{
    public string name;
    public AudioClip clip;
    public bool shareInMultiplayer = true;
    public float defaultVolume = 1f;
}

public class SFXManager : MonoBehaviourPun
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public float masterVolume = 1f;
    
    [Header("Multiplayer Audio")]
    [Range(0f, 1f)]
    public float multiplayerVolumeMultiplier = 0.5f;
    
    [Header("SFX Configuration")]
    public SFXClip[] sfxClips;
    
    [Header("Killfeed Sounds")]
    public AudioClip[] killFeedSounds; 
    [Range(0f, 1f)]
    public float killFeedVolume = 0.8f; 
    
    [Header("Countdown Sounds")]
    public AudioClip countdownBeepSound;
    [Range(0f, 1f)]
    public float countdownVolume = 0.9f; 
    
    private static SFXManager instance;
    private Dictionary<string, SFXClip> sfxDictionary;
    
    public static SFXManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<SFXManager>();
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSFXDictionary();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }
    
    private void InitializeSFXDictionary()
    {
        sfxDictionary = new Dictionary<string, SFXClip>();
        foreach (var sfxClip in sfxClips)
        {
            if (!string.IsNullOrEmpty(sfxClip.name) && sfxClip.clip != null)
            {
                sfxDictionary[sfxClip.name] = sfxClip;
            }
        }
    }
    
    public void PlaySFX(string sfxName, float volumeMultiplier = 1f)
    {
        PlaySFX(sfxName, volumeMultiplier, 1.0f);
    }

    public void PlaySFX(string sfxName, float volumeMultiplier, float pitch)
    {
        if (string.IsNullOrEmpty(sfxName) || sfxDictionary == null) return;
        
        if (!sfxDictionary.TryGetValue(sfxName, out SFXClip sfxClip))
        {
            Debug.LogWarning($"[SFX] Audio clip not found: {sfxName}");
            return;
        }
        
        float finalVolume = sfxClip.defaultVolume * volumeMultiplier;
        
        if (sfxClip.shareInMultiplayer)
        {
            photonView.RPC("RPC_PlaySFX", RpcTarget.Others, sfxName, finalVolume, pitch);
        }
        
        PlayLocalSFX(sfxClip.clip, finalVolume, pitch);
    }
    
    private void PlayLocalSFX(AudioClip clip, float volume, float pitch)
    {
        if (audioSource != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, volume * masterVolume);
            audioSource.pitch = 1f; 
        }
    }
    
    [PunRPC]
    void RPC_PlaySFX(string sfxName, float volume, float pitch)
    {
        if (sfxDictionary != null && sfxDictionary.TryGetValue(sfxName, out SFXClip sfxClip))
        {
            PlayLocalSFX(sfxClip.clip, volume * multiplayerVolumeMultiplier, pitch);
        }
    }
    
    public void PlayShieldActivation()
    {
        PlaySFX("shield_activation", 0.8f);
    }
    
    public void PlayExplosion()
    {
        PlaySFX("explosion_big", 1f);
    }
    
    public void PlayWeaponFire()
    {
        PlaySFX("weapon_fire", 0.6f);
    }
    
    public void PlayTankDeath()
    {
        PlaySFX("tank_death", 1f);
    }
    
    public void PlayPowerupPickup()
    {
        PlaySFX("powerup_pickup", 0.7f);
    }
    
    public void PlayRandomKillFeedSoundLocal()
    {
        
        if (killFeedSounds == null || killFeedSounds.Length == 0)
        {
            return;
        }
        
        int randomIndex = Random.Range(0, killFeedSounds.Length);
        AudioClip randomClip = killFeedSounds[randomIndex];
        
        if (randomClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(randomClip, killFeedVolume * masterVolume);
        }
        else
        {
            Debug.LogError("[SFX] ❌ Le clip audio ou AudioSource est null !");
        }
    }
    
    public void PlayCountdownBeep()
    {
        
        if (countdownBeepSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(countdownBeepSound, countdownVolume * masterVolume);
        }
        else
        {
            Debug.LogWarning("[SFX] ⚠️ Son de décompte ou AudioSource manquant !");
        }
    }
    
    [PunRPC]
    void RPC_PlayKillFeedSFX(int clipIndex, float volume)
    {
        if (killFeedSounds != null && clipIndex >= 0 && clipIndex < killFeedSounds.Length)
        {
            AudioClip clipToPlay = killFeedSounds[clipIndex];
            if (clipToPlay != null)
            {
                PlayLocalSFX(clipToPlay, volume * multiplayerVolumeMultiplier, 1.0f);
            }
        }
    }
}
