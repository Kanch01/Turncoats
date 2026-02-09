using System.Security;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("Prefabs (must contain PlayerInput)")]
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform heroSpawn;
    [SerializeField] private Transform bossSpawn;

    [Header("Input")]
    [SerializeField] private string controlScheme = "Joystick";

    [Header("Dev")]
    [SerializeField] private bool devMode = false;

    private void initializeDev()
    {
        // Spawn Hero controlled by gamepad #1
        // var p1 = Joystick.all[0];
        // var hero = PlayerInput.Instantiate(
        //     heroPrefab,
        //     playerIndex: 0,
        //     controlScheme: controlScheme,
        //     pairWithDevice: p1
        // );

        // // Position them
        // if (heroSpawn != null) hero.transform.position = heroSpawn.position;
        // hero.SwitchCurrentActionMap("Gameplay");
        // Debug.Log($"Spawned Hero in dev mode with {p1.displayName}");
        Debug.Log("Debug mode activated");
    }

    private void Start()
    {
        if (devMode)
        {
            initializeDev();
            return;
        }
        // Ensure exactly two gamepads are present
        if (Joystick.all.Count < 2)
        {
            Debug.LogError($"Need 2 Joysticks connected. Found: {Joystick.all.Count}");
            return;
        }

        var p1 = Joystick.all[0];
        var p2 = Joystick.all[1];

        // Spawn Hero controlled by gamepad #1
        var hero = PlayerInput.Instantiate(
            heroPrefab,
            playerIndex: 0,
            controlScheme: controlScheme,
            pairWithDevice: p1
        );

        // Spawn Boss controlled by gamepad #2
        var boss = PlayerInput.Instantiate(
            bossPrefab,
            playerIndex: 1,
            controlScheme: controlScheme,
            pairWithDevice: p2
        );

        // Position them
        if (heroSpawn != null) hero.transform.position = heroSpawn.position;
        if (bossSpawn != null) boss.transform.position = bossSpawn.position;

        // Safety: make sure both are on the right action map
        hero.SwitchCurrentActionMap("Gameplay");
        boss.SwitchCurrentActionMap("Gameplay");

        Debug.Log($"Spawned Hero with {p1.displayName}, Boss with {p2.displayName}");
    }
}
