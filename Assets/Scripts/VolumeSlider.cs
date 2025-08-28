using UnityEngine;
using UnityEngine.UI;

public enum AudioChannel { Music, SFX }

[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    [Header("Audio Channel")]
    [Tooltip("Définit si ce slider contrôle la musique ou les effets sonores (SFX).")]
    public AudioChannel channel;

    private Slider slider;
    private UnityEngine.Events.UnityAction<float> volumeListener;

    void Start()
    {
        slider = GetComponent<Slider>();
        // Attendre que VolumeController soit prêt
        StartCoroutine(WaitForVolumeController());
    }

    private System.Collections.IEnumerator WaitForVolumeController()
    {
        while (VolumeController.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        ConnectToVolumeController();
    }

    private void ConnectToVolumeController()
    {
        if (slider != null && VolumeController.Instance != null)
        {
            // Régler la valeur initiale du slider et créer le listener approprié
            switch (channel)
            {
                case AudioChannel.Music:
                    slider.value = VolumeController.Instance.GetMusicVolume();
                    volumeListener = (volume) => VolumeController.Instance.SetMusicVolume(volume);
                    break;
                case AudioChannel.SFX:
                    slider.value = VolumeController.Instance.GetSfxVolume();
                    volumeListener = (volume) => VolumeController.Instance.SetSfxVolume(volume);
                    break;
            }
            
            // Ajouter le listener
            slider.onValueChanged.AddListener(volumeListener);
            Debug.Log($"[VolumeSlider] for {channel} connected to VolumeController");
        }
    }

    private void OnDestroy()
    {
        // Nettoyer le listener pour éviter les erreurs
        if (slider != null && volumeListener != null)
        {
            slider.onValueChanged.RemoveListener(volumeListener);
        }
    }
}
