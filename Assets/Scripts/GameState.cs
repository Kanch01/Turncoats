using UnityEngine;
using UnityEngine.InputSystem;

public enum Role { None, Hero, Boss }

[System.Serializable]
public class PlayerConfig
{
    public int attack;
    public int speed;
    public int health;
    public int jump;
    public int playerInd;

    public int weapon;
    public int ability;
}

public class GameState : MonoBehaviour
{
    [Header("Devices")]
    public InputDevice player0Device;
    public InputDevice player1Device;

    [Header("Roles")]
    public Role player0Role = Role.None;
    public Role player1Role = Role.None;

    [Header("Configs")]
    public PlayerConfig hero = new PlayerConfig();
    public PlayerConfig boss = new PlayerConfig();

    public void ResetRun()
    {
        hero = new PlayerConfig();
        boss = new PlayerConfig();

        player0Role = Role.None;
        player1Role = Role.None;
    }

    public int GetPlayerIndexForRole(Role role)
    {
        if (player0Role == role) return 0;
        if (player1Role == role) return 1;
        return -1;
    }

    public InputDevice GetDeviceForPlayerIndex(int idx)
    {
        return idx == 0 ? player0Device : player1Device;
    }

    public InputDevice GetDeviceForRole(Role role)
    {
        int idx = GetPlayerIndexForRole(role);
        return idx < 0 ? null : GetDeviceForPlayerIndex(idx);
    }
    
    public PlayerConfig GetConfigForRole(Role role)
    {
        return role switch
        {
            Role.Hero => hero,
            Role.Boss => boss,
            _ => null
        };
    }
}
