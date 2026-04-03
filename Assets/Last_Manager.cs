using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// كومبوننت للكانفاس يتحكم في شاشة Last Decision
/// يحتوي على fade in، موسيقى خلفية، وأزرار للتحكم
/// </summary>
public class LastDecisionCanvas : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("الكانفاس جروب المستخدم للـ Fade In")]
    public CanvasGroup canvasGroup;
    
    [Tooltip("صورة الـ Fade (اختياري - لعمل fade على صورة معينة مثل شاشة سوداء)")]
    public Image fadeImage;
    
    [Tooltip("مدة الـ Fade In بالثواني")]
    [Range(0.1f, 5f)]
    public float fadeDuration = 1.5f;
    
    [Tooltip("هل يبدأ الـ Fade In تلقائياً عند بداية المشهد؟")]
    public bool autoFadeOnStart = true;
    
    [Tooltip("نوع الـ Fade: Canvas Group أو Fade Image أو كلاهما")]
    public FadeType fadeType = FadeType.Both;

    [Header("Background Music")]
    [Tooltip("مصدر الصوت للموسيقى الخلفية")]
    public AudioSource backgroundMusicSource;
    
    [Tooltip("الموسيقى الخلفية (اختياري - يمكن تركه فارغ إذا كان موجود في AudioSource)")]
    public AudioClip backgroundMusicClip;
    
    [Tooltip("هل يتم تشغيل الموسيقى تلقائياً عند بداية المشهد؟")]
    public bool autoPlayMusicOnStart = true;
    
    [Tooltip("مستوى صوت الموسيقى")]
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;

    [Header("Button Click Sound")]
    [Tooltip("مصدر الصوت لأصوات النقر على الأزرار")]
    public AudioSource buttonClickSource;
    
    [Tooltip("صوت النقر على الأزرار")]
    public AudioClip buttonClickClip;
    
    [Tooltip("مستوى صوت النقر")]
    [Range(0f, 1f)]
    public float clickVolume = 1f;

    [Header("Buttons")]
    [Tooltip("زر الرجوع للقائمة الرئيسية")]
    public Button mainMenuButton;
    
    [Tooltip("زر إغلاق اللعبة")]
    public Button exitButton;

    [Header("Scene Management")]
    [Tooltip("اسم مشهد القائمة الرئيسية")]
    public string mainMenuSceneName = "MainMenu";
    
    [Tooltip("هل يتم استخدام Fade Out قبل الانتقال للقائمة الرئيسية؟")]
    public bool fadeOutBeforeMainMenu = true;
    
    [Tooltip("مدة الـ Fade Out بالثواني")]
    [Range(0.1f, 3f)]
    public float fadeOutDuration = 1f;

    [Header("Exit Confirmation")]
    [Tooltip("هل يتم عرض تأكيد قبل إغلاق اللعبة؟")]
    public bool confirmBeforeExit = false;

    private bool isFading = false;

    public enum FadeType
    {
        CanvasGroupOnly,    // فقط CanvasGroup
        FadeImageOnly,      // فقط Fade Image
        Both                // كلاهما
    }

    void Start()
    {
        // التأكد من وجود CanvasGroup إذا كان مطلوب
        if ((fadeType == FadeType.CanvasGroupOnly || fadeType == FadeType.Both) && canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // ربط الأزرار بالوظائف
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }
        else
        {
            Debug.LogWarning("Main Menu Button غير مربوط في الـ Inspector!");
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
        else
        {
            Debug.LogWarning("Exit Button غير مربوط في الـ Inspector!");
        }

        // إعداد الموسيقى الخلفية
        if (backgroundMusicSource != null)
        {
            if (backgroundMusicClip != null)
            {
                backgroundMusicSource.clip = backgroundMusicClip;
            }
            backgroundMusicSource.volume = musicVolume;
            backgroundMusicSource.loop = true;

            if (autoPlayMusicOnStart)
            {
                backgroundMusicSource.Play();
            }
        }

        // إعداد مصدر صوت النقر
        if (buttonClickSource != null)
        {
            buttonClickSource.volume = clickVolume;
            buttonClickSource.loop = false;
        }

        // بدء الـ Fade In
        if (autoFadeOnStart)
        {
            StartCoroutine(FadeIn());
        }
    }

    /// <summary>
    /// كوروتين لعمل Fade In للكانفاس
    /// </summary>
    IEnumerator FadeIn()
    {
        isFading = true;
        float elapsedTime = 0f;

        // إعداد القيم الأولية
        if ((fadeType == FadeType.CanvasGroupOnly || fadeType == FadeType.Both) && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if ((fadeType == FadeType.FadeImageOnly || fadeType == FadeType.Both) && fadeImage != null)
        {
            Color imageColor = fadeImage.color;
            imageColor.a = 1f; // نبدأ بالصورة مرئية بالكامل
            fadeImage.color = imageColor;
        }

        // تنفيذ الـ Fade
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / fadeDuration;

            // Fade In للـ Canvas Group (من 0 إلى 1)
            if ((fadeType == FadeType.CanvasGroupOnly || fadeType == FadeType.Both) && canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(alpha);
            }

            // Fade Out للـ Fade Image (من 1 إلى 0)
            if ((fadeType == FadeType.FadeImageOnly || fadeType == FadeType.Both) && fadeImage != null)
            {
                Color imageColor = fadeImage.color;
                imageColor.a = 1f - Mathf.Clamp01(alpha);
                fadeImage.color = imageColor;
            }

            yield return null;
        }

        // التأكد من القيم النهائية
        if ((fadeType == FadeType.CanvasGroupOnly || fadeType == FadeType.Both) && canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        if ((fadeType == FadeType.FadeImageOnly || fadeType == FadeType.Both) && fadeImage != null)
        {
            Color imageColor = fadeImage.color;
            imageColor.a = 0f;
            fadeImage.color = imageColor;
            fadeImage.gameObject.SetActive(false); // إخفاء الصورة بعد الـ Fade
        }

        isFading = false;
    }

    /// <summary>
    /// كوروتين لعمل Fade Out للكانفاس
    /// </summary>
    IEnumerator FadeOut()
    {
        isFading = true;
        float elapsedTime = 0f;

        // تفعيل الـ Fade Image إذا كانت موجودة
        if ((fadeType == FadeType.FadeImageOnly || fadeType == FadeType.Both) && fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
        }

        // تنفيذ الـ Fade
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / fadeOutDuration;

            // Fade Out للـ Canvas Group (من 1 إلى 0)
            if ((fadeType == FadeType.CanvasGroupOnly || fadeType == FadeType.Both) && canvasGroup != null)
            {
                canvasGroup.alpha = 1f - Mathf.Clamp01(alpha);
            }

            // Fade In للـ Fade Image (من 0 إلى 1)
            if ((fadeType == FadeType.FadeImageOnly || fadeType == FadeType.Both) && fadeImage != null)
            {
                Color imageColor = fadeImage.color;
                imageColor.a = Mathf.Clamp01(alpha);
                fadeImage.color = imageColor;
            }

            yield return null;
        }

        // التأكد من القيم النهائية
        if ((fadeType == FadeType.CanvasGroupOnly || fadeType == FadeType.Both) && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if ((fadeType == FadeType.FadeImageOnly || fadeType == FadeType.Both) && fadeImage != null)
        {
            Color imageColor = fadeImage.color;
            imageColor.a = 1f;
            fadeImage.color = imageColor;
        }

        isFading = false;
    }

    /// <summary>
    /// دالة تشغيل صوت النقر
    /// </summary>
    void PlayClickSound()
    {
        if (buttonClickSource != null && buttonClickClip != null)
        {
            buttonClickSource.PlayOneShot(buttonClickClip, clickVolume);
        }
    }

    /// <summary>
    /// دالة تنفذ عند النقر على زر القائمة الرئيسية
    /// </summary>
    public void OnMainMenuButtonClicked()
    {
        if (isFading) return;

        PlayClickSound();

        if (fadeOutBeforeMainMenu)
        {
            StartCoroutine(FadeOutAndLoadMainMenu());
        }
        else
        {
            LoadMainMenu();
        }
    }

    /// <summary>
    /// كوروتين لعمل Fade Out ثم تحميل القائمة الرئيسية
    /// </summary>
    IEnumerator FadeOutAndLoadMainMenu()
    {
        yield return StartCoroutine(FadeOut());
        LoadMainMenu();
    }

    /// <summary>
    /// تحميل مشهد القائمة الرئيسية
    /// </summary>
    void LoadMainMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("اسم مشهد القائمة الرئيسية غير محدد!");
        }
    }

    /// <summary>
    /// دالة تنفذ عند النقر على زر الخروج
    /// </summary>
    public void OnExitButtonClicked()
    {
        PlayClickSound();

        if (confirmBeforeExit)
        {
            // يمكن إضافة نافذة تأكيد هنا
            Debug.Log("هل أنت متأكد من الخروج؟");
        }

        ExitGame();
    }

    /// <summary>
    /// إغلاق اللعبة
    /// </summary>
    void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// دالة عامة لبدء الـ Fade In يدوياً
    /// </summary>
    public void TriggerFadeIn()
    {
        if (!isFading)
        {
            StartCoroutine(FadeIn());
        }
    }

    /// <summary>
    /// دالة عامة لبدء الـ Fade Out يدوياً
    /// </summary>
    public void TriggerFadeOut()
    {
        if (!isFading)
        {
            StartCoroutine(FadeOut());
        }
    }

    /// <summary>
    /// تشغيل الموسيقى الخلفية يدوياً
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (backgroundMusicSource != null && !backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Play();
        }
    }

    /// <summary>
    /// إيقاف الموسيقى الخلفية
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Stop();
        }
    }

    /// <summary>
    /// تغيير مستوى صوت الموسيقى
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.volume = musicVolume;
        }
    }
}
