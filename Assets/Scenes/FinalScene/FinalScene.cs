using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class FinalSceneController : MonoBehaviour
{
    [Header("Fade Settings")]
    public Image fadeImage;
    [Tooltip("??? ?????? ?????? ????????")]
    public float fadeDuration = 1.5f;

    [Header("UI Elements")]
    public GameObject gameTitle; // ???? ???? ??????? ???

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip clickSound;

    [Header("Navigation")]
    public Object targetScene;
    public Object restartScene;

    private void Start()
    {
        // ????? ??????? ?? ??????? ????? ??? ????? ??? ?????
        if (gameTitle != null) gameTitle.SetActive(false);

        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 1);
            StartCoroutine(FadeInSequence());
        }
    }

    IEnumerator FadeInSequence()
    {
        // ????? ?? (?????? ?????? ????????)
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // ???? ???????? ????? ??? ????? ?????? ?? ??? Inspector
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 0);

        // ????? ??????? ??? ?????? ?????
        if (gameTitle != null) gameTitle.SetActive(true);
    }

    public void OnButtonClick()
    {
        PlayClickSound();
        if (targetScene != null) SceneManager.LoadScene(targetScene.name);
    }

    public void OnRestartClick()
    {
        PlayClickSound();
        if (restartScene != null) SceneManager.LoadScene(restartScene.name);
    }

    private void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }
}
