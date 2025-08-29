using UnityEngine;
using UnityEngine.Audio;

public class VolumeController : MonoBehaviour
{
    public static VolumeController Instance { get; private set; }

    [Header("Audio Mixer")]
    public AudioMixer masterMixer; 

    public const string MUSIC_VOLUME_KEY = "MusicVolume";
    public const string SFX_VOLUME_KEY = "SfxVolume";

    private const string MUSIC_PREF_KEY = "MusicVolumePreference";
    private const string SFX_PREF_KEY = "SfxVolumePreference";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadVolumeSettings();
    }

    private void LoadVolumeSettings()
    {
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_PREF_KEY, 0.75f);
        SetMusicVolume(musicVolume, false); 

        float sfxVolume = PlayerPrefs.GetFloat(SFX_PREF_KEY, 0.75f);
        SetSfxVolume(sfxVolume, false); 
    }

    private float ConvertToDecibel(float volume)
    {
        return Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
    }

    public void SetMusicVolume(float volume, bool savePreference = true)
    {
        if (masterMixer == null) return;
        masterMixer.SetFloat(MUSIC_VOLUME_KEY, ConvertToDecibel(volume));
        if (savePreference)
        {
            PlayerPrefs.SetFloat(MUSIC_PREF_KEY, volume);
            PlayerPrefs.Save();
        }
    }

    public void SetSfxVolume(float volume, bool savePreference = true)
    {
        if (masterMixer == null) return;
        masterMixer.SetFloat(SFX_VOLUME_KEY, ConvertToDecibel(volume));
        if (savePreference)
        {
            PlayerPrefs.SetFloat(SFX_PREF_KEY, volume);
            PlayerPrefs.Save();
        }
    }

    public float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MUSIC_PREF_KEY, 0.75f);
    }

    public float GetSfxVolume()
    {
        return PlayerPrefs.GetFloat(SFX_PREF_KEY, 0.75f);
    }
}
