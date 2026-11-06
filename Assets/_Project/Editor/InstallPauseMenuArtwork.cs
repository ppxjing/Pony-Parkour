using UnityEditor;
using UnityEngine;

public static class InstallPauseMenuArtwork
{
    [MenuItem("Tools/Toy Dash/Install Pause Menu Artwork")]
    public static void Install()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Resources/UI/Pause" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
            Configure(importer);
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Toy Dash pause menu artwork installed for Beach, Kitchen and Bedroom.");
    }

    internal static void Configure(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
    }
}

public sealed class PauseMenuTexturePostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/_Project/Resources/UI/Pause/")) return;
        InstallPauseMenuArtwork.Configure((TextureImporter)assetImporter);
    }
}
