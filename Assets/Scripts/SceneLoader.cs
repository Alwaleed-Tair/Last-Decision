using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public void LoadLevel(string sceneName)
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = false;
        }

        SceneManager.LoadScene(sceneName);
    }
}