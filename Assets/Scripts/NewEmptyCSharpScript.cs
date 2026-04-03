using UnityEngine;
using UnityEngine.UI;

public class StickerManager : MonoBehaviour
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

    void Start()
    {
        // Check every door when the scene loads
        UpdateDoor("Ideal", idealSticker);
        UpdateDoor("Control", controlSticker);
        UpdateDoor("Pride", prideSticker);
        UpdateDoor("Fear", fearSticker);
        UpdateDoor("Anger", angerSticker);
        UpdateDoor("Joy", joySticker);
    }

    void UpdateDoor(string doorKey, UnityEngine.UI.Image stickerImage)
    {
        if (stickerImage == null) return;

        // Get the decision: 1 for Spare, 0 for Kill, -1 for Not Done Yet
        int decision = PlayerPrefs.GetInt("Decision_" + doorKey, -1);

        if (decision == -1)
        {
            stickerImage.gameObject.SetActive(false); // Hide if not played
        }
        else
        {
            stickerImage.gameObject.SetActive(true); // Show if played
            stickerImage.sprite = (decision == 1) ? spareSprite : killSprite;
        }
    }
}