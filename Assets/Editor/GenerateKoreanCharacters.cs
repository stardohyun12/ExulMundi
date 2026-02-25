using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// 모든 한글 음절을 생성하는 에디터 스크립트
/// </summary>
public class GenerateKoreanCharacters : EditorWindow
{
    [MenuItem("Tools/Generate Korean Characters")]
    static void ShowWindow()
    {
        GenerateAllKorean();
    }

    static void GenerateAllKorean()
    {
        StringBuilder sb = new StringBuilder();

        // 영문, 숫자, 기호
        sb.Append("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz !@#$%^&*()_+-=[]{}|;:'\"<>,.?/\\~`");

        // 모든 한글 완성형 (가-힣)
        // Unicode: AC00-D7A3 (11,172자)
        for (int i = 0xAC00; i <= 0xD7A3; i++)
        {
            sb.Append((char)i);
        }

        string result = sb.ToString();

        // 클립보드에 복사
        GUIUtility.systemCopyBuffer = result;

        Debug.Log($"모든 한글 음절 생성 완료! ({result.Length}자)");
        Debug.Log("클립보드에 복사되었습니다. Font Asset Creator의 Custom Character List에 붙여넣으세요.");
    }
}
