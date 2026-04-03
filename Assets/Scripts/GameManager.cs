using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    // ========================================================================
    //                                 DEFINITIONS & VARIABLES
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
    public Button nextButton;          // للصور فقط (Stage 2)
    public Button prevButton;          // للصور فقط (Stage 2)
    public Button nextSentenceButton;  // للنصوص (Stage 1 و 2)
    public Button skipButton;          // تخطي المشهد (Stage 1 و 2)


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


    [Header("Audio Objects")]
    public GameObject backgroundMusicObject;
    public GameObject typewriterSoundObject;
    public GameObject buttonClickSoundObject;
    public GameObject suddenSoundObject;
    public GameObject doorOpenSoundObject;
    public GameObject killDelaySoundObject;
    public GameObject bigDoorOpenSoundObject; // جديد: صوت الباب الكبير


    [Header("Audio Volume Settings")]
    public float masterVolume = 1f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 0.8f;
    public float typewriterVolume = 1f;


    [Header("Scene Settings")]
    public string nextSceneName = "EndScene";
    
    [Header("Big Door Settings")]
    public int bigDoorID = 7; // رقم الباب الكبير (غيّره حسب الباب عندك)
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


    // Typewriter & Button Flags
    private bool isTyping = false;
    private bool cancelTyping = false;      // NextSentence يحطها true حتى الـ Typewriter يكمل النص فوراً
    private bool waitForNextClick = false;   // بعد انتهاء النص الـ Coroutine ينتظر هذي حتى يضغط NextSentence
    private bool isFadingOut = false;        // وقت الـ StartFinalFade شغال ما نسمح بأي ضغطة


    private bool suddenSoundPlayed = false;
    private bool doorSoundPlayedForCurrentCharacter = false; // لتتبع صوت الباب العادي
    private bool bigDoorSoundPlayed = false; // جديد: لتتبع صوت الباب الكبير (يشتغل مرة وحدة في كل اللعبة)
    #endregion


    // ========================================================================
    //                                 UNITY LIFECYCLE
    // ========================================================================
    #region Unity Lifecycle
    private void Start()
    {
        InitializeAudio();
        SetupButtons();


        if (decisionPanel != null) decisionPanel.SetActive(false);


        UpdateCounterDisplay();


        string selectedName = PlayerPrefs.GetString("SelectedCharacter", "");


        for (int i = 0; i < allCharacters.Length; i++)
        {
            if (allCharacters[i].characterName == selectedName)
            {
                currentCharacterIndex = i;
                break;
            }
        }


        SetState(GameState.Dialogue);
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
    //                                 STATE MACHINE
    // ========================================================================
    #region State Management
    public void SetState(GameState newState)
    {
        currentState = newState;
        StopAllCoroutines();
        isPhotoTimerActive = false;
        isTyping = false;
        cancelTyping = false;
        waitForNextClick = false;
        isFadingOut = false;


        // إخفاء كل الزرين أولاً
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (prevButton != null) prevButton.gameObject.SetActive(false);
        if (nextSentenceButton != null) nextSentenceButton.gameObject.SetActive(false);
        if (skipButton != null) skipButton.gameObject.SetActive(false);
        if (spareButton != null) spareButton.gameObject.SetActive(false);
        if (killButton != null) killButton.gameObject.SetActive(false);
        if (decisionPanel != null) decisionPanel.SetActive(false);


        // ريسيت صوت الباب لما ننتقل لشخصية جديدة
        if (newState == GameState.Dialogue)
        {
            doorSoundPlayedForCurrentCharacter = false;
        }


        switch (currentState)
        {
            case GameState.Dialogue:
                // Stage 1: NextSentence + Skip فقط
                if (nextSentenceButton != null) nextSentenceButton.gameObject.SetActive(true);
                if (skipButton != null) skipButton.gameObject.SetActive(true);
                if (textBar != null) textBar.SetActive(true);
                StartCoroutine(HandleDialogueState());
                break;


            case GameState.Photos:
                // Stage 2: Next + Prev + NextSentence + Skip
                if (nextButton != null) nextButton.gameObject.SetActive(true);
                if (prevButton != null) prevButton.gameObject.SetActive(true);
                if (nextSentenceButton != null) nextSentenceButton.gameObject.SetActive(true);
                if (skipButton != null) skipButton.gameObject.SetActive(true);
                if (textBar != null) textBar.SetActive(true);
                suddenSoundPlayed = false;
                StartCoroutine(HandlePhotosState());
                break;


            case GameState.Decision:
                // Stage 3: Spare + Kill فقط بعد الـ Fade، باقي الزرين مخفية
                if (textBar != null) textBar.SetActive(false);
                StartCoroutine(HandleDecisionState());
                break;
        }
    }
    #endregion


    // ========================================================================
    //                                 COROUTINES
    // ========================================================================
    #region Coroutines
    IEnumerator HandleDialogueState()
    {
        if (allCharacters == null || allCharacters.Length == 0) yield break;
        if (currentCharacterIndex >= allCharacters.Length) yield break;


        CharacterData data = allCharacters[currentCharacterIndex];
        imageFrame.SetActive(false);
        if (data.backgroundImage != null) backgroundDisplay.sprite = data.backgroundImage;


        // تحقق: هل هذا الباب الكبير؟
        bool isBigDoor = (data.doorID == bigDoorID);
        
        // صوت فتح الباب يشتغل مرة وحدة فقط لكل شخصية
        if (!doorSoundPlayedForCurrentCharacter)
        {
            if (isBigDoor && !bigDoorSoundPlayed)
            {
                // الباب الكبير وأول مرة يفتح
                PlayBigDoorOpenSound();
                bigDoorSoundPlayed = true;
            }
            else if (!isBigDoor)
            {
                // باب عادي
                PlayDoorOpenSound();
            }
            // إذا كان الباب الكبير لكن bigDoorSoundPlayed = true، ما نشغل الصوت
            
            doorSoundPlayedForCurrentCharacter = true;
        }


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
        yield return StartCoroutine(TypewriterByLine(data.dialogueText));


        // النص كامل انتهى، ننتظر ضغطة NextSentence للـ انتقال للـ Photos
        waitForNextClick = true;
        while (waitForNextClick)
        {
            yield return null;
        }


        // اللاعب ضغط NextSentence → ننتقل للـ Photos
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


        yield return new WaitForSeconds(1f);


        yield return StartCoroutine(TypewriterByLine(data.stage2DialogueText));


        // النص كامل انتهى، ننتظر ضغطة NextSentence قبل التوقيت يبدأ
        waitForNextClick = true;
        while (waitForNextClick)
        {
            yield return null;
        }


        if (!suddenSoundPlayed && UnityEngine.Random.value > 0.5f)
        {
            PlaySuddenSound();
            suddenSoundPlayed = true;
        }


        // بدأ التوقيت
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


        // نظهر Spare و Kill بعد ما كل شي جاهز
        if (decisionPanel != null) decisionPanel.SetActive(true);
        if (spareButton != null) spareButton.gameObject.SetActive(true);
        if (killButton != null) killButton.gameObject.SetActive(true);


        yield return StartCoroutine(FadeEffect(0f));
    }


    // الـ Coroutine حق Skip — يسوي Fade للأسود ثم ينتقل للـ State التالي
    IEnumerator StartFinalFade()
    {
        isFadingOut = true;
        yield return StartCoroutine(FadeEffect(1f));
        isFadingOut = false;


        // بعد الـ Fade ننتقل للـ State التالي
        if (currentState == GameState.Dialogue)
            SetState(GameState.Photos);
        else if (currentState == GameState.Photos)
            SetState(GameState.Decision);
    }


    IEnumerator FadeAndChangeCharacter()
    {
        yield return StartCoroutine(FadeEffect(1f));
        yield return new WaitForSeconds(0.3f);
        SetState(GameState.Dialogue);
    }


    #endregion


    // ========================================================================
    //                                 INPUT HANDLING
    // ========================================================================
    #region Input Handling
    // كل سطر يكتب حرف حرف، بعدين ينتظر ضغطة NextSentence، بعدين يحذف ويكتب السطر التالي
    IEnumerator TypewriterByLine(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;


        // نفصل النص على السطور - نستخدم \n العادي أو الـ line breaks الحقيقية
        string[] lines = text.Split(new string[] { "\\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);


        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim(); // نحذف المسافات الزايدة
            if (string.IsNullOrEmpty(line)) continue; // نتخطى السطور الفاضية
            
            dialogueText.text = "";
            isTyping = true;
            cancelTyping = false;


            // نكتب السطر حرف حرف
            foreach (char c in line)
            {
                if (cancelTyping)
                {
                    dialogueText.text = line; // نكمل السطر فوراً
                    break;
                }


                dialogueText.text += c;
                PlayTypewriterSound();
                yield return new WaitForSeconds(typewriterSpeed);
            }


            isTyping = false;
            cancelTyping = false;


            // بعد كل سطر (حتى الأخير) ننتظر ضغطة NextSentence
            waitForNextClick = true;
            while (waitForNextClick)
            {
                yield return null;
            }
            
            // بعد الضغط، نحذف النص ونكتب السطر التالي
            dialogueText.text = "";
        }
    }


    // زر NextSentence
    private void OnNextSentenceClicked()
    {
        // إذا كان الـ Fade شغال (بعد ضغط Skip) لا نسوي شي
        if (isFadingOut) return;


        if (isTyping)
        {
            // النص يكتب → نوقفه ونكمل النص فوراً
            cancelTyping = true;
        }
        else if (waitForNextClick)
        {
            // النص انتهى والـ Coroutine ينتظر → نعدي للـ State التالي
            waitForNextClick = false;
            PlayButtonClickSound();
        }
    }


    // زر Skip — يسوي Fade ثم ينتقل للـ State التالي
    private void OnSkipClicked()
    {
        // إذا كان الـ Fade شغال لا نسوي شي
        if (isFadingOut) return;


        PlayButtonClickSound();
        // نشغل الـ StartFinalFade الذي يسوي Fade ثم ينتقل
        StartCoroutine(StartFinalFade());
    }


    private void OnNextPhotoClicked() { PlayButtonClickSound(); NextPhoto(); }
    private void OnPrevPhotoClicked() { PlayButtonClickSound(); PrevPhoto(); }
    private void OnSpareButtonClicked() { PlayButtonClickSound(); OnSparePressed(); }
    private void OnKillButtonClicked() { PlayButtonClickSound(); OnKillPressed(); }


  public void OnSparePressed()
{
    if (currentState != GameState.Decision) return;
    CharacterData data = allCharacters[currentCharacterIndex];

    // SAVE: Use the new characterID for specific ending logic
    // This creates keys like "Decision_Nurse" or "Decision_DJ"
    PlayerPrefs.SetInt("Decision_" + data.characterID, 1); 

    // Keep your existing sticker and count logic
    PlayerPrefs.SetInt("Decision_" + data.characterName, 1);
    
    if (data.type == CharacterData.CharacterType.Human)
    {
        int hCount = PlayerPrefs.GetInt("FinalHumansSpared", 0);
        PlayerPrefs.SetInt("FinalHumansSpared", hCount + 1);
    }
    else 
    {
        int aCount = PlayerPrefs.GetInt("FinalAiSpared", 0);
        PlayerPrefs.SetInt("FinalAiSpared", aCount + 1);
    }

    PlayerPrefs.Save();
    UpdateCounterDisplay();
    StartCoroutine(FadeAndReturnToHub());
}
    public void OnKillPressed()
    {
        if (currentState != GameState.Decision) return;
        CharacterData data = allCharacters[currentCharacterIndex];


        PlayerPrefs.SetInt("Decision_" + data.characterName, 0);
        PlayerPrefs.Save();


        // الصوت يشتغل لحاله (بدون coroutine منفصلة)
        StartCoroutine(PlayKillSoundDelayed());
        
        // الـ Fade يبدأ مباشرة
        StartCoroutine(FadeAndReturnToHub());
    }


    // Coroutine للصوت بس (منفصل تماماً)
    IEnumerator PlayKillSoundDelayed()
    {
        yield return new WaitForSeconds(1f);
        
        if (killDelaySoundObject != null)
        {
            AudioSource audio = killDelaySoundObject.GetComponent<AudioSource>();
            if (audio != null && audio.clip != null)
            {
                audio.PlayOneShot(audio.clip);
            }
        }
    }


    // الـ Fade والانتقال (نفس الشي للـ Spare والـ Kill)
    IEnumerator FadeAndReturnToHub()
    {
        yield return StartCoroutine(FadeEffect(1f));
        yield return new WaitForSeconds(1.5f); // وقت إضافي قبل الانتقال
        SceneManager.LoadScene("MainHub");
    }


    #endregion


    // ========================================================================
    //                                 HELPERS
    // ========================================================================
    #region Helpers & UI
    private void InitializeAudio()
    {
        if (backgroundMusicObject != null)
        {
            AudioSource bg = backgroundMusicObject.GetComponent<AudioSource>();
            if (bg != null)
            {
                bg.volume = musicVolume * masterVolume;
                if (!bg.isPlaying) bg.Play();
            }
        }
    }


    private void PlayTypewriterSound()
    {
        if (typewriterSoundObject != null)
        {
            AudioSource audio = typewriterSoundObject.GetComponent<AudioSource>();
            if (audio != null && audio.clip != null)
                audio.PlayOneShot(audio.clip, typewriterVolume);
        }
    }


    private void PlayButtonClickSound()
    {
        if (buttonClickSoundObject != null)
        {
            AudioSource audio = buttonClickSoundObject.GetComponent<AudioSource>();
            if (audio != null && audio.clip != null)
                audio.PlayOneShot(audio.clip);
        }
    }


    private void PlaySuddenSound()
    {
        if (suddenSoundObject != null)
        {
            AudioSource audio = suddenSoundObject.GetComponent<AudioSource>();
            if (audio != null && audio.clip != null)
                audio.PlayOneShot(audio.clip);
        }
    }


    private void PlayDoorOpenSound()
    {
        if (doorOpenSoundObject != null)
        {
            AudioSource audio = doorOpenSoundObject.GetComponent<AudioSource>();
            if (audio != null && audio.clip != null)
                audio.PlayOneShot(audio.clip);
        }
    }

    // جديد: function لصوت الباب الكبير
    private void PlayBigDoorOpenSound()
    {
        if (bigDoorOpenSoundObject != null)
        {
            AudioSource audio = bigDoorOpenSoundObject.GetComponent<AudioSource>();
            if (audio != null && audio.clip != null)
                audio.PlayOneShot(audio.clip);
        }
    }


    private void SetupButtons()
    {
        if (nextButton != null) { nextButton.onClick.RemoveAllListeners(); nextButton.onClick.AddListener(OnNextPhotoClicked); }
        if (prevButton != null) { prevButton.onClick.RemoveAllListeners(); prevButton.onClick.AddListener(OnPrevPhotoClicked); }
        if (nextSentenceButton != null) { nextSentenceButton.onClick.RemoveAllListeners(); nextSentenceButton.onClick.AddListener(OnNextSentenceClicked); }
        if (skipButton != null) { skipButton.onClick.RemoveAllListeners(); skipButton.onClick.AddListener(OnSkipClicked); }
        if (spareButton != null) { spareButton.onClick.RemoveAllListeners(); spareButton.onClick.AddListener(OnSpareButtonClicked); }
        if (killButton != null) { killButton.onClick.RemoveAllListeners(); killButton.onClick.AddListener(OnKillButtonClicked); }
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
        if (counterText != null)
        {
            int totalHumans = PlayerPrefs.GetInt("FinalHumansSpared", 0);
            int totalAI = PlayerPrefs.GetInt("FinalAiSpared", 0);
            int combinedTotal = totalHumans + totalAI;
            counterText.text = $"Team: {combinedTotal}";
        }
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


    void TransitionToEndScene()
    {
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
