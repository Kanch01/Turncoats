using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject[] playerPrefabs;
    
    [SerializeField] private int player1PrefabIndex = 0;
    [SerializeField] private int player2PrefabIndex = 1;
    
    [Header("UI")]
    [SerializeField] private HealthBarUI[] healthBars;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints = new Transform[2];

    [Header("Options")]
    [SerializeField] private bool disableJoiningAfterSpawn = true;
    [SerializeField] private bool allowJoinIfMissingSecondDevice = true;

    private PlayerInputManager _pim;
    private int deviceCount;
    private void Awake()
    {
        _pim = GetComponent<PlayerInputManager>();
    }
    
    private void Start()
    {
        // StartGame();
    }

    public void StartGame()
    {
        if (FindObjectsByType<PlayerInput>(0).Length > 0)
        {
            if (disableJoiningAfterSpawn) _pim.DisableJoining();
            return;
        }

        if (playerPrefabs == null || playerPrefabs.Length == 0)
        {
            Debug.LogError("AutoTwoPlayerSpawner: No playerPrefabs assigned.");
            return;
        }

        // Gather devices
        var devices = new List<InputDevice>();
        foreach (var g in Gamepad.all) devices.Add(g);
        foreach (var j in Joystick.all) devices.Add(j);

        deviceCount = Mathf.Min(2, devices.Count);

        for (int i = 0; i < deviceCount; i++)
        {
            var device = devices[i];
            string scheme = device is Gamepad ? "Gamepad" : "Joystick";

            // Choose prefab for player i
            var prefab = GetPrefabForPlayerIndex(i);

            if (prefab == null)
            {
                Debug.LogError($"AutoTwoPlayerSpawner: Prefab for player {i} is null.");
                continue;
            }

            // Spawn and pair device
            var player = PlayerInput.Instantiate(
                prefab,
                playerIndex: i,
                controlScheme: scheme,
                pairWithDevice: device
            );

            // Move to spawn point
            if (spawnPoints != null && spawnPoints.Length > i && spawnPoints[i] != null)
            {
                player.transform.SetPositionAndRotation(
                    spawnPoints[i].position,
                    spawnPoints[i].rotation
                );
            }

            player.gameObject.name = $"P{i + 1}_{prefab.name}_{device.displayName}";

            FindFirstObjectByType<MultiTargetCamera>().RegisterPlayer(player.transform);

            // Connect health to correct UI
            HealthManager health = player.GetComponent<HealthManager>();

            if (healthBars != null && healthBars.Length > i && healthBars[i] != null)
            {
                healthBars[i].Initialize(health);
            }
        }

        // Joining behavior after spawn
        if (disableJoiningAfterSpawn && deviceCount == 2)
            _pim.DisableJoining();
        else if (!allowJoinIfMissingSecondDevice)
            _pim.DisableJoining();
        else
            _pim.EnableJoining();
    }

    private GameObject GetPrefabForPlayerIndex(int playerIndex)
    {
        int chosenIndex = (playerIndex == 0) ? player1PrefabIndex : player2PrefabIndex;
        chosenIndex = Mathf.Clamp(chosenIndex, 0, playerPrefabs.Length - 1);
        return playerPrefabs[chosenIndex];
    }
}

