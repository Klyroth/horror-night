// Horror Night batch screenshots: main menu + night map from spawn.
// Batch-mode only. Renders Camera.main to Assets/Screenshots/*.png.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HorrorScreenshots
{
    public static void Render()
    {
        Directory.CreateDirectory("Assets/Screenshots");
        RenderScene("Assets/Scenes/MainMenu.unity", "horror-mainmenu");
        RenderScene("Assets/Scenes/NightCity.unity", "horror-nightcity-spawn");
        Debug.Log("HorrorScreenshots: done");
    }

    private static void SaveShot(Camera cam, string name)
    {
        int w = 1280, h = 720;
        RenderTexture rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        string path = "Assets/Screenshots/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log("HorrorScreenshots: saved " + path);
    }

    private static void RenderScene(string scenePath, string name)
    {
        EditorSceneManager.OpenScene(scenePath);
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("HorrorScreenshots: No main camera in " + scenePath);
            return;
        }
        SaveShot(cam, name);
    }
}
