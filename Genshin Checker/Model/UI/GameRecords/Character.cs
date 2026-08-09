using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genshin_Checker.Model.UI.Character
{
    public class Root
    {
        public List<Character> Chara { get; set; } = new();
    }

    public class Character
    {
        public int Id { get; set; }

    }
    public class CharacterInfo
    {
        public string Name { get; set; } = "";
        public string Element { get; set; } = "";
        public int WeaponType { get; set; } = 0;
        public string Icon { get; set; } = "";
        public string IconSide { get; set; } = "";
    }

}
