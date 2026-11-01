using System.IO;
using UnityEditor;
using UnityEngine;

public static class InstallCharacterCardArtwork
{
    private const string Source = @"C:\Users\Linaeve\Desktop\三款花纹坐骑选择卡牌界面.png";
    private const string Destination = "Assets/_Project/Resources/UI/character-select-cards.png";

    [MenuItem("Toy Dash/UI/Install Character Card Artwork")]
    public static void Install()
    {
        if (!File.Exists(Source))
            throw new FileNotFoundException("Character card artwork was not found.", Source);

        string absoluteDestination = Path.Combine(Directory.GetCurrentDirectory(), Destination);
        Directory.CreateDirectory(Path.GetDirectoryName(absoluteDestination));
        File.Copy(Source, absoluteDestination, true);
        AssetDatabase.ImportAsset(Destination, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(Destination) as TextureImporter;
        if (importer == null)
            throw new IOException("Unity could not create a texture importer for " + Destination);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();

        AssetDatabase.SaveAssets();
        Debug.Log("Character selection card artwork installed at " + Destination);
    }
}
