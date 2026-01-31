 using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using static GameManager;

public class GameManager : MonoBehaviour
{
    // ========================================================================
    //                                DEFINITIONS & VARIABLES
    // ========================================================================
    #region Definitions & UI Variables
    public enum GameState { Dialogue, Photos, Decision }

    [Header("Current State")]
    public GameState currentState;

    [Header("UI Elements")]
    public UnityEngine.UI.Image fadeImage;
    public float fadeSpeed = 2f;
    public GameObject imageFrame;
    public UnityEngine.UI.Image photoDisplay;
    public TextMeshProUGUI dialogueText;
    public UnityEngine.UI.Image backgroundDisplay;
    public UnityEngine.UI.Image characterDisplay;
    public GameObject textBar;

    [Header("Navigation Buttons")]
    public Button nextButton;
    public Button prevButton;
    public Button skipButton;

    [Header("Decision UI")]
    public GameObject decisionPanel;
    public Button spareButton;
    public Button killButton;

    [Header("Counter UI")]
    public TextMeshProUGUI counterText;
    #endregion

    #region Settings & Audio Variables
    [Header("Typewriter Settings")]
    public float typewriterSpeed = 0.12f;

    [Header("Audio Sources")]
    public AudioSource backgroundMusicSource;
    public AudioSource typewriterSoundSource;
    public AudioSource buttonClickSoundSource;
    public AudioSource suddenSoundSource;

    [Header("Audio Volume Settings")]
    public float masterVolume = 1f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 0.8f;
    public float typewriterVolume = 1f;

    [Header("Scene Settings")]
    public string nextSceneName = "EndScene";
    #endregion

    #region Game Data Variables
    [Header("Game Data")]
    public CharacterData[] allCharacters;
    private int currentCharacterIndex = 0;
    private int currentPhotoIndex = 0;

    private int visibleTeamCount = 0;
    private int hiddenHumanCount = 0;

    // Timer & Skip Variables
    private float photoTimerDuration = 6f;
    private float photoTimer = 0f;
    private bool isPhotoTimerActive = false;
    private bool isTyping = false;
    private bool skipRequested = false;

    private bool suddenSoundPlayed = false;
    #endregion

    // ========================================================================
    //                                UNITY LIFECYCLE
    // ========================================================================
    #region Unity Lifecycle
    private void Start()
    {
        InitializeAudio();
        SetupButtons();

        SetButtonsActive(false);
        if (decisionPanel != null) decisionPanel.SetActive(false);

        UpdateCounterDisplay();

        string selectedName = PlayerPrefs.GetString("SelectedCharacter", "");

        // 2. Find the character in your library
        for (int i = 0; i < allCharacters.Length; i++)
        {
            if (allCharacters[i].characterName == selectedName)
            {
                currentCharacterIndex = i; // Set the index for the sequence
                break;
            }
        }

        SetState(GameState.Dialogue); // Start the intro

    }

    private void Update()
    {
        if (isPhotoTimerActive && currentState == GameState.Photos)
        {
            photoTimer -= Time.deltaTime;
            if (photoTimer <= 0f)
            {
                isPhotoTimerActive = false;
                SetState(GameState.Decision);
            }
        }
    }
    #endregion

    // ========================================================================
    //                                STATE MACHINE
    // ========================================================================
    #region State Management
    public void SetState(GameState newState)
    {
        currentState = newState;
        StopAllCoroutines();
        isPhotoTimerActive = false;
        isTyping = false;
        skipRequested = false;

        if (skipButton != null) skipButton.gameObject.SetActive(false);

        switch (currentState)
        {
            case GameState.Dialogue:
                SetButtonsActive(false);
                if (decisionPanel != null) decisionPanel.SetActive(false);
                if (textBar != null) textBar.SetActive(true);   // ✅ SHOW
                StartCoroutine(HandleDialogueState());
                break;

            case GameState.Photos:
                SetButtonsActive(true);
                if (decisionPanel != null) decisionPanel.SetActive(false);
                if (textBar != null) textBar.SetActive(true);   // ✅ SHOW
                suddenSoundPlayed = false;
                StartCoroutine(HandlePhotosState());
                break;

            case GameState.Decision:
                SetButtonsActive(false);
                StartCoroutine(HandleDecisionState());
                break;
        }
    }

    #endregion

    #region Coroutines
    IEnumerator HandleDialogueState()
    {
        if (allCharacters == null || allCharacters.Length == 0) yield break;
        if (currentCharacterIndex >= allCharacters.Length) yield break;

        CharacterData data = allCharacters[currentCharacterIndex];

        imageFrame.SetActive(false);
        if (data.backgroundImage != null) backgroundDisplay.sprite = data.backgroundImage;

        if (data.characterSprite != null && characterDisplay != null)
        {
            characterDisplay.sprite = data.characterSprite;
            RectTransform charRT = characterDisplay.GetComponent<RectTransform>();
            if (charRT != null)
            {
                charRT.anchoredPosition = data.spawnPosition;
                charRT.localScale = Vector3.one * data.characterScale;
            }
            characterDisplay.gameObject.SetActive(true);
        }

        yield return StartCoroutine(FadeEffect(0f));
        yield return StartCoroutine(TypewriterEffect(data.dialogueText));

        // Skipable wait after typing - logic remains but won't trigger if skipRequested was just used
        float waitTimer = 2f;
        while (waitTimer > 0 && !skipRequested)
        {
            waitTimer -= Time.deltaTime;
            yield return null;
        }
        skipRequested = false;

        SetState(GameState.Photos);
    }

    IEnumerator HandlePhotosState()
    {
        if (allCharacters == null || allCharacters.Length == 0) yield break;
        CharacterData data = allCharacters[currentCharacterIndex];

        yield return StartCoroutine(FadeEffect(1f));
        yield return new WaitForSeconds(0.5f);

        dialogueText.text = "";
        if (data.stage2Background != null) backgroundDisplay.sprite = data.stage2Background;

        if (characterDisplay != null)
        {
            RectTransform charRT = characterDisplay.GetComponent<RectTransform>();
            if (charRT != null)
            {
                charRT.anchoredPosition = data.stage2Position;
                charRT.localScale = Vector3.one * data.stage2Scale;
            }
        }

        imageFrame.SetActive(true);
        RectTransform frameRT = imageFrame.GetComponent<RectTransform>();
        if (frameRT != null)
        {
            frameRT.anchoredPosition = data.framePosition;
            frameRT.localScale = Vector3.one * data.frameScale;
        }

        currentPhotoIndex = 0;
        if (data.storyImages != null && data.storyImages.Length > 0)
        {
            photoDisplay.sprite = data.storyImages[0];
            UpdateButtonStates(data);
        }

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FadeEffect(0f));

        float waitBefore = 1f;
        while (waitBefore > 0 && !skipRequested) { waitBefore -= Time.deltaTime; yield return null; }
        skipRequested = false;

        yield return StartCoroutine(TypewriterEffect(data.stage2DialogueText));

        float waitAfter = 1f;
        while (waitAfter > 0 && !skipRequested) { waitAfter -= Time.deltaTime; yield return null; }
        skipRequested = false;

        if (!suddenSoundPlayed && UnityEngine.Random.value > 0.5f)
        {
            PlaySuddenSound();
            suddenSoundPlayed = true;
        }

        photoTimer = photoTimerDuration;
        isPhotoTimerActive = true;

        while (isPhotoTimerActive && currentState == GameState.Photos)
        {
            yield return null;
        }
    }

    IEnumerator HandleDecisionState()
    {
        // Fade to black FIRST
        yield return StartCoroutine(FadeEffect(1f));

        // NOW hide dialogue UI
        if (textBar != null) textBar.SetActive(false);
        dialogueText.text = "";

        yield return new WaitForSeconds(0.3f);

        imageFrame.SetActive(false);
        CharacterData data = allCharacters[currentCharacterIndex];

        if (data.stage3Background != null)
            backgroundDisplay.sprite = data.stage3Background;

        if (characterDisplay != null)
        {
            RectTransform charRT = characterDisplay.GetComponent<RectTransform>();
            if (charRT != null)
            {
                charRT.anchoredPosition = data.stage3Position;
                charRT.localScale = Vector3.one * data.stage3Scale;
            }
        }

        if (decisionPanel != null) decisionPanel.SetActive(true);
        if (spareButton != null) spareButton.gameObject.SetActive(true);
        if (killButton != null) killButton.gameObject.SetActive(true);

        // Fade back in
        yield return StartCoroutine(FadeEffect(0f));
    }

    IEnumerator FadeAndChangeCharacter()
    {
        yield return StartCoroutine(FadeEffect(1f));
        yield return new WaitForSeconds(0.3f);
        SetState(GameState.Dialogue);
    }
    #endregion

    // ========================================================================
    //                                INPUT HANDLING
    // ========================================================================
    #region Input Handling
    IEnumerator TypewriterEffect(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        isTyping = true;
        skipRequested = false;
        dialogueText.text = "";

        // Ensure the button is hidden or non-interactable until typing starts
        if (skipButton != null && currentState != GameState.Decision)
            skipButton.gameObject.SetActive(true);

        foreach (char c in text)
        {
            if (skipRequested)
            {
                dialogueText.text = text; // Instantly show the full text
                break;
            }

            if (c == '\n')
            {
                dialogueText.text += c; // Add the newline instead of clearing
            }
            else
            {
                dialogueText.text += c;
                PlayTypewriterSound();
            }
            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;
        // Wait a short duration so the "finish typing" click 
        yield return new WaitForSeconds(0.3f);
        skipRequested = false;
    }

    // 3. Updated OnSkipClicked with a text length check
    private void OnSkipClicked()
    {
        // Don't do anything if the text hasn't even started (length 0)
        if (dialogueText.text.Length == 0) return;

        PlayButtonClickSound();

        if (isTyping)
        {
            skipRequested = true;
        }
        else
        {
            AdvanceGameState();
        }
    }

    private void AdvanceGameState()
    {
        // Safety: Do not skip to next state if the typewriter is still working 
        // (This prevents the "double jump" if clicking very fast)
        if (isTyping) return;

        StopAllCoroutines();
        isTyping = false;
        skipRequested = false;

        if (currentState == GameState.Dialogue) SetState(GameState.Photos);
        else if (currentState == GameState.Photos) SetState(GameState.Decision);
    }

    private void OnNextPhotoClicked() { PlayButtonClickSound(); NextPhoto(); }
    private void OnPrevPhotoClicked() { PlayButtonClickSound(); PrevPhoto(); }
    private void OnSpareButtonClicked() { PlayButtonClickSound(); OnSparePressed(); }
    private void OnKillButtonClicked() { PlayButtonClickSound(); OnKillPressed(); }

public void OnSparePressed()
{
    if (currentState != GameState.Decision) return;
    CharacterData data = allCharacters[currentCharacterIndex];

    // SAVE: 1 for Spare
    PlayerPrefs.SetInt("Decision_" + data.characterName, 1);
    PlayerPrefs.Save();

    StartCoroutine(FadeAndReturnToHub());
}

public void OnKillPressed()
{
    if (currentState != GameState.Decision) return;
    CharacterData data = allCharacters[currentCharacterIndex];

    // SAVE: 0 for Kill
    PlayerPrefs.SetInt("Decision_" + data.characterName, 0);
    PlayerPrefs.Save();

    StartCoroutine(FadeAndReturnToHub());
}

IEnumerator FadeAndReturnToHub()
{
    yield return StartCoroutine(FadeEffect(1f)); // Fade to black
    SceneManager.LoadScene("MainHub"); // Back to the Hub
}
#endregion

// ========================================================================
//                                HELPERS
// ========================================================================
#region Helpers & UI
private void InitializeAudio()
    {
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.volume = musicVolume * masterVolume;
            if (!backgroundMusicSource.isPlaying) backgroundMusicSource.Play();
        }
        if (typewriterSoundSource != null) typewriterSoundSource.volume = sfxVolume * masterVolume;
        if (buttonClickSoundSource != null) buttonClickSoundSource.volume = sfxVolume * masterVolume;
        if (suddenSoundSource != null) suddenSoundSource.volume = sfxVolume * masterVolume;
    }

    private void PlayTypewriterSound()
    {
        if (typewriterSoundSource != null && typewriterSoundSource.clip != null)
            typewriterSoundSource.PlayOneShot(typewriterSoundSource.clip, typewriterVolume);
    }

    private void PlayButtonClickSound()
    {
        if (buttonClickSoundSource != null && buttonClickSoundSource.clip != null)
            buttonClickSoundSource.PlayOneShot(buttonClickSoundSource.clip);
    }

    private void PlaySuddenSound()
    {
        if (suddenSoundSource != null && suddenSoundSource.clip != null)
            suddenSoundSource.PlayOneShot(suddenSoundSource.clip);
    }

    private void SetupButtons()
    {
        if (nextButton != null) { nextButton.onClick.RemoveAllListeners(); nextButton.onClick.AddListener(OnNextPhotoClicked); }
        if (prevButton != null) { prevButton.onClick.RemoveAllListeners(); prevButton.onClick.AddListener(OnPrevPhotoClicked); }
        if (spareButton != null) { spareButton.onClick.RemoveAllListeners(); spareButton.onClick.AddListener(OnSpareButtonClicked); }
        if (killButton != null) { killButton.onClick.RemoveAllListeners(); killButton.onClick.AddListener(OnKillButtonClicked); }
        if (skipButton != null) { skipButton.onClick.RemoveAllListeners(); skipButton.onClick.AddListener(OnSkipClicked); }
    }

    private void SetButtonsActive(bool active)
    {
        if (nextButton != null) nextButton.gameObject.SetActive(active);
        if (prevButton != null) prevButton.gameObject.SetActive(active);

        // ⭐ Hide skipButton if we are in Decision state, regardless of 'active' param
        if (skipButton != null)
        {
            if (currentState == GameState.Decision)
                skipButton.gameObject.SetActive(false);
            else
                skipButton.gameObject.SetActive(true);
        }

        if (spareButton != null) spareButton.gameObject.SetActive(false);
        if (killButton != null) killButton.gameObject.SetActive(false);
    }

    private void UpdateButtonStates(CharacterData data)
    {
        bool interactable = (data.storyImages != null && data.storyImages.Length > 0);
        if (nextButton != null) nextButton.interactable = interactable;
        if (prevButton != null) prevButton.interactable = interactable;
    }

    public void NextPhoto()
    {
        if (currentState != GameState.Photos) return;
        CharacterData data = allCharacters[currentCharacterIndex];
        currentPhotoIndex = (currentPhotoIndex + 1) % data.storyImages.Length;
        photoDisplay.sprite = data.storyImages[currentPhotoIndex];
    }

    public void PrevPhoto()
    {
        if (currentState != GameState.Photos) return;
        CharacterData data = allCharacters[currentCharacterIndex];
        currentPhotoIndex = (currentPhotoIndex - 1 + data.storyImages.Length) % data.storyImages.Length;
        photoDisplay.sprite = data.storyImages[currentPhotoIndex];
    }

    private void UpdateCounterDisplay()
    {
        if (counterText != null) counterText.text = $"Team : {visibleTeamCount}";
    }

    IEnumerator FadeEffect(float target)
    {
        fadeImage.gameObject.SetActive(true);
        float start = fadeImage.color.a;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * fadeSpeed;
            fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(start, target, t));
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, target);
        if (target == 0) fadeImage.gameObject.SetActive(false);
    }

    void NextCharacter()
    {
        currentCharacterIndex++;
        if (currentCharacterIndex < allCharacters.Length) StartCoroutine(FadeAndChangeCharacter());
        else TransitionToEndScene();
    }

    void TransitionToEndScene()
    {
        PlayerPrefs.SetInt("FinalHumansSpared", hiddenHumanCount);
        PlayerPrefs.SetInt("FinalAiSpared", visibleTeamCount - hiddenHumanCount);
        PlayerPrefs.Save();
        StartCoroutine(FadeAndLoadScene(nextSceneName));
    }

    IEnumerator FadeAndLoadScene(string sceneName)
    {
        yield return StartCoroutine(FadeEffect(1f));
        SceneManager.LoadScene(sceneName);
    }
    #endregion
}