using Godot;
using Godot.Collections;

public partial class BuildBar : Node2D
{
    private Dictionary<string, Variant> items = new Dictionary<string, Variant>();
    private Array<Buildable> ActiveMenuItems = new Array<Buildable>();
    private Dictionary<string, Array<Buildable>> MenuItems = new Dictionary<string, Array<Buildable>>();

    private void parseMenuItems(Dictionary<string, Variant> dict)
    {
        foreach (var key in dict.Keys)
        {
            switch (key)
            {
                case "build":
                    MenuItems[key] = dict[key].As<Array<Buildable>>();
                    break;
                case "craft":
                    MenuItems[key] = dict[key].As<Array<Buildable>>();
                    break;
                case "pop":
                    MenuItems[key] = dict[key].As<Array<Buildable>>();
                    break;
                case "tow":
                    MenuItems[key] = dict[key].As<Array<Buildable>>();
                    break;
            }
        }
    }


    private void changeActiveSubmenu(string key)
    {
        switch (key)
        {
            case "none":
                ActiveMenuItems.Clear();
                break;
            default:
                ActiveMenuItems = MenuItems[key];
                break;
        }
    }
}
