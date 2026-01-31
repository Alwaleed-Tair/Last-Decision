using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour
{
    public string characterName; // e.g., "IdealCharacter"
    public string gameSceneName = "CharacterScene"; // The name of your 2nd scene

    public void EnterDoor()
    {
        // Save which character to load so the next scene knows who it is
        PlayerPrefs.SetString("SelectedCharacter", characterName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }
}