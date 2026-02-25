using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("Role Prefabs")]
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform heroSpawn;
    [SerializeField] private Transform bossSpawn;

    public void StartGame()
    {
        // If already spawned, do nothing
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
        SpawnForRole(
            prefab: heroPrefab,
            playerIndex: state.GetPlayerIndexForRole(Role.Hero),
            device: heroDevice,
            spawn: heroSpawn,
            namePrefix: "HERO"
        );

        // Spawn BOSS
        SpawnForRole(
            prefab: bossPrefab,
            playerIndex: state.GetPlayerIndexForRole(Role.Boss),
            device: bossDevice,
            spawn: bossSpawn,
            namePrefix: "BOSS"
        );
    }

    private void SpawnForRole(GameObject prefab, int playerIndex, InputDevice device, Transform spawn, string namePrefix)
    {
        if (prefab == null)
        {
            Debug.LogError($"PlayerManager: {namePrefix} prefab not set.");
            return;
        }

        string scheme = device is Gamepad ? "Gamepad" : "Joystick";

        var pi = PlayerInput.Instantiate(
            prefab,
            playerIndex: playerIndex,
            controlScheme: scheme,
            pairWithDevice: device
        );

        if (spawn != null)
        {
            pi.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        }

        pi.gameObject.name = $"{namePrefix}_P{playerIndex}_{prefab.name}_{device.displayName}";

        var cam = FindFirstObjectByType<MultiTargetCamera>();
        if (cam != null) cam.RegisterPlayer(pi.transform);
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

