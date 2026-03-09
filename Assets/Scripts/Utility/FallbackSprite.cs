using UnityEngine;

/// <summary>
/// SpriteRenderer에 sprite가 없을 때 단색 사각형을 자동으로 생성합니다.
/// 에셋 없이 프로토타입 시각화를 빠르게 할 때 사용합니다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FallbackSprite : MonoBehaviour
{
    private void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr.sprite != null) return;

        var tex = new Texture2D(32, 32);
        var pixels = new Color[32 * 32];
        var c = sr.color;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
        tex.SetPixels(pixels);
        tex.Apply();

        sr.sprite = Sprite.Create(
            tex,
            new Rect(0, 0, 32, 32),
            new Vector2(0.5f, 0.5f),
            32f
        );
        // 색은 이미 적용했으므로 흰색으로 리셋
        sr.color = Color.white;
    }
}
