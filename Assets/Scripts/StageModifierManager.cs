using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.InputSystem;

public class StageModifierManager : MonoBehaviour
{
    [Header("Placement Settings")]
    public Camera mainCamera;
    public GameObject[] placeablePrefabs;       // Put stage modifier prefabs here
    public float gridSize = 0f;                 // Set >0 for optional grid snapping

    [Header("Input Actions")]
    public InputActionReference moveAction;         // Control to move virtual cursor
    public InputActionReference placeAction;        // Button to place object
    public InputActionReference rotateAction;       // Button to rotate preview
    public InputActionReference nextPrefabAction;   // Button to cycle stage modifiers

    [Header("Cursor Settings")]
    public float controllerCursorSpeed = 1000f;     // Speed of virtual cursor

    [SerializeField] private GameObject spawnPoints;
    [SerializeField] private GameObject playerManager;
    [SerializeField] private GameObject gameCamera;

    private GameObject currentPreview;
    private int selectedIndex = 0;
    private float rotationZ = 0f;           // 2D rotation (around Z axis)
    private Vector2 virtualCursor;
    private PointerEventData pointerData;
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;
    private bool hoveringUI = false;

    void OnEnable()
    {
        // Enable controls and connect callbacks
        moveAction.action.Enable();
        placeAction.action.Enable();
        rotateAction.action.Enable();
        nextPrefabAction.action.Enable();

        placeAction.action.performed += _ => TryPlaceObject();
        rotateAction.action.performed += _ => RotatePreview();
        nextPrefabAction.action.performed += _ => NextPrefab();
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        placeAction.action.Disable();
        rotateAction.action.Disable();
        nextPrefabAction.action.Disable();
    }

    public void OnButtonPress()
    {
        if (gameCamera != null)
        {
            mainCamera.enabled = false;
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
            UnityEngine.Debug.Log("SpawnPoints not found");
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
            UnityEngine.Debug.Log("PlayerManager not found");
        }

        // Disable this button
        gameObject.SetActive(false);
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
        // transition.onClick.AddListener(OnButtonPressed);
    }

    void Update()
    {
        UpdateCursorPosition();

        // Reset hovering flag each frame
        hoveringUI = false;

        // Set the pointer position for UI raycasting
        pointerData.position = virtualCursor;
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        // Handle UI buttons under cursor
        foreach (var r in results)
        {
            Button b = r.gameObject.GetComponent<Button>();
            if (b != null)
            {
                hoveringUI = true;

                // Let EventSystem handle hover/highlight
                if (eventSystem.currentSelectedGameObject != b.gameObject)
                    eventSystem.SetSelectedGameObject(b.gameObject);

                // Trigger button click if submit pressed
                if (placeAction.action.triggered)
                    b.onClick.Invoke();

                break; // Only handle first button under cursor
            }
        }

        // Reset selection if no buttons under cursor
        if (!hoveringUI && eventSystem.currentSelectedGameObject != null)
            eventSystem.SetSelectedGameObject(null);

        // Show/hide preview based on UI hovering
        if (currentPreview != null)
            currentPreview.SetActive(!hoveringUI);

        // Only update preview and allow placement/cycling if not over UI
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
        // Make sure z position is right
        Vector3 screenPos = new Vector3(virtualCursor.x, virtualCursor.y, -mainCamera.transform.position.z);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        // Optional grid snapping
        if (gridSize > 0f)
        {
            worldPos.x = Mathf.Round(worldPos.x / gridSize) * gridSize;
            worldPos.y = Mathf.Round(worldPos.y / gridSize) * gridSize;
        }

        currentPreview.transform.position = worldPos;
        currentPreview.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
    }

    private void TryPlaceObject()
    {
        // Place a new object if it isn't colliding
        if (currentPreview == null) return;

        if (!IsPlacementValid(currentPreview.transform.position, currentPreview))
        {
            UnityEngine.Debug.Log("Can't place here bozo!");
            return;
        }

        Instantiate(placeablePrefabs[selectedIndex], currentPreview.transform.position, currentPreview.transform.rotation, null);
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
        Collider2D col = preview.GetComponent<Collider2D>();
        if (col == null) return true;

        Collider2D[] hits = Physics2D.OverlapBoxAll(col.bounds.center, col.bounds.size, rotationZ);
        foreach (var hit in hits)
        {
            if (!hit.isTrigger && hit.gameObject != preview)
            {
                return false;
            }
        }
        UnityEngine.Debug.Log("Oh yeah, placing!");
        return true;
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