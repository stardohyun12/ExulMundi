using UnityEngine;
using UnityEditor;

/// <summary>
/// 프로토타입용 단색 스프라이트를 자동 생성합니다.
/// 메뉴: Exul Mundi / Setup / Create Default Sprites
/// </summary>
public static class CreateDefaultSprites
{
    [MenuItem("Exul Mundi/Setup/Create Default Sprites")]
    public static void Create()
    {
        CreateSprite("WhiteCircle", Color.white, "/Assets/Sprites/WhiteCircle.png");
        AssetDatabase.Refresh();
        Debug.Log("[Setup] 기본 스프라이트 생성 완료 → Assets/Sprites/");
    }

    private static void CreateSprite(string name, Color color, string path)
    {
        // 32×32 단색 텍스처 생성
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixels = new Color[32 * 32];

        // 원형 마스크 적용
        var center = new Vector2(16f, 16f);
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % 32;
            int y = i / 32;
            float dist = Vector2.Distance(new Vector2(x, y), center);
            pixels[i] = dist <= 15f ? color : Color.clear;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // PNG 저장
        var pngData = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        var fullPath = Application.dataPath + path.Replace("/Assets", "");
        var dir = System.IO.Path.GetDirectoryName(fullPath);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        System.IO.File.WriteAllBytes(fullPath, pngData);
    }
}
