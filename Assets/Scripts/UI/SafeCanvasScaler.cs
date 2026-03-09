using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CanvasScaler를 보완해 화면이 너무 작아질 때 최소 스케일을 보장합니다.
/// Canvas → CanvasScaler와 함께 이 컴포넌트를 추가하세요.
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class SafeCanvasScaler : MonoBehaviour
{
    [Tooltip("허용할 최소 UI 스케일. 이 값 이하로는 줄어들지 않습니다.")]
    [SerializeField] private float minScale = 0.5f;

    private CanvasScaler _scaler;
    private Canvas       _canvas;

    private void Awake()
    {
        _scaler = GetComponent<CanvasScaler>();
        _canvas = GetComponent<Canvas>();
    }

    private void Update()
    {
        if (_canvas == null) return;
        float current = _canvas.scaleFactor;
        if (current < minScale)
            _canvas.scaleFactor = minScale;
    }
}
