using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WASD 이동 + Space 대시. XZ 평면에서 움직이며 Y축은 물리에 맡깁니다.
/// Rigidbody (3D) 기반. 카메라는 위에서 내려다보는 탑다운 시점을 가정합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("대시")]
    [SerializeField] private float dashSpeed    = 18f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;

    private Rigidbody   _rb;
    private InputAction _moveAction;
    private InputAction _dashAction;
    private bool        _isDashing;
    private float       _dashCooldownTimer;

    /// <summary>대시 시작 시 발생합니다. 파라미터는 대시 방향(월드 공간).</summary>
    public event Action<Vector3> OnDashStarted;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // XZ 평면 이동 — Rigidbody가 회전하지 않도록 제약
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

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

        // Vector2 입력(XY)을 XZ 평면으로 변환
        Vector2 input  = _moveAction.ReadValue<Vector2>();
        Vector3 moveDir = new Vector3(input.x, 0f, input.y);

        // Y축 속도(중력)는 유지하고 XZ만 제어
        _rb.linearVelocity = new Vector3(
            moveDir.x * moveSpeed,
            _rb.linearVelocity.y,
            moveDir.z * moveSpeed);
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (_isDashing || _dashCooldownTimer > 0f) return;

        Vector2 input = _moveAction.ReadValue<Vector2>();
        Vector3 dir   = input.sqrMagnitude > 0.01f
            ? new Vector3(input.x, 0f, input.y).normalized
            : Vector3.forward;

        StartCoroutine(DashRoutine(dir));
    }

    private IEnumerator DashRoutine(Vector3 direction)
    {
        _isDashing         = true;
        _dashCooldownTimer = dashCooldown;
        _rb.linearVelocity = direction * dashSpeed;
        OnDashStarted?.Invoke(direction);
        yield return new WaitForSeconds(dashDuration);
        _isDashing = false;
    }
}
