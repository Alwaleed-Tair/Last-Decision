
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour
{
    public string characterName; // e.g., "MiddleAge"
    public string gameSceneName = "GamePlay";

    public void EnterDoor()
    {
        // LOG 1: Check if the button click is even reaching the code
        Debug.Log("Button Clicked: Attempting to enter door for " + characterName);

        if (string.IsNullOrEmpty(characterName))
        {
            Debug.LogError("Click Failed: You forgot to type a Character Name in the Inspector!");
            return;
        }

        // Save which character to load
        PlayerPrefs.SetString("SelectedCharacter", characterName);
        PlayerPrefs.Save();

        // LOG 2: Verify data was saved before leaving the scene
        Debug.Log("Data Saved: 'SelectedCharacter' is now set to " + characterName);

        // LOG 3: Check if the scene transition is starting
        Debug.Log("Scene Loading: Moving to " + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }
}