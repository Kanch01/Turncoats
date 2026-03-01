using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("Role Prefabs")]
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private HealthBarUI[] healthBars;

    [Header("Spawn Points")]
    [SerializeField] private Transform heroSpawn;
    [SerializeField] private Transform bossSpawn;

    public void StartGame()
    {
        if (FindObjectsByType<PlayerInput>(FindObjectsSortMode.None).Length > 0)
            return;

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
        var heroPI = SpawnForRole(
            role: Role.Hero,
            prefab: heroPrefab,
            playerIndex: state.GetPlayerIndexForRole(Role.Hero),
            device: heroDevice,
            spawn: heroSpawn,
            namePrefix: "HERO",
            state: state
        );

        // Spawn BOSS
        var bossPI = SpawnForRole(
            role: Role.Boss,
            prefab: bossPrefab,
            playerIndex: state.GetPlayerIndexForRole(Role.Boss),
            device: bossDevice,
            spawn: bossSpawn,
            namePrefix: "BOSS",
            state: state
        );

        // Hook up health bars to the spawned instances
        WireHealthBar(roleIndex: 0, player: heroPI);
        WireHealthBar(roleIndex: 1, player: bossPI);
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

    // ✅ Change return type to PlayerInput
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

        var cam = FindFirstObjectByType<MultiTargetCamera>();
        if (cam != null) cam.RegisterPlayer(pi.transform);

        return pi;
    }
}


/*
 * // Connect health to correct UI
   HealthManager health = player.GetComponent<HealthManager>();

   if (healthBars != null && healthBars.Length > i && healthBars[i] != null)
   {
       healthBars[i].Initialize(health);
   }
 */

