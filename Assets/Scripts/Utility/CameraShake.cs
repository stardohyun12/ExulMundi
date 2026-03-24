using UnityEngine;
using System.Collections;

/// <summary>
/// 카메라 셰이크 유틸리티.
/// PlayerHealth.OnHealthChanged를 구독해 HP 감소 시 자동으로 셰이크합니다.
/// 외부에서 Shake()를 직접 호출할 수도 있습니다.
/// </summary>
public class CameraShake : MonoBehaviour
{
    private const float DefaultDuration  = 0.18f;
    private const float DefaultMagnitude = 0.12f;

    public static CameraShake Instance { get; private set; }

    [SerializeField] private float defaultDuration  = DefaultDuration;
    [SerializeField] private float defaultMagnitude = DefaultMagnitude;

    private Vector3   _originalPosition;
    private Coroutine _shakeCoroutine;
    private int       _lastHP = int.MaxValue;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _originalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            _lastHP = ph.CurrentHP;
            ph.OnHealthChanged += OnHealthChanged;
        }
    }

    private void OnDisable()
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
            ph.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int current, int max)
    {
        if (current < _lastHP)
            Shake(defaultDuration, defaultMagnitude);
        _lastHP = current;
    }

    /// <summary>카메라를 지정한 지속시간과 강도로 흔듭니다.</summary>
    public void Shake(float duration, float magnitude)
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dampened         = magnitude * (1f - elapsed / duration);
            transform.localPosition = _originalPosition + (Vector3)Random.insideUnitCircle * dampened;
            elapsed               += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = _originalPosition;
        _shakeCoroutine         = null;
    }
}
