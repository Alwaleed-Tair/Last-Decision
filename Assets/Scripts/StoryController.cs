using UnityEngine;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StoryController : MonoBehaviour
{
    [Header("UI Elements (Drag & Drop)")]
    public TextMeshProUGUI storyText;
    public Button nextButton; // 👈 اسحب زر النيكست هنا
    public Button skipButton; // 👈 اسحب زر السكيب هنا
    public Image fadeImage;

    [Header("Story Content")]
    [TextArea(15, 20)]
    public string fullStoryText;

    [Header("Typing Settings")]
    public float defaultTypingSpeed = 0.05f;
    public float startDelay = 0.5f;
    public float pauseDuration = 1.2f;

    [Header("Fade Settings")]
    public float fadeInSpeed = 1.0f;
    public float fadeOutDuration = 2.0f;

    [Header("Audio Sources")]
    public AudioSource typeSoundAudio;
    public AudioSource buttonClickAudio;

    [Header("Volume Control")]
    [Range(0f, 1f)]
    public float typingVolume = 0.5f;
    [Range(0f, 1f)]
    public float buttonVolume = 0.7f;
    public float typingVolumeMultiplier = 1.5f;

    [Header("Scene Control")]
    public string nextSceneName = "MainHub";

    private float currentSpeed;
    private bool isStoryFinished = false;
    private bool isFadingOut = false;
    
    private bool isTyping = false;
    private bool cancelTyping = false;
    private bool waitForNextClick = false;

    void Start()
    {
        currentSpeed = defaultTypingSpeed;
        storyText.richText = true;
        
        // ربط الأزرار برمجياً عند البداية
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);
        
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipStory);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
        }

        StartCoroutine(StartStoryWithDelay());
    }

    private IEnumerator StartStoryWithDelay()
    {
        yield return StartCoroutine(FadeIn());
        yield return new WaitForSeconds(startDelay);
        yield return StartCoroutine(TypeStory());
        
        if (!isFadingOut)
        {
            yield return StartCoroutine(StartFinalFade());
        }
    }

    private IEnumerator TypeStory()
    {
        string[] pages = fullStoryText.Split(new string[] { "||" }, System.StringSplitOptions.None);

        for (int i = 0; i < pages.Length; i++)
        {
            if (isFadingOut) yield break;

            string page = pages[i];
            string processedText = ProcessRedTags(page);
            string cleanTextForDisplay = Regex.Replace(processedText, @"<slow>|</slow>|<fast>|</fast>|\[pause\]", "");
            
            storyText.text = cleanTextForDisplay;
            storyText.maxVisibleCharacters = 0;
            storyText.ForceMeshUpdate();

            int totalVisibleCharacters = storyText.textInfo.characterCount;
            int counter = 0;
            int originalCharIndex = 0;

            isTyping = true;
            cancelTyping = false;

            while (counter <= totalVisibleCharacters)
            {
                if (isFadingOut) yield break;
                
                if (cancelTyping)
                {
                    storyText.maxVisibleCharacters = totalVisibleCharacters;
                    break;
                }

                UpdateSpeedFromOriginalText(page, ref originalCharIndex, counter);
                storyText.maxVisibleCharacters = counter;

                if (counter > 0 && counter <= totalVisibleCharacters)
                {
                    if (typeSoundAudio != null)
                    {
                        char c = storyText.textInfo.characterInfo[counter - 1].character;
                        if (!char.IsWhiteSpace(c) && c != '.' && c != ',' && c != '\'')
                        {
                            float finalVol = typingVolume * typingVolumeMultiplier;
                            typeSoundAudio.PlayOneShot(typeSoundAudio.clip, finalVol);
                        }
                    }
                }

                counter++;
                yield return new WaitForSeconds(currentSpeed);
            }

            isTyping = false;
            
            if (i == pages.Length - 1)
            {
                waitForNextClick = true;
                while (waitForNextClick && !isFadingOut) yield return null;
                yield break; 
            }

            waitForNextClick = true;
            while (waitForNextClick && !isFadingOut) yield return null;
            
            storyText.text = "";
        }
    }

    public void OnNextButtonClicked()
    {
        if (isFadingOut) return;

        if (isTyping)
        {
            cancelTyping = true;
        }
        else if (waitForNextClick)
        {
            waitForNextClick = false;
            if (buttonClickAudio != null)
                buttonClickAudio.PlayOneShot(buttonClickAudio.clip, buttonVolume);
        }
    }


    public void SkipStory()
    {
        if (isFadingOut) return;

        if (buttonClickAudio != null)
            buttonClickAudio.PlayOneShot(buttonClickAudio.clip, buttonVolume);

        // 1. Change the destination to the Hub scene name
        nextSceneName = "MainHub";

        // 2. Start the fade and load the scene
        StartCoroutine(StartFinalFade());
    }

    private string ProcessRedTags(string text)
    {
        text = text.Replace("<red>", "<color=#FF0000>");
        text = text.Replace("</red>", "</color>");
        return text;
    }

    private void UpdateSpeedFromOriginalText(string originalPage, ref int originalIndex, int cleanCounter)
    {
        while (originalIndex < originalPage.Length)
        {
            string remaining = originalPage.Substring(originalIndex);

            if (remaining.StartsWith("<red>")) originalIndex += 5;
            else if (remaining.StartsWith("</red>")) originalIndex += 6;
            else if (remaining.StartsWith("<slow>")) { currentSpeed = 0.18f; originalIndex += 6; }
            else if (remaining.StartsWith("</slow>")) { currentSpeed = defaultTypingSpeed; originalIndex += 7; }
            else if (remaining.StartsWith("<fast>")) { currentSpeed = 0.01f; originalIndex += 6; }
            else if (remaining.StartsWith("</fast>")) { currentSpeed = defaultTypingSpeed; originalIndex += 7; }
            else if (remaining.StartsWith("[pause]")) { originalIndex += 7; }
            else { originalIndex++; break; }
        }
    }

    private IEnumerator StartFinalFade()
    {
        if (isFadingOut) yield break;
        isFadingOut = true;
        waitForNextClick = false;

        float timer = 0;
        float startTypeVol = typeSoundAudio != null ? typeSoundAudio.volume : 0;

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeOutDuration;

            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = Mathf.Lerp(0, 1, t);
                fadeImage.color = c;
            }

            if (typeSoundAudio != null)
                typeSoundAudio.volume = Mathf.Lerp(startTypeVol, 0, t);

            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator FadeIn()
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

    void Update()
    {
        // دعم لوحة المفاتيح
        if (Input.GetKeyDown(KeyCode.Return))
            OnNextButtonClicked();

        if (Input.GetKeyDown(KeyCode.Space))
            SkipStory();
    }
}
