using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CanvasScaler(ScaleWithScreenSize)가 계산한 스케일이 최솟값 미만으로 떨어지지 않도록 보호합니다.
/// CanvasScaler와 같은 Canvas GameObject에 추가하세요.
///
/// ScaleWithScreenSize 모드에서 CanvasScaler는 scaleFactor를 자동으로 관리합니다.
/// 이 컴포넌트는 LateUpdate에서 그 결과를 읽어 minScale 미만이면 강제로 끌어올립니다.
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class SafeCanvasScaler : MonoBehaviour
{
    [Tooltip("허용할 최소 UI 스케일. 이 값 이하로는 줄어들지 않습니다.")]
    [SerializeField] private float minScale = 0.4f;

    private Canvas _canvas;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
    }

    // CanvasScaler가 Update에서 scaleFactor를 쓰므로 LateUpdate에서 clamp합니다.
    private void LateUpdate()
    {
        if (_canvas == null) return;
        if (_canvas.scaleFactor < minScale)
            _canvas.scaleFactor = minScale;
    }
}
