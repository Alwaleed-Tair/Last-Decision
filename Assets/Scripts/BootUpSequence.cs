using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class BootUpSequence : MonoBehaviour
{
    public TextMeshProUGUI bootText;
    public AudioSource typeSoundAudio;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float typingVolume = 1f; // التحكم في قوة الصوت
    public Vector2 pitchRange = new Vector2(0.85f, 1.15f); // التحكم في حدة الصوت (الأدنى والأعلى)

    [Header("Typing Speeds")]
    public float loadingLetterSpeed = 0.08f;
    public float normalLetterSpeed = 0.12f;
    public float dotSpeed = 0.35f;

    [Header("Content")]
    [TextArea(10, 20)]
    public string fullBootText;

    public string nextSceneName = "Story";

    void Start()
    {
        bootText.text = "";
        bootText.verticalAlignment = VerticalAlignmentOptions.Top;
        bootText.overflowMode = TextOverflowModes.Overflow;

        StartCoroutine(BootSequence());
    }

    IEnumerator BootSequence()
    {
        // ===== LOADING =====
        yield return StartCoroutine(TypeWord("LOADING", loadingLetterSpeed));

        // ===== نقاط التحميل (2-3 مرات) =====
        int loops = Random.Range(2, 4);
        for (int i = 0; i < loops; i++)
        {
            yield return StartCoroutine(LoadingDots());
        }

        // مسح سطر التحميل
        yield return new WaitForSeconds(0.4f);
        bootText.text = "";

        // ===== بقية النص =====
        string[] lines = fullBootText.Replace("\r", "").Split('\n');
        foreach (string line in lines)
        {
            yield return StartCoroutine(TypeLine(line));
            yield return new WaitForSeconds(0.8f);
            bootText.text = "";
            yield return new WaitForSeconds(0.25f);
        }

        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator TypeWord(string word, float speed)
    {
        foreach (char c in word)
        {
            bootText.text += c;
            PlayTypeSound();
            yield return new WaitForSeconds(speed);
        }
    }

    IEnumerator LoadingDots()
    {
        for (int i = 1; i <= 3; i++)
        {
            bootText.text = "LOADING" + new string('.', i);
            PlayTypeSound();
            yield return new WaitForSeconds(dotSpeed);
        }
    }

    IEnumerator TypeLine(string line)
    {
        foreach (char c in line)
        {
            bootText.text += c;

            if (!char.IsWhiteSpace(c))
                PlayTypeSound();

            yield return new WaitForSeconds(normalLetterSpeed);
        }
    }

    void PlayTypeSound()
    {
        if (typeSoundAudio == null || typeSoundAudio.clip == null) return;

        // تعديل حدة الصوت بناءً على النطاق المحدد في يونتي
        typeSoundAudio.pitch = Random.Range(pitchRange.x, pitchRange.y);
        
        // تشغيل الصوت مع التحكم في قوته
        typeSoundAudio.PlayOneShot(typeSoundAudio.clip, typingVolume);
    }
}