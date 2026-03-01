using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
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

    [Header("Ground Check")]
    [SerializeField] public Transform groundCheck;
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

    private float _moveX;

    private bool _isGrounded;
    private bool _isDashing;
    private bool _canDash = true;

    private bool _isAttacking;
    private bool _attackMoveLocked;
    
    private Vector3 _baseScale;
    private float _facingSign = 1f;
    private Vector2 actingKnockbackForce = Vector2.zero; 

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerInput = GetComponent<PlayerInput>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Get actions from player's PlayerInput
        _moveAction = _playerInput.actions["Move"];
        _jumpAction = _playerInput.actions["Jump"];
        _dashAction = _playerInput.actions["Dash"];
        _attackAction = _playerInput.actions["Attack"];
        
        _baseScale = transform.localScale;
        _facingSign = Mathf.Sign(_baseScale.x);
        if (_facingSign == 0f) _facingSign = 1f;
    }

    private void OnEnable()
    {
        _jumpAction.performed += OnJump;
        _dashAction.performed += OnDash;
        _attackAction.performed += OnAttack;
    }

    private void OnDisable()
    {
        _jumpAction.performed -= OnJump;
        _dashAction.performed -= OnDash;
        _attackAction.performed -= OnAttack;
    }

    private void Update()
    {
        Vector2 move = _moveAction.ReadValue<Vector2>();
        _moveX = Mathf.Clamp(move.x, -1f, 1f);
        
        _isGrounded = CheckGrounded();

        // Update animations every frame
        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        if (_isDashing)
            return;

        // If attacking lock movement briefly
        float effectiveMoveX = _attackMoveLocked ? 0f : _moveX;
        
        _rb.linearVelocity = new Vector2(
            effectiveMoveX * moveSpeed + actingKnockbackForce.x,
            _rb.linearVelocity.y + actingKnockbackForce.y
        );

        // Add knockback force
        // _rb.AddForce(actingKnockbackForce, ForceMode2D.Force);

        // Face direction
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
        if (_isDashing) return;
        if (_isAttacking) return;
        if (!_isGrounded) return;
        
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
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

        // No vertical velocity on dash
        _rb.linearVelocity = new Vector2(0f, 0f);

        float t = 0f;
        while (t < dashDuration)
        {
            _rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        _isDashing = false;

        // cooldown
        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
    }

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        if (_isAttacking) return;

        StartCoroutine(AttackRoutine());
    }


    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        // Fire the attack animation once
        animator.ResetTrigger(AttackTrigger);
        animator.SetTrigger(AttackTrigger);

        // Lock movement during hit
        _attackMoveLocked = true;
        yield return new WaitForSeconds(attackLockoutMoveTime);
        _attackMoveLocked = false;
        
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            yield return null;

        // Wait until Attack animation finishes
        while (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack") &&
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        _isAttacking = false;
    }

    /// <summary>
    /// Check whether player is on ground
    /// </summary>
    private bool CheckGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        // Attacking overrides everything
        
        float speedForAnim;

        if (_isAttacking)
        {
            // speedForAnim = 0f;
            animator.SetFloat(SpeedParam, 0f);
            return;
        }
        else if (!_isGrounded)
        {
            speedForAnim = 0f; 
        }
        else if (_isDashing)
        {
            speedForAnim = 1f;
        }
        else
        {
            speedForAnim = Mathf.Abs(_moveX);
        }

        animator.SetFloat("Speed", speedForAnim);
    }

    public void AddActingKnockbackForce(Vector2 knockback)
    {
        actingKnockbackForce += knockback;
    }

    public void SetActingKnockbackForce(Vector2 knockback)
    {
        actingKnockbackForce = knockback;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
#endif
}
