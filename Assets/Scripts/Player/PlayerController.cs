using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>WASD 이동 + Space 대시. InputAction을 코드에서 직접 생성합니다.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("대시")]
    [SerializeField] private float dashSpeed    = 18f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;

    private Rigidbody2D _rb;
    private InputAction _moveAction;
    private InputAction _dashAction;
    private bool        _isDashing;
    private float       _dashCooldownTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _moveAction = new InputAction("Move", InputActionType.Value);
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/w")
            .With("Down",  "<Keyboard>/s")
            .With("Left",  "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Up",    "<Keyboard>/upArrow")
            .With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        _dashAction = new InputAction("Dash", InputActionType.Button);
        _dashAction.AddBinding("<Keyboard>/space");

        _moveAction.Enable();
        _dashAction.Enable();
        _dashAction.performed += OnDash;
    }

    private void OnDestroy()
    {
        _moveAction?.Disable();
        _dashAction?.Disable();
        if (_dashAction != null) _dashAction.performed -= OnDash;
    }

    private void Update()
    {
        if (_dashCooldownTimer > 0f) _dashCooldownTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (_isDashing) return;
        _rb.linearVelocity = _moveAction.ReadValue<Vector2>() * moveSpeed;
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (_isDashing || _dashCooldownTimer > 0f) return;
        Vector2 dir = _moveAction.ReadValue<Vector2>();
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
        StartCoroutine(DashRoutine(dir.normalized));
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        _isDashing         = true;
        _dashCooldownTimer = dashCooldown;
        _rb.linearVelocity = direction * dashSpeed;
        yield return new WaitForSeconds(dashDuration);
        _isDashing = false;
    }
}
