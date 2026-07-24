using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Linq;
#endif

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    public enum AnimState
    {
        Idle,
        Move,
        Attack
    }

    [Header("Sprites")]
    public Sprite[] idleSprites;
    public Sprite[] moveSprites;
    public Sprite[] attackSprites;

    [Header("Settings")]
    [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private bool loopIdle = true;
    [SerializeField] private bool loopMove = true;
    [SerializeField] private bool loopAttack = false;

    private SpriteRenderer spriteRenderer;
    private AnimState currentState = AnimState.Idle;
    private float timer = 0f;
    private int currentFrame = 0;
    private bool isAnimationPlaying = true;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Ensure characters render in front of floor assets (which default to 0)
        spriteRenderer.sortingOrder = 5;
    }

    private void Start()
    {
        PlayState(AnimState.Idle);
    }

    private void Update()
    {
        if (!isAnimationPlaying) return;

        timer += Time.deltaTime;
        float frameInterval = 1f / framesPerSecond;

        if (timer >= frameInterval)
        {
            timer -= frameInterval;
            StepAnimationFrame();
        }
    }

    private void StepAnimationFrame()
    {
        Sprite[] activeArray = GetActiveSpriteArray();
        if (activeArray == null || activeArray.Length == 0) return;

        currentFrame++;

        bool loop = GetLoopSetting(currentState);
        if (currentFrame >= activeArray.Length)
        {
            if (loop)
            {
                currentFrame = 0;
            }
            else
            {
                currentFrame = activeArray.Length - 1;
                isAnimationPlaying = false; // Stop playing non-looping animation
            }
        }

        spriteRenderer.sprite = activeArray[currentFrame];
    }

    public void PlayState(AnimState newState, bool forceRestart = false)
    {
        if (currentState == newState && !forceRestart && isAnimationPlaying) return;

        currentState = newState;
        currentFrame = 0;
        timer = 0f;
        isAnimationPlaying = true;

        Sprite[] activeArray = GetActiveSpriteArray();
        if (activeArray != null && activeArray.Length > 0)
        {
            spriteRenderer.sprite = activeArray[0];
        }
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Flip sprite based on movement direction (assuming asset faces right by default)
        if (direction.x < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
        else if (direction.x > 0.01f)
        {
            spriteRenderer.flipX = false;
        }
    }

    private Sprite[] GetActiveSpriteArray()
    {
        switch (currentState)
        {
            case AnimState.Idle: return idleSprites;
            case AnimState.Move: return moveSprites;
            case AnimState.Attack: return attackSprites;
            default: return null;
        }
    }

    private bool GetLoopSetting(AnimState state)
    {
        switch (state)
        {
            case AnimState.Idle: return loopIdle;
            case AnimState.Move: return loopMove;
            case AnimState.Attack: return loopAttack;
            default: return true;
        }
    }

    public AnimState GetCurrentState()
    {
        return currentState;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Automatically searches for and loads skeleton sprites from Assets/asset/images/zombie when the script is added or reset.
    /// </summary>
    private void Reset()
    {
        string searchPath = "Assets/asset/images/zombie";
        if (!Directory.Exists(searchPath))
        {
            Debug.LogWarning($"Zombie sprites path not found at: {searchPath}. Please manually assign sprites.");
            return;
        }

        // Helper to load and sort sprites
        System.Func<string, Sprite[]> loadAndSortSprites = (prefix) =>
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { searchPath });
            var spriteList = new List<Sprite>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string filename = Path.GetFileNameWithoutExtension(path);
                
                if (filename.StartsWith(prefix))
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                    {
                        spriteList.Add(sprite);
                    }
                }
            }

            // Extract numeric index from filename (e.g. skeleton-idle_12 -> 12) to sort correctly
            return spriteList
                .OrderBy(s => {
                    string name = s.name;
                    int underscoreIndex = name.LastIndexOf('_');
                    if (underscoreIndex != -1 && int.TryParse(name.Substring(underscoreIndex + 1), out int num))
                    {
                        return num;
                    }
                    return 999;
                })
                .ToArray();
        };

        idleSprites = loadAndSortSprites("skeleton-idle_");
        moveSprites = loadAndSortSprites("skeleton-move_");
        attackSprites = loadAndSortSprites("skeleton-attack_");

        Debug.Log($"[SpriteAnimator] Automatically loaded: {idleSprites.Length} Idle sprites, {moveSprites.Length} Move sprites, {attackSprites.Length} Attack sprites.");
    }
#endif
}
