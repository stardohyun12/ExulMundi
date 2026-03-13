using UnityEngine;

/// <summary>
/// 카메라가 항상 지정된 기준 해상도(16:9)를 기준으로 일정한 월드 영역을 보여주도록 합니다.
/// 화면이 더 넓으면 좌우가 더 보이고, 더 좁으면 letterbox 없이 세로 기준으로 유지됩니다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraAspectLocker : MonoBehaviour
{
    [Tooltip("기준 해상도 너비 (기본 1920)")]
    [SerializeField] private float referenceWidth = 1920f;

    [Tooltip("기준 해상도 높이 (기본 1080)")]
    [SerializeField] private float referenceHeight = 1080f;

    [Tooltip("기준 해상도에서의 orthographicSize (기본 6)")]
    [SerializeField] private float referenceOrthographicSize = 6f;

    private Camera _camera;
    private float _lastAspect;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        ApplyAspect();
    }

    private void Update()
    {
        float currentAspect = (float)Screen.width / Screen.height;
        if (!Mathf.Approximately(currentAspect, _lastAspect))
            ApplyAspect();
    }

    /// <summary>화면 종횡비에 맞춰 orthographicSize를 재계산합니다.</summary>
    private void ApplyAspect()
    {
        float currentAspect = (float)Screen.width / Screen.height;
        float referenceAspect = referenceWidth / referenceHeight;

        // 현재 화면이 기준보다 좁으면 세로 기준을 유지하면서 orthographicSize를 키워
        // 동일한 세로 범위를 보장합니다.
        if (currentAspect >= referenceAspect)
        {
            // 더 넓은 화면 → 세로 기준 유지, 가로만 더 보임
            _camera.orthographicSize = referenceOrthographicSize;
        }
        else
        {
            // 더 좁은 화면 → 기준 가로 범위를 유지하기 위해 세로를 키움
            _camera.orthographicSize = referenceOrthographicSize * (referenceAspect / currentAspect);
        }

        _lastAspect = currentAspect;
    }
}
