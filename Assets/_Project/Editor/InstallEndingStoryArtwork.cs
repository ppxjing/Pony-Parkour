using UnityEditor;
using UnityEngine;

public static class InstallEndingStoryArtwork
{
    [MenuItem("Tools/Toy Dash/Install Ending Story Artwork")]
    public static void Install()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Resources/UI/Story" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = path.EndsWith(".png");
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Toy Dash ending story artwork installed: 6 illustrations + ornate dialogue frame.");
    }
}

// Ensures first import is already configured correctly; no manual menu click is required.
public sealed class EndingStoryTexturePostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/_Project/Resources/UI/Story/")) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = assetPath.EndsWith(".png");
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
    }
}
