using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genshin_Checker.Model.HoYoLab.BatchAvaterListGet
{
    public class Root : Model.HoYoLab.Root<Data>
    {
    }

    public class Data
    {
        public List<Character> list { get; set; } = new();
        public int total { get; set; }
    }

    public class Character
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string icon { get; set; } = "";
        public int weapon_cat_id { get; set; }
        public int avatar_level { get; set; }
        public int element_attr_id { get; set; }
        public int max_level { get; set; }
        public int level_current { get; set; }
        public int promote_level { get; set; }
        public List<SkillList> skill_list { get; set; } = new();
        public Weapon weapon { get; set; } = new();
        public List<ReliquaryList> reliquary_list { get; set; } = new();
        public string wiki_url { get; set; } = "";
        public string wiki_recommend_weapon_url { get; set; } = "";
        public int constellation_num { get; set; }
        public int fetter_level { get; set; }
    }

    public class ReliquaryList
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string icon { get; set; } = "";
        public int reliquary_cat_id { get; set; }
        public int reliquary_level { get; set; }
        public int level_current { get; set; }
        public int max_level { get; set; }
    }


    public class SkillList
    {
        public int id { get; set; }
        public int group_id { get; set; }
        public string name { get; set; } = "";
        public string icon { get; set; } = "";
        public int max_level { get; set; }
        public int level_current { get; set; }
    }

    public class Weapon
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string icon { get; set; } = "";
        public int weapon_cat_id { get; set; }
        public int weapon_level { get; set; }
        public int max_level { get; set; }
        public int level_current { get; set; }
    }


}