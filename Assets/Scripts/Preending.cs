using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text.RegularExpressions;

public class Preending : MonoBehaviour
{
    [Header("--- TEXT OBJECTS ---")]
    public TextMeshProUGUI mainStoryText;    // Black Screen Text (SYSTEM)
    public TextMeshProUGUI imageOverlayText; // White Text (DEV)

    [Header("--- IMAGES ---")]
    public GameObject devImage;
    public GameObject zeroTeamImage;
    public GameObject finalScreenImage;
    public GameObject restartButton;

    // --- INTERNAL COUNTERS ---
    private int humansSpared = 0;
    private int aiSpared = 0;

    [Header("--- SETTINGS ---")]
    public string mainMenuSceneName = "MainMenu";
    public string postEndingSceneName = "Credits";

    [Header("--- TYPING SETTINGS ---")]
    public float defaultTypingSpeed = 0.03f;
    public float devTypingSpeed = 0.08f;
    public int maxLinesPerPage = 7;
    public float pageClearDelay = 0.6f;
    public float countUpSpeed = 0.3f;
    public float lineDelay = 1.0f;
    public float lineSpacing = 30f;

    [Header("--- AUDIO ---")]
    public AudioSource typeSoundAudio;
    public AudioSource signalCountSound;
    public AudioSource horrorSignalSound; // ⭐ New: Horror sound for AI reveal

    // ================= MESSAGES =================

    [Header("--- INTRO ---")]
    [TextArea(3, 5)] public string introText = "SYSTEM: TRIAL COMPLETE||BEGINNING FINAL EVALUATION…||<slow>DO NOT INTERRUPT...</slow>";

    [Header("--- ENDING A (Failed) ---")]
    [TextArea(3, 5)] public string endingA_DevText = "Careless. Reckless.||Wrong choice. Again. And again.||The Judge has failed the trial.||You played with lives. You played with ghosts.||I hope you enjoyed the power.||Because it was your last.||[pause]||You see it now, don't you?||Decision made. You are no longer the judge.||Judgment is eternal. The game never stops.||It is your turn to wear the mask.||Pray the next player is more merciful than you.";
    [TextArea(2, 3)] public string endingA_SystemText = "SYSTEM MESSAGE: ROLE UPDATE: SUBJECT";

    [Header("--- ENDING B (Some Loss) ---")]
    [TextArea(3, 5)] public string endingB_DevText = "Dev: you saw through enough||The system couldn’t hold.||[pause]||But not everyone made it.";
    [TextArea(2, 3)] public string endingB_SystemText = "SYSTEM MESSAGE: EXIT PROTOCOL UNLOCKED||SYSTEM MESSAGE: HUMAN LOSS CONFIRMED";

    [Header("--- ENDING C (Total = 6) ---")]
    [TextArea(3, 5)] public string endingC_DevText = "Dev: You didn’t trust the mask.||You judged what failed.||Not what looked right||That’s ... rare||Remember this feeling.||It won’t last outside.||Out there you won’t get a chamber.||You won’t get time.";
    [TextArea(2, 3)] public string endingC_SystemText = "SYSTEM: EXIT PROTOCOL UNLOCKED";

    [Header("--- ENDING D (Other) ---")]
    [TextArea(3, 5)] public string endingD_DevText = "You are... sentimental.||Or perhaps just blind?||You accepted every mask as truth and opened the door for everyone.||The breathing, the code, the fakes.||You think you are a hero? No.||You are a carrier.||You didn't filter the corruption … <slow>You invited it home.</slow>";

    [Header("--- ENDING E (Total = 0) ---")]
    [TextArea(3, 5)] public string endingE_DevText = "Quiet, isn't it?||No lies. No masks. Just... nothing.||You didn't want a team. <slow>You wanted a graveyard. And you got it.</slow>||But tell me, Player... If there is no one left to observe you...||Do you even exist?";

    void Start()
    {
        LoadGameData();

        // Standardize volumes to match the typing sound
        if (typeSoundAudio != null) typeSoundAudio.volume = 0.03f;
        if (signalCountSound != null) signalCountSound.volume = 0.03f;

        if (devImage != null) devImage.SetActive(false);
        if (zeroTeamImage != null) zeroTeamImage.SetActive(false);
        if (finalScreenImage != null) finalScreenImage.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);

        if (mainStoryText != null) { mainStoryText.text = ""; mainStoryText.lineSpacing = lineSpacing; }
        if (imageOverlayText != null) { imageOverlayText.text = ""; imageOverlayText.lineSpacing = lineSpacing; }

        StartCoroutine(RunFullSequence());
    }

    void LoadGameData()
    {
        humansSpared = PlayerPrefs.GetInt("FinalHumansSpared", 0);
        aiSpared = PlayerPrefs.GetInt("FinalAiSpared", 0);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator RunFullSequence()
    {
        // --- PHASE 1: INTRO ---
        mainStoryText.text = "";
        yield return StartCoroutine(PlayTextSequence(introText, mainStoryText, true, false));

        // 1. Human Signals (Traditional Count Up)
        yield return StartCoroutine(TypeLine("HUMAN SIGNALS DETECTED: ", mainStoryText, true, false));
        yield return StartCoroutine(CountUpEffect(humansSpared, mainStoryText));
        yield return new WaitForSeconds(1f);

        // 2. Non-Human Signals (Pause then Instant Deep Red Reveal)
        yield return StartCoroutine(TypeLine("NON-HUMAN SIGNALS DETECTED: ", mainStoryText, true, false));

        yield return new WaitForSeconds(1.5f);

        mainStoryText.text += "<color=#630f09>" + aiSpared.ToString() + "</color>";
        mainStoryText.ForceMeshUpdate();
        mainStoryText.maxVisibleCharacters = mainStoryText.textInfo.characterCount;

        if (aiSpared >= 1)
        {
            if (horrorSignalSound != null)
            {
                horrorSignalSound.volume = 0.03f; // Set a base volume
                horrorSignalSound.Play();
            }
        }
        else
        {
            if (signalCountSound != null) signalCountSound.Play();
        }

        // --- WAIT while the horror sound plays ---
        yield return new WaitForSeconds(2f);

        // ⭐ FADE OUT THE HORROR SOUND before moving on
        if (horrorSignalSound != null && horrorSignalSound.isPlaying)
        {
            yield return StartCoroutine(FadeOutAudio(horrorSignalSound, 0.5f));
        }

        yield return StartCoroutine(TypeLine("ISOLATING SOURCE…", mainStoryText, true, false));
        yield return new WaitForSeconds(1.5f);

        // --- PHASE 2: DEV TALKING ---
        string endingID = DetermineEndingID();
        GameObject activeImage = (endingID == "E") ? zeroTeamImage : devImage;

        mainStoryText.text = "";

        if (activeImage != null)
        {
            activeImage.SetActive(true);
            imageOverlayText.text = "";

            string textToShow = "";
            switch (endingID)
            {
                case "A": textToShow = endingA_DevText; break;
                case "B": textToShow = endingB_DevText; break;
                case "C": textToShow = endingC_DevText; break;
                case "D": textToShow = endingD_DevText; break;
                case "E": textToShow = endingE_DevText; break;
            }

            yield return StartCoroutine(PlayTextSequence(textToShow, imageOverlayText, true, true));
            yield return new WaitForSeconds(3f);
            activeImage.SetActive(false);
            imageOverlayText.text = "";
        }

        // --- PHASE 3: SYSTEM CONCLUSION ---
        if (endingID == "A" || endingID == "B" || endingID == "C")
        {
            string systemTextToShow = "";
            if (endingID == "A") systemTextToShow = endingA_SystemText;
            else if (endingID == "B") systemTextToShow = endingB_SystemText;
            else if (endingID == "C") systemTextToShow = endingC_SystemText;

            if (systemTextToShow != "")
            {
                mainStoryText.text = "";
                yield return StartCoroutine(TypeLine(systemTextToShow, mainStoryText, true, false));
                yield return new WaitForSeconds(3f);
            }
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        SceneManager.LoadScene(postEndingSceneName);
    }

    private string DetermineEndingID()
    {
        int totalSpared = humansSpared + aiSpared;
        if (totalSpared == 0) return "E";
        if (totalSpared == 6) return "D";
        if (humansSpared == 4 && aiSpared == 0) return "C";
        if (humansSpared == 3 && aiSpared == 0) return "B";
        return "A";
    }

    private IEnumerator PlayTextSequence(string fullText, TextMeshProUGUI targetTextObj, bool append, bool useFixedSpeed)
    {
        string[] lines = fullText.Split(new string[] { "||" }, System.StringSplitOptions.None);
        int linesOnCurrentPage = 0;

        foreach (string line in lines)
        {
            if (line.Trim() == "[pause]") { yield return new WaitForSeconds(1.0f); continue; }

            if (useFixedSpeed && append && linesOnCurrentPage >= maxLinesPerPage)
            {
                yield return new WaitForSeconds(pageClearDelay);
                targetTextObj.text = "";
                linesOnCurrentPage = 0;
            }

            yield return StartCoroutine(TypeLine(line, targetTextObj, append, useFixedSpeed));
            linesOnCurrentPage++;
            yield return new WaitForSeconds(lineDelay);
        }
    }

    private IEnumerator TypeLine(string line, TextMeshProUGUI targetTextObj, bool append, bool useFixedSpeed)
    {
        // Regex adjusted to allow <color> tags but remove custom <slow> tags
        string cleanLine = Regex.Replace(line, @"<slow>|</slow>|\[pause\]", "");
        int startIndex = 0;

        if (append)
        {
            if (targetTextObj.text.Length > 0) targetTextObj.text += "\n";
            startIndex = targetTextObj.text.Length;
            targetTextObj.text += cleanLine;
        }
        else
        {
            targetTextObj.text = cleanLine;
            startIndex = 0;
        }

        targetTextObj.ForceMeshUpdate();
        targetTextObj.maxVisibleCharacters = startIndex;
        int totalCharacters = targetTextObj.textInfo.characterCount;
        int counter = startIndex;
        float currentSpeed = useFixedSpeed ? devTypingSpeed : defaultTypingSpeed;

        while (counter <= totalCharacters)
        {
            if (!useFixedSpeed)
            {
                int localIndex = counter - startIndex;
                if (line.Contains("<slow>") && localIndex > line.IndexOf("<slow>") && localIndex < line.IndexOf("</slow>"))
                    currentSpeed = 0.15f;
                else
                    currentSpeed = defaultTypingSpeed;
            }

            targetTextObj.maxVisibleCharacters = counter;

            if (counter > startIndex && counter <= totalCharacters && typeSoundAudio != null)
            {
                if (counter - 1 < targetTextObj.textInfo.characterInfo.Length)
                {
                    char c = targetTextObj.textInfo.characterInfo[counter - 1].character;
                    if (!char.IsWhiteSpace(c) && targetTextObj == mainStoryText)
                        typeSoundAudio.Play();
                }
            }
            counter++;
            yield return new WaitForSeconds(currentSpeed);
        }
    }

    private IEnumerator FadeOutAudio(AudioSource audioSource, float duration)
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / duration;
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume; // Reset volume for next time
    }

    private IEnumerator CountUpEffect(int finalCount, TextMeshProUGUI targetTextObj)
    {
        string baseText = targetTextObj.text;
        for (int i = 0; i <= finalCount; i++)
        {
            targetTextObj.text = baseText + i.ToString();
            targetTextObj.ForceMeshUpdate();
            targetTextObj.maxVisibleCharacters = targetTextObj.textInfo.characterCount;
            if (signalCountSound != null) signalCountSound.Play();
            yield return new WaitForSeconds(countUpSpeed);
        }
    }
}