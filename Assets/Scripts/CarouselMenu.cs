using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;


public class CarouselMenu : MonoBehaviour
{
    public List<RectTransform> bosses;
    public float spacing = 200f;
    public float verticalOffset = 30f;
    public float animationSpeed = 10f;

    private float currentIndex = 0f;
    private int targetIndex = 0;
    private float currentIndexVelocity;

    private void Awake()
    {
        if (bosses == null || bosses.Count == 0)
        {
            bosses = new List<RectTransform>();
            foreach (RectTransform child in transform)
            {
                if (child.GetComponent<Button>() != null)
                    bosses.Add(child);
            }
        }
    }

    private void Start()
    {
        EventSystem.current.SetSelectedGameObject(bosses[0].gameObject);
        return;
    }

    void Update()
    {
        UpdateTargetFromEventSystem();
        AnimateLayout();
    }

    void UpdateTargetFromEventSystem()
    {
        // Select new button
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return; // :(

        for (int i = 0; i < bosses.Count; i++)
        {
            if (bosses[i].gameObject == selected)
            {
                targetIndex = i;
                break;
            }
        }
    }
    
    public void OnConfirmPressed()
    {
        var state = GameFlowManager.Instance.State;
        var cfg = state.boss;

        var randHealth = (int)Random.Range(-6f, 6f);
        var randAttack = (int)Random.Range(-2f, 2f);

        UnityEngine.Debug.Log($"Random Health: {randHealth}     Random Attack: {randAttack}");

        if (targetIndex == 0)
        {
            // Mole is tank
            cfg.attack = (int)(state.hero.health/10) + randAttack;
            cfg.health = state.hero.attack*10 + randHealth;  // 10 hits to die
            cfg.jump = 18;
            cfg.speed = 8;
        }
        else if (targetIndex == 1)
        {
            // Clouds is glass cannon
            cfg.attack = (int)(state.hero.health/6) + randAttack;
            cfg.health = state.hero.attack*6 + randHealth;   // 6 hits to die
            cfg.jump = 25;
            cfg.speed = 17;
        }
        else
        {
            // Map 2 is balanced
            cfg.attack = (int)(state.hero.health/8) + randAttack;
            cfg.health = state.hero.attack*8 + randHealth;   // 8 hits to die
            cfg.jump = 20;
            cfg.speed = 12;
        }
        
        
        // Get the selected boss
        RectTransform selectedBoss = bosses[targetIndex];

        // Optionally get its name or some identifier
        string bossName = selectedBoss.name;

        Debug.Log("Selected boss: " + bossName);
        GameFlowManager.Instance.GoBossToStageMod(targetIndex);
    }

    void AnimateLayout()
    {
        // Move smoothly to newly selected boss button
        currentIndex = Mathf.SmoothDamp(currentIndex, targetIndex, ref currentIndexVelocity, 0.15f);


        for (int i = 0; i < bosses.Count; i++)
        {
            // Target boss button is either left or right * spacing with small vertical offset
            Vector2 targetPos = new Vector2(
                (i - currentIndex) * spacing,
                -Mathf.Abs(i - currentIndex) * verticalOffset
            );

            // Lerp button i to target position
            bosses[i].anchoredPosition = Vector2.Lerp(
                bosses[i].anchoredPosition,
                targetPos,
                Time.deltaTime * animationSpeed
            );

            // Scale button i
            float distance = Mathf.Abs(i - currentIndex);
            float scale = 1f - distance * 0.15f;
            bosses[i].localScale = Vector3.Lerp(
                bosses[i].localScale,
                Vector3.one * Mathf.Clamp(scale, 0.6f, 1f),
                Time.deltaTime * animationSpeed
            );

            // Show or hide boss description
            Transform description = bosses[i].Find("Description");
            if (description != null)
                description.gameObject.SetActive(i == targetIndex);
        }
    }
}
