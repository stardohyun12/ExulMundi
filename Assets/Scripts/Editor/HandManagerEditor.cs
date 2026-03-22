using UnityEditor;
using UnityEngine;

/// <summary>
/// HandManager 커스텀 에디터.
/// Edit 모드에서 Scene 뷰에 카드 위치 미리보기를 표시합니다.
/// Play 모드에서는 OnValidate가 ArrangeHand()를 직접 호출합니다.
/// </summary>
[CustomEditor(typeof(HandManager))]
public class HandManagerEditor : Editor
{
    // ── 상수 ──────────────────────────────────────────────────────────────────

    private const float CardW = 130f;
    private const float CardH = 190f;

    private static readonly Color FillColor   = new Color(0.35f, 0.65f, 1f, 0.15f);
    private static readonly Color BorderColor = new Color(0.35f, 0.65f, 1f, 0.85f);
    private static readonly Color LabelColor  = new Color(0.35f, 0.65f, 1f, 1.00f);

    // ── 직렬화 프로퍼티 ───────────────────────────────────────────────────────

    private SerializedProperty _arcRadius;
    private SerializedProperty _cardSpacing;
    private SerializedProperty _maxHalfAngle;
    private SerializedProperty _maxHalfAngleTwoCards;
    private SerializedProperty _cardUplift;
    private SerializedProperty _cardsParent;
    private SerializedProperty _previewCount;

    // ── Unity 에디터 생명주기 ─────────────────────────────────────────────────

    private void OnEnable()
    {
        _arcRadius            = serializedObject.FindProperty("arcRadius");
        _cardSpacing          = serializedObject.FindProperty("cardSpacing");
        _maxHalfAngle         = serializedObject.FindProperty("maxHalfAngle");
        _maxHalfAngleTwoCards = serializedObject.FindProperty("maxHalfAngleTwoCards");
        _cardUplift           = serializedObject.FindProperty("cardUplift");
        _cardsParent          = serializedObject.FindProperty("cardsParent");
        _previewCount         = serializedObject.FindProperty("editorPreviewCount");
    }

    // ── Inspector UI ──────────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Edit Mode Preview", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int newCount = EditorGUILayout.IntSlider(
            new GUIContent("Preview Card Count", "Scene 뷰에서 미리볼 카드 수. 값은 씬에 저장됩니다."),
            _previewCount.intValue, 1, 12);
        if (EditorGUI.EndChangeCheck())
        {
            _previewCount.intValue = newCount;
            changed = true;
        }

        if (changed)
        {
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
    }

    // ── Scene 뷰 Gizmo 그리기 ─────────────────────────────────────────────────

    private void OnSceneGUI()
    {
        // Play 모드에서는 실제 카드가 보이므로 미리보기 불필요
        if (Application.isPlaying) return;

        serializedObject.Update();

        // cardsParent가 없으면 기준 좌표계를 알 수 없음
        var cardsParent = _cardsParent.objectReferenceValue as Transform;
        if (cardsParent == null) return;

        float arcRadius           = _arcRadius.floatValue;
        float cardSpacing         = _cardSpacing.floatValue;
        float maxHalfAngle        = _maxHalfAngle.floatValue;
        float maxHalfAngleTwoCards = _maxHalfAngleTwoCards.floatValue;
        float cardUplift          = _cardUplift.floatValue;
        int   count               = _previewCount.intValue;

        // ArrangeHand()와 동일한 계산
        float activeMaxHalf = count == 2 ? maxHalfAngleTwoCards : maxHalfAngle;
        float halfAngle = count > 1 ? activeMaxHalf / (count + 1) : 0f;
        float halfRad   = halfAngle * Mathf.Deg2Rad;

        float anglePerCardRad = count > 1 ? halfRad * 2f / (count - 1) : 0f;
        float dynamicRadius   = anglePerCardRad > 0.001f
            ? cardSpacing / (2f * Mathf.Sin(anglePerCardRad * 0.5f))
            : arcRadius;
        dynamicRadius = Mathf.Max(dynamicRadius, arcRadius);

        float yDrop = dynamicRadius * (1f - Mathf.Cos(halfRad));

        for (int i = 0; i < count; i++)
        {
            float t     = count > 1 ? (float)i / (count - 1) : 0.5f;
            float angle = count > 1 ? Mathf.Lerp(-halfAngle, halfAngle, t) : 0f;
            float rad   = angle * Mathf.Deg2Rad;

            float lx = dynamicRadius * Mathf.Sin(rad);
            float ly = cardUplift + yDrop + dynamicRadius * (Mathf.Cos(rad) - 1f);

            // cardsParent 로컬 좌표 → 월드 좌표로 변환 후 카드 TRS 행렬 적용
            Matrix4x4 saved = Handles.matrix;
            Handles.matrix = cardsParent.localToWorldMatrix
                           * Matrix4x4.TRS(
                                 new Vector3(lx, ly, 0f),
                                 Quaternion.Euler(0f, 0f, -angle),
                                 Vector3.one);

            // 카드 사각형 (pivot = 하단 중앙)
            Vector3[] corners =
            {
                new Vector3(-CardW * 0.5f, 0f,    0f),
                new Vector3( CardW * 0.5f, 0f,    0f),
                new Vector3( CardW * 0.5f, CardH, 0f),
                new Vector3(-CardW * 0.5f, CardH, 0f),
            };
            Handles.DrawSolidRectangleWithOutline(corners, FillColor, BorderColor);

            // 카드 번호 라벨
            Color prev = Handles.color;
            Handles.color = LabelColor;
            Handles.Label(new Vector3(0f, CardH * 0.5f, 0f),
                          $"{i + 1}",
                          new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = LabelColor } });
            Handles.color = prev;

            Handles.matrix = saved;
        }
    }
}
