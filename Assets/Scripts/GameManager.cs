using UnityEngine;
using UnityEngine.UI; // Needed for Button
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    // ========================================================================
    //                                DEFINITIONS & VARIABLES
    // ========================================================================
    #region Definitions & UI Variables
    public enum GameState { Dialogue, Photos, Decision }

    [Header("Current State")]
    public GameState currentState;

    [Header("UI Elements")]
    // ⭐ FIX: Explicit UnityEngine.UI to avoid conflict with System libraries
    public UnityEngine.UI.Image fadeImage;
    public float fadeSpeed = 2f;
    public GameObject imageFrame;
    public UnityEngine.UI.Image photoDisplay;
    public TextMeshProUGUI dialogueText;
    public UnityEngine.UI.Image backgroundDisplay;
    public UnityEngine.UI.Image characterDisplay;

    [Header("Navigation Buttons")]
    public Button nextButton;
    public Button prevButton;

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

    // Timer Variables
    private float photoTimerDuration = 6f;
    private float photoTimer = 0f;
    private bool isPhotoTimerActive = false;

    // Audio Flags
    private bool suddenSoundPlayed = false;
    #endregion

    // ========================================================================
    //                                UNITY LIFECYCLE
    // ========================================================================
    #region Unity Lifecycle
    private void Start()
    {
        InitializeAudio();
        SetupButtons();

        // Initial UI State
        SetButtonsActive(false);
        if (decisionPanel != null)
        {
            decisionPanel.SetActive(false);
        }

        UpdateCounterDisplay();

        // Start Game
        SetState(GameState.Dialogue);
    }

    private void Update()
    {
        // Handle Timer
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
    //                                STATE MACHINE
    // ========================================================================
    #region State Management
    public void SetState(GameState newState)
    {
        currentState = newState;
        StopAllCoroutines();
        isPhotoTimerActive = false;

        switch (currentState)
        {
            case GameState.Dialogue:
                SetButtonsActive(false);
                if (decisionPanel != null) decisionPanel.SetActive(false);
                StartCoroutine(HandleDialogueState());
                break;

            case GameState.Photos:
                SetButtonsActive(true);
                if (decisionPanel != null) decisionPanel.SetActive(false);
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

    // ========================================================================
    //                                COROUTINES (THE LOGIC)
    // ========================================================================
    #region Coroutines
    IEnumerator HandleDialogueState()
    {
        // Safety Checks
        if (allCharacters == null || allCharacters.Length == 0) yield break;
        if (currentCharacterIndex >= allCharacters.Length) yield break;

        CharacterData data = allCharacters[currentCharacterIndex];

        // I have kept the logs removed here as requested in previous steps, 
        // but the TIMING is restored to normal.

        // 1. Setup Scene
        imageFrame.SetActive(false);
        if (data.backgroundImage != null) backgroundDisplay.sprite = data.backgroundImage;

        // 2. Setup Character
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

        // 3. Fade In
        yield return StartCoroutine(FadeEffect(0f));

        // 4. Type Text (Normal Speed)
        yield return StartCoroutine(TypewriterEffect(data.dialogueText));

        // ✅ RESTORED: Wait 2 seconds before moving to photos
        yield return new WaitForSeconds(2f);

        SetState(GameState.Photos);
    }

    IEnumerator HandlePhotosState()
    {
        if (allCharacters == null || allCharacters.Length == 0) yield break;

        CharacterData data = allCharacters[currentCharacterIndex];

        // 1. Blink
        yield return StartCoroutine(FadeEffect(1f));

        // ✅ RESTORED: Wait 0.5s
        yield return new WaitForSeconds(0.5f);

        // 2. Setup Photo UI
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

        // ✅ RESTORED: Wait 0.5s
        yield return new WaitForSeconds(0.5f);

        // 3. Fade In & Text
        yield return StartCoroutine(FadeEffect(0f));

        // ✅ RESTORED: Wait 1s before typing
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(TypewriterEffect(data.stage2DialogueText));

        // ✅ RESTORED: Wait 1s after typing
        yield return new WaitForSeconds(1f);

        // 4. Random Sound
        // ⭐ FIX: Explicit UnityEngine.Random
        if (!suddenSoundPlayed && UnityEngine.Random.value > 0.5f)
        {
            PlaySuddenSound();
            suddenSoundPlayed = true;
        }

        // 5. Start Timer
        // ✅ RESTORED: Original Timer Duration (6 seconds)
        photoTimer = photoTimerDuration;
        isPhotoTimerActive = true;

        while (isPhotoTimerActive && currentState == GameState.Photos)
        {
            yield return null;
        }
    }

    IEnumerator HandleDecisionState()
    {
        yield return StartCoroutine(FadeEffect(1f));
        yield return new WaitForSeconds(0.3f);

        // Setup Decision Screen
        imageFrame.SetActive(false);
        CharacterData data = allCharacters[currentCharacterIndex];

        if (data.stage3Background != null) backgroundDisplay.sprite = data.stage3Background;

        if (characterDisplay != null)
        {
            RectTransform charRT = characterDisplay.GetComponent<RectTransform>();
            if (charRT != null)
            {
                charRT.anchoredPosition = data.stage3Position;
                charRT.localScale = Vector3.one * data.stage3Scale;
            }
        }

        dialogueText.text = "";
        if (decisionPanel != null) decisionPanel.SetActive(true);

        if (spareButton != null) spareButton.gameObject.SetActive(true);
        if (killButton != null) killButton.gameObject.SetActive(true);

        yield return StartCoroutine(FadeEffect(0f));
    }

    IEnumerator TypewriterEffect(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        dialogueText.text = "";
        foreach (char c in text)
        {
            if (c == '\n')
            {
                dialogueText.text = "";
            }
            else
            {
                dialogueText.text += c;
                PlayTypewriterSound();
            }

            // ✅ RESTORED: Typewriter Speed Delay
            yield return new WaitForSeconds(typewriterSpeed);
        }
        yield return null;
    }

    IEnumerator FadeAndChangeCharacter()
    {
        yield return StartCoroutine(FadeEffect(1f));
        yield return new WaitForSeconds(0.3f);
        SetState(GameState.Dialogue);
    }
    #endregion

    // ========================================================================
    //                                INPUT HANDLING
    // ========================================================================
    #region Input Handling
    private void OnNextPhotoClicked()
    {
        PlayButtonClickSound();
        NextPhoto();
    }

    private void OnPrevPhotoClicked()
    {
        PlayButtonClickSound();
        PrevPhoto();
    }

    private void OnSpareButtonClicked()
    {
        PlayButtonClickSound();
        OnSparePressed();
    }

    private void OnKillButtonClicked()
    {
        PlayButtonClickSound();
        OnKillPressed();
    }

    public void OnSparePressed()
    {
        if (currentState != GameState.Decision) return;

        CharacterData data = allCharacters[currentCharacterIndex];

        visibleTeamCount++;
        UpdateCounterDisplay();

        if (data.type == CharacterData.CharacterType.Human)
        {
            hiddenHumanCount++;
        }
        else
        {
        }

        NextCharacter();
    }

    public void OnKillPressed()
    {
        if (currentState != GameState.Decision) return;
        NextCharacter();
    }
    #endregion

    // ========================================================================
    //                                HELPER FUNCTIONS
    // ========================================================================
    #region Audio System
    private void InitializeAudio()
    {
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.volume = musicVolume * masterVolume;
            if (!backgroundMusicSource.isPlaying && backgroundMusicSource.clip != null)
            {
                backgroundMusicSource.loop = true;
                backgroundMusicSource.Play();
            }
        }

        if (typewriterSoundSource != null) typewriterSoundSource.volume = sfxVolume * masterVolume;
        if (buttonClickSoundSource != null) buttonClickSoundSource.volume = sfxVolume * masterVolume;
        if (suddenSoundSource != null) suddenSoundSource.volume = sfxVolume * masterVolume;
    }

    private void PlayTypewriterSound()
    {
        if (typewriterSoundSource != null && typewriterSoundSource.clip != null)
        {
            typewriterSoundSource.volume = typewriterVolume * masterVolume;
            typewriterSoundSource.PlayOneShot(typewriterSoundSource.clip);
        }
    }

    private void PlayButtonClickSound()
    {
        if (buttonClickSoundSource != null && buttonClickSoundSource.clip != null)
        {
            buttonClickSoundSource.volume = sfxVolume * masterVolume;
            buttonClickSoundSource.PlayOneShot(buttonClickSoundSource.clip);
        }
    }

    private void PlaySuddenSound()
    {
        if (suddenSoundSource != null && suddenSoundSource.clip != null)
        {
            suddenSoundSource.volume = sfxVolume * masterVolume;
            suddenSoundSource.PlayOneShot(suddenSoundSource.clip);
        }
    }
    #endregion

    #region UI & Navigation Helpers
    private void SetupButtons()
    {
        if (nextButton != null) { nextButton.onClick.RemoveAllListeners(); nextButton.onClick.AddListener(OnNextPhotoClicked); }
        if (prevButton != null) { prevButton.onClick.RemoveAllListeners(); prevButton.onClick.AddListener(OnPrevPhotoClicked); }
        if (spareButton != null) { spareButton.onClick.RemoveAllListeners(); spareButton.onClick.AddListener(OnSpareButtonClicked); }
        if (killButton != null) { killButton.onClick.RemoveAllListeners(); killButton.onClick.AddListener(OnKillButtonClicked); }
    }

    private void SetButtonsActive(bool active)
    {
        if (nextButton != null) nextButton.gameObject.SetActive(active);
        if (prevButton != null) prevButton.gameObject.SetActive(active);
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
        if (data.storyImages == null || data.storyImages.Length == 0) return;

        currentPhotoIndex = (currentPhotoIndex + 1) % data.storyImages.Length;
        photoDisplay.sprite = data.storyImages[currentPhotoIndex];
        UpdateButtonStates(data);
    }

    public void PrevPhoto()
    {
        if (currentState != GameState.Photos) return;
        CharacterData data = allCharacters[currentCharacterIndex];
        if (data.storyImages == null || data.storyImages.Length == 0) return;

        currentPhotoIndex = (currentPhotoIndex - 1 + data.storyImages.Length) % data.storyImages.Length;
        photoDisplay.sprite = data.storyImages[currentPhotoIndex];
        UpdateButtonStates(data);
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
    #endregion

    #region Scene & Flow Helpers
    void NextCharacter()
    {
        currentCharacterIndex++;

        if (currentCharacterIndex < allCharacters.Length)
        {
            currentPhotoIndex = 0;
            StartCoroutine(FadeAndChangeCharacter());
        }
        else
        {
            TransitionToEndScene();
        }
    }

    void TransitionToEndScene()
    {
        int finalHumans = hiddenHumanCount;
        int finalAI = visibleTeamCount - hiddenHumanCount;

        PlayerPrefs.SetInt("FinalHumansSpared", finalHumans);
        PlayerPrefs.SetInt("FinalAiSpared", finalAI);
        PlayerPrefs.Save();

        StartCoroutine(FadeAndLoadScene(nextSceneName));
    }

    IEnumerator FadeAndLoadScene(string sceneName)
    {
        yield return StartCoroutine(FadeEffect(1f));
        SceneManager.LoadScene(sceneName);
    }

    // Audio Setters
    public void SetMasterVolume(float volume) { masterVolume = Mathf.Clamp01(volume); InitializeAudio(); }
    public void SetMusicVolume(float volume) { musicVolume = Mathf.Clamp01(volume); InitializeAudio(); }
    public void SetSFXVolume(float volume) { sfxVolume = Mathf.Clamp01(volume); InitializeAudio(); }
    public void SetTypewriterVolume(float volume) { typewriterVolume = Mathf.Clamp01(volume); InitializeAudio(); }
    public void SetTypewriterSpeed(float speed) { typewriterSpeed = Mathf.Clamp(speed, 0.01f, 0.5f); }
    public void SetNextSceneName(string sceneName) { nextSceneName = sceneName; }
    #endregion
}