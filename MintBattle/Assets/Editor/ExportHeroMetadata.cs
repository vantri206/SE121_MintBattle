using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ExportHeroMetadata : EditorWindow
{
    private const string HERO_PATH = "Data/Heroes";
    private const string SKILL_PATH = "Data/Skills";
    private const string OUTPUT_PATH = "Assets/Metadata";

    [MenuItem("Tools/Export Hero Metadata")]
    public static void Export()
    {
        if (!Directory.Exists(OUTPUT_PATH))
            Directory.CreateDirectory(OUTPUT_PATH);

        var heroes = Resources.LoadAll<HeroData>(HERO_PATH);
        if (heroes.Length == 0)
        {
            Debug.LogWarning("No heroes found in Resources");
            return;
        }

        var allSkills = Resources.LoadAll<SkillData>(SKILL_PATH);
        Dictionary<string, SkillData> skillDict = new Dictionary<string, SkillData>();
        foreach (var skill in allSkills)
            skillDict[skill.Id] = skill;

        foreach (var hero in heroes)
        {
            string folder = Path.Combine(OUTPUT_PATH, hero.Id);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string imageFile = hero.Id + ".png";
            string imagePath = Path.Combine(folder, imageFile);
            SaveSpriteToPNG(hero.Image, imagePath);

            string jsonFile = hero.Id + ".json";
            string jsonPath = Path.Combine(folder, jsonFile);

            string json = GenerateHeroJson(hero, imageFile, skillDict, folder);
            File.WriteAllText(jsonPath, json);

            Debug.Log($"Exported {hero.Id} metadata");
        }

        AssetDatabase.Refresh();
        Debug.Log("Export done");
    }

    private static void SaveSpriteToPNG(Sprite sprite, string path)
    {
        if (sprite == null)
        {
            Debug.LogError("Sprite is NULL.");
            return;
        }

        Texture2D tex = new Texture2D(
            (int)sprite.rect.width,
            (int)sprite.rect.height,
            TextureFormat.RGBA32,
            false
        );

        var pixels = sprite.texture.GetPixels(
            (int)sprite.rect.x,
            (int)sprite.rect.y,
            (int)sprite.rect.width,
            (int)sprite.rect.height
        );

        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    private static string GenerateHeroJson(HeroData hero, string heroImageFile, Dictionary<string, SkillData> allSkills, string folder)
    {
        string skillsArray = "[\n";
        for (int i = 0; i < hero.Skills.Count(); i++)
        {
            string skillId = hero.Skills[i];
            if (!allSkills.TryGetValue(skillId, out SkillData skill))
            {
                Debug.LogWarning($"Skill {skillId} not found");
                continue;
            }

            skillsArray += "  {\n" +
                           $"    \"name\": \"{skill.Name}\",\n" +
                           $"    \"description\": \"{skill.Description}\",\n" +
                           $"    \"cooldown\": {skill.Cooldown}\n" +
                           "  }";

            if (i < hero.Skills.Count() - 1)
                skillsArray += ",\n";
            else
                skillsArray += "\n";
        }
        skillsArray += "]";
        return "{\n" +
               $"  \"heroClassId\": \"{hero.Id}\",\n" +
               $"  \"name\": \"{hero.Name}\",\n" +
               $"  \"baseHP\": {hero.BaseHP},\n" +
               $"  \"baseAttack\": {hero.BaseAttack},\n" +
               $"  \"baseDefense\": {hero.BaseDefense},\n" +
               $"  \"baseSpeed\": {hero.BaseSpeed},\n" +
               $"  \"growthStats\": {hero.growthStats},\n" +
               $"  \"skills\": {skillsArray}\n" +
               "}";
    }
}
