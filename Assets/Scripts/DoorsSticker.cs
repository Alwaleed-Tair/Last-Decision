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

    void Start()
    {
        // Update every door when the Hub scene loads
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

        // Fetch the decision saved from the Gameplay scene
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