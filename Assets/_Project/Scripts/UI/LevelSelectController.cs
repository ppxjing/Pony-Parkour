using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelSelectController : MonoBehaviour
{
    public void OpenKitchen() => SceneManager.LoadScene("Kitchen_Adventure");
    public void Back() => SceneManager.LoadScene("MainMenuScene");
}
