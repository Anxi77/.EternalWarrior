using System;
using System.Collections.Generic;
using UnityEngine;

public static class DataSystem
{
    public static SkillDataSystem SkillDataSystem;
    public static ItemDataSystem ItemDataSystem;
    public static PlayerDataSystem PlayerDataSystem;

    static DataSystem()
    {
        SkillDataSystem = new SkillDataSystem();
        SkillDataSystem.LoadRuntimeData();
        ItemDataSystem = new ItemDataSystem();
        ItemDataSystem.LoadRuntimeData();
        PlayerDataSystem = new PlayerDataSystem();
        PlayerDataSystem.LoadRuntimeData();
    }
}
