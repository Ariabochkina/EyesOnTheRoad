using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(2, "dialogue")]
public class DialogueScriptImporter : ScriptedImporter
{
    const string Template =
        "# Episode\n" +
        "Реплика попутчика.\n" +
        "  1) Первый ответ [0 / 0]\n" +
        "  2) Второй ответ [0 / 0]\n" +
        "  3) Третий ответ [0 / 0]\n" +
        "  молчание [0 / 0]\n";

    public override void OnImportAsset(AssetImportContext ctx)
    {
        string source = File.ReadAllText(ctx.assetPath, Encoding.UTF8);
        var parser = new DialogueScriptParser();
        parser.Parse(source);

        var script = ScriptableObject.CreateInstance<DialogueScript>();
        script.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
        ctx.AddObjectToAsset("script", script);
        ctx.SetMainObject(script);

        foreach (DialogueScriptParser.ParsedEpisode parsed in parser.Episodes)
        {
            var episode = ScriptableObject.CreateInstance<DialogueEpisode>();
            episode.name = parsed.name;
            episode.lines = parsed.lines;
            ctx.AddObjectToAsset("episode:" + parsed.name, episode);
            script.episodes.Add(episode);
        }

        foreach (string error in parser.Errors)
            ctx.LogImportError($"{ctx.assetPath}, {error}", script);
    }

    [MenuItem("Assets/Create/Eyes on the Road/Dialogue Script", priority = 80)]
    static void CreateDialogueScript()
    {
        ProjectWindowUtil.CreateAssetWithTextContent("NewDialogue.dialogue", Template);
    }
}
