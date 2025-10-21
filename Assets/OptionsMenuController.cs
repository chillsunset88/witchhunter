using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsMenuController : MonoBehaviour
{
    [SerializeField] Slider masterVolume;
    [SerializeField] Toggle fullscreen;
    [SerializeField] TMP_Dropdown quality;

    const string KEY_VOL = "opt_volume";

    void OnEnable()
    {
        // init UI
        float vol = PlayerPrefs.GetFloat(KEY_VOL, 0.8f);
        masterVolume.SetValueWithoutNotify(vol);
        fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);

        quality.ClearOptions();
        quality.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        quality.SetValueWithoutNotify(QualitySettings.GetQualityLevel());

        ApplyAll(); // sinkron engine dengan UI
    }

    public void OnVolumeChanged(float v) => AudioListener.volume = v;
    public void OnFullscreenChanged(bool on) => Screen.fullScreen = on;
    public void OnQualityChanged(int idx) => QualitySettings.SetQualityLevel(idx, true);

    public void OnApply()
    {
        PlayerPrefs.SetFloat(KEY_VOL, masterVolume.value);
        PlayerPrefs.Save();
        ApplyAll();
    }

    void ApplyAll()
    {
        AudioListener.volume = masterVolume.value;
        Screen.fullScreen = fullscreen.isOn;
        QualitySettings.SetQualityLevel(quality.value, true);
    }
}
