using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsAudioController : MonoBehaviour
{
    private const string MasterVolumeSaveKey =
        "MasterVolume";

    private const float StartupDefaultVolume = 0.8f;

    [Header("UI")]
    [SerializeField]
    private Slider masterVolumeSlider;

    [SerializeField]
    private TMP_Text masterVolumeValueText;

    [Header("Default Value")]
    [SerializeField]
    [Range(0f, 1f)]
    private float defaultVolume = 0.8f;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad
    )]
    private static void ApplySavedVolumeAtStartup()
    {
        float savedVolume = PlayerPrefs.GetFloat(
            MasterVolumeSaveKey,
            StartupDefaultVolume
        );

        ApplyMasterVolume(savedVolume);
    }

    private void Awake()
    {
        float savedVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(
                MasterVolumeSaveKey,
                defaultVolume
            )
        );

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(
                savedVolume
            );
        }

        ApplyMasterVolume(savedVolume);
        UpdateVolumeText(savedVolume);
    }

    private void OnEnable()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(
                HandleMasterVolumeChanged
            );
        }
    }

    private void OnDisable()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(
                HandleMasterVolumeChanged
            );
        }
    }

    private void HandleMasterVolumeChanged(float volume)
    {
        volume = Mathf.Clamp01(volume);

        ApplyMasterVolume(volume);
        UpdateVolumeText(volume);

        PlayerPrefs.SetFloat(
            MasterVolumeSaveKey,
            volume
        );

        PlayerPrefs.Save();
    }

    private static void ApplyMasterVolume(float volume)
    {
        // Bütün AudioSource'ları etkiler; her sesi tek tek mixer grubuna
        // yönlendirmek gerekmez.
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    private void UpdateVolumeText(float volume)
    {
        if (masterVolumeValueText == null)
            return;

        masterVolumeValueText.text =
            $"{Mathf.RoundToInt(volume * 100f)}%";
    }
}
