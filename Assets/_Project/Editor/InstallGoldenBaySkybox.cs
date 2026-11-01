using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class InstallGoldenBaySkybox
{
    private const string SourceExr = @"C:\Users\Linaeve\Downloads\golden_bay_4k.exr";
    private const string AssetFolder = "Assets/_Project/Environment/Skyboxes";
    private const string TexturePath = AssetFolder + "/golden_bay_4k.exr";
    private const string MaterialPath = AssetFolder + "/GoldenBaySkybox.mat";

    private static readonly string[] ScenePaths =
    {
        "Assets/_Project/Scenes/Beach.unity",
        "Assets/_Project/Scenes/Kitchen_Adventure.unity",
        "Assets/_Project/Scenes/Bedroom.unity"
    };

    [MenuItem("Toy Dash/Environment/Install Golden Bay Skybox")]
    public static void InstallFromMenu()
    {
        Install();
    }

    public static void Install()
    {
        if (!File.Exists(SourceExr))
            throw new FileNotFoundException("Golden Bay EXR was not found.", SourceExr);

        EnsureFolder("Assets/_Project/Environment");
        EnsureFolder(AssetFolder);

        string absoluteTexturePath = Path.Combine(Directory.GetCurrentDirectory(), TexturePath);
        File.Copy(SourceExr, absoluteTexturePath, true);
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        ConfigureTextureImporter();
        Material skybox = CreateOrUpdateMaterial();

        foreach (string scenePath in ScenePaths)
            ApplyToScene(scenePath, skybox);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Golden Bay skybox installed in Beach, Kitchen_Adventure, and Bedroom.");
    }

    private static void ConfigureTextureImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Could not load the EXR texture importer.");

        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.sRGBTexture = false;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial()
    {
        Shader shader = Shader.Find("Skybox/Panoramic");
        if (shader == null)
            throw new InvalidOperationException("Unity shader 'Skybox/Panoramic' is unavailable.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "GoldenBaySkybox" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(TexturePath);
        if (texture == null)
            throw new InvalidOperationException("The imported EXR texture could not be loaded.");

        material.SetTexture("_Tex", texture);
        material.SetFloat("_Exposure", 1.0f);
        material.SetFloat("_Rotation", 0.0f);
        if (material.HasProperty("_ImageType")) material.SetFloat("_ImageType", 0.0f);
        if (material.HasProperty("_MirrorOnBack")) material.SetFloat("_MirrorOnBack", 0.0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyToScene(string scenePath, Material skybox)
    {
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
            throw new FileNotFoundException("Scene was not found.", scenePath);

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        RenderSettings.skybox = skybox;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 1.0f;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Applied Golden Bay skybox to " + scenePath);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Invalid Unity asset folder: " + path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
