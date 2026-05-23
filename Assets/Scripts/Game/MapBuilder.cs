using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the vertical tower arena at runtime using sprite assets.
/// Creates platforms, walls, traps (lava, electricity, laser), background,
/// and spawn markers for player and boss.
/// 
/// Attach this to an empty GameObject in the scene. On Awake(), it loads
/// sprites from Resources or by reference and constructs the full map.
/// </summary>
public class MapBuilder : MonoBehaviour
{
    [Header("Sprite References")]
    [SerializeField] private Sprite tileSprite;               // TileNewCleaned_0
    [SerializeField] private Sprite backgroundSprite;         // SceneBackgroundClean_0
    [SerializeField] private Sprite[] lavaPoolFrames;         // lavapoolCleanSprite_0..3
    [SerializeField] private Sprite[] electricityFrames;      // ElectricitySpriteClean_0..2
    [SerializeField] private Sprite[] laserFrames;            // LaserSpriteClean_0,3,4,5
    
    [Header("Vesper Sprite References")]
    [SerializeField] private Sprite[] vesperMovementFrames;
    [SerializeField] private Sprite[] vesperJumpFrames;
    [SerializeField] private Sprite[] vesperHurtFrames;
    [SerializeField] private Sprite[] vesperDeathFrames;
    [SerializeField] private Sprite heartIcon;

    [Header("Malikor Sprite References")]
    [SerializeField] private Sprite[] malikorMovementFrames;
    [SerializeField] private Sprite[] malikorAttackFrames;
    [SerializeField] private Sprite[] malikorJumpFrames;
    [SerializeField] private Sprite[] malikorDamageFrames;

    [Header("Arena Dimensions")]
    [SerializeField] private float arenaWidth = 20f;
    [SerializeField] private float arenaHeight = 70f;

    [Header("Tile Settings")]
    [SerializeField] private float tileWorldSize = 2f;

    [Header("Sorting")]
    [SerializeField] private string backgroundSortingLayer = "Default";
    [SerializeField] private int backgroundSortingOrder = -10;
    [SerializeField] private int platformSortingOrder = 5;
    [SerializeField] private int trapSortingOrder = 3;

    // Computed
    private float halfWidth;
    private float wallThickness = 1f;
    private Transform arenaParent;

    // Layer for ground (platforms + walls)
    private int groundLayerIndex;

    private void Awake()
    {
        // Force dimensions to override Unity Inspector caching from old script versions
        arenaHeight = 70f;
        arenaWidth = 20f;

        halfWidth = arenaWidth / 2f;
        groundLayerIndex = LayerMask.NameToLayer("Default");
        BuildArena();
    }

    private void BuildArena()
    {
        // Create parent container
        arenaParent = new GameObject("TowerArena").transform;
        arenaParent.position = Vector3.zero;

        // Build each section
        CreateBackground();
        CreateWalls();
        CreateFloor();
        CreatePlatforms();
        CreateLavaPool();
        CreateElectricTraps();
        CreateLaserTrap();
        CreateSpawnMarkers();
        
        CreatePlayerAndUI();

        Debug.Log("[MapBuilder] Tower arena built successfully!");
    }


    // =============================================
    // BACKGROUND
    // =============================================
    private void CreateBackground()
    {
        if (backgroundSprite == null) return;

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(arenaParent);

        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = backgroundSprite;
        sr.sortingOrder = backgroundSortingOrder;

        // Scale background to cover the arena
        float spriteWidth = backgroundSprite.bounds.size.x;
        float spriteHeight = backgroundSprite.bounds.size.y;

        float scaleX = (arenaWidth + 4f) / spriteWidth;
        float scaleY = (arenaHeight + 4f) / spriteHeight;
        float scale = Mathf.Max(scaleX, scaleY);

        bg.transform.localScale = new Vector3(scale, scale, 1f);
        bg.transform.position = new Vector3(0f, arenaHeight / 2f, 1f);
    }

    // =============================================
    // WALLS (invisible colliders)
    // =============================================
    private void CreateWalls()
    {
        GameObject wallsParent = new GameObject("Walls");
        wallsParent.transform.SetParent(arenaParent);

        // Left wall
        CreateWall("LeftWall", new Vector3(-halfWidth - wallThickness / 2f, arenaHeight / 2f, 0f),
                   new Vector2(wallThickness, arenaHeight + 4f), wallsParent.transform);

        // Right wall
        CreateWall("RightWall", new Vector3(halfWidth + wallThickness / 2f, arenaHeight / 2f, 0f),
                   new Vector2(wallThickness, arenaHeight + 4f), wallsParent.transform);

        // Ceiling
        CreateWall("Ceiling", new Vector3(0f, arenaHeight + wallThickness / 2f, 0f),
                   new Vector2(arenaWidth + 2f, wallThickness), wallsParent.transform);
    }

    private void CreateWall(string name, Vector3 position, Vector2 size, Transform parent)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.layer = groundLayerIndex;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    // =============================================
    // FLOOR (invisible collider under lava)
    // =============================================
    private void CreateFloor()
    {
        GameObject floor = new GameObject("Floor");
        floor.transform.SetParent(arenaParent);
        floor.transform.position = new Vector3(0f, -1f, 0f);
        floor.layer = groundLayerIndex;

        BoxCollider2D col = floor.AddComponent<BoxCollider2D>();
        col.size = new Vector2(arenaWidth + 2f, 1f);
    }

    // =============================================
    // PLATFORMS
    // =============================================
    private void CreatePlatforms()
    {
        if (tileSprite == null)
        {
            Debug.LogWarning("[MapBuilder] No tile sprite assigned!");
            return;
        }

        GameObject platformsParent = new GameObject("Platforms");
        platformsParent.transform.SetParent(arenaParent);

        float t = tileWorldSize;
        float left = -halfWidth;
        float right = halfWidth;

        // ---- BOTTOM BASE PLATFORMS (wide, Y=6) ----
        // Left base: 3 tiles starting from left wall
        for (int i = 0; i < 3; i++)
        {
            CreatePlatformTile($"Platform_Base_L_{i}",
                new Vector3(left + 2f + i * t, 6f, 0f), platformsParent.transform);
        }
        // Right base: 3 tiles ending at right wall
        for (int i = 0; i < 3; i++)
        {
            CreatePlatformTile($"Platform_Base_R_{i}",
                new Vector3(right - 2f - (2 - i) * t, 6f, 0f), platformsParent.transform);
        }

        // ---- ASCENDING PLATFORMS (staggered pattern, doubled vertical distance) ----

        // Row 1 (Y=12): center-left and center-right
        CreatePlatformTile("Platform_C1_L", new Vector3(-4f, 12f, 0f), platformsParent.transform);
        CreatePlatformTile("Platform_C1_R", new Vector3(4f, 12f, 0f), platformsParent.transform);

        // Row 2 (Y=18): far left and far right
        CreatePlatformTile("Platform_L2", new Vector3(left + 2f, 18f, 0f), platformsParent.transform);
        CreatePlatformTile("Platform_R2", new Vector3(right - 2f, 18f, 0f), platformsParent.transform);

        // Row 3 (Y=24): inner
        CreatePlatformTile("Platform_L3", new Vector3(-5f, 24f, 0f), platformsParent.transform);
        CreatePlatformTile("Platform_R3", new Vector3(5f, 24f, 0f), platformsParent.transform);

        // Row 4 (Y=30): center pair
        CreatePlatformTile("Platform_C4_L", new Vector3(-4f, 30f, 0f), platformsParent.transform);
        CreatePlatformTile("Platform_C4_R", new Vector3(4f, 30f, 0f), platformsParent.transform);

        // Row 5 (Y=36): far left and far right (flanking laser)
        CreatePlatformTile("Platform_L5", new Vector3(left + 2f, 36f, 0f), platformsParent.transform);
        CreatePlatformTile("Platform_R5", new Vector3(right - 2f, 36f, 0f), platformsParent.transform);

        // Row 6 (Y=42): inner
        CreatePlatformTile("Platform_L6", new Vector3(-5f, 42f, 0f), platformsParent.transform);
        CreatePlatformTile("Platform_R6", new Vector3(5f, 42f, 0f), platformsParent.transform);

        // Row 7 (Y=48): center pair
        CreatePlatformTile("Platform_C7_L", new Vector3(-4f, 48f, 0f), platformsParent.transform);
        CreatePlatformTile("Platform_C7_R", new Vector3(4f, 48f, 0f), platformsParent.transform);

        // Row 8 (Y=54): far left and far right
        CreatePlatformTile("Platform_L8", new Vector3(left + 2f, 54f, 0f), platformsParent.transform);
        CreatePlatformTile("Platform_R8", new Vector3(right - 2f, 54f, 0f), platformsParent.transform);

        // Row 9 (Y=60): center pair (near electric traps)
        CreatePlatformTile("Platform_C9_L", new Vector3(-4f, 60f, 0f), platformsParent.transform);
        CreatePlatformTile("Platform_C9_R", new Vector3(4f, 60f, 0f), platformsParent.transform);
    }

    private void CreatePlatformTile(string name, Vector3 position, Transform parent)
    {
        GameObject tile = new GameObject(name);
        tile.transform.SetParent(parent);
        tile.transform.position = position;
        tile.layer = groundLayerIndex;

        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        sr.sprite = tileSprite;
        sr.sortingOrder = platformSortingOrder;

        // Scale the tile sprite to desired world size
        float spriteSize = tileSprite.bounds.size.x;
        float scale = tileWorldSize / spriteSize;
        tile.transform.localScale = new Vector3(scale, scale, 1f);

        // Add collider — size is in local space, so use the original sprite bounds
        BoxCollider2D col = tile.AddComponent<BoxCollider2D>();
        col.size = tileSprite.bounds.size;
    }

    // =============================================
    // LAVA POOL (animated, full-width at bottom)
    // =============================================
    private void CreateLavaPool()
    {
        GameObject trapsParent = GetOrCreateChild(arenaParent, "Traps");

        // Lava pool spans the full width at the bottom (Y=0 to Y=4)
        float lavaY = 2f;
        float lavaHeight = 4f;

        // Create multiple lava tiles across the width
        int lavaCount = Mathf.CeilToInt(arenaWidth / 5f) + 1;
        float startX = -halfWidth;

        for (int i = 0; i < lavaCount; i++)
        {
            float x = startX + i * (arenaWidth / lavaCount) + (arenaWidth / lavaCount / 2f);

            GameObject lava = new GameObject($"LavaPool_{i}");
            lava.transform.SetParent(trapsParent.transform);
            lava.transform.position = new Vector3(x, lavaY, 0f);

            SpriteRenderer sr = lava.AddComponent<SpriteRenderer>();
            sr.sortingOrder = trapSortingOrder;

            if (lavaPoolFrames != null && lavaPoolFrames.Length > 0)
            {
                sr.sprite = lavaPoolFrames[0];

                // Scale to cover section
                float spriteWidth = lavaPoolFrames[0].bounds.size.x;
                float spriteH = lavaPoolFrames[0].bounds.size.y;
                float targetWidth = arenaWidth / lavaCount + 0.5f; // slight overlap
                float scaleX = targetWidth / spriteWidth;
                float scaleY = lavaHeight / spriteH;
                lava.transform.localScale = new Vector3(scaleX, scaleY, 1f);

                // Add animator
                TrapAnimator anim = lava.AddComponent<TrapAnimator>();
                anim.SetFrames(lavaPoolFrames);
                anim.SetFrameRate(4f);
            }
        }

        // Lava trigger zone (one big collider)
        GameObject lavaTrigger = new GameObject("LavaTrapZone");
        lavaTrigger.transform.SetParent(trapsParent.transform);
        lavaTrigger.transform.position = new Vector3(0f, lavaY, 0f);

        BoxCollider2D lavaCol = lavaTrigger.AddComponent<BoxCollider2D>();
        lavaCol.size = new Vector2(arenaWidth, lavaHeight);
        lavaCol.isTrigger = true;

        TrapZone lavaTrap = lavaTrigger.AddComponent<TrapZone>();
        lavaTrap.SetTrapType(TrapType.Lava);
    }

    // =============================================
    // ELECTRIC TRAPS (animated, at ceiling)
    // =============================================
    private void CreateElectricTraps()
    {
        GameObject trapsParent = GetOrCreateChild(arenaParent, "Traps");

        float electricY = 65f;
        int count = 8;

        for (int i = 0; i < count; i++)
        {
            float x = -halfWidth + 2f + i * ((arenaWidth - 4f) / (count - 1));

            GameObject elec = new GameObject($"ElectricTrap_{i}");
            elec.transform.SetParent(trapsParent.transform);
            elec.transform.position = new Vector3(x, electricY, 0f);

            SpriteRenderer sr = elec.AddComponent<SpriteRenderer>();
            sr.sortingOrder = trapSortingOrder;

            if (electricityFrames != null && electricityFrames.Length > 0)
            {
                sr.sprite = electricityFrames[0];

                // Scale electric sprites to reasonable size
                float spriteSize = electricityFrames[0].bounds.size.x;
                float targetSize = 2f;
                float scale = targetSize / spriteSize;
                elec.transform.localScale = new Vector3(scale, scale, 1f);

                TrapAnimator anim = elec.AddComponent<TrapAnimator>();
                // Use only the good frames (0, 1, 2) — frames 3, 4 are tiny artifacts
                Sprite[] goodFrames;
                if (electricityFrames.Length >= 3)
                {
                    goodFrames = new Sprite[] { electricityFrames[0], electricityFrames[1], electricityFrames[2] };
                }
                else
                {
                    goodFrames = electricityFrames;
                }
                anim.SetFrames(goodFrames);
                anim.SetFrameRate(6f);
            }
        }

        // Electric trigger zone (one big collider across ceiling)
        GameObject elecTrigger = new GameObject("ElectricTrapZone");
        elecTrigger.transform.SetParent(trapsParent.transform);
        elecTrigger.transform.position = new Vector3(0f, electricY, 0f);

        BoxCollider2D elecCol = elecTrigger.AddComponent<BoxCollider2D>();
        elecCol.size = new Vector2(arenaWidth - 2f, 3f);
        elecCol.isTrigger = true;

        TrapZone elecTrap = elecTrigger.AddComponent<TrapZone>();
        elecTrap.SetTrapType(TrapType.Electricity);
    }

    // =============================================
    // LASER TRAP (animated, middle of arena)
    // =============================================
    private void CreateLaserTrap()
    {
        GameObject trapsParent = GetOrCreateChild(arenaParent, "Traps");

        float laserY = 36f; // Same row as platforms L5/R5
        float laserWidth = 10f; // Spans the center gap between platforms

        GameObject laser = new GameObject("LaserTrap");
        laser.transform.SetParent(trapsParent.transform);
        laser.transform.position = new Vector3(0f, laserY, 0f);

        SpriteRenderer sr = laser.AddComponent<SpriteRenderer>();
        sr.sortingOrder = trapSortingOrder;

        if (laserFrames != null && laserFrames.Length > 0)
        {
            sr.sprite = laserFrames[0];

            // Scale to span the gap
            float spriteWidth = laserFrames[0].bounds.size.x;
            float spriteHeight = laserFrames[0].bounds.size.y;
            float scaleX = laserWidth / spriteWidth;
            float scaleY = 1.5f / spriteHeight; // thin beam
            laser.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            TrapAnimator anim = laser.AddComponent<TrapAnimator>();
            anim.SetFrames(laserFrames);
            anim.SetFrameRate(5f);
        }

        // Laser trigger zone
        BoxCollider2D laserCol = laser.AddComponent<BoxCollider2D>();
        if (laserFrames != null && laserFrames.Length > 0)
        {
            laserCol.size = laserFrames[0].bounds.size;
        }
        else
        {
            laserCol.size = new Vector2(laserWidth, 1f);
        }
        laserCol.isTrigger = true;

        TrapZone laserTrap = laser.AddComponent<TrapZone>();
        laserTrap.SetTrapType(TrapType.Laser);
    }

    // =============================================
    // SPAWN MARKERS
    // =============================================
    private void CreateSpawnMarkers()
    {
        // Player spawn: on the first left base platform tile (Y=6 is tile center, so top = 6 + tileWorldSize/2 = 7)
        // Place Vesper just above the tile surface
        float tileTopY = 6f + tileWorldSize / 2f;
        GameObject playerSpawn = new GameObject("PlayerSpawn");
        playerSpawn.transform.SetParent(arenaParent);
        playerSpawn.transform.position = new Vector3(-halfWidth + 2f + tileWorldSize, tileTopY + 0.5f, 0f);

        // Boss spawn: center, mid-level
        GameObject bossSpawn = new GameObject("BossSpawn");
        bossSpawn.transform.SetParent(arenaParent);
        bossSpawn.transform.position = new Vector3(0f, 32f, 0f);
    }

    // =============================================
    // HELPERS
    // =============================================
    private GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child;
    }

    // =============================================
    // PLAYER AND UI
    // =============================================
    private void CreatePlayerAndUI()
    {
        // 1. Create Player
        GameObject playerSpawn = GameObject.Find("PlayerSpawn");
        Vector3 spawnPos = playerSpawn != null ? playerSpawn.transform.position : new Vector3(-halfWidth + 4f, 7.5f, 0f);

        GameObject player = new GameObject("Vesper");
        player.transform.position = spawnPos;
        player.tag = "Player";

        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;
        
        // Add physics
        BoxCollider2D col = player.AddComponent<BoxCollider2D>();
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        
        // Setup animator (handles its own scaling via normalization)
        VesperAnimator anim = player.AddComponent<VesperAnimator>();
        anim.Setup(vesperMovementFrames, vesperJumpFrames, vesperHurtFrames, vesperDeathFrames);
        
        // Setup controller
        VesperController controller = player.AddComponent<VesperController>();
        
        // Set collider to a tight box around the character body
        if (vesperMovementFrames != null && vesperMovementFrames.Length > 2)
        {
            Vector2 spriteSize = vesperMovementFrames[2].bounds.size; // Use idle frame
            // Narrow hitbox: roughly 30% width, 90% height
            col.size = new Vector2(spriteSize.x * 0.3f, spriteSize.y * 0.9f);
            // Shift collider down so feet align with bottom
            col.offset = new Vector2(0f, -(spriteSize.y * 0.05f));
        }

        // Scale Vesper down to fit the tile size properly
        // The animator normalizes to idle sprite size; we need an additional global scale
        // Tiles are 2x2 world units. Vesper should be roughly 1.2 tiles tall
        if (vesperMovementFrames != null && vesperMovementFrames.Length > 2)
        {
            float idleHeight = vesperMovementFrames[2].bounds.size.y;
            float targetHeight = tileWorldSize * 1.2f; // Slightly taller than one tile
            float scaleFactor = targetHeight / idleHeight;
            player.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
            anim.SetBaseScale(scaleFactor);
        }

        // 2. Setup Camera — keep CameraPan on the camera but also follow player
        // DON'T parent camera to player (breaks CameraPan). Instead add a smooth follow.
        CameraPan pan = Camera.main.GetComponent<CameraPan>();
        if (pan != null)
        {
            // Keep pan but disable it during gameplay — we'll add a follow script
            pan.enabled = false;
        }

        // Set camera to center on arena at start
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 12f; // Show a good portion of the arena
        Camera.main.transform.position = new Vector3(0f, arenaHeight * 0.2f, -10f);

        // Add a simple follow component
        CameraFollow follow = Camera.main.gameObject.AddComponent<CameraFollow>();
        follow.Setup(player.transform, arenaWidth, arenaHeight);

        // 3. Create HUD — Respawns only
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("GameCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        GameObject hudPanel = new GameObject("VesperHUD");
        hudPanel.transform.SetParent(canvas.transform, false);
        RectTransform hudRect = hudPanel.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(1, 1);
        hudRect.anchorMax = new Vector2(1, 1);
        hudRect.pivot = new Vector2(1, 1);
        hudRect.anchoredPosition = new Vector2(-30, -15);
        hudRect.sizeDelta = new Vector2(220, 40);

        // Background for readability
        Image bgImg = hudPanel.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.5f);

        PlayerUIManager uiManager = hudPanel.AddComponent<PlayerUIManager>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject textObj = new GameObject("Respawn_Text");
        textObj.transform.SetParent(hudPanel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);
        Text respawnText = textObj.AddComponent<Text>();
        respawnText.font = defaultFont;
        respawnText.fontSize = 22;
        respawnText.alignment = TextAnchor.MiddleCenter;
        respawnText.color = Color.white;
        respawnText.text = $"Respawns: {controller.maxLives}";

        uiManager.Setup(respawnText);
    }
}

