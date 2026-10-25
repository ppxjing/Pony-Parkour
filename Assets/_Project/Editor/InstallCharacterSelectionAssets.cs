using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class InstallCharacterSelectionAssets
{
    private const string SourceRoot = "Assets/_Project/Prefabs/Player";
    private const string TargetRoot = "Assets/_Project/Resources/PlayerCharacters";
    private const string PrefabRoot = "Assets/_Project/Resources/PlayerCharacterPrefabs";
    private static bool running;

    static InstallCharacterSelectionAssets()
    {
        EditorApplication.delayCall += Install;
    }

    [MenuItem("Tools/Adventure/Install Character Selection Assets")]
    public static void Install()
    {
        if (running) return;
        running = true;
        try
        {
            EnsureFolder("Assets/_Project/Resources");
            EnsureFolder(TargetRoot);
            EnsureFolder(PrefabRoot);
            MoveCharacter("flowerhorse");
            MoveCharacter("flowerhorse_b");
            MoveCharacter("horse");
            CreateVisualPrefab("flowerhorse", "flowerhorse.fbx");
            CreateVisualPrefab("flowerhorse_b", "flowerhorse_b.fbx");
            CreateVisualPrefab("horse", "horse.fbx");
            RepairCharacterMaterials();

            ConfigureSprite("Assets/_Project/Resources/UI/character-select-bg.png");
            ConfigureSprite("Assets/_Project/Resources/UI/Characters/flowerhorse.png");
            ConfigureSprite("Assets/_Project/Resources/UI/Characters/flowerhorse-b.png");
            ConfigureSprite("Assets/_Project/Resources/UI/Characters/horse.png");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Character selection assets installed: flowerhorse, flowerhorse_b, horse.");
        }
        finally
        {
            running = false;
        }
    }

    private static void MoveCharacter(string name)
    {
        string source = SourceRoot + "/" + name;
        string target = TargetRoot + "/" + name;
        if (AssetDatabase.IsValidFolder(target)) return;
        if (!AssetDatabase.IsValidFolder(source))
        {
            Debug.LogError("Character source folder is missing: " + source);
            return;
        }
        string error = AssetDatabase.MoveAsset(source, target);
        if (!string.IsNullOrEmpty(error)) Debug.LogError("Could not move " + name + ": " + error);
    }

    private static void ConfigureSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static void CreateVisualPrefab(string id, string fileName)
    {
        string prefabPath = PrefabRoot + "/" + id + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(TargetRoot + "/" + id + "/" + fileName);
        if (model == null) return;

        GameObject root = new GameObject(id + " Player Visual");
        GameObject child = PrefabUtility.InstantiatePrefab(model) as GameObject;
        child.name = "Model - Rotate This Object";
        child.transform.SetParent(root.transform, false);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
    }

    private static void RepairCharacterMaterials()
    {
        SetTexture("flowerhorse", "fabric_White", "3d66Model-12477424-files-42.jpg");
        SetTexture("flowerhorse", "wood_NATURAL", "3d66Model-12477424-files-37.jpg");
        SetTexture("flowerhorse_b", "fabric_Brown", "3d66Model-12477424-files-40.jpg");
        SetTexture("flowerhorse_b", "wood_NATURAL", "3d66Model-12477424-files-37.jpg");

        string materialFolder = TargetRoot + "/horse/Material";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TargetRoot + "/horse/horse.fbm/3d66Model-12477424-files-27.png");
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { materialFolder }))
            AssignTexture(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)), texture);
    }

    private static void SetTexture(string id, string materialName, string textureName)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(TargetRoot + "/" + id + "/Material/" + materialName + ".mat");
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TargetRoot + "/" + id + "/" + id + ".fbm/" + textureName);
        AssignTexture(material, texture);
    }

    private static void AssignTexture(Material material, Texture2D texture)
    {
        if (material == null || texture == null) return;
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        material.mainTexture = texture;
        EditorUtility.SetDirty(material);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        string name = path.Substring(slash + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
