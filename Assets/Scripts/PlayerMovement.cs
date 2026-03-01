using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Stats (runtime from GameState)")]
    [SerializeField] private int attack = 1;
    [SerializeField] private int health = 10;

    // These already existed as floats; we will overwrite them from config at runtime:
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpImpulse = 12f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Attack")]
    [SerializeField] private float attackLockoutMoveTime = 0.15f;
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    [Header("Parry")]
    [SerializeField] private float parryWindow = 1f;
    [SerializeField] private float parryCooldown = 0.5f;

    // When parry succeeds, we "always hit" by applying damage directly after this delay.
    [SerializeField] private float parryCounterHitDelay = 0.08f;

    // How long we keep the attacker frozen at most (safety fallback).
    [SerializeField] private float parryFreezeMaxDuration = 0.35f;

    [SerializeField] private SpriteRenderer spriteRenderer; // optional; auto-found if null

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody2D _rb;
    private PlayerInput _playerInput;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;
    private InputAction _attackAction;
    private InputAction _parryAction;

    private float _moveX;

    private bool _isGrounded;
    private bool _isDashing;
    private bool _canDash = true;

    private bool _isAttacking;
    private bool _attackMoveLocked;

    // Parry state
    private bool _isParrying;
    private bool _parryOnCooldown;
    private Coroutine _parryRoutine;
    private Coroutine _parryCooldownRoutine;

    // Freeze state (used when YOU are the attacker frozen by someone else's parry)
    private bool _isFrozenByParry;

    private Vector3 _baseScale;
    private float _facingSign = 1f;

    private Color _originalColor;
    private bool _hasOriginalColor;

    // ---- Public read access for other systems (damage, UI, etc.) ----
    public int Attack => attack;
    public int Health => health;
    public float MoveSpeed => moveSpeed;
    public float JumpImpulse => jumpImpulse;

    public bool IsParrying => _isParrying;

    /// <summary>
    /// Call this immediately after spawning to overwrite stats from GameState.
    /// </summary>
    public void ApplyConfig(PlayerConfig cfg)
    {
        if (cfg == null) return;

        attack = cfg.attack;
        health = cfg.health;

        moveSpeed = cfg.speed;
        jumpImpulse = cfg.jump;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerInput = GetComponent<PlayerInput>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            _originalColor = spriteRenderer.color;
            _hasOriginalColor = true;
        }

        _moveAction = _playerInput.actions["Move"];
        _jumpAction = _playerInput.actions["Jump"];
        _dashAction = _playerInput.actions["Dash"];
        _attackAction = _playerInput.actions["Attack"];
        _parryAction = _playerInput.actions["Parry"];

        _baseScale = transform.localScale;
        _facingSign = Mathf.Sign(_baseScale.x);
        if (_facingSign == 0f) _facingSign = 1f;
    }

    private void OnEnable()
    {
        _jumpAction.performed += OnJump;
        _dashAction.performed += OnDash;
        _attackAction.performed += OnAttack;

        if (_parryAction != null)
            _parryAction.performed += OnParry;
    }

    private void OnDisable()
    {
        _jumpAction.performed -= OnJump;
        _dashAction.performed -= OnDash;
        _attackAction.performed -= OnAttack;

        if (_parryAction != null)
            _parryAction.performed -= OnParry;
    }

    private void Update()
    {
        Vector2 move = _moveAction.ReadValue<Vector2>();
        _moveX = Mathf.Clamp(move.x, -1f, 1f);

        _isGrounded = CheckGrounded();
        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        // If you are frozen by someone else's parry, you do nothing.
        if (_isFrozenByParry)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // If you are dashing, keep dash control.
        if (_isDashing)
            return;

        // If parrying: remain idle (no horizontal motion)
        if (_isParrying)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }

        float effectiveMoveX = _attackMoveLocked ? 0f : _moveX;

        _rb.linearVelocity = new Vector2(effectiveMoveX * moveSpeed, _rb.linearVelocity.y);

        if (effectiveMoveX > 0.01f) _facingSign = 1f;
        else if (effectiveMoveX < -0.01f) _facingSign = -1f;

        transform.localScale = new Vector3(
            Mathf.Abs(_baseScale.x) * _facingSign,
            _baseScale.y,
            _baseScale.z
        );
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (_isFrozenByParry) return;
        if (_isParrying) return;

        if (_isDashing) return;
        if (_isAttacking) return;
        if (!_isGrounded) return;

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (_isFrozenByParry) return;
        if (_isParrying) return;

        if (_isDashing) return;
        if (!_canDash) return;

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        _canDash = false;
        _isDashing = true;

        float dir = Mathf.Abs(_moveX) > 0.01f ? Mathf.Sign(_moveX) : Mathf.Sign(transform.localScale.x);
        if (dir == 0) dir = 1;

        _rb.linearVelocity = new Vector2(0f, 0f);

        float t = 0f;
        while (t < dashDuration)
        {
            _rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        _isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
    }

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        if (_isFrozenByParry) return;
        if (_isParrying) return;

        if (_isAttacking) return;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        animator.ResetTrigger(AttackTrigger);
        animator.SetTrigger(AttackTrigger);

        _attackMoveLocked = true;
        yield return new WaitForSeconds(attackLockoutMoveTime);
        _attackMoveLocked = false;

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            yield return null;

        while (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack") &&
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        _isAttacking = false;
    }
    
    private void OnParry(InputAction.CallbackContext ctx)
    {
        if (_parryOnCooldown) return;
        if (_isParrying) return;

       
        if (_isDashing) return;

        StartParry();
    }

    private void StartParry()
    {
        // If something was running, stop 
        if (_parryRoutine != null)
        {
            StopCoroutine(_parryRoutine);
            _parryRoutine = null;
        }

        _isParrying = true;

        // Visual + idle lock
        _attackMoveLocked = true;
        if (spriteRenderer != null)
        {
            if (!_hasOriginalColor)
            {
                _originalColor = spriteRenderer.color;
                _hasOriginalColor = true;
            }
            spriteRenderer.color = Color.black;
        }

        _parryRoutine = StartCoroutine(ParryWindowRoutine());
    }

    private IEnumerator ParryWindowRoutine()
    {
        yield return new WaitForSeconds(parryWindow);

        EndParryVisuals();
        _parryRoutine = null;

        // Start cooldown after a normal parry
        StartParryCooldown();
    }   

    private void EndParryVisuals()
    {
        _isParrying = false;
        _attackMoveLocked = false;

        if (spriteRenderer != null && _hasOriginalColor)
            spriteRenderer.color = _originalColor;
    }

    private void StartParryCooldown()
    {
        // Always ensure cooldown gets reset
        if (_parryCooldownRoutine != null)
            StopCoroutine(_parryCooldownRoutine);

        _parryCooldownRoutine = StartCoroutine(ParryCooldownRoutine());
    }

    private IEnumerator ParryCooldownRoutine()
    {
        _parryOnCooldown = true;
        yield return new WaitForSeconds(parryCooldown);
        _parryOnCooldown = false;
        _parryCooldownRoutine = null;
    }
    
    public void HandleParrySuccess(GameObject attacker)
    {
        if (!_isParrying) return;

        // Stop active window coroutine 
        if (_parryRoutine != null)
        {
            StopCoroutine(_parryRoutine);
            _parryRoutine = null;
        }

        EndParryVisuals();
        
        StartParryCooldown();

        // Counter logic
        StartCoroutine(ParryCounterRoutine(attacker));
    }

    private IEnumerator ParryCounterRoutine(GameObject attacker)
    {
        if (attacker == null) yield break;

        // Freeze attacker until counter lands
        var attackerMove = attacker.GetComponent<PlayerMovement>();
        if (attackerMove != null)
            attackerMove.FreezeForParryUntilHit();

        // Optional: play your attack animation as the "counter"
        // (This does not control hitboxes; we apply damage directly.)
        if (animator != null)
        {
            animator.ResetTrigger(AttackTrigger);
            animator.SetTrigger(AttackTrigger);
        }

        float elapsed = 0f;

        // Wait until the counter "hit moment"
        while (elapsed < parryCounterHitDelay)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Always-hit: apply damage directly to attacker
        var attackerHealth = attacker.GetComponent<HealthManager>();
        if (attackerHealth != null)
        {
            attackerHealth.TakeDamage(attack, this.gameObject);
        }

        // Safety: if something goes wrong, don't freeze attacker forever
        float remaining = Mathf.Max(0f, parryFreezeMaxDuration - parryCounterHitDelay);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        if (attackerMove != null)
            attackerMove.ReleaseParryFreeze();
    }
    
    public void FreezeForParryUntilHit()
    {
        _isFrozenByParry = true;
        _rb.linearVelocity = Vector2.zero;
    }

    public void ReleaseParryFreeze()
    {
        _isFrozenByParry = false;
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        // If parrying or frozen, stay idle
        if (_isParrying || _isFrozenByParry)
        {
            animator.SetFloat(SpeedParam, 0f);
            return;
        }

        if (_isAttacking)
        {
            animator.SetFloat(SpeedParam, 0f);
            return;
        }

        float speedForAnim;

        if (!_isGrounded) speedForAnim = 0f;
        else if (_isDashing) speedForAnim = 1f;
        else speedForAnim = Mathf.Abs(_moveX);

        animator.SetFloat("Speed", speedForAnim);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
#endif
}
