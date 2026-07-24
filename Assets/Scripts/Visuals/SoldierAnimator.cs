using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

[RequireComponent(typeof(SpriteRenderer))]
public class SoldierAnimator : MonoBehaviour
{
    public enum AnimState
    {
        Idle,
        Walk,
        Shoot,
        Hit
    }

    [Header("Spritesheets")]
    public Texture2D walkTexture;
    public Texture2D shootTexture;
    public Texture2D hitTexture;

    [Header("Loaded Sprites (Auto-populated)")]
    public Sprite[] walkSprites;
    public Sprite[] shootSprites;
    public Sprite[] hitSprites;

    [Header("Settings")]
    [SerializeField] private float framesPerSecond = 10f;

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
                isAnimationPlaying = false;
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

    private Sprite[] GetActiveSpriteArray()
    {
        switch (currentState)
        {
            case AnimState.Idle:
                // Use first frame of walk as idle
                return (walkSprites != null && walkSprites.Length > 0) ? new Sprite[] { walkSprites[0] } : null;
            case AnimState.Walk: return walkSprites;
            case AnimState.Shoot: return shootSprites;
            case AnimState.Hit: return hitSprites;
            default: return null;
        }
    }

    private bool GetLoopSetting(AnimState state)
    {
        switch (state)
        {
            case AnimState.Idle: return true;
            case AnimState.Walk: return true;
            case AnimState.Shoot: return false; // Shoot once per trigger
            case AnimState.Hit: return false;   // Hit response is one-shot
            default: return true;
        }
    }

    public AnimState GetCurrentState()
    {
        return currentState;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        // Try auto-locating textures in the same folder
        string enemyFolder = "Assets/asset/images/enemy";
        if (System.IO.Directory.Exists(enemyFolder))
        {
            walkTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{enemyFolder}/2DPIXX - Free Topdown Shooter - Soldier - Walk.png");
            shootTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{enemyFolder}/2DPIXX - Free Topdown Shooter - Soldier - Shoot.png");
            hitTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{enemyFolder}/2DPIXX - Free Topdown Shooter - Soldier - Hit.png");
        }

        LoadAllTextures();
    }

    [ContextMenu("Load Sprites from Textures")]
    public void LoadAllTextures()
    {
        System.Func<Texture2D, Sprite[]> loadSpritesFromTexture = (tex) =>
        {
            if (tex == null) return new Sprite[0];
            string path = AssetDatabase.GetAssetPath(tex);
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
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

        walkSprites = loadSpritesFromTexture(walkTexture);
        shootSprites = loadSpritesFromTexture(shootTexture);
        hitSprites = loadSpritesFromTexture(hitTexture);

        EditorUtility.SetDirty(this);
        Debug.Log($"[SoldierAnimator] Loaded: {walkSprites.Length} Walk, {shootSprites.Length} Shoot, {hitSprites.Length} Hit sprites.");
    }
#endif
}
