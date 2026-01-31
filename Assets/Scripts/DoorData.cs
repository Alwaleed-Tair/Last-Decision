using UnityEngine;
using UnityEngine.UI;

public class DoorData : MonoBehaviour
{
    [Header("Door Settings")]
    public int doorID;
    public CharacterData linkedCharacter;
    public bool isCompleted = false;

    private Button doorButton;

    private void Start()
    {
        doorButton = GetComponent<Button>();
        if (doorButton != null)
        {
            doorButton.onClick.AddListener(OnDoorClicked);
        }
    }

    private void OnDoorClicked()
    {
        // لا يسمح بالضغط إذا كان مكتمل
        if (isCompleted) return;

        if (DoorSystemManager.Instance != null)
        {
            DoorSystemManager.Instance.EnterDoor(this);
        }
    }

    public void MarkAsCompleted()
    {
        isCompleted = true;
        
        // تعطيل الزر بعد الإكمال
        if (doorButton != null)
        {
            doorButton.interactable = false;
        }
    }
}
