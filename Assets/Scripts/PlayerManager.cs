using System.Net.Http.Headers;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    [Header("Role Prefabs")]
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private HealthBarUI[] healthBars;

    [Header("Spawn Points")]
    [SerializeField] private Transform heroSpawn;
    [SerializeField] private Transform bossSpawn;
    [SerializeField] private float respawnDelay = 1f;

    private PlayerInput heroInstance;
    private PlayerInput bossInstance;
    private bool respawning = false;
    private MultiTargetCamera cam;
    private Vector3 cameraStartPos;
    private Quaternion cameraStartRot;
    private TMP_Text respawnText;
    private TMP_Text countdownText;

    private void Awake()
    {
        cam = FindFirstObjectByType<MultiTargetCamera>();
    }

    private void Start()
    {
        // cam = FindFirstObjectByType<MultiTargetCamera>();
        if (cam != null)
        {
            cameraStartPos = cam.transform.position;
            cameraStartRot = cam.transform.rotation;
        }

        respawnText = GameObject.Find("RespawnText").GetComponent<TMP_Text>();
        if (respawnText != null)
        {
            respawnText.gameObject.SetActive(false);
        }
        else
        {
            UnityEngine.Debug.LogWarning("RespawnText GameObject not found!");
        }

        countdownText = GameObject.Find("CountdownText").GetComponent<TMP_Text>();
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        else
        {
            UnityEngine.Debug.LogWarning("CountdownText GameObject not found!");
        }
    }

    public void StartGame()
    {
        if (FindObjectsByType<PlayerInput>(FindObjectsSortMode.None).Length > 0)
        {
            UnityEngine.Debug.Log("returning, player input still exists");
            return;
        }

        var flow = GameFlowManager.Instance;
        if (flow == null || flow.State == null)
        {
            
            Debug.LogError("PlayerManager: GameFlowManager/State missing.");
            return;
        }

        var state = flow.State;

        var heroDevice = state.GetDeviceForRole(Role.Hero);
        var bossDevice = state.GetDeviceForRole(Role.Boss);

        if (heroDevice == null || bossDevice == null)
        {
            Debug.LogError("PlayerManager: Hero/Boss devices not assigned.");
            return;
        }

        // Spawn HERO
        heroInstance = SpawnForRole(
            role: Role.Hero,
            prefab: heroPrefab,
            playerIndex: state.GetPlayerIndexForRole(Role.Hero),
            device: heroDevice,
            spawn: heroSpawn,
            namePrefix: "HERO",
            state: state
        );

        // Spawn BOSS
        bossInstance = SpawnForRole(
            role: Role.Boss,
            prefab: bossPrefab,
            playerIndex: state.GetPlayerIndexForRole(Role.Boss),
            device: bossDevice,
            spawn: bossSpawn,
            namePrefix: "BOSS",
            state: state
        );

        // Hook up health bars to the spawned instances
        WireHealthBar(roleIndex: 0, player: heroInstance);
        WireHealthBar(roleIndex: 1, player: bossInstance);
    }

    private void WireHealthBar(int roleIndex, PlayerInput player)
    {
        if (player == null) return;

        if (healthBars == null || healthBars.Length <= roleIndex || healthBars[roleIndex] == null)
        {
            Debug.LogWarning($"PlayerManager: Health bar slot {roleIndex} not assigned.");
            return;
        }

        var health = player.GetComponent<HealthManager>();
        if (health == null)
        {
            Debug.LogWarning($"PlayerManager: Spawned player {player.name} has no HealthManager.");
            return;
        }

        healthBars[roleIndex].Initialize(health);
    }

    // Change return type to PlayerInput
    private PlayerInput SpawnForRole(
        Role role,
        GameObject prefab,
        int playerIndex,
        InputDevice device,
        Transform spawn,
        string namePrefix,
        GameState state
    )
    {
        if (prefab == null)
        {
            Debug.LogError($"PlayerManager: {namePrefix} prefab not set.");
            return null;
        }

        string scheme = device is Gamepad ? "Gamepad" : "Joystick";

        var pi = PlayerInput.Instantiate(
            prefab,
            playerIndex: playerIndex,
            controlScheme: scheme,
            pairWithDevice: device
        );

        if (spawn != null)
            pi.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        pi.gameObject.name = $"{namePrefix}_P{playerIndex}_{prefab.name}_{device.displayName}";

        // Apply customized stats from GameState to the spawned prefab
        var cfg = state != null ? state.GetConfigForRole(role) : null;

        var movement = pi.GetComponent<PlayerMovement>();
        if (movement != null) movement.ApplyConfig(cfg);
        
        var healthMgr = pi.GetComponent<HealthManager>();
        if (healthMgr != null && cfg != null)
        {
            // cfg.health is int; HealthManager uses float
            healthMgr.ApplyMaxHealth(cfg.health, fillToMax: true);
        }

        var attackMgr = pi.GetComponent<AttackHitboxController>();
        if (attackMgr != null && cfg != null)
        {
            // cfg.health is int; HealthManager uses float
            attackMgr.ApplyNewAttack(cfg.attack);
        }

        if (cam != null) cam.RegisterPlayer(pi.transform);

        return pi;
    }

    public void OnPlayerDeath(HealthManager deadPlayer)
    {
        if (!respawning)
            StartCoroutine(RespawnBoth(deadPlayer));
    }

    private System.Collections.IEnumerator RespawnBoth(HealthManager deadPlayer)
    {
        respawning = true;

        string deathName = deadPlayer.GetComponent<PlayerMovement>().IsHero ? "Boss" : "Hero";

        // Display death message
        if (respawnText != null)
            respawnText.gameObject.SetActive(true);
            respawnText.text = $"{deathName} Wins!";

        // Enable countdown and display game over text
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        float timer = respawnDelay;
        while (timer > 0f)
        {
            if (countdownText != null)
                countdownText.text = $"Respawning in {Mathf.Ceil(timer)}";

            timer -= Time.deltaTime;
            yield return null;
        }

        // Move camera back to start
        if (cam != null)
        {
            cam.transform.position = cameraStartPos;
            cam.transform.rotation = cameraStartRot;
        }

        if (respawnText != null)
            respawnText.gameObject.SetActive(false);
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (heroInstance != null)
            Destroy(heroInstance.gameObject);

        if (bossInstance != null)
            Destroy(bossInstance.gameObject);

        // Wait one frame so Destroy actually completes
        yield return null;

        StartGame();

        respawning = false;
    }
}
