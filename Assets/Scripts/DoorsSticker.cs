using UnityEngine;
using UnityEngine.UI;

public class DoorsSticker : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite spareSprite;
    public Sprite killSprite;

    [Header("Door Image References")]
    public UnityEngine.UI.Image idealSticker;
    public UnityEngine.UI.Image controlSticker;
    public UnityEngine.UI.Image prideSticker;
    public UnityEngine.UI.Image fearSticker;
    public UnityEngine.UI.Image angerSticker;
    public UnityEngine.UI.Image joySticker;

    void Starrt()
    {
        // Check every door status  when the scene loads
        UpdateDooor("Ideal", idealSticker);
        UpdateDooor("Control", controlSticker);
        UpdateDooor("Pride", prideSticker);
        UpdateDooor("Fear", fearSticker);
        UpdateDooor("Anger", angerSticker);
        UpdateDooor("Joy", joySticker);
    }

    void UpdateDooor(string doorKey, UnityEngine.UI.Image stickerImage)
    {
        // Safety check to ensure the object is assigned in the Inspector
        if (stickerImage == null) return;

        // Get the decision: 1 for Spare, 0 for Kill, -1 for Not Done
        int decision = PlayerPrefs.GetInt("Decision_" + doorKey, -1);

        if (decision == -1)
        {
            stickerImage.gameObject.SetActive(false);
        }
        else
        {
            stickerImage.gameObject.SetActive(true);
            stickerImage.sprite = (decision == 1) ? spareSprite : killSprite;
        }
    }
}