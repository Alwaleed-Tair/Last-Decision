using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager - CanvasGroup Fade Version (RESOLVED)
/// Combines improvements from both branches
/// </summary>
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

    [Header("Object Container")]
    public Transform objectContainer;
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
    private CharacterData currentCharacter;
    private int currentPhotoIndex = 0;

    private int visibleTeamCount = 0;
    private int hiddenHumanCount = 0;

    private float photoTimerDuration = 6f;
    private float photoTimer = 0f;
    private bool isPhotoTimerActive = false;
    private bool isTyping = false;
    private bool skipRequested = false;

    private bool suddenSoundPlayed = false;

    private GameObject currentSpawnedObject;
    
    private Coroutine currentTypewriterCoroutine;
    
    private CanvasGroup fadeCanvasGroup;
    #endregion

    // ========================================================================
    //                                UNITY LIFECYCLE
    // ========================================================================
    #region Unity Lifecycle
    private void Start()
    {
        // Setup Fade - Using CanvasGroup
        if (fadeImage == null)
        {
            Debug.LogError("❌ FADE IMAGE IS NULL! Assign it in Inspector!");
            return;
        }

        fadeCanvasGroup = fadeImage.GetComponent<CanvasGroup>();
        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();
            Debug.Log("✅ Added CanvasGroup to FadeImage");
        }
        
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = Color.black;
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable = false;
        
        Debug.Log("✅ GameManager Started - Screen is BLACK (CanvasGroup)");
        Debug.Log($"   FadeImage: {fadeImage.name}");
        Debug.Log($"   CanvasGroup Alpha: {fadeCanvasGroup.alpha}");

        InitializeAudio();
        SetupButtons();
        SetButtonsActive(false);
        
        if (decisionPanel != null) 
            decisionPanel.SetActive(false);
        
        UpdateCounterDisplay();
        
        StartCoroutine(InitialFadeIn());
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

    IEnumerator InitialFadeIn()
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log("🎬 Initial Fade In - Revealing Door Room");
        yield return StartCoroutine(FadeToAlpha(0f));
    }
    #endregion

    // ========================================================================
    //                         CHARACTER SEQUENCE CONTROL
    // ========================================================================
    #region Character Sequence Control
    
    public void StartCharacterSequence(CharacterData character)
    {
        if (character == null)
        {
            Debug.LogError("❌ Character is NULL!");
            return;
        }
        
        currentCharacter = character;
        currentPhotoIndex = 0;
        suddenSoundPlayed = false;
        
        Debug.Log($"🎭 Starting Character: {character.name}");
        
        StartCoroutine(StartCharacterWithFade());
    }

    IEnumerator StartCharacterWithFade()
    {
        Debug.Log("🎬 Fade Out - Transitioning to Character");
        yield return StartCoroutine(FadeToAlpha(1f));
        yield return new WaitForSeconds(0.3f);
        
        SetState(GameState.Dialogue);
    }
    
    #endregion

    // ========================================================================
    //                                STATE MACHINE
    // ========================================================================
    #region State Management
    
    public void SetState(GameState newState)
    {
        currentState = newState;
        
        if (currentTypewriterCoroutine != null)
        {
            StopCoroutine(currentTypewriterCoroutine);
            currentTypewriterCoroutine = null;
        }
        
        isPhotoTimerActive = false;
        isTyping = false;
        skipRequested = false;

        // Hide skip button immediately until typewriter is ready
        if (skipButton != null) skipButton.gameObject.SetActive(false);

        Debug.Log($"🔄 State Changed: {newState}");

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
    //                           STATE HANDLERS (3 STAGES)
    // ========================================================================
    #region State Handlers
    
    IEnumerator HandleDialogueState()
    {
        if (currentCharacter == null) yield break;

        Debug.Log("📝 Stage 1: Dialogue Started");

        imageFrame.SetActive(false);
        
        if (textBar != null) 
            textBar.SetActive(true);
        
        if (currentCharacter.backgroundImage != null) 
            backgroundDisplay.sprite = currentCharacter.backgroundImage;

        if (currentCharacter.characterSprite != null && characterDisplay != null)
        {
            characterDisplay.sprite = currentCharacter.characterSprite;
            RectTransform charRT = characterDisplay.GetComponent<RectTransform>();
            if (charRT != null)
            {
                charRT.anchoredPosition = currentCharacter.spawnPosition;
                charRT.localScale = Vector3.one * currentCharacter.characterScale;
            }
            characterDisplay.gameObject.SetActive(true);
        }

        Debug.Log("🎬 Fade In - Stage 1");
        yield return StartCoroutine(FadeToAlpha(0f));
        
        currentTypewriterCoroutine = StartCoroutine(TypewriterEffect(currentCharacter.dialogueText));
        yield return currentTypewriterCoroutine;

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
        if (currentCharacter == null) yield break;

        Debug.Log("📸 Stage 2: Photos Started");

        Debug.Log("🎬 Fade Out - Before Stage 2");
        yield return StartCoroutine(FadeToAlpha(1f));
        yield return new WaitForSeconds(0.5f);

        dialogueText.text = "";
        
        if (textBar != null) 
            textBar.SetActive(true);
        
        if (currentCharacter.stage2Background != null) 
            backgroundDisplay.sprite = currentCharacter.stage2Background;

        if (characterDisplay != null)
        {
            RectTransform charRT = characterDisplay.GetComponent<RectTransform>();
            if (charRT != null)
            {
                charRT.anchoredPosition = currentCharacter.stage2Position;
                charRT.localScale = Vector3.one * currentCharacter.stage2Scale;
            }
        }

        imageFrame.SetActive(true);
        RectTransform frameRT = imageFrame.GetComponent<RectTransform>();
        if (frameRT != null)
        {
            frameRT.anchoredPosition = currentCharacter.framePosition;
            frameRT.localScale = Vector3.one * currentCharacter.frameScale;
        }

        currentPhotoIndex = 0;
        if (currentCharacter.storyImages != null && currentCharacter.storyImages.Length > 0)
        {
            photoDisplay.sprite = currentCharacter.storyImages[0];
            UpdateButtonStates(currentCharacter);
        }

        yield return new WaitForSeconds(0.5f);
        
        Debug.log("🎬 Fade In - Stage 2");
        yield return StartCoroutine(FadeToAlpha(0f));

        float waitBefore = 1f;
        while (waitBefore > 0 && !skipRequested) 
        { 
            waitBefore -= Time.deltaTime; 
            yield return null; 
        }
        skipRequested = false;

        currentTypewriterCoroutine = StartCoroutine(TypewriterEffect(currentCharacter.stage2DialogueText));
        yield return currentTypewriterCoroutine;

        float waitAfter = 1f;
        while (waitAfter > 0 && !skipRequested) 
        { 
            waitAfter -= Time.deltaTime; 
            yield return null; 
        }
        skipRequested = false;

        if (!suddenSoundPlayed && UnityEngine.Random.value > 0.5f)
        {
            PlaySuddenSound();
            suddenSoundPlayed = true;
        }

        photoTimer = photoTimerDuration;
        isPhotoTimerActive = true;

        Debug.Log($"⏱️ Photo Timer Started: {photoTimerDuration}s");

        while (isPhotoTimerActive && currentState == GameState.Photos)
        {
            yield return null;
        }
        
        Debug.Log("⏱️ Photo Timer Finished");
    }

    IEnumerator HandleDecisionState()
    {
        Debug.Log("⚖️ Stage 3: Decision Started");

        Debug.Log("🎬 Fade Out - Before Stage 3");
        yield return StartCoroutine(FadeToAlpha(1f));
        yield return new WaitForSeconds(0.3f);

        imageFrame.SetActive(false);
        
        if (textBar != null) 
            textBar.SetActive(false);

        if (currentCharacter.stage3Background != null) 
            backgroundDisplay.sprite = currentCharacter.stage3Background;

        if (characterDisplay != null)
        {
            RectTransform charRT = characterDisplay.GetComponent<RectTransform>();
            if (charRT != null)
            {
                charRT.anchoredPosition = currentCharacter.stage3Position;
                charRT.localScale = Vector3.one * currentCharacter.stage3Scale;
            }
        }

        dialogueText.text = "";
        
        if (decisionPanel != null) 
            decisionPanel.SetActive(true);
        if (spareButton != null) 
            spareButton.gameObject.SetActive(true);
        if (killButton != null) 
            killButton.gameObject.SetActive(true);

        Debug.Log("🎬 Fade In - Stage 3");
        yield return StartCoroutine(FadeToAlpha(0f));
        
        Debug.Log("⚖️ Waiting for player decision...");
    }
    
    #endregion

    // ========================================================================
    //                                INPUT HANDLING
    // ========================================================================
    #region Input Handling
    
    IEnumerator TypewriterEffect(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        isTyping = true;
        skipRequested = false;
        dialogueText.text = "";

        // Show skip button once typing starts (but not in Decision state)
        if (skipButton != null && currentState != GameState.Decision)
            skipButton.gameObject.SetActive(true);

        foreach (char c in text)
        {
            if (skipRequested)
            {
                dialogueText.text = text;
                break;
            }

            if (c == '\n')
            {
                dialogueText.text = "";
            }
            else
            {
                dialogueText.text += c;
                PlayTypewriterSound();
            }

            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;
        // Wait briefly so "finish typing" click doesn't immediately advance
        yield return new WaitForSeconds(0.3f);
        skipRequested = false;
        currentTypewriterCoroutine = null;
    }

    private void OnSkipClicked()
    {
        // Don't do anything if text hasn't started yet
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
        if (isTyping) return;

        if (currentTypewriterCoroutine != null)
        {
            StopCoroutine(currentTypewriterCoroutine);
            currentTypewriterCoroutine = null;
        }
        
        isTyping = false;
        skipRequested = false;

        if (currentState == GameState.Dialogue) 
            SetState(GameState.Photos);
        else if (currentState == GameState.Photos) 
            SetState(GameState.Decision);
    }

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
        if (currentState != GameState.Decision || currentCharacter == null) 
            return;
        
        Debug.Log($"💚 SPARE Decision - Character: {currentCharacter.name}");
        
        visibleTeamCount++;
        UpdateCounterDisplay();
        
        if (currentCharacter.type == CharacterData.CharacterType.Human) 
            hiddenHumanCount++;

        SpawnDecisionObject(currentCharacter.spareModification);

        if (DoorSystemManager.Instance != null)
        {
            DoorSystemManager.Instance.OnCharacterDecisionMade(currentCharacter, true);
        }

        StartCoroutine(FadeOutAndReturn());
    }

    public void OnKillPressed()
    {
        if (currentState != GameState.Decision || currentCharacter == null) 
            return;

        Debug.Log($"💔 KILL Decision - Character: {currentCharacter.name}");

        SpawnDecisionObject(currentCharacter.killModification);

        if (DoorSystemManager.Instance != null)
        {
            DoorSystemManager.Instance.OnCharacterDecisionMade(currentCharacter, false);
        }

        StartCoroutine(FadeOutAndReturn());
    }

    IEnumerator FadeOutAndReturn()
    {
        Debug.Log("🎬 Fade Out - Returning to Door Room");
        yield return StartCoroutine(FadeToAlpha(1f));
        yield return new WaitForSeconds(0.3f);
        
        currentCharacter = null;
        
        Debug.Log("✅ Returned to Door Room (waiting for DoorSystemManager to Fade In)");
    }
    
    #endregion

    // ========================================================================
    //                            OBJECT SPAWNING
    // ========================================================================
    #region Object Spawning
    
    private void SpawnDecisionObject(ObjectModification modification)
    {
        if (modification == null || modification.imageToAdd == null) 
            return;

        if (currentSpawnedObject != null)
        {
            Destroy(currentSpawnedObject);
        }

        Transform parent = objectContainer != null ? objectContainer : backgroundDisplay.transform;
        
        currentSpawnedObject = Instantiate(modification.imageToAdd, parent);

        RectTransform rt = currentSpawnedObject.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = modification.positionOnBackground;
            rt.localScale = Vector3.one * modification.scale;
        }

        Canvas canvas = currentSpawnedObject.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = modification.sortingOrder;
        }
        else
        {
            canvas = currentSpawnedObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = modification.sortingOrder;
        }
        
        Debug.Log($"🎨 Spawned Object: {modification.imageToAdd.name}");
    }

    private void ClearSpawnedObjects()
    {
        if (currentSpawnedObject != null)
        {
            Destroy(currentSpawnedObject);
            currentSpawnedObject = null;
        }
    }
    
    #endregion

    // ========================================================================
    //                                HELPERS
    // ========================================================================
    #region Helpers & UI
    
    private void InitializeAudio()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.enabled)
        {
            backgroundMusicSource.volume = musicVolume * masterVolume;
            if (!backgroundMusicSource.isPlaying) 
                backgroundMusicSource.Play();
        }
        
        if (typewriterSoundSource != null) 
            typewriterSoundSource.volume = sfxVolume * masterVolume;
        
        if (buttonClickSoundSource != null) 
            buttonClickSoundSource.volume = sfxVolume * masterVolume;
        
        if (suddenSoundSource != null) 
            suddenSoundSource.volume = sfxVolume * masterVolume;
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
        if (nextButton != null) 
        { 
            nextButton.onClick.RemoveAllListeners(); 
            nextButton.onClick.AddListener(OnNextPhotoClicked); 
        }
        
        if (prevButton != null) 
        { 
            prevButton.onClick.RemoveAllListeners(); 
            prevButton.onClick.AddListener(OnPrevPhotoClicked); 
        }
        
        if (spareButton != null) 
        { 
            spareButton.onClick.RemoveAllListeners(); 
            spareButton.onClick.AddListener(OnSpareButtonClicked); 
        }
        
        if (killButton != null) 
        { 
            killButton.onClick.RemoveAllListeners(); 
            killButton.onClick.AddListener(OnKillButtonClicked); 
        }
        
        if (skipButton != null) 
        { 
            skipButton.onClick.RemoveAllListeners(); 
            skipButton.onClick.AddListener(OnSkipClicked); 
        }
    }

    private void SetButtonsActive(bool active)
    {
        if (nextButton != null) 
            nextButton.gameObject.SetActive(active);
        
        if (prevButton != null) 
            prevButton.gameObject.SetActive(active);

        // Hide skipButton if in Decision state, regardless of 'active' param
        if (skipButton != null)
        {
            if (currentState == GameState.Decision)
                skipButton.gameObject.SetActive(false);
            else
                skipButton.gameObject.SetActive(true);
        }

        if (spareButton != null) 
            spareButton.gameObject.SetActive(false);
        
        if (killButton != null) 
            killButton.gameObject.SetActive(false);
    }

    private void UpdateButtonStates(CharacterData data)
    {
        bool interactable = (data.storyImages != null && data.storyImages.Length > 0);
        
        if (nextButton != null) 
            nextButton.interactable = interactable;
        
        if (prevButton != null) 
            prevButton.interactable = interactable;
    }

    public void NextPhoto()
    {
        if (currentState != GameState.Photos || currentCharacter == null) 
            return;
        
        currentPhotoIndex = (currentPhotoIndex + 1) % currentCharacter.storyImages.Length;
        photoDisplay.sprite = currentCharacter.storyImages[currentPhotoIndex];
        
        Debug.Log($"📸 Next Photo: {currentPhotoIndex + 1}/{currentCharacter.storyImages.Length}");
    }

    public void PrevPhoto()
    {
        if (currentState != GameState.Photos || currentCharacter == null) 
            return;
        
        currentPhotoIndex = (currentPhotoIndex - 1 + currentCharacter.storyImages.Length) % currentCharacter.storyImages.Length;
        photoDisplay.sprite = currentCharacter.storyImages[currentPhotoIndex];
        
        Debug.Log($"📸 Previous Photo: {currentPhotoIndex + 1}/{currentCharacter.storyImages.Length}");
    }

    private void UpdateCounterDisplay()
    {
        if (counterText != null) 
            counterText.text = $"Team : {visibleTeamCount}";
    }

    #endregion

    // ========================================================================
    //                            FADE SYSTEM - CanvasGroup
    // ========================================================================
    #region Fade System
    
    IEnumerator FadeToAlpha(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("❌ CanvasGroup is NULL!");
            yield break;
        }

        string direction = targetAlpha == 1f ? "OUT (to BLACK)" : "IN (to CLEAR)";
        Debug.Log($"🎬 FADE {direction} Started");
        Debug.Log($"   Current Alpha: {fadeCanvasGroup.alpha} → Target: {targetAlpha}");

        fadeImage.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable = false;
        
        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = 1f / fadeSpeed;

        Debug.Log($"   Duration: {duration}s");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            
            if (Time.frameCount % 10 == 0)
            {
                Debug.Log($"   Progress: {t:F2} | Alpha: {fadeCanvasGroup.alpha:F3}");
            }
            
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = false;
        
        Debug.Log($"✅ FADE {direction} Complete | Final Alpha: {fadeCanvasGroup.alpha}");

        if (targetAlpha == 0f)
        {
            fadeImage.gameObject.SetActive(false);
            Debug.Log("   FadeImage GameObject disabled");
        }
    }

    #endregion

    // ========================================================================
    //                            END GAME
    // ========================================================================
    #region End Game
    
    public void TransitionToEndScene()
    {
        Debug.Log("🏁 Transitioning to End Scene");
        Debug.Log($"   Humans Spared: {hiddenHumanCount}");
        Debug.Log($"   AI Spared: {visibleTeamCount - hiddenHumanCount}");
        
        PlayerPrefs.SetInt("FinalHumansSpared", hiddenHumanCount);
        PlayerPrefs.SetInt("FinalAiSpared", visibleTeamCount - hiddenHumanCount);
        PlayerPrefs.Save();
        
        StartCoroutine(FadeAndLoadScene(nextSceneName));
    }

    IEnumerator FadeAndLoadScene(string sceneName)
    {
        yield return StartCoroutine(FadeToAlpha(1f));
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log($"🔄 Loading Scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
    
    #endregion
}