using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputLockManager : MonoBehaviour
{
    public static InputLockManager Instance { get; private set; }

    private readonly HashSet<InputDevice> _lockedOff = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void EnableAllKnownDevices(GameState state)
    {
        if (state == null) return;

        EnableDeviceSafe(state.player0Device);
        EnableDeviceSafe(state.player1Device);
    }

    public void LockToOnlyDevice(GameState state, InputDevice allowed)
    {
        if (state == null) return;

        // Enable allowed
        EnableDeviceSafe(allowed);

        // Disable the other known device(s)
        if (state.player0Device != null && state.player0Device != allowed) DisableDeviceSafe(state.player0Device);
        if (state.player1Device != null && state.player1Device != allowed) DisableDeviceSafe(state.player1Device);
    }

    private void DisableDeviceSafe(InputDevice d)
    {
        if (d == null) return;
        if (!d.enabled) return;
        InputSystem.DisableDevice(d);
        _lockedOff.Add(d);
    }

    private void EnableDeviceSafe(InputDevice d)
    {
        if (d == null) return;
        if (d.enabled) return;
        InputSystem.EnableDevice(d);
        _lockedOff.Remove(d);
    }
}
