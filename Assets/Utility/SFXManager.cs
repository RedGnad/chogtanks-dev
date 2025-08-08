using Fusion;
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

public class SFXManager : NetworkBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public float masterVolume = 1f;
    
    [Header("Multiplayer Audio")]
    [Range(0f, 1f)]
    public float multiplayerVolumeMultiplier = 0.5f;
    
    [Header("SFX Configuration")]
    public SFXClip[] sfxClips;
    
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
        if (string.IsNullOrEmpty(sfxName) || sfxDictionary == null) return;
        
        if (!sfxDictionary.TryGetValue(sfxName, out SFXClip sfxClip))
        {
            Debug.LogWarning($"[SFX] Audio clip not found: {sfxName}");
            return;
        }
        
        float finalVolume = sfxClip.defaultVolume * volumeMultiplier;
        
        if (sfxClip.shareInMultiplayer)
        {
            PlaySFXRPC(sfxName, finalVolume);
        }
        
        PlayLocalSFX(sfxClip.clip, finalVolume);
    }
    
    private void PlayLocalSFX(AudioClip clip, float volume)
    {
        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume * masterVolume);
        }
    }
    
    [Rpc]
    void PlaySFXRPC(string sfxName, float volume)
    {
        if (sfxDictionary != null && sfxDictionary.TryGetValue(sfxName, out SFXClip sfxClip))
        {
            PlayLocalSFX(sfxClip.clip, volume * multiplayerVolumeMultiplier);
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
}
