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

    [Header("Door Button References")]
    public Button idealBtn;
    public Button controlBtn;
    public Button prideBtn;
    public Button fearBtn;
    public Button angerBtn;
    public Button joyBtn;

    [Header("Middle Door Visuals")]
    public GameObject closedDoorImage; // The big image blocking the way
    public Button middleDoorButton;   // The actual door button behind the image

    void Start()
    {
        // On Start, we just update the stickers and lock the buttons
        UpdateAllStickers();
    }

    void Update()
    {
        // Keyboard Shortcut Reset (Left Shift + R)
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R))
        {
            ResetGameProgress();
        }
    }

    // ⭐ THIS IS THE FUNCTION FOR YOUR BIG CLOSED DOOR BUTTON
    public void OpenMasterDoor()
    {
        int totalCompleted = UpdateAllStickers();

        // Check if the player has finished all 6 doors
        if (totalCompleted >= 5)
        {
            // Success: Hide the blocking image and enable the final door
            if (closedDoorImage != null) closedDoorImage.SetActive(false);
            if (middleDoorButton != null) middleDoorButton.interactable = true;

            UnityEngine.Debug.Log("Master Door Unlocked! The path is clear.");
        }
        else
        {
            // Fail: The door stays closed
            UnityEngine.Debug.Log("The door is sealed. You have only finished " + totalCompleted + "/6 doors.");
        }
    }

    private int UpdateAllStickers()
    {
        int count = 0;
        // Names MUST match the 'characterName' in your Door.cs scripts exactly
        count += UpdateDoorState("Ideal", idealSticker, idealBtn);
        count += UpdateDoorState("Control", controlSticker, controlBtn);
        count += UpdateDoorState("Pride", prideSticker, prideBtn);
        count += UpdateDoorState("Fear", fearSticker, fearBtn);
        count += UpdateDoorState("Anger", angerSticker, angerBtn);
        count += UpdateDoorState("Joy", joySticker, joyBtn);
        return count;
    }

    int UpdateDoorState(string doorKey, UnityEngine.UI.Image stickerImage, Button doorBtn)
    {
        int decision = PlayerPrefs.GetInt("Decision_" + doorKey, -1);

        if (decision == -1)
        {
            if (stickerImage != null) stickerImage.gameObject.SetActive(false);
            if (doorBtn != null) doorBtn.interactable = true;
            return 0;
        }
        else
        {
            if (stickerImage != null)
            {
                stickerImage.gameObject.SetActive(true);
                stickerImage.sprite = (decision == 1) ? spareSprite : killSprite;
            }
            if (doorBtn != null) doorBtn.interactable = false;
            return 1;
        }
    }

    public void ResetGameProgress()
    {
        PlayerPrefs.DeleteKey("Decision_Ideal");
        PlayerPrefs.DeleteKey("Decision_Control");
        PlayerPrefs.DeleteKey("Decision_Pride");
        PlayerPrefs.DeleteKey("Decision_Fear");
        PlayerPrefs.DeleteKey("Decision_Anger");
        PlayerPrefs.DeleteKey("Decision_Joy");
        PlayerPrefs.DeleteKey("FinalHumansSpared");
        PlayerPrefs.DeleteKey("FinalAiSpared");
        PlayerPrefs.Save();

        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}