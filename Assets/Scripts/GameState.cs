using UnityEngine;

[System.Serializable]
public class PlayerConfig
{
    public int attack;
    public int speed;
    public int health;
    public int jump;
    
    public int weapon;
    public int ability;
}

public class GameState : MonoBehaviour
{
    public PlayerConfig player = new PlayerConfig();
    public void ResetRun() => player = new PlayerConfig();
}
