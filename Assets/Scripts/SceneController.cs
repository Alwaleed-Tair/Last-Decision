using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    [Header("Blink Transition Settings")]
    public Image blinkImage; // صورة سوداء للغمضة
    public float blinkSpeed = 0.5f; // سرعة الغمضة
    
    [Header("Button Sound Settings")]
    public AudioSource buttonAudioSource; // مصدر الصوت (اسحب الأوبجكت هنا)

    private void Start()
    {
        // إذا كانت الصورة موجودة، نبدأ بفتح العين
        if (blinkImage != null)
        {
            StartCoroutine(OpenEye());
        }
    }

    // دالة لتشغيل صوت الزر
    private void PlayButtonSound()
    {
        if (buttonAudioSource != null)
        {
            buttonAudioSource.Play();
        }
    }

    // للانتقال لأي مشهد مع تأثير الغمضة
    public void LoadScene(string sceneName)
    {
        PlayButtonSound();
        StartCoroutine(BlinkAndLoadScene(sceneName));
    }

    // للانتقال لأي مشهد باستخدام الرقم
    public void LoadSceneByIndex(int sceneIndex)
    {
        PlayButtonSound();
        StartCoroutine(BlinkAndLoadSceneByIndex(sceneIndex));
    }

    // للرجوع للقائمة الرئيسية
    public void LoadMainMenu()
    {
        PlayButtonSound();
        StartCoroutine(BlinkAndLoadScene("MainMenu"));
    }

    // لإعادة المشهد الحالي
    public void RestartScene()
    {
        PlayButtonSound();
        StartCoroutine(BlinkAndLoadSceneByIndex(SceneManager.GetActiveScene().buildIndex));
    }

    // للخروج من اللعبة
    public void QuitGame()
    {
        PlayButtonSound();
        Application.Quit();
        Debug.Log("Game Quit!");
    }

    // ===== الدالة المطلوبة للـ GameManager =====
    // للغمضة مع تنفيذ كود (بدون الانتقال لمشهد)
    public void BlinkWithCallback(System.Action onBlinkComplete)
    {
        PlayButtonSound();
        StartCoroutine(BlinkCoroutine(onBlinkComplete));
    }

    // Coroutine للغمضة مع callback
    private IEnumerator BlinkCoroutine(System.Action onBlinkComplete)
    {
        if (blinkImage != null)
        {
            // إغلاق العين
            yield return StartCoroutine(CloseEye());
            
            // تنفيذ الكود (مثلاً: تغيير الشخصية)
            onBlinkComplete?.Invoke();
            
            // فتح العين
            yield return StartCoroutine(OpenEye());
        }
        else
        {
            // إذا ما فيه صورة، نستدعي الدالة مباشرة
            onBlinkComplete?.Invoke();
        }
    }

    // نسخة مبسطة للغمضة بدون callback
    public void JustBlink()
    {
        PlayButtonSound();
        StartCoroutine(JustBlinkCoroutine());
    }

    private IEnumerator JustBlinkCoroutine()
    {
        if (blinkImage != null)
        {
            yield return StartCoroutine(CloseEye());
            yield return StartCoroutine(OpenEye());
        }
    }

    // Coroutine للغمضة والانتقال
    private IEnumerator BlinkAndLoadScene(string sceneName)
    {
        if (blinkImage != null)
        {
            // إغلاق العين (fade to black)
            yield return StartCoroutine(CloseEye());
            
            // الانتقال للمشهد
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            // إذا ما فيه صورة، ننتقل مباشرة
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator BlinkAndLoadSceneByIndex(int sceneIndex)
    {
        if (blinkImage != null)
        {
            yield return StartCoroutine(CloseEye());
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            SceneManager.LoadScene(sceneIndex);
        }
    }

    // إغلاق العين (fade to black)
    private IEnumerator CloseEye()
    {
        blinkImage.raycastTarget = true; // منع التفاعل أثناء الغمضة
        
        float elapsedTime = 0f;
        Color color = blinkImage.color;
        while (elapsedTime < blinkSpeed)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsedTime / blinkSpeed);
            blinkImage.color = color;
            yield return null;
        }
        color.a = 1f;
        blinkImage.color = color;
    }

    // فتح العين (fade from black)
    private IEnumerator OpenEye()
    {
        float elapsedTime = 0f;
        Color color = blinkImage.color;
        color.a = 1f;
        blinkImage.color = color;
        yield return new WaitForSeconds(0.1f); // انتظار صغير

        while (elapsedTime < blinkSpeed)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsedTime / blinkSpeed);
            blinkImage.color = color;
            yield return null;
        }
        color.a = 0f;
        blinkImage.color = color;
        
        blinkImage.raycastTarget = false; // تفعيل التفاعل بعد الفتح
    }
}