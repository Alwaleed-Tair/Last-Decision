using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class VideoSequenceController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Image fadeOverlay;
    public float fadeDuration = 1.0f;
    public string nextSceneName = "MainMenu"; // Where to go after video

    void Start()
    {
        // Start the sequence
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // 1. FADE IN (Black to Transparent)
        yield return StartCoroutine(Fade(1, 0));

        // 2. WAIT FOR VIDEO (8 seconds)
        // We wait slightly less than 8s to start the fade out smoothly
        yield return new WaitForSeconds(7.0f); 

        // 3. FADE OUT (Transparent to Black)
        yield return StartCoroutine(Fade(0, 1));

        // 4. NEXT SCENE
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            fadeOverlay.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeOverlay.color = new Color(0, 0, 0, endAlpha);
    }
}