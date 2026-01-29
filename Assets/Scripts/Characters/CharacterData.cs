using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Story/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName;
    public enum CharacterType { Human, Robot }
    public CharacterType type;

    [Header("Stage 1 (Dialogue) Settings")]
    public Sprite backgroundImage;
    [TextArea(3, 10)]
    public string dialogueText;
    public Sprite characterSprite;
    public Vector2 spawnPosition;
    public float characterScale = 1f;

    [Header("Stage 2 (Photos) Settings")]
    public Sprite stage2Background;
    public Vector2 stage2Position;
    public float stage2Scale = 1f;
    public Sprite[] storyImages;

    [Header("Photo Frame Settings")]
    public Vector2 framePosition;
    public float frameScale = 1f;

    [Header("Stage 3 (Decision) Settings")]
    public Sprite stage3Background; // خلفية مرحلة القرار
    public Vector2 stage3Position;  // موقع الشخصية في مرحلة القرار
    public float stage3Scale = 1f;  // حجم الشخصية في مرحلة القرار

    [TextArea(3, 10)]
    public string stage2DialogueText; // ⭐ النص الخاص بـ Stage 2


}
