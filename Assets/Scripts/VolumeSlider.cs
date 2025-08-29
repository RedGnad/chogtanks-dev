using UnityEngine;
using UnityEngine.UI;

public enum AudioChannel { Music, SFX }

[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    [Header("Audio Channel")]
    public AudioChannel channel;

    private Slider slider;
    private UnityEngine.Events.UnityAction<float> volumeListener;

    void Start()
    {
        slider = GetComponent<Slider>();
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
            
            slider.onValueChanged.AddListener(volumeListener);
        }
    }

    private void OnDestroy()
    {
        if (slider != null && volumeListener != null)
        {
            slider.onValueChanged.RemoveListener(volumeListener);
        }
    }
}
