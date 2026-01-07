using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ExportWeaponMetadata : EditorWindow
{
    private const string ITEM_PATH = "Data/Items";
    private const string OUTPUT_PATH = "Assets/Metadata";

    [MenuItem("Tools/Export Weapon Metadata")]
    public static void Export()
    {
        if (!Directory.Exists(OUTPUT_PATH))
            Directory.CreateDirectory(OUTPUT_PATH);

        var weapons = Resources.LoadAll<WeaponItem>(ITEM_PATH);

        foreach (var weapon in weapons)
        {
            string itemFolder = Path.Combine(OUTPUT_PATH, weapon.Id);
            if (!Directory.Exists(itemFolder))
                Directory.CreateDirectory(itemFolder);

            string imageFile = weapon.Id + ".png";
            string imagePath = Path.Combine(itemFolder, imageFile);
            SaveSpriteToPNG(weapon.icon, imagePath);

            ApplyPixelArtSettings(imagePath);

            string jsonFile = weapon.Id + ".json";
            string jsonPath = Path.Combine(itemFolder, jsonFile);
            string json = GenerateWeaponJson(weapon, imageFile);
            File.WriteAllText(jsonPath, json);

            Debug.Log($"[Export] Success export metadata for: {weapon.Id}");
        }

        AssetDatabase.Refresh();
    }

    private static string GenerateWeaponJson(WeaponItem weapon, string imageFileName)
    {
        List<string> attributes = new List<string>();

        if (weapon.attackBonus != 0)
            attributes.Add($"{{\"trait_type\": \"Attack Bonus\", \"value\": {weapon.attackBonus}}}");
        if (weapon.hpBonus != 0)
            attributes.Add($"{{\"trait_type\": \"HP Bonus\", \"value\": {weapon.hpBonus}}}");
        if (weapon.defenseBonus != 0)
            attributes.Add($"{{\"trait_type\": \"Defense Bonus\", \"value\": {weapon.defenseBonus}}}");
        if (weapon.speedBonus != 0)
            attributes.Add($"{{\"trait_type\": \"Speed Bonus\", \"value\": {weapon.speedBonus}}}");

        string attributesJson = string.Join(",\n    ", attributes);
        return "{\n" +
               $"  \"name\": \"{weapon.Id}\",\n" +
               $"  \"image\": \"{imageFileName}\",\n" +
               "  \"attributes\": [\n    " +
               attributesJson +
               "\n  ]\n" +
               "}";
    }
    public static void ApplyPixelArtSettings(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; 
            importer.mipmapEnabled = false; 
            importer.alphaIsTransparency = true;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }
    private static void SaveSpriteToPNG(Sprite sprite, string path)
    {
        if (sprite == null) return;

        Texture2D tex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
        var pixels = sprite.texture.GetPixels(
            (int)sprite.rect.x,
            (int)sprite.rect.y,
            (int)sprite.rect.width,
            (int)sprite.rect.height
        );
        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);
    }
}