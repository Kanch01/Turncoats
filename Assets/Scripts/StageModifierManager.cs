using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using TMPro;

public class StageModifierManager : MonoBehaviour
{
    [Header("Placement Settings")]
    public Camera stageModCamera;
    public GameObject[] placeablePrefabs;       // Put stage modifier prefabs here
    public float gridSize = 0f;                 // Set >0 for optional grid snapping
    public Tilemap groundTilemap;
    public float budget = 20f;
    public GameObject confirmPanel;

    [Header("Input Actions")]
    public InputActionReference moveAction;         // Control to move virtual cursor
    public InputActionReference placeAction;        // Button to place object
    public InputActionReference rotateAction;       // Button to rotate preview
    public InputActionReference nextPrefabAction;   // Button to cycle stage modifiers
    public InputActionReference buttonAction;       // UI Button to move on to battle

    [Header("Cursor Settings")]
    public float controllerCursorSpeed = 1000f;     // Speed of virtual cursor

    [Header("To Enable")]
    [SerializeField] private GameObject spawnPoints;
    [SerializeField] private GameObject playerManager;
    [SerializeField] private GameObject gameCamera;
    [SerializeField] private GameObject gameUI;

    private GameObject currentPreview;
    private int selectedIndex = 0;
    private float rotationZ = 0f;           // 2D rotation (around Z axis)
    private Vector2 virtualCursor;
    private PointerEventData pointerData;
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;
    private bool hoveringUI = false;
    private TMP_Text budgetText;
    private bool confirming = false;

    void OnEnable()
    {
        // Enable controls and connect callbacks
        moveAction.action.Enable();
        placeAction.action.Enable();
        rotateAction.action.Enable();
        nextPrefabAction.action.Enable();
        buttonAction.action.Enable();

        placeAction.action.performed += _ => TryPlaceObject();
        rotateAction.action.performed += _ => RotatePreview();
        nextPrefabAction.action.performed += _ => NextPrefab();
        buttonAction.action.performed += _ => OnButtonPress();
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        placeAction.action.Disable();
        rotateAction.action.Disable();
        nextPrefabAction.action.Disable();
        buttonAction.action.Disable();
    }

    public void OnButtonPress()
    {
        confirmPanel.SetActive(true);
        confirming = true;
        
        /*
        if (yesButton != null)
        {
            evsystem.SetSelectedGameObject(null);
            evsystem.SetSelectedGameObject(yesButton);

            var btn = yesButton.GetComponent<Button>();
            if (btn != null) btn.Select();
        }
        */

        // Set focus to confirm panel
        // Button yesButton = confirmPanel.transform.Find("YesButton").GetComponent<Button>();
        // EventSystem.current.SetSelectedGameObject(yesButton.gameObject);
    }

    public void ConfirmStartGame()
    {
        confirming = false;
        confirmPanel.SetActive(false);
        UnityEngine.Debug.Log("Pressed button!");
        Destroy(currentPreview);
        var flow = GameFlowManager.Instance;
        if (flow != null && flow.State != null && InputLockManager.Instance != null)
        {
            InputLockManager.Instance.EnableAllKnownDevices(flow.State);
        }
        
        if (gameCamera != null)
        {
            stageModCamera.enabled = false;
            gameCamera.SetActive(true);
            gameCamera.GetComponent<Camera>().enabled = true;
        }
        else
        {
            UnityEngine.Debug.Log("GameCamera not found");
        }
        if (spawnPoints != null)
        {
            spawnPoints.SetActive(true);
        }
        else
        {
            UnityEngine.Debug.LogWarning("SpawnPoints not found");
        }
        if (gameUI != null)
        {
            gameUI.SetActive(true);
        }
        else
        {
            UnityEngine.Debug.LogWarning("UI not found");
        }
        if (playerManager != null)
        { 
            playerManager.SetActive(true);

            // Spawn players
            var pm = playerManager.GetComponent<PlayerManager>();
            if (pm != null)
                pm.StartGame();
        }
        else
        {
            UnityEngine.Debug.LogWarning("PlayerManager not found");
        }

        // Disable this button
        gameObject.SetActive(false);
    }

    public void CancelStartGame()
    {
        Debug.Log("Cancel starting game");
        confirming = false;
        confirmPanel.SetActive(false);
    }

    void Start()
    {
        virtualCursor = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // UI raycasting setup
        eventSystem = EventSystem.current;
        raycaster = GetComponent<Canvas>().GetComponent<GraphicRaycaster>();
        pointerData = new PointerEventData(eventSystem);

        // Enable transition button
        // Button transition = GetComponentInChildren<Button>();
        // transition.onClick.AddListener(OnButtonPress);

        // Use the class name GameObject, not the instance
        budgetText = GameObject.Find("BudgetText").GetComponent<TMP_Text>();

        if (budgetText != null)
            budgetText.text = $"Budget: {budget:F0}";
        else
            UnityEngine.Debug.LogWarning("BudgetText GameObject not found!");
    }

    private void HandleStageModPlacement(List<RaycastResult> results)
    {
        // Handle UI buttons under cursor
        foreach (var r in results)
        {
            Button b = r.gameObject.GetComponent<Button>();
            if (b != null)
            {
                hoveringUI = true;

                if (eventSystem.currentSelectedGameObject != b.gameObject)
                    eventSystem.SetSelectedGameObject(b.gameObject);

                if (placeAction.action.triggered)
                    b.onClick.Invoke();

                break;
            }
        }

        if (!hoveringUI && eventSystem.currentSelectedGameObject != null)
            eventSystem.SetSelectedGameObject(null);

        if (currentPreview != null)
            currentPreview.SetActive(!hoveringUI);

        if (!hoveringUI)
        {
            if (currentPreview == null)
                SpawnPreview();

            if (currentPreview != null)
                UpdatePreviewPosition();

            placeAction.action.Enable();
            rotateAction.action.Enable();
            nextPrefabAction.action.Enable();
        }
        else
        {
            placeAction.action.Disable();
            nextPrefabAction.action.Disable();
        }
    }

    void Update()
    {
        UpdateCursorPosition(); // cursor can still move

        hoveringUI = false;
        pointerData.position = virtualCursor;
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        // Only handle stage-mod placement if NOT confirming
        if (!confirming)
        {
            HandleStageModPlacement(results);
        }
        else
        {
            // Hide preview while confirming
            if (currentPreview != null)
                currentPreview.SetActive(false);

            // Raycast UI while confirming so hovering Yes/No updates selection
            Button hoveredButton = null;
            foreach (var r in results)
            {
                // Only consider buttons that are inside the confirm panel
                if (!r.gameObject.transform.IsChildOf(confirmPanel.transform))
                    continue;

                var b = r.gameObject.GetComponent<Button>();
                if (b != null)
                {
                    hoveredButton = b;
                    break;
                }
            }

            if (hoveredButton != null)
            {
                if (eventSystem.currentSelectedGameObject != hoveredButton.gameObject)
                    eventSystem.SetSelectedGameObject(hoveredButton.gameObject);
            }
            else
            {
                // If nothing is hovered, make sure something is selected (default to Yes)
                if (eventSystem.currentSelectedGameObject == null && confirmPanel.activeSelf)
                {
                    var yes = confirmPanel.transform.Find("YesButton")?.GetComponent<Button>();
                    if (yes != null)
                        eventSystem.SetSelectedGameObject(yes.gameObject);
                }
            }

            // Click whichever confirm button is currently selected (now updated by hover)
            if (placeAction.action.triggered)
            {
                var selected = eventSystem.currentSelectedGameObject?.GetComponent<Button>();
                if (selected != null && selected.interactable)
                    selected.onClick.Invoke();
            }
        }
    }

    private void UpdateCursorPosition()
    {
        // Interpolate virtual cursor
        Vector2 delta = moveAction.action.ReadValue<Vector2>();
        virtualCursor += delta * controllerCursorSpeed * Time.deltaTime;
        virtualCursor.x = Mathf.Clamp(virtualCursor.x, 0, Screen.width);
        virtualCursor.y = Mathf.Clamp(virtualCursor.y, 0, Screen.height);
    }

    private void SpawnPreview()
    {
        // Preview is a clone of the selcted prefab but a bit transparent
        currentPreview = Instantiate(placeablePrefabs[selectedIndex], Vector3.zero, Quaternion.identity, null); // Null makes it under root
        SetPreviewMaterial(currentPreview);
    }

    private void UpdatePreviewPosition()
    {
        // Convert virtual cursor to world position
        Vector3 screenPos = new Vector3(virtualCursor.x, virtualCursor.y, -stageModCamera.transform.position.z);
        Vector3 worldPos = stageModCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        if (currentPreview == null) return;

        var placement = currentPreview.GetComponentInChildren<StageModifierPlacement>();

        if (placement != null)
        {
            placement.groundTilemap = groundTilemap;
        }
        if (placement != null && placement.placementMode == PlacementMode.Grid && placement.groundTilemap != null)
        {
            Vector3 targetPos = worldPos;

            // If the placement is on a child, keep offset
            if (placement.transform != currentPreview.transform)
            {
                Vector3 offset = currentPreview.transform.position - placement.transform.position;
                targetPos -= offset;
                Vector3Int cellPos = placement.groundTilemap.WorldToCell(targetPos);
                targetPos = placement.groundTilemap.GetCellCenterWorld(cellPos) + offset;
            }
            else
            {
                // Root prefab: snap directly
                Vector3Int cellPos = placement.groundTilemap.WorldToCell(targetPos);
                targetPos = placement.groundTilemap.GetCellCenterWorld(cellPos);
            }

            worldPos = targetPos;
        }
        else if (gridSize > 0f)
        {
            worldPos.x = Mathf.Round(worldPos.x / gridSize) * gridSize;
            worldPos.y = Mathf.Round(worldPos.y / gridSize) * gridSize;
        }

        currentPreview.transform.position = worldPos;
        currentPreview.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
    }

    private void TryPlaceObject()
    {
        if (confirming)
            return;

        // Place a new object if it isn't colliding
        if (currentPreview == null) return;

        if (!IsPlacementValid(currentPreview.transform.position, currentPreview))
        {
            UnityEngine.Debug.Log("Can't place here bozo...");
            return;
        }
        UnityEngine.Debug.Log("Oh yeah, placing!");

        GameObject placed = Instantiate(placeablePrefabs[selectedIndex], currentPreview.transform.position, currentPreview.transform.rotation, null);

        // Disable the tiles under the placed object
        SpriteRenderer rend = placed.GetComponent<SpriteRenderer>();
        StageModifierPlacement placement = placed.GetComponent<StageModifierPlacement>();
        if (rend != null && placement.mustBeOnTile)
        {
            DisableTilesUnderObject(groundTilemap, rend);
        }
    }

    private void RotatePreview()
    {
        rotationZ += 90f; // Rotate 90 degrees per press
        rotationZ %= 360f;
    }

    private void NextPrefab()
    {
        // Cycle to next using prefab cycle button
        selectedIndex = (selectedIndex + 1) % placeablePrefabs.Length;
        if (currentPreview != null)
            Destroy(currentPreview);
        SpawnPreview();
    }

    private bool IsPlacementValid(Vector3 position, GameObject preview)
    {
        // Only check collisions if the stage mod wants it
        var placement = preview.GetComponentInChildren<StageModifierPlacement>();
        if (placement == null || !placement.ignoreCollisions)
        {
            // Get all colliders on root and children
            Collider2D[] cols = preview.GetComponentsInChildren<Collider2D>();
            foreach (var col in cols)
            {
                // Check for overlapping non-trigger colliders
                Collider2D[] hits = Physics2D.OverlapBoxAll(col.bounds.center, col.bounds.size, rotationZ);
                foreach (var hit in hits)
                {
                    // Ignore the stage mod's own colliders
                    if (!hit.isTrigger && System.Array.IndexOf(cols, hit) == -1)
                        return false;
                }
            }
        }

        // Tilemap requirement for some stage mods
        if (placement != null)
        {
            placement.groundTilemap = groundTilemap;
        }
        if (placement != null && placement.mustBeOnTile && placement.groundTilemap != null)
        {
            Vector3Int cellPos = placement.groundTilemap.WorldToCell(position);
            if (!placement.groundTilemap.HasTile(cellPos))
                return false;
        }

        // Check budget before placing
        StageModifier script = preview.GetComponentInChildren<StageModifier>();
        if (script != null)
        {
            float new_budget = budget - script.data.cost;
            if (new_budget < 0.0)
            {
                UnityEngine.Debug.Log("You broke!");
                return false;
            }
            else
            {
                budget = new_budget;
                budgetText.text = $"Budget: {budget:F0}";
            }
        }

        return true;
    }

    public void DisableTilesUnderObject(Tilemap tilemap, SpriteRenderer rend)
{
    Bounds bounds = rend.bounds;

    // Iterate over all tiles that the bounds might cover
    Vector3Int minCell = tilemap.WorldToCell(bounds.min);
    Vector3Int maxCell = tilemap.WorldToCell(bounds.max);

    for (int x = minCell.x; x <= maxCell.x; x++)
    {
        for (int y = minCell.y; y <= maxCell.y; y++)
        {
            Vector3Int cellPos = new Vector3Int(x, y, 0);
            Vector3 cellWorldPos = tilemap.GetCellCenterWorld(cellPos);

            // Only remove tile if its center is inside the bounds
            if (bounds.Contains(cellWorldPos))
            {
                tilemap.SetTile(cellPos, null);
            }
        }
    }
}

    private void SetPreviewMaterial(GameObject preview)
    {
        foreach (var rend in preview.GetComponentsInChildren<SpriteRenderer>())
        {
            Color c = rend.color;
            c.a = 0.5f;
            rend.color = c;
        }
    }
}