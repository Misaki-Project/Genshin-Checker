using Genshin_Checker.Model.HoYoLab.CalculatorComputeBatchGet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genshin_Checker.Model.HoYoLab.BatchAvaterListPost
{
    public class Root
    {

        public List<object> element_attr_ids { get; set; } = new();
        public string lang { get; set; } = "ja-jp";
        public int page { get; set; } = 1;
        public string region { get; set; } = "os_asia";
        public int size { get; set; } = 200;
        public string uid { get; set; } = "";
        public List<object> weapon { get; set; } = new();
    }
}