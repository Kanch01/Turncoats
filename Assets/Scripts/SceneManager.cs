using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }
    [Header("References (attached on Root object)")]
    [SerializeField] private GameState state; 
    [SerializeField] private InputLockManager inputLock;
    public GameState State => state;
    
    [Header("Root Safety")]
    [SerializeField] private string rootSceneName = "Root";

    [Header("Scene Names")]
    [SerializeField] string mainMenu = "StartMenu";
    [SerializeField] string playerSelect = "PlayerSelect";
    [SerializeField] string statsMenu = "HeroStats";
    [SerializeField] string weaponMenu = "HeroWeapon";
    [SerializeField] string abilityMenu = "HeroAbility";
    [SerializeField] string bossSelect = "BossSelect";
    [SerializeField] string map0 = "Map0";
    [SerializeField] string map1 = "Map1";
    [SerializeField] string map2 = "Map2";
    [SerializeField] string battle = "Battle";
    [SerializeField] private Camera rootCamera;

    [Header("Runtime")]
    public int roleChooserPlayerIndex = -1; // who is allowed to press buttons on PlayerSelect

    string currentLoadedContentScene;
    bool isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // IMPORTANT: Use attached components instead of auto-creating duplicates
        if (state == null) state = GetComponent<GameState>();
        if (inputLock == null) inputLock = GetComponent<InputLockManager>();

        if (state == null)
        {
            Debug.LogError("GameFlowManager: No GameState found. Attach GameState.cs to the SAME Root object, or assign it in inspector.");
        }

        if (inputLock == null)
        {
            Debug.LogError("GameFlowManager: No InputLockManager found. Attach InputLockManager.cs to the SAME Root object, or assign it in inspector.");
        }
    }

    private IEnumerator Start()
    {
        // Hard validation: if this fails, you will see a blank scene.
        if (!Application.CanStreamedLevelBeLoaded(mainMenu))
        {
            Debug.LogError($"GameFlowManager: Cannot load mainMenu scene '{mainMenu}'. " +
                           "Check the spelling AND ensure it is added to Build Settings / Build Profile Scenes list.");
            yield break;
        }

        yield return LoadContentScene(mainMenu);
        ApplyInputPolicyForScene(SceneManager.GetActiveScene().name);
    }

    public void StartNewGame()
    {
        State.ResetRun();

        // 1) Capture the two devices NOW (not at battle time)
        if (!TryAssignTwoDevices(out var p0, out var p1))
        {
            Debug.LogError("Need two controllers connected (Gamepad or Joystick).");
            return;
        }

        State.player0Device = p0;
        State.player1Device = p1;

        // 2) Randomly pick who can choose roles on PlayerSelect
        roleChooserPlayerIndex = Random.Range(0, 2);

        _ = StartCoroutine(LoadContentScene(playerSelect));
    }

    // Called by PlayerSelect buttons:
    public void ChooserSelectsHero()
    {
        AssignRoles(chooserIsHero: true);
        _ = StartCoroutine(LoadContentScene(statsMenu));
    }

    public void ChooserSelectsBoss()
    {
        AssignRoles(chooserIsHero: false);
        _ = StartCoroutine(LoadContentScene(statsMenu));
    }

    private void AssignRoles(bool chooserIsHero)
    {
        int chooser = roleChooserPlayerIndex;
        int other = 1 - chooser;

        if (chooserIsHero)
        {
            State.player0Role = (chooser == 0) ? Role.Hero : Role.Boss;
            State.player1Role = (chooser == 1) ? Role.Hero : Role.Boss;
        }
        else
        {
            State.player0Role = (chooser == 0) ? Role.Boss : Role.Hero;
            State.player1Role = (chooser == 1) ? Role.Boss : Role.Hero;
        }

        // Set indices inside configs (optional but useful)
        State.hero.playerInd = State.GetPlayerIndexForRole(Role.Hero);
        State.boss.playerInd = State.GetPlayerIndexForRole(Role.Boss);
    }

    public void GoStatsToWeapon() => _ = StartCoroutine(LoadContentScene(weaponMenu));
    public void GoWeaponToAbility() => _ = StartCoroutine(LoadContentScene(abilityMenu));
    public void GoAbilityToBattle() => _ = StartCoroutine(LoadContentScene(battle));
    public void GoAbilityToBoss() => _ = StartCoroutine(LoadContentScene(bossSelect));

    // Each boss has a different map
    public void GoBossToStageMod(int bossIndex)
    {
        string mapToLoad = bossIndex switch
        {
            0 => map0,
            1 => map1,
            2 => map2,
            _ => map0
        };

        _ = StartCoroutine(LoadContentScene(mapToLoad));
    }
    public void BackToMainMenu() => _ = StartCoroutine(LoadContentScene(mainMenu));

    private IEnumerator LoadContentScene(string next)
    {
        if (isLoading) yield break;
        isLoading = true;

        if (!Application.CanStreamedLevelBeLoaded(next))
        {
            Debug.LogError($"GameFlowManager: Cannot load scene '{next}'. " +
                           "Check spelling AND add it to Build Settings / Build Profile Scenes list.");
            isLoading = false;
            yield break;
        }

        if (!IsSceneLoaded(next))
        {
            var loadOp = SceneManager.LoadSceneAsync(next, LoadSceneMode.Additive);
            while (!loadOp.isDone) yield return null;
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(next));
        SetCameraForActiveScene();

        // Unload previous content scene (NEVER unload the root scene)
        if (!string.IsNullOrEmpty(currentLoadedContentScene) &&
            currentLoadedContentScene != next &&
            currentLoadedContentScene != rootSceneName)
        {
            var unloadOp = SceneManager.UnloadSceneAsync(currentLoadedContentScene);
            while (unloadOp != null && !unloadOp.isDone) yield return null;
        }

        currentLoadedContentScene = next;

        ApplyInputPolicyForScene(next);

        isLoading = false;
    }

    private void ApplyInputPolicyForScene(string activeScene)
    {
        // Default: allow both
        var lockMgr = InputLockManager.Instance;

        if (activeScene == mainMenu)
        {
            lockMgr.EnableAllKnownDevices(State);
            return;
        }

        if (activeScene == playerSelect)
        {
            var chooserDevice = State.GetDeviceForPlayerIndex(roleChooserPlayerIndex);
            lockMgr.LockToOnlyDevice(State, chooserDevice);
            return;
        }

        // Hero customization scenes
        if (activeScene == statsMenu || activeScene == weaponMenu || activeScene == abilityMenu)
        {
            var heroDevice = State.GetDeviceForRole(Role.Hero);
            lockMgr.LockToOnlyDevice(State, heroDevice);
            return;
        }

        // Boss customization scenes
        if (activeScene == bossSelect || activeScene == map0 || activeScene == map1 || activeScene == map2)
        {
            var bossDevice = State.GetDeviceForRole(Role.Boss);
            lockMgr.LockToOnlyDevice(State, bossDevice);
            return;
        }

        // Battle: allow both
        if (activeScene == battle)
        {
            lockMgr.EnableAllKnownDevices(State);
            return;
        }

        lockMgr.EnableAllKnownDevices(State);
    }

    private void SetCameraForActiveScene()
    {
        var active = SceneManager.GetActiveScene().name;

        if (active == map0 || active == map1 || active == map2 || active == battle)
        {
            SetCameraEnabled(rootCamera, false);
        }
        else
        {
            SetCameraEnabled(rootCamera, true);
        }
    }

    private void SetCameraEnabled(Camera cam, bool enabled)
    {
        if (cam == null) return;
        cam.enabled = enabled;

        var al = cam.GetComponent<AudioListener>();
        if (al != null) al.enabled = enabled;
    }

    bool IsSceneLoaded(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).name == name) return true;
        return false;
    }

    private bool TryAssignTwoDevices(out InputDevice p0, out InputDevice p1)
    {
        // same approach you used in PlayerManager, but now done at the start
        var devices = new List<InputDevice>();
        foreach (var g in Gamepad.all) devices.Add(g);
        foreach (var j in Joystick.all) devices.Add(j);

        if (devices.Count < 2)
        {
            p0 = null; p1 = null;
            return false;
        }

        p0 = devices[0];
        p1 = devices[1];
        return true;
    }
}
