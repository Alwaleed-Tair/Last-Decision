using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Globalization;

public class BootUpSequence : MonoBehaviour
{
    [Header("UI Text Elements")]
    public TextMeshProUGUI bootText;
    public TextMeshProUGUI bootText2; 
    
    [Header("Audio Settings")]
    public AudioSource typeSoundAudio;
    [Range(0f, 1f)]
    public float typingVolume = 1f;
    public Vector2 pitchRange = new Vector2(0.85f, 1.15f);

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeInSpeed = 1.0f;
    public float fadeOutDuration = 2.0f;

    [Header("Mask Animation Settings")]
    public GameObject maskHappy; 
    public GameObject maskSad;   
    public GameObject maskMad;   
    public float maskSwitchInterval = 0.5f; 

    [Header("Default Typing Speeds")]
    public float loadingLetterSpeed = 0.08f;
    public float normalLetterSpeed = 0.12f;
    public float dotSpeed = 0.35f;
    public float dotSpeed2 = 0.5f;

    [Header("Content Settings")]
    [TextArea(10, 20)]
    public string fullBootText;
    public string text2Prefix = "Connecting";

    [Header("Timing Control")]
    public bool startText2Simultaneously = false;
    public float text2StartDelay = 0f;
    public string nextSceneName = "Story";

    private bool isText1Finished = false;
    private bool stopMaskAnimation = false;
    private float currentTypingSpeed;

    void Start()
    {
        bootText.text = "";
        if (bootText2 != null) bootText2.text = "";
        currentTypingSpeed = normalLetterSpeed;

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
        }

        SetMasks(false, false, false);
        StartCoroutine(BootSequence());
    }

    IEnumerator AnimateThreeMasks()
    {
        while (!stopMaskAnimation)
        {
            SetMasks(true, false, false);
            yield return new WaitForSeconds(maskSwitchInterval);
            if (stopMaskAnimation) break;

            SetMasks(false, true, false);
            yield return new WaitForSeconds(maskSwitchInterval);
            if (stopMaskAnimation) break;

            SetMasks(false, false, true);
            yield return new WaitForSeconds(maskSwitchInterval);
        }
    }

    void SetMasks(bool happy, bool sad, bool mad)
    {
        if (maskHappy != null) maskHappy.SetActive(happy);
        if (maskSad != null) maskSad.SetActive(sad);
        if (maskMad != null) maskMad.SetActive(mad);
    }

    IEnumerator BootSequence()
    {
        // 1. Fade In
        yield return StartCoroutine(FadeIn());

        if (maskHappy != null && maskSad != null && maskMad != null)
        {
            StartCoroutine(AnimateThreeMasks());
        }

        isText1Finished = false;

        if (startText2Simultaneously && bootText2 != null)
            StartCoroutine(BootSequence2());
        else if (!startText2Simultaneously && bootText2 != null && text2StartDelay > 0)
            StartCoroutine(DelayedBootSequence2());

        // LOADING
        yield return StartCoroutine(TypeWord(bootText, "LOADING", loadingLetterSpeed));
        int loops = Random.Range(2, 4);
        for (int i = 0; i < loops; i++)
            yield return StartCoroutine(LoadingDots(bootText, dotSpeed));

        yield return new WaitForSeconds(0.4f);
        bootText.text = "";

        // TEXT CONTENT WITH TAGS
        string[] lines = fullBootText.Replace("\r", "").Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            bool shouldFadeAtEnd = false;
            yield return StartCoroutine(TypeLineWithTags(bootText, lines[i], (fade) => shouldFadeAtEnd = fade));
            
            if (shouldFadeAtEnd)
            {
                // 🎯 تم اكتشاف وسم <fade> - إيقاف كل شيء وبدء الـ Fade Out
                stopMaskAnimation = true;
                isText1Finished = true;
                
                float timer = 0;
                while (timer < fadeOutDuration)
                {
                    timer += Time.deltaTime;
                    if (fadeImage != null)
                    {
                        Color c = fadeImage.color;
                        c.a = Mathf.Lerp(0, 1, timer / fadeOutDuration);
                        fadeImage.color = c;
                    }
                    yield return null;
                }
                break; // الخروج من اللوب للانتقال للمشهد التالي
            }
            else
            {
                // أسطر عادية
                yield return new WaitForSeconds(0.8f);
                bootText.text = "";
                yield return new WaitForSeconds(0.25f);
            }
        }

        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator TypeLineWithTags(TextMeshProUGUI targetText, string line, System.Action<bool> onFadeTagFound)
    {
        targetText.text = "";
        currentTypingSpeed = normalLetterSpeed;
        
        for (int i = 0; i < line.Length; i++)
        {
            // التحقق من وجود وسم يبدأ بـ <
            if (line[i] == '<')
            {
                int closeIndex = line.IndexOf('>', i);
                if (closeIndex != -1)
                {
                    string tag = line.Substring(i + 1, closeIndex - i - 1).ToLower();
                    
                    // معالجة الوسوم
                    if (tag == "fade")
                    {
                        onFadeTagFound?.Invoke(true);
                        i = closeIndex; // تخطي الوسم
                        continue;
                    }
                    else if (tag.StartsWith("wait="))
                    {
                        string val = tag.Split('=')[1];
                        if (float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out float waitTime))
                            yield return new WaitForSeconds(waitTime);
                        i = closeIndex;
                        continue;
                    }
                    else if (tag.StartsWith("speed="))
                    {
                        string val = tag.Split('=')[1];
                        float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out currentTypingSpeed);
                        i = closeIndex;
                        continue;
                    }
                }
            }

            // كتابة الحرف العادي
            targetText.text += line[i];
            if (!char.IsWhiteSpace(line[i])) PlayTypeSound();
            yield return new WaitForSeconds(currentTypingSpeed);
        }
    }

    IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;
        float timer = 0;
        while (timer < 1f)
        {
            timer += Time.deltaTime * fadeInSpeed;
            Color c = fadeImage.color;
            c.a = Mathf.Lerp(1, 0, timer);
            fadeImage.color = c;
            yield return null;
        }
        Color finalColor = fadeImage.color;
        finalColor.a = 0;
        fadeImage.color = finalColor;
    }

    IEnumerator DelayedBootSequence2()
    {
        yield return new WaitForSeconds(text2StartDelay);
        yield return StartCoroutine(BootSequence2());
    }

    IEnumerator BootSequence2()
    {
        if (bootText2 == null) yield break;
        while (!isText1Finished)
        {
            bootText2.text = text2Prefix;
            for (int i = 0; i < 3; i++)
            {
                if (isText1Finished) break;
                bootText2.text += " .";
                yield return new WaitForSeconds(dotSpeed2);
            }
            if (!isText1Finished) yield return new WaitForSeconds(dotSpeed2);
        }
    }

    IEnumerator TypeWord(TextMeshProUGUI targetText, string word, float speed)
    {
        foreach (char c in word)
        {
            targetText.text += c;
            PlayTypeSound();
            yield return new WaitForSeconds(speed);
        }
    }

    IEnumerator LoadingDots(TextMeshProUGUI targetText, float speed)
    {
        for (int i = 1; i <= 3; i++)
        {
            targetText.text = "LOADING" + new string('.', i);
            PlayTypeSound();
            yield return new WaitForSeconds(speed);
        }
    }

    void PlayTypeSound()
    {
        if (typeSoundAudio == null || typeSoundAudio.clip == null) return;
        typeSoundAudio.pitch = Random.Range(pitchRange.x, pitchRange.y);
        typeSoundAudio.PlayOneShot(typeSoundAudio.clip, typingVolume);
    }
}
