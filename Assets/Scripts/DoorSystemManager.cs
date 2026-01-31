using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DoorSystemManager : MonoBehaviour
{
    public static DoorSystemManager Instance { get; private set; }

    [Header("Door Room")]
    public GameObject doorRoomCanvas;
    public Image doorRoomBackground;
    public List<DoorData> allDoors;

    [Header("Locked Door Overlay")]
public GameObject lockedDoorOverlay;
public GameObject unlockedDoorButton; // ⭐ لازم يكون موجود

    [Header("Background Modifications")]
    public GameObject modificationsParent;
    
    private List<GameObject> addedModifications = new List<GameObject>();
    
    [Header("Game Manager Reference")]
    public GameManager gameManager;

    private DoorData currentSelectedDoor;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ShowDoorRoom();
    }

    public void ShowDoorRoom()
    {
        if (doorRoomCanvas != null)
        {
            doorRoomCanvas.SetActive(true);
        }
        
        if (gameManager != null)
        {
            gameManager.gameObject.SetActive(false);
        }
    }

    public void HideDoorRoom()
    {
        if (doorRoomCanvas != null)
        {
            doorRoomCanvas.SetActive(false);
        }
    }

    public void EnterDoor(DoorData door)
    {
        if (door == null || door.linkedCharacter == null) return;

        currentSelectedDoor = door;
        HideDoorRoom();
        
        if (gameManager != null)
        {
            gameManager.gameObject.SetActive(true);
            gameManager.StartCharacterSequence(door.linkedCharacter);
        }
    }

    public void OnCharacterDecisionMade(CharacterData character, bool wasSpared)
    {
        // إضافة التعديل المناسب على الخلفية
        ObjectModification modification = wasSpared ? 
            character.spareModification : character.killModification;
        
        if (modification != null && modification.imageToAdd != null)
        {
            AddBackgroundModification(modification);
        }

        // تحديث حالة الباب
        if (currentSelectedDoor != null)
        {
            currentSelectedDoor.MarkAsCompleted();
        }

        // العودة إلى غرفة الأبواب
        if (gameManager != null)
        {
            gameManager.gameObject.SetActive(false);
        }
        
        ShowDoorRoom();
        
        // التحقق من إكمال جميع الأبواب
        CheckAllDoorsCompleted();
    }

    private void AddBackgroundModification(ObjectModification modification)
    {
        if (modificationsParent == null || modification.imageToAdd == null) return;

        GameObject newModification = Instantiate(modification.imageToAdd, modificationsParent.transform);
        
        RectTransform rt = newModification.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = modification.positionOnBackground;
            rt.localScale = Vector3.one * modification.scale;
        }
        else
        {
            newModification.transform.localPosition = new Vector3(
                modification.positionOnBackground.x, 
                modification.positionOnBackground.y, 
                0
            );
            newModification.transform.localScale = Vector3.one * modification.scale;
        }

        Canvas canvas = newModification.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = modification.sortingOrder;
        }

        SpriteRenderer spriteRenderer = newModification.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = modification.sortingOrder;
        }
        
        addedModifications.Add(newModification);
    }

    private void CheckAllDoorsCompleted()
    {
        bool allCompleted = true;
        
        foreach (DoorData door in allDoors)
        {
            if (!door.isCompleted)
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted)
        {
            UnlockDoor();
        }
    }

    private void UnlockDoor()
{
    // إخفاء صورة الباب المقفل
    if (lockedDoorOverlay != null)
    {
        lockedDoorOverlay.SetActive(false);
    }

    // إظهار زر الباب المفتوح
    GameObject unlockedButton = GameObject.Find("UnlockedDoorButton");
    if (unlockedButton != null)
    {
        unlockedButton.SetActive(true);
    }
}


    public void OnUnlockedDoorClicked()
    {
        // تستدعى من زر الباب المفتوح
        if (gameManager != null)
        {
            gameManager.TransitionToEndScene();
        }
    }

    public void ResetDoorSystem()
    {
        foreach (DoorData door in allDoors)
        {
            door.isCompleted = false;
        }

        foreach (GameObject mod in addedModifications)
        {
            if (mod != null)
            {
                Destroy(mod);
            }
        }
        addedModifications.Clear();

        if (lockedDoorOverlay != null)
        {
            lockedDoorOverlay.SetActive(true);
        }
    }
}
