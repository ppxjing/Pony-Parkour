using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject levelsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    private const string VolumeKey = "master-volume";

    private void Awake()
    {
        float volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
        AudioListener.volume = volume;

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    public void Play()
    {
        CharacterSelectionPage.Show(() =>
            StoryDialoguePage.Show(() => LoadScene("Kitchen_Adventure")));
    }

    public void OpenLevels()
    {
        CharacterSelectionPage.Show(() => LoadScene("StartScene"));
    }

    public void OpenSettings()
    {
        levelsPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void ClosePanels()
    {
        levelsPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void LoadAdventure() => LoadScene("Kitchen_Adventure");
    public void LoadPlayground() => LoadScene("Kitchen_Playground");

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void LoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogError($"Scene '{sceneName}' is not included in Build Settings.");
    }

    private static void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private static void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
    }
}
