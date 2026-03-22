using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 오트보이(oatboy) 튜토리얼 방식의 픽셀아트 렌더링 구현체.
/// 씬을 저해상도 RenderTexture에 렌더링한 뒤,
/// FilterMode.Point 쿼드 카메라로 업스케일해 크런치한 도트 룩을 만듭니다.
///
/// 참고: https://oatboy.medium.com/tutorial-easy-lo-fi-pixel-goodness-in-unity-dc8fc999d2de
///       https://oatboy.medium.com/tutorial-pixel-perfect-crunchy-3d-rendering-3f1ba5e97fa7
/// </summary>
[RequireComponent(typeof(Camera))]
public class PixelArtCamera : MonoBehaviour
{
    [Header("픽셀 설정")]
    [Tooltip("렌더링 세로 해상도. 가로는 종횡비에서 자동 계산됩니다.")]
    [SerializeField, Min(1)] private int pixelHeight = 180;

    [Tooltip("카메라 위치를 픽셀 그리드에 정렬해 서브픽셀 지터를 제거합니다.")]
    [SerializeField] private bool pixelSnapping = true;

    // 블릿 쿼드를 올릴 레이어. TransparentFX(1)를 기본값으로 사용합니다.
    // 해당 레이어가 씬에서 이미 사용 중이라면 다른 미사용 레이어 번호로 변경하세요.
    private const int BlitLayer = 1;

    private Camera        _mainCamera;
    private RenderTexture _pixelRT;
    private GameObject    _blitRig;
    private Material      _blitMaterial;
    private Transform     _quadTransform;

    private int _prevScreenWidth;
    private int _prevScreenHeight;

    // ── 라이프사이클 ─────────────────────────────────────────────────────────

    private void Awake()
    {
        _mainCamera = GetComponent<Camera>();
        ConfigureMainCamera();
        CreateBlitRig();
        RebuildRenderTexture();
    }

    private void LateUpdate()
    {
        if (Screen.width != _prevScreenWidth || Screen.height != _prevScreenHeight)
            RebuildRenderTexture();

        if (pixelSnapping && _mainCamera.orthographic)
            ApplyPixelSnapping();
    }

    private void OnDestroy()
    {
        _mainCamera.targetTexture = null;

        if (_pixelRT      != null) { _pixelRT.Release(); Destroy(_pixelRT); }
        if (_blitRig      != null) Destroy(_blitRig);
        if (_blitMaterial != null) Destroy(_blitMaterial);
    }

    // ── 메인 카메라 설정 ─────────────────────────────────────────────────────

    /// <summary>MSAA와 포스트 프로세싱을 끄고 블릿 레이어를 컬링합니다.</summary>
    private void ConfigureMainCamera()
    {
        _mainCamera.allowMSAA     = false;
        _mainCamera.cullingMask  &= ~(1 << BlitLayer);

        var urpData = _mainCamera.GetUniversalAdditionalCameraData();
        if (urpData == null) return;
        urpData.renderPostProcessing = false;
        urpData.antialiasing         = AntialiasingMode.None;
    }

    // ── RenderTexture 생성 / 갱신 ────────────────────────────────────────────

    /// <summary>저해상도 RenderTexture를 생성하고 메인 카메라와 쿼드 머티리얼에 연결합니다.</summary>
    private void RebuildRenderTexture()
    {
        _prevScreenWidth  = Screen.width;
        _prevScreenHeight = Screen.height;

        float aspect     = (float)Screen.width / Screen.height;
        int   pixelWidth = Mathf.Max(1, Mathf.RoundToInt(pixelHeight * aspect));

        // 기존 RT 해제
        _mainCamera.targetTexture = null;
        if (_pixelRT != null)
        {
            _pixelRT.Release();
            Destroy(_pixelRT);
        }

        // FilterMode.Point = 픽셀아트 룩의 핵심 (블러 없는 최근접 업스케일)
        _pixelRT = new RenderTexture(pixelWidth, pixelHeight, 24, RenderTextureFormat.Default)
        {
            filterMode   = FilterMode.Point,
            wrapMode     = TextureWrapMode.Clamp,
            antiAliasing = 1
        };
        _pixelRT.Create();

        _mainCamera.targetTexture = _pixelRT;

        if (_blitMaterial != null)
            _blitMaterial.SetTexture("_MainTex", _pixelRT);

        // 쿼드 종횡비 업데이트
        if (_quadTransform != null)
            _quadTransform.localScale = new Vector3(aspect, 1f, 1f);
    }

    // ── 블릿 리그 생성 ───────────────────────────────────────────────────────

    /// <summary>
    /// 씬 밖(y=9999)에 블릿 카메라와 풀스크린 쿼드를 생성합니다.
    /// 쿼드는 BlitLayer에만 속하고 블릿 카메라만 해당 레이어를 렌더링합니다.
    /// </summary>
    private void CreateBlitRig()
    {
        _blitRig = new GameObject("[PixelBlitRig]")
        {
            transform = { position = new Vector3(0f, 9999f, 0f) }
        };

        CreateBlitCamera();
        CreateBlitQuad();
    }

    private void CreateBlitCamera()
    {
        var camGO = new GameObject("[PixelBlitCamera]");
        camGO.transform.SetParent(_blitRig.transform, false);

        var cam = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 0.5f;
        cam.nearClipPlane    = 0.1f;
        cam.farClipPlane     = 2f;
        cam.depth            = _mainCamera.depth + 1;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = Color.black;
        cam.cullingMask      = 1 << BlitLayer;
        cam.allowMSAA        = false;

        var urpData = camGO.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderPostProcessing = false;
        urpData.antialiasing         = AntialiasingMode.None;
    }

    private void CreateBlitQuad()
    {
        float aspect = (float)Screen.width / Screen.height;

        var quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadGO.name  = "[PixelBlitQuad]";
        quadGO.layer = BlitLayer;
        Destroy(quadGO.GetComponent<Collider>());

        quadGO.transform.SetParent(_blitRig.transform, false);
        quadGO.transform.localPosition = new Vector3(0f, 0f, 1f);
        quadGO.transform.localScale    = new Vector3(aspect, 1f, 1f);
        _quadTransform = quadGO.transform;

        // Custom/PixelBlit : sampler_point_clamp으로 최근접 샘플링을 강제합니다.
        // URP/Unlit의 bilinear 샘플러 문제를 완전히 우회합니다.
        _blitMaterial = new Material(Shader.Find("Custom/PixelBlit"));
        _blitMaterial.SetTexture("_MainTex", _pixelRT);

        var meshRenderer = quadGO.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial    = _blitMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows    = false;
    }

    // ── 픽셀 스내핑 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 카메라 위치를 픽셀 그리드에 정렬합니다.
    /// 오쏘그래픽 카메라 전용입니다. 서브픽셀 지터를 제거합니다.
    /// </summary>
    private void ApplyPixelSnapping()
    {
        // 픽셀 1개에 해당하는 월드 단위 크기
        float worldHeight = _mainCamera.orthographicSize * 2f;
        float pixelSize   = worldHeight / pixelHeight;
        if (pixelSize <= 0f) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x / pixelSize) * pixelSize;
        pos.y = Mathf.Round(pos.y / pixelSize) * pixelSize;
        transform.position = pos;
    }
}
