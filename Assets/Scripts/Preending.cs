using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// --- JSON DATA STRUCTURES ---
[System.Serializable]
public class WordEntry {
    public string word;
    public float start;
    public float end;
    public int startOffset; 
    public int endOffset;
}

[System.Serializable]
public class EndingData {
    public string transcript;
    public List<WordEntry> words;
}

public class Preending : MonoBehaviour
{
    [Header("--- TEXT OBJECTS ---")]
    public TextMeshProUGUI mainStoryText;
    public TextMeshProUGUI imageOverlayText;

    [Header("--- IMAGES ---")]
    public GameObject devImage;
    public GameObject zeroTeamImage;
    public GameObject finalScreenImage;
    public GameObject restartButton;

    [Header("--- ENDING ASSETS ---")]
    public TextAsset[] endingJsons;
    public AudioClip[] endingAudios;

    [Header("--- AUDIO ---")]
    public AudioSource typeSoundAudio;
    public AudioSource signalCountSound;
    public AudioSource horrorSignalSound;
    
    [Header("--- DEV TALK AUDIO LAYERS ---")]
    public AudioSource devAudioLayer1;
    public AudioSource devAudioLayer2;
    [Range(0f, 1f)] public float devVolume = 0.5f;

    [Header("--- TYPING SETTINGS ---")]
    public float defaultTypingSpeed = 0.03f;
    public float countUpSpeed = 0.8f;
    public float lineSpacing = 30f;

    [Header("--- INTRO TEXT ---")]
    [TextArea(3, 5)] public string introText = "SYSTEM: TRIAL COMPLETE||BEGINNING FINAL EVALUATION…||<slow>DO NOT INTERRUPT...</slow>";

    [Header("--- SCENE OVERRIDE ---")]
    [Tooltip("اتركه فارغاً لاستخدام المنطق الافتراضي، أو اكتب اسم المشهد للتحكم يدوياً")]
    public string nextSceneOverride = "";

    [Header("--- FADE ---")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 1.5f;

    // Internal Counters
    private int humansSpared = 0;
    private int aiSpared = 0;

    // Ending Texts (System Messages)
    private string endingA_SystemText = "SYSTEM MESSAGE: ROLE UPDATE: SUBJECT";
    private string endingB_SystemText = "SYSTEM MESSAGE: EXIT PROTOCOL UNLOCKED||SYSTEM MESSAGE: HUMAN LOSS CONFIRMED";
    private string endingC_SystemText = "SYSTEM: EXIT PROTOCOL UNLOCKED";

    void Start()
    {
        humansSpared = PlayerPrefs.GetInt("FinalHumansSpared", 0);
        aiSpared = PlayerPrefs.GetInt("FinalAiSpared", 0);

        if (devImage != null) devImage.SetActive(false);
        if (zeroTeamImage != null) zeroTeamImage.SetActive(false);
        if (finalScreenImage != null) finalScreenImage.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);

        mainStoryText.text = "";
        imageOverlayText.text = "";
        mainStoryText.lineSpacing = lineSpacing;
        imageOverlayText.lineSpacing = lineSpacing;

        // ابدأ من أسود كامل
        if (fadePanel != null) fadePanel.alpha = 1f;

        StartCoroutine(RunFullSequence());
    }

    private bool IsOverflowing(TextMeshProUGUI tmp)
    {
        tmp.ForceMeshUpdate();
        int lineCount = tmp.textInfo.lineCount;
        float lineH = tmp.fontSize + tmp.lineSpacing;
        return (lineCount * lineH) > tmp.rectTransform.rect.height;
    }

    // --- Fade In: من أسود للشفاف ---
    private IEnumerator FadeIn(float duration)
    {
        if (fadePanel == null) yield break;
        float t = 0f;
        fadePanel.alpha = 1f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadePanel.alpha = 1f - (t / duration);
            yield return null;
        }
        fadePanel.alpha = 0f;
    }

    // --- Fade Out: من شفاف للأسود ---
    private IEnumerator FadeOut(float duration)
    {
        if (fadePanel == null) yield break;
        float t = 0f;
        fadePanel.alpha = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadePanel.alpha = t / duration;
            yield return null;
        }
        fadePanel.alpha = 1f;
    }

    private IEnumerator RunFullSequence()
    {
        // Fade in عند بداية المشهد
        yield return StartCoroutine(FadeIn(fadeDuration));

        mainStoryText.text = "";
        yield return StartCoroutine(PlayTextSequence(introText, mainStoryText));

        yield return StartCoroutine(TypeLine("HUMAN SIGNALS DETECTED: ", mainStoryText, true));
        yield return StartCoroutine(CountUpEffect(humansSpared, mainStoryText));
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(TypeLine("NON-HUMAN SIGNALS DETECTED: ", mainStoryText, true));
        yield return new WaitForSeconds(1.5f);
        
        if (aiSpared > 0) {
            if (horrorSignalSound != null) horrorSignalSound.Play();
            mainStoryText.text += "<color=#630f09>" + aiSpared + "</color>";
        } else {
            if (signalCountSound != null) signalCountSound.Play();
            mainStoryText.text += "0";
        }
        
        mainStoryText.ForceMeshUpdate();
        mainStoryText.maxVisibleCharacters = mainStoryText.textInfo.characterCount;
        yield return new WaitForSeconds(2f);

        if (horrorSignalSound != null && horrorSignalSound.isPlaying) 
            yield return StartCoroutine(FadeOutAudio(horrorSignalSound, 0.5f));

        yield return StartCoroutine(TypeLine("ISOLATING SOURCE…", mainStoryText, true));
        yield return new WaitForSeconds(0.7f);

        string id = DetermineEndingID();
        int index = GetEndingIndex(id);
        GameObject activeImage = (id == "E") ? zeroTeamImage : devImage;

        if (activeImage != null && index < endingJsons.Length)
        {
            mainStoryText.text = "";

            // Fade out ثم اظهر الـ Dev
            yield return StartCoroutine(FadeOut(fadeDuration));
            activeImage.SetActive(true);
            imageOverlayText.text = "";
            yield return StartCoroutine(FadeIn(fadeDuration));

            devAudioLayer1.clip = endingAudios[index];
            devAudioLayer1.volume = devVolume;
            devAudioLayer1.Play();
            if(devAudioLayer2 != null) devAudioLayer2.Play();

            EndingData data = JsonUtility.FromJson<EndingData>(endingJsons[index].text);
            yield return StartCoroutine(TypeLineWithJson(data, imageOverlayText, devAudioLayer1));

            yield return new WaitForSeconds(3f);
            if (devAudioLayer1 != null) StartCoroutine(FadeOutAudio(devAudioLayer1, 1.5f));
            if (devAudioLayer2 != null) StartCoroutine(FadeOutAudio(devAudioLayer2, 1.5f));

            // Fade out ثم اخفي الـ Dev
            yield return StartCoroutine(FadeOut(fadeDuration));
            activeImage.SetActive(false);
            imageOverlayText.text = "";
            yield return StartCoroutine(FadeIn(fadeDuration));
        }

        string sysText = GetSystemText(id);
        if (!string.IsNullOrEmpty(sysText)) {
            mainStoryText.text = "";
            yield return StartCoroutine(PlayTextSequence(sysText, mainStoryText));
            yield return new WaitForSeconds(3f);
        }

        // Fade out قبل الانتقال للمشهد
        yield return StartCoroutine(FadeOut(fadeDuration));

        // PHASE 4: TRANSITION
        if (!string.IsNullOrEmpty(nextSceneOverride))
        {
            SceneManager.LoadScene(nextSceneOverride);
        }
        else if (aiSpared > 0) 
        {
            SceneManager.LoadScene("AiReveal");
        } 
        else 
        {
            SceneManager.LoadScene("Final scenes");
        }
    }

    private IEnumerator PlayTextSequence(string fullText, TextMeshProUGUI target)
    {
        string[] lines = fullText.Split(new string[] { "||" }, System.StringSplitOptions.None);
        foreach (string line in lines)
        {
            if (line.Trim() == "[pause]") { yield return new WaitForSeconds(1.0f); continue; }
            yield return StartCoroutine(TypeLine(line, target, true));
            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator TypeLine(string line, TextMeshProUGUI target, bool append)
    {
        string cleanLine = Regex.Replace(line, @"<slow>|</slow>|\[pause\]", "");
        if (append && target.text.Length > 0) target.text += "\n";

        target.ForceMeshUpdate();
        int existingVisibleChars = target.textInfo.characterCount;

        target.text += cleanLine;
        target.ForceMeshUpdate();
        target.maxVisibleCharacters = existingVisibleChars;

        int totalChars = target.textInfo.characterCount;

        for (int i = existingVisibleChars; i <= totalChars; i++)
        {
            float currentSpeed = defaultTypingSpeed;

            int localIndex = i - existingVisibleChars;
            if (line.Contains("<slow>") && localIndex > line.IndexOf("<slow>") && localIndex < line.IndexOf("</slow>"))
                currentSpeed = 0.15f;

            target.maxVisibleCharacters = i;

            if (IsOverflowing(target))
            {
                target.text = cleanLine;
                target.ForceMeshUpdate();
                target.maxVisibleCharacters = 0;
                existingVisibleChars = 0;
                totalChars = target.textInfo.characterCount;
                i = 0;
            }

            if (typeSoundAudio != null && target == mainStoryText)
            {
                if (i > existingVisibleChars && i <= totalChars)
                {
                    char c = target.textInfo.characterInfo[i - 1].character;
                    if (!char.IsWhiteSpace(c)) typeSoundAudio.Play();
                }
            }
            yield return new WaitForSeconds(currentSpeed);
        }
    }

    private IEnumerator TypeLineWithJson(EndingData timingData, TextMeshProUGUI targetTextObj, AudioSource voiceSource)
    {
        targetTextObj.text = "";
        string fullTranscript = timingData.transcript;

        for (int i = 0; i < timingData.words.Count; i++)
        {
            WordEntry entry = timingData.words[i];
            while (voiceSource.time < entry.start) yield return null;

            // --- LINE BREAK LOGIC ---
            if (i > 0)
            {
                int prevEnd = timingData.words[i - 1].endOffset;
                int currStart = entry.startOffset;

                if (prevEnd >= 0 && currStart >= prevEnd && currStart <= fullTranscript.Length)
                {
                    string gap = fullTranscript.Substring(prevEnd, currStart - prevEnd);
                    if (gap.Contains("||||")) targetTextObj.text = "";
                    else if (gap.Contains("||")) targetTextObj.text += "\n";
                }
            }

            // --- PUNCTUATION GRABBER ---
            int safeEnd = Mathf.Clamp(entry.endOffset, 0, fullTranscript.Length);
            string displayedWord = entry.word;

            int nextCharIdx = safeEnd;
            while (nextCharIdx < fullTranscript.Length)
            {
                char c = fullTranscript[nextCharIdx];
                if (c == '.' || c == ',' || c == '?' || c == '!' || c == ':')
                {
                    displayedWord += c;
                    nextCharIdx++;
                }
                else break;
            }

            displayedWord = Regex.Replace(displayedWord, @"<[^>]*>", "");

            targetTextObj.text += displayedWord + " ";
            targetTextObj.ForceMeshUpdate();

            if (IsOverflowing(targetTextObj))
            {
                targetTextObj.text = displayedWord + " ";
                targetTextObj.ForceMeshUpdate();
            }
        }
    }

    private IEnumerator CountUpEffect(int final, TextMeshProUGUI target)
    {
        string baseTxt = target.text;
        for (int i = 0; i <= final; i++) {
            target.text = baseTxt + i;
            target.ForceMeshUpdate();
            target.maxVisibleCharacters = target.textInfo.characterCount;
            if (signalCountSound != null) signalCountSound.Play();
            yield return new WaitForSeconds(countUpSpeed);
        }
        target.text += "\n";
    }

    private IEnumerator FadeOutAudio(AudioSource source, float duration)
    {
        float startVol = source.volume;
        while (source.volume > 0) {
            source.volume -= startVol * Time.deltaTime / duration;
            yield return null;
        }
        source.Stop();
        source.volume = startVol;
    }

    private string DetermineEndingID() {
        int total = humansSpared + aiSpared;
        if (total == 0) return "E";
        if (total == 6) return "D";
        if (humansSpared == 4 && aiSpared == 0) return "C";
        if (humansSpared == 3 && aiSpared == 0) return "B";
        return "A";
    }

    private int GetEndingIndex(string id) => id switch { "A"=>0, "B"=>1, "C"=>2, "D"=>3, "E"=>4, _=>0 };
    private string GetSystemText(string id) => id switch { "A"=>endingA_SystemText, "B"=>endingB_SystemText, "C"=>endingC_SystemText, _=>"" };
}