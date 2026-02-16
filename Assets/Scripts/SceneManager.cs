using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Phase { StartMenu, Battle }

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }
    public GameState State { get; private set; }

    [Header("Scene Names")]
    [SerializeField] string mainMenu = "StartMenu";
    [SerializeField] string statsMenu = "HeroStats";
    [SerializeField] string weaponMenu = "HeroWeapon";
    [SerializeField] string abilityMenu = "HeroAbility";
    [SerializeField] string battle = "Battle";

    string currentLoadedContentScene; 
    bool isLoading;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create/attach GameState
        var stateObj = new GameObject("GameState");
        State = stateObj.AddComponent<GameState>();
        DontDestroyOnLoad(stateObj);
    }

    IEnumerator Start()
    {
        yield return LoadContentScene(mainMenu);
    }

    public void StartNewGame()
    {
        State.ResetRun();
        _ = StartCoroutine(LoadContentScene(statsMenu));
    }

    public void GoStatsToWeapon() => _ = StartCoroutine(LoadContentScene(weaponMenu));
    public void GoWeaponToAbility() => _ = StartCoroutine(LoadContentScene(abilityMenu));
    public void GoAbilityToBattle() => _ = StartCoroutine(LoadContentScene(battle));
    public void BackToMainMenu() => _ = StartCoroutine(LoadContentScene(mainMenu));

    IEnumerator LoadContentScene(string next)
    {
        if (isLoading) yield break;
        isLoading = true;
        
        if (!IsSceneLoaded(next))
        {
            var loadOp = SceneManager.LoadSceneAsync(next, LoadSceneMode.Additive);
            while (!loadOp.isDone) yield return null;
        }
        
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(next));
        
        if (!string.IsNullOrEmpty(currentLoadedContentScene) && currentLoadedContentScene != next)
        {
            var unloadOp = SceneManager.UnloadSceneAsync(currentLoadedContentScene);
            while (unloadOp != null && !unloadOp.isDone) yield return null;
        }

        currentLoadedContentScene = next;

        isLoading = false;
    }

    bool IsSceneLoaded(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).name == name) return true;
        return false;
    }
}
