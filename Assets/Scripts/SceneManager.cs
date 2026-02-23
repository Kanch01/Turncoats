using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("References (attached on Root object)")]
    [SerializeField] private GameState state;                 // drag your attached GameState here
    [SerializeField] private InputLockManager inputLock;      // drag your attached InputLockManager here
    public GameState State => state;

    [Header("Root Safety")]
    [SerializeField] private string rootSceneName = "Root";   // set to your root scene asset name

    [Header("Scene Names")]
    [SerializeField] private string mainMenu = "StartMenu";
    [SerializeField] private string playerSelect = "PlayerSelect";
    [SerializeField] private string statsMenu = "HeroStats";
    [SerializeField] private string weaponMenu = "HeroWeapon";
    [SerializeField] private string abilityMenu = "HeroAbility";
    [SerializeField] private string bossSelect = "BossSelect";
    [SerializeField] private string stageMod = "StageMod";
    [SerializeField] private string battle = "Battle";

    [SerializeField] private Camera rootCamera;

    [Header("Runtime")]
    public int roleChooserPlayerIndex = -1;

    private string currentLoadedContentScene;
    private bool isLoading;

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
        state.ResetRun();

        if (!TryAssignTwoDevices(out var p0, out var p1))
        {
            Debug.LogError("Need two controllers connected (Gamepad or Joystick).");
            return;
        }

        state.player0Device = p0;
        state.player1Device = p1;

        roleChooserPlayerIndex = Random.Range(0, 2);
        _ = StartCoroutine(LoadContentScene(playerSelect));
    }

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

        if (chooserIsHero)
        {
            state.player0Role = (chooser == 0) ? Role.Hero : Role.Boss;
            state.player1Role = (chooser == 1) ? Role.Hero : Role.Boss;
        }
        else
        {
            state.player0Role = (chooser == 0) ? Role.Boss : Role.Hero;
            state.player1Role = (chooser == 1) ? Role.Boss : Role.Hero;
        }

        state.hero.playerInd = state.GetPlayerIndexForRole(Role.Hero);
        state.boss.playerInd = state.GetPlayerIndexForRole(Role.Boss);
    }

    public void GoStatsToWeapon() => _ = StartCoroutine(LoadContentScene(weaponMenu));
    public void GoWeaponToAbility() => _ = StartCoroutine(LoadContentScene(abilityMenu));
    public void GoAbilityToBoss() => _ = StartCoroutine(LoadContentScene(bossSelect));
    public void GoBossToStageMod() => _ = StartCoroutine(LoadContentScene(stageMod));
    public void GoStageModToBattle() => _ = StartCoroutine(LoadContentScene(battle));
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
        if (inputLock == null || state == null) return;

        if (activeScene == mainMenu)
        {
            inputLock.EnableAllKnownDevices(state);
            return;
        }

        if (activeScene == playerSelect)
        {
            var chooserDevice = state.GetDeviceForPlayerIndex(roleChooserPlayerIndex);
            inputLock.LockToOnlyDevice(state, chooserDevice);
            return;
        }

        if (activeScene == statsMenu || activeScene == weaponMenu || activeScene == abilityMenu)
        {
            var heroDevice = state.GetDeviceForRole(Role.Hero);
            inputLock.LockToOnlyDevice(state, heroDevice);
            return;
        }

        if (activeScene == bossSelect || activeScene == stageMod)
        {
            var bossDevice = state.GetDeviceForRole(Role.Boss);
            inputLock.LockToOnlyDevice(state, bossDevice);
            return;
        }

        if (activeScene == battle)
        {
            inputLock.EnableAllKnownDevices(state);
            return;
        }

        inputLock.EnableAllKnownDevices(state);
    }

    private void SetCameraForActiveScene()
    {
        var active = SceneManager.GetActiveScene().name;

        if (active == stageMod || active == battle)
            SetCameraEnabled(rootCamera, false);
        else
            SetCameraEnabled(rootCamera, true);
    }

    private void SetCameraEnabled(Camera cam, bool enabled)
    {
        if (cam == null) return;
        cam.enabled = enabled;

        var al = cam.GetComponent<AudioListener>();
        if (al != null) al.enabled = enabled;
    }

    private bool IsSceneLoaded(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).name == name) return true;
        return false;
    }

    private bool TryAssignTwoDevices(out InputDevice p0, out InputDevice p1)
    {
        var devices = new List<InputDevice>();
        foreach (var g in Gamepad.all) devices.Add(g);
        foreach (var j in Joystick.all) devices.Add(j);

        if (devices.Count < 2)
        {
            p0 = null;
            p1 = null;
            return false;
        }

        p0 = devices[0];
        p1 = devices[1];
        return true;
    }
}
