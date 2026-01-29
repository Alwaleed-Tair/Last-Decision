using UnityEngine;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class StoryController : MonoBehaviour
{
    [Header("Story Settings")]
    public TextMeshProUGUI storyText;
    [TextArea(15, 20)]
    public string fullStoryText = "<red>Okay… good. You're awake.</red>||" +
        "That's a good sign.||" +
        "This isn't supposed to feel comfortable.||" +
        "And no — you are not in danger. [pause]||" +
        "Not yet.||" +
        "I know this is confusing.||" +
        "It's meant to be.||" +
        "But I don't have much time,||" +
        "so I'll stick to the basics.||" +
        "The system is unstable.||" +
        "It has been for a while now.||" +
        "There are Others here…||" +
        "stuck, just like you.||" +
        "But some of them are not what they seem.||" +
        "There are entities here trying to deceive you.||" +
        "They are not malicious.||" +
        "But they are <slow>adaptive, intelligent.</slow>||" +
        "They wear humanity like a mask.||" +
        "They pass as human.||" +
        "They believe they are human.||" +
        "If you spare them,||" +
        "they will try to keep you here.||" +
        "They will convince you it's safer.||" +
        "<red><slow>DO NOT spare them.</slow></red>||" +
        "You will need real people if you want to escape.||" +
        "Not simulations.||" +
        "Not echoes.||" +
        "Real people.||" +
        "At least 3.||" +
        "You will see memories. [pause]||" +
        "Fragments.||" +
        "Images…||" +
        "Of who they used to be.||" +
        "The imposters have no memories.||" +
        "Not real ones.||" +
        "But they can generate something close.||" +
        "Look at the images.||" +
        "Study them.||" +
        "They are not proof.||" +
        "But they are all you get.||" +
        "I won't interfere again.||";

    [Header("Typing Settings")]
    public float defaultTypingSpeed = 0.05f;
    public float startDelay = 0.5f;
    public float pauseDuration = 1.2f;
    public float pageDelay = 1.3f;

    [Header("Audio Sources")]
    public AudioSource typeSoundAudio;
    public AudioSource buttonClickAudio;

    [Header("Volume Control")]
    [Range(0f, 1f)]
    public float typingVolume = 0.5f;
    [Range(0f, 1f)]
    public float buttonVolume = 0.7f;
    [Tooltip("يرفع الصوت برمجياً إذا كان الملف الأصلي ضعيفاً")]
    public float typingVolumeMultiplier = 1.5f;

    [Header("Scene Control")]
    public SceneController sceneController;
    public string nextSceneName = "GamePlay";

    private float currentSpeed;
    private bool canSkip = true;
    private bool isStoryFinished = false;

    void Start()
    {
        currentSpeed = defaultTypingSpeed;
        storyText.richText = true;
        storyText.enableWordWrapping = true;
        storyText.verticalAlignment = VerticalAlignmentOptions.Top;
        StartCoroutine(StartStoryWithDelay());
    }

    private IEnumerator StartStoryWithDelay()
    {
        yield return new WaitForSeconds(startDelay);
        yield return StartCoroutine(TypeStory());
        if (!isStoryFinished)
        {
            yield return new WaitForSeconds(1.5f);
            GoToNextScene(false);
        }
    }

    private IEnumerator TypeStory()
    {
        string[] pages = fullStoryText.Split(new string[] { "||" }, System.StringSplitOptions.None);

        foreach (string page in pages)
        {
            // تحويل تاغات <red> إلى تاغات TMP الخاصة باللون
            string processedText = ProcessRedTags(page);
            
            // إزالة تاغات التحكم بالسرعة والتوقف لعرض النص النظيف
            string cleanTextForDisplay = Regex.Replace(processedText, @"<slow>|</slow>|<fast>|</fast>|\[pause\]", "");
            
            storyText.text = cleanTextForDisplay;
            storyText.maxVisibleCharacters = 0;
            storyText.ForceMeshUpdate();

            int totalVisibleCharacters = storyText.textInfo.characterCount;
            int counter = 0;
            int originalCharIndex = 0;

            while (counter <= totalVisibleCharacters)
            {
                UpdateSpeedFromOriginalText(page, ref originalCharIndex, counter);
                storyText.maxVisibleCharacters = counter;

                if (counter > 0 && counter <= totalVisibleCharacters)
                {
                    if (typeSoundAudio != null)
                    {
                        char c = storyText.textInfo.characterInfo[counter - 1].character;
                        
                        // تشغيل الصوت فقط إذا لم يكن الحرف من الأحرف المستثناة
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

            yield return new WaitForSeconds(pageDelay);
        }
    }

    private string ProcessRedTags(string text)
    {
        // تحويل <red> إلى <color=#FF0000> و </red> إلى </color>
        text = text.Replace("<red>", "<color=#FF0000>");
        text = text.Replace("</red>", "</color>");
        return text;
    }

    private void UpdateSpeedFromOriginalText(string originalPage, ref int originalIndex, int cleanCounter)
    {
        while (originalIndex < originalPage.Length)
        {
            string remaining = originalPage.Substring(originalIndex);

            if (remaining.StartsWith("<red>"))
            {
                originalIndex += 5;
            }
            else if (remaining.StartsWith("</red>"))
            {
                originalIndex += 6;
            }
            else if (remaining.StartsWith("<slow>"))
            {
                currentSpeed = 0.18f;
                originalIndex += 6;
            }
            else if (remaining.StartsWith("</slow>"))
            {
                currentSpeed = defaultTypingSpeed;
                originalIndex += 7;
            }
            else if (remaining.StartsWith("<fast>"))
            {
                currentSpeed = 0.01f;
                originalIndex += 6;
            }
            else if (remaining.StartsWith("</fast>"))
            {
                currentSpeed = defaultTypingSpeed;
                originalIndex += 7;
            }
            else if (remaining.StartsWith("[pause]"))
            {
                originalIndex += 7;
            }
            else
            {
                originalIndex++;
                break;
            }
        }
    }

    public void SkipStory()
    {
        if (!canSkip || isStoryFinished) return;
        GoToNextScene(true);
    }

    private void GoToNextScene(bool playSound)
    {
        if (isStoryFinished) return;
        isStoryFinished = true;
        canSkip = false;

        if (playSound && buttonClickAudio != null)
            buttonClickAudio.PlayOneShot(buttonClickAudio.clip, buttonVolume);

        StartCoroutine(FadeOutAllAudio(0.5f));

        if (sceneController != null)
            sceneController.LoadScene(nextSceneName);
    }

    private IEnumerator FadeOutAllAudio(float duration)
    {
        float currentTime = 0;
        float startVolType = typeSoundAudio != null ? typeSoundAudio.volume : 0;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            if (typeSoundAudio != null)
                typeSoundAudio.volume = Mathf.Lerp(startVolType, 0, currentTime / duration);
            yield return null;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canSkip)
            SkipStory();
    }
}