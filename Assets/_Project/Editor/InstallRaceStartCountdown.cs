using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InstallRaceStartCountdown
{
    private const string UiRoot = "Assets/_Project/UI/Countdown";
    private static readonly string[] ScenePaths =
    {
        "Assets/_Project/Scenes/Kitchen_Adventure.unity",
        "Assets/_Project/Scenes/Kitchen_Playground.unity"
    };

    public static void Run()
    {
        ImportSprites();
        foreach (string path in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            RaceStartCountdown existing = Object.FindFirstObjectByType<RaceStartCountdown>();
            if (existing == null || existing.gameObject.scene != scene)
            {
                GameObject countdown = new GameObject("RaceStartCountdown", typeof(RaceStartCountdown));
                existing = countdown.GetComponent<RaceStartCountdown>();
                EditorSceneManager.MarkSceneDirty(scene);
            }
            SerializedObject serialized = new SerializedObject(existing);
            serialized.FindProperty("threeSprite").objectReferenceValue = Load("countdown-3.png");
            serialized.FindProperty("twoSprite").objectReferenceValue = Load("countdown-2.png");
            serialized.FindProperty("oneSprite").objectReferenceValue = Load("countdown-1.png");
            serialized.FindProperty("goSprite").objectReferenceValue = Load("countdown-go.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existing);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Installed 3-second race countdown in {path}");
        }
        AssetDatabase.SaveAssets();
    }

    private static void ImportSprites()
    {
        string[] names = { "countdown-3.png", "countdown-2.png", "countdown-1.png", "countdown-go.png" };
        foreach (string name in names)
        {
            string path = $"{UiRoot}/{name}";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiRoot}/{name}");
}
