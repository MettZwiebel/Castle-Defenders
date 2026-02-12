using Godot.Collections;
using Godot;

public class ProfileStats
{
    //Stats of all runs combined
    public string SavefileName { get; set; } = "save.txt";
    public long TotalKills { get; set; }
    public long ProjectilesFired { get; set; }

    //Stats of Current run
    public uint CurrentDay { get; set; }
    public long CurrentKills { get; set; }
    public long CurrentProjectilesFired { get; set; }



    public Dictionary<string, Variant> Save()
    {
        return new Dictionary<string, Variant>()
        {
            {"name",SavefileName},
            {"tkil", TotalKills},
            {"tpro", ProjectilesFired},

            {"cday", CurrentDay},
            {"ckill",CurrentKills},
            {"cpro", CurrentProjectilesFired}
        };
    }

    public void Load(Dictionary<string, Variant> dict)
    {
        foreach (var key in dict.Keys)
            switch (key)
            {
                case "name":
                    this.SavefileName = dict[key].As<string>();
                    break;
                case "tkil":
                    this.TotalKills = dict[key].As<long>();
                    break;
                case "tpro":
                    this.ProjectilesFired = dict[key].As<long>();
                    break;

                case "cday":
                    this.CurrentDay = dict[key].As<uint>();
                    break;
                case "ckill":
                    this.CurrentKills = dict[key].As<long>();
                    break;
                case "cpro":
                    this.CurrentProjectilesFired = dict[key].As<long>();
                    break;
            }
    }
}
