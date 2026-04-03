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
    public string nextSceneName = "MainMenu";

    [Header("Ending Clips")]
    public VideoClip bothSparedVideo;  // Scenario 1: Nurse & DJ spared
    public VideoClip nurseOnlyVideo;   // Scenario 2: Just Nurse spared
    public VideoClip djOnlyVideo;      // Scenario 3: Just DJ spared

    void Start()
    {
        SetupEndingVideo();
        StartCoroutine(PlaySequence());
    }

    void SetupEndingVideo()
    {
        // These strings MUST match the characterID in your ScriptableObjects
        bool sparedNurse = PlayerPrefs.GetInt("Decision_Nurse", 0) == 1;
        bool sparedDJ = PlayerPrefs.GetInt("Decision_DJ", 0) == 1;

        if (sparedNurse && sparedDJ) {
            videoPlayer.clip = bothSparedVideo;
        }
        else if (sparedNurse) {
            videoPlayer.clip = nurseOnlyVideo;
        }
        else if (sparedDJ) {
            videoPlayer.clip = djOnlyVideo;
        }
    }

   IEnumerator PlaySequence()
    {
        if (videoPlayer.clip == null) {
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        // 1. Prepare the video first to prevent the 'flicker'
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.Play();
        
        // 2. FADE IN
        yield return StartCoroutine(Fade(1, 0));

        // 3. STABALIZED WAIT
        // We wait at least 1 second to make sure it doesn't skip instantly
        yield return new WaitForSeconds(1.0f);

        while (videoPlayer.isPlaying)
        {
            if (videoPlayer.time >= videoPlayer.clip.length - fadeDuration) break;
            yield return null;
        }

        // 4. FADE OUT
        yield return StartCoroutine(Fade(0, 1));
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
    }
}