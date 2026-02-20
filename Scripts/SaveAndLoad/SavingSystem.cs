using System.ComponentModel;
using Godot;
using Godot.Collections;

public class SavingSystem
{
    public static string DefaultSaveLocation { get; private set; } = "C:/Saves/";
    private static readonly Dictionary<string, Variant> SavefileCache = new Dictionary<string, Variant>();
    public static void Save(string path, Dictionary<string, Variant> dict)
    {
        var writer = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        var s = Json.Stringify(dict);
        GD.Print(FileAccess.GetOpenError());
        writer.StoreLine(s);
        writer.Close();
    }

    public static Dictionary<string, Variant> Load(string path)
    {
        if (SavefileCache.ContainsKey(path))
            return SavefileCache[path].As<Dictionary<string, Variant>>();

        var reader = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var result = Json.ParseString(reader.GetAsText()).As<Dictionary<string, Variant>>();
        reader.Close();

        SavefileCache[path] = result;
        return result;
    }
}
