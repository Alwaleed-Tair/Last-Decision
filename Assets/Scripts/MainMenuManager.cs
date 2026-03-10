using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
   
    public void StartNewGame()
{
    // Clear the specific door keys used by DoorsSticker.cs
    string[] doorKeys = { "Ideal", "Control", "Pride", "Fear", "Anger", "Joy" };
    foreach (string key in doorKeys)
    {
        PlayerPrefs.DeleteKey("Decision_" + key);
    }

    // Clear Video Reveal keys
    PlayerPrefs.DeleteKey("Decision_Nurse");
    PlayerPrefs.DeleteKey("Decision_DJ");

    // Clear global counters
    PlayerPrefs.DeleteKey("FinalHumansSpared");
    PlayerPrefs.DeleteKey("FinalAiSpared");

    PlayerPrefs.Save();

    // Load the Hub
    UnityEngine.SceneManagement.SceneManager.LoadScene("BootScene");
}
}