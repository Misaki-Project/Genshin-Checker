using Genshin_Checker.Core.HoYoLab;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Genshin_Checker.Core.General.Convert;
using Genshin_Checker.GUI.Window.PopupWindow;
using Genshin_Checker.Resource.Languages;
using Genshin_Checker.Core;
using static Genshin_Checker.Window.ExWindow.CharacterCalculator.CalculateResult;

namespace Genshin_Checker.Window.ExWindow.CharacterCalculator
{
    
    public partial class CalculateResult : Form
    {
        Account Account;
        List<Input> Inputs;
        public CalculateResult(Account account,List<Input> inputs)
        {
            InitializeComponent();
            Inputs = inputs;
            Account = account;
            Icon = Icon.FromHandle(resource.icon.calculator_new.GetHicon());
            Text = Localize.WindowName_CalculateResult;
        }
        public class Input
        {
            public int characterID { get; set; }
            public Range Level { get; set; } = new(1,90);
            public List<Range> Talent { get; set; } = new();
        }
        public class Range
        {
            public Range(int current, int to)
            {
                Current = current;
                To = to;
            }
            public int Current { get; set;}
            public int To { get; set; }
        }
        DataGridViewRow? GetRow(DataGridView data, Predicate<DataGridViewRow> predicate)
        {
            return data.Rows.Cast<DataGridViewRow>().ToList().Find(predicate);
        }
        void CalcurateResin(int CharaExp, int CharaMora,int BossItem,int TalentMora, int TalentBooks)
        {
            int world = Account.EnkaNetwork.Data.playerInfo.worldLevel;
            bool reached = Account.EnkaNetwork.Data.playerInfo.level == 60;
            double Drop = world switch
            {
                0 => 1.6185,
                1 => 1.6185,
                2 => 1.7037,
                3 => 1.8741,
                4 => 2.0445,
                5 => 2.2149,
                6 => 2.3852,
                7 => 2.5556,
                _ => 2.5556
            };
            double bossresin = BossItem / Drop * 40.0;
            double talentresin = TalentBooks / 9.0 * 20.0;
            int per = world switch
            {
                0 => 12000,
                1 => 20000,
                2 => 28000,
                3 => 36000,
                4 => 44000,
                5 => 52000,
                _ => 60000
            };
            per += reached ? 100 : 0;
            double chararesin = CharaMora / per * 20;
            double talentmoraresin = TalentMora / per * 20;

            per = world switch
            {
                0 => 25000,
                1 => 38500,
                2 => 52500,
                3 => 67500,
                4 => 82500,
                5 => 102500,
                _ => 122500
            };
            double charaexpresin = CharaExp / per * 20;
            ResinCharaExp.Text = $"{charaexpresin:#,##0.00}";
            ResinBossItem.Text = $"{bossresin:#,##0.00}";
            ResinCharacterMora.Text = $"{chararesin:#,##0.00}";
            ResinTalentBooks.Text = $"{talentresin:#,##0.00}";
            ResinTalentMora.Text = $"{talentmoraresin:#,##0.00}";
            double total = bossresin + talentresin + chararesin + talentmoraresin + charaexpresin;
            ResinTotal.Text = $"{total:#,##0.00}";


        }
        List<CharacterData> characterDatas = new();
        private async void CalculateResult_Load(object sender, EventArgs e)
        {
            int CharacterMora = 0;
            int CharacterExp = 0;
            int TalentMora = 0;
            int TalentCrown = 0;
            int BossItemCount = 0;
            int TalentBooksCount = 0;
            int cnt = 0;
            var PostData = new Model.HoYoLab.CalculatorComputeBatchPost.Root();
            var CharacterDatas = new List<CharacterData>();
            try
            {
                characterDatas.Clear();
                var characters = await Account.Characters.GetData();
                var avaterList = Account.CharacterDetail.CachedAvaterList();
                foreach (Input input in Inputs)
                {
                    var RequiedItemData = new CharacterData();
                    RequiedItemData.id = input.characterID;
                    var avater = avaterList.Find(a=>a.id == input.characterID);
                    if (avater == null) throw new InvalidOperationException();
                    var chr = characters.list.Find(a => a.id == input.characterID);
                    if (chr != null)
                    {
                        RequiedItemData.name = chr.name;
                        RequiedItemData.element = Element.GetElementEnum(chr.element);
                        RequiedItemData.star = chr.rarity;
                    }
                    CharacterDatas.Add(RequiedItemData);
                    cnt++;
                    //ProgressState.Text = string.Format(Localize.WindowName_CalculateResult_Loading, cnt, Inputs.Count);
                    //progressBar.Value = 10000 * cnt / Inputs.Count;
                    var detail = await Account.CharacterDetail.GetData(input.characterID);
                    //キャラクターのスキルを取得     
                    var skill = avater.skill_list.FindAll(a => a.max_level >= 10);
                    if (skill.Count != 3)
                        throw new ArgumentException(string.Format(Localize.Error_CalculateResult_InvalidTalentCount, skill.Count));
                    var character = characters.list.Find(a => a.id == input.characterID);
                    if (character == null) throw new ArgumentNullException(nameof(character), string.Format(Localize.Error_CalculateResult_CharacterNotFound, input.characterID));
                    var skilllist = new List<Model.HoYoLab.CalculatorComputeBatchPost.SkillList>();
                    
                    for (int i = 0; i < skill.Count; i++)
                    {
                        skilllist.Add(new() { id = skill[i].group_id, level_current = input.Talent[i].Current, level_target = input.Talent[i].To });
                    }
                    foreach (var data in skill.FindAll(a => a.max_level == 1))
                    {
                        skilllist.Add(new() { id = data.group_id, level_current = 1, level_target = 1 });
                    }
                    PostData.items.Add(new Model.HoYoLab.CalculatorComputeBatchPost.Characters()
                    {
                        avatar_id = input.characterID,
                        avatar_level_current = input.Level.Current,
                        avatar_level_target = input.Level.To,
                        element_attr_id = (int)Element.GetElementEnum(character.element),
                        skill_list = skilllist
                    });
                }
                var result = await Account.Endpoint.ComputeBatchCalculate(PostData);
                foreach(var a in result.single_role_result){

                    var RequiedItemData = CharacterDatas[0];
                    CharacterDatas.RemoveAt(0);
                    foreach (var res in a.overall_material_consume.avatar_consume)
                    {
                        foreach (var data in res.consume)
                        {
                            if (data.id == 202) //モラ
                            {
                                RequiedItemData.Mora = data.num;
                                CharacterMora += data.num;
                                CharacterMoraResult.Text = $"{CharacterMora:#,##0}";
                                TotalMoraResult.Text = $"{(TalentMora + CharacterMora):#,##0}";
                            }
                            else if (data.id == 104003) //大英雄の経験
                            {
                                RequiedItemData.HerosWit = data.num;
                                CharacterExp += data.num;
                                CharacterExpResult.Text = $"{CharacterExp:#,##0}";
                            }
                            else if (data.id > 104100 && data.id < 104200)
                            {
                                int id = (data.id - 104100) / 10;
                                string type = "";
                                switch (data.id % 10) //アイテムIDの1の位
                                {
                                    case 1:
                                        RequiedItemData.ascension.Sliver = data.num;
                                        type = nameof(AscensionNumSliver);
                                        break;
                                    case 2:
                                        RequiedItemData.ascension.Fragment = data.num;
                                        type = nameof(AscensionNumFragment);
                                        break;
                                    case 3:
                                        RequiedItemData.ascension.Chunk = data.num;
                                        type = nameof(AscensionNumChunk);
                                        break;
                                    case 4:
                                        RequiedItemData.ascension.Gemstone = data.num;
                                        type = nameof(AscensionNumGemstone);
                                        break;
                                    default: throw new ArgumentException(string.Format(Localize.Error_CalculateResult_InvalidItemID, data.id, data.name));
                                }
                                var row = GetRow(ViewAscensionMaterial, a => (int)a.Cells[nameof(AscensionTypeID)].Value == id);
                                if (row == null)
                                {
                                    ViewAscensionMaterial.Rows.Add(id, id, 0, 0, 0, 0);
                                    row = GetRow(ViewAscensionMaterial, a => (int)a.Cells[nameof(AscensionTypeID)].Value == id);
                                }
                                if (row != null) row.Cells[type].Value = (int)row.Cells[type].Value + data.num;
                            }
                            else if (data.id > 113000 && data.id < 114000)
                            {
                                RequiedItemData.enemybossitem = new() { iconurl = data.icon_url, name = data.name, id = data.id, num = data.num };
                                var row = GetRow(ViewBossItem, a => (int)a.Cells[nameof(BossItemID)].Value == data.id);
                                if (row == null)
                                {
                                    ViewBossItem.Rows.Add(data.id, await Core.WebRequest.ImageGetRequest(data.icon_url), data.name, data.num);
                                }
                                else row.Cells[nameof(BossItemNum)].Value = (int)row.Cells[nameof(BossItemNum)].Value + data.num;
                                BossItemCount += data.num;
                            }
                            else if ((data.id > 100000 && data.id <= 100099) || (data.id > 101200 && data.id <= 101299))
                            {
                                RequiedItemData.localitem = new() { iconurl = data.icon_url, name = data.name, id = data.id, num = data.num };
                                var row = GetRow(ViewLocalSpecialtyItem, a => (int)a.Cells[nameof(LocalSpecialtyID)].Value == data.id);
                                if (row == null)
                                {
                                    ViewLocalSpecialtyItem.Rows.Add(data.id, await Core.WebRequest.ImageGetRequest(data.icon_url), data.name, data.num);
                                }
                                else row.Cells[nameof(LocalSpecialtyItemNum)].Value = (int)row.Cells[nameof(LocalSpecialtyItemNum)].Value + data.num;
                            }
                            else if ((data.id >= 112002 && data.id < 113000))
                            {
                                var find = RequiedItemData.items.Find(a => a.id == data.id);
                                if (find != null) find.num += data.num;
                                else RequiedItemData.items.Add(new() { iconurl = data.icon_url, name = data.name, id = data.id, num = data.num });
                                var row = GetRow(ViewEnemyItems, a => (int)a.Cells[nameof(EnemyItemID)].Value == data.id);
                                if (row == null)
                                {
                                    ViewEnemyItems.Rows.Add(data.id, await Core.WebRequest.ImageGetRequest(data.icon_url), data.name, data.num, 0, data.num);
                                }
                                else
                                {
                                    row.Cells[nameof(EnemyItemCharacterNum)].Value = (int)row.Cells[nameof(EnemyItemCharacterNum)].Value + data.num;
                                    row.Cells[nameof(EnemyItemTotalNum)].Value = (int)row.Cells[nameof(EnemyItemCharacterNum)].Value + (int)row.Cells[nameof(EnemyItemTalentNum)].Value;
                                }
                            }
                        }
                    }

                    foreach (var res in a.overall_material_consume.avatar_skill_consume)
                    {
                        foreach (var data in res.consume)
                        {
                            if (data.id == 202)
                            {
                                RequiedItemData.Mora += data.num;
                                TalentMora += data.num;
                                TalentMoraResult.Text = $"{TalentMora:#,##0}";
                                TotalMoraResult.Text = $"{(TalentMora + CharacterMora):#,##0}";
                            }
                            else if (data.id == 104319)
                            {
                                RequiedItemData.talentcrown += data.num;
                                TalentCrown += data.num;
                                TalentCrownResult.Text = $"{TalentCrown:#,##0}";
                            }
                            else if (data.id > 104300 && data.id < 104400)
                            {
                                var id = data.id - 104301;
                                if (id >= 19) id -= 1;
                                string pos = "";
                                switch (id % 3)
                                {
                                    case 0:
                                        pos = nameof(TalentItemNumTeachings);
                                        RequiedItemData.talent.teaching = data.num;
                                        break;
                                    case 1:
                                        pos = nameof(TalentItemNumGuide);
                                        RequiedItemData.talent.guide = data.num;
                                        break;
                                    case 2:
                                        pos = nameof(TalentItemNumPhilosophies);
                                        RequiedItemData.talent.philosophies = data.num;
                                        break;
                                    default:
                                        throw new ArgumentException(string.Format(Localize.Error_CalculateResult_InvalidItemID, data.id, data.name));
                                };
                                RequiedItemData.talent.type = id / 3;
                                var row = GetRow(ViewTalentItems, a => (int)a.Cells[nameof(TalentItemID)].Value == id / 3);
                                if (row == null)
                                {
                                    ViewTalentItems.Rows.Add((int)id / 3, (int)id / 3, (int)id / 3 % 3, 0, 0, 0);
                                    row = GetRow(ViewTalentItems, a => (int)a.Cells[nameof(TalentItemID)].Value == id / 3);
                                }
                                if (row != null) row.Cells[pos].Value = (int)row.Cells[pos].Value + data.num;
                                int x = 1;
                                if (id % 3 == 1) x = 3;
                                if (id % 3 == 2) x = 9;
                                TalentBooksCount += data.num * x;
                            }
                            else if (data.id > 113000 && data.id < 114000)
                            {
                                RequiedItemData.weeklybossitem = new() { iconurl = data.icon_url, name = data.name, id = data.id, num = data.num };
                                var row = GetRow(ViewWeeklyBossItems, a => (int)a.Cells[nameof(WeeklyBossItemID)].Value == data.id);
                                if (row == null)
                                {
                                    ViewWeeklyBossItems.Rows.Add(data.id, await Core.WebRequest.ImageGetRequest(data.icon_url), data.name, data.num);
                                }
                                else row.Cells[nameof(WeeklyBossItemNum)].Value = (int)row.Cells[nameof(WeeklyBossItemNum)].Value + data.num;
                            }
                            else if ((data.id >= 112002 && data.id < 113000))
                            {
                                var find = RequiedItemData.items.Find(a => a.id == data.id);
                                if (find != null) find.num += data.num;
                                else RequiedItemData.items.Add(new() { iconurl = data.icon_url, name = data.name, id = data.id, num = data.num });
                                var row = GetRow(ViewEnemyItems, a => (int)a.Cells[nameof(EnemyItemID)].Value == data.id);
                                if (row == null)
                                {
                                    ViewEnemyItems.Rows.Add(data.id, await Core.WebRequest.ImageGetRequest(data.icon_url), data.name, 0, data.num, data.num);
                                }
                                else
                                {
                                    row.Cells[nameof(EnemyItemTalentNum)].Value = (int)row.Cells[nameof(EnemyItemTalentNum)].Value + data.num;
                                    row.Cells[nameof(EnemyItemTotalNum)].Value = (int)row.Cells[nameof(EnemyItemCharacterNum)].Value + (int)row.Cells[nameof(EnemyItemTalentNum)].Value;
                                }
                            }
                        }
                    }
                    characterDatas.Add(RequiedItemData);
                    CalcurateResin(CharacterExp*20000,CharacterMora, BossItemCount, TalentMora, TalentBooksCount);
                }
                CharacterView.Rows.Clear();
                foreach(var row in characterDatas)
                {
                    CharacterView.Rows.Add(
                        row.id,
                        row.star,
                        row.name,
                        row.localitem != null ? await Core.WebRequest.ImageGetRequest(row.localitem.iconurl):new Bitmap(1, 1),//特産アイコン
                        row.localitem != null ? row.localitem.name : "",
                        row.localitem != null ? row.localitem.num : 0,
                        row.talent.type,
                        "",
                        row.talent.teaching,
                        row.talent.guide,
                        row.talent.philosophies,
                        row.HerosWit,
                        row.Mora
                        );
                };
                ViewAscensionMaterial.Sort(ViewAscensionMaterial.Columns[nameof(AscensionTypeID)], ListSortDirection.Ascending);
                ViewAscensionMaterial.ClearSelection();
                ViewBossItem.Sort(ViewBossItem.Columns[nameof(BossItemID)], ListSortDirection.Ascending);
                ViewBossItem.ClearSelection();
                ViewLocalSpecialtyItem.Sort(ViewLocalSpecialtyItem.Columns[nameof(LocalSpecialtyID)], ListSortDirection.Ascending);
                ViewLocalSpecialtyItem.ClearSelection();
                ViewTalentItems.Sort(ViewTalentItems.Columns[nameof(TalentItemID)], ListSortDirection.Ascending);
                ViewTalentItems.ClearSelection();
                ViewWeeklyBossItems.Sort(ViewWeeklyBossItems.Columns[nameof(WeeklyBossItemID)], ListSortDirection.Ascending);   
                ViewWeeklyBossItems.ClearSelection();
                ViewEnemyItems.Sort(ViewEnemyItems.Columns[nameof(EnemyItemID)], ListSortDirection.Ascending);
                ViewEnemyItems.ClearSelection();
                ProgressPanel.Visible = false;
            }
            catch(Exception ex)
            {
                Dialog.Error(Localize.Error_CharacterResult_FailedToLoad, ex.ToString());
            }
        }

        private void TableFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender is not DataGridView) return;
            if (e.CellStyle == null) return;
            var Table = (DataGridView)sender;
            var row = Table.Rows[e.RowIndex];
            switch (Table.Name)
            {
                // 元素宝石
                case nameof(ViewAscensionMaterial):
                    switch (Table.Columns[e.ColumnIndex].Name)
                    {
                        case nameof(AscensionType):
                            e.Value = (int)row.Cells[nameof(AscensionTypeID)].Value switch
                            {
                                0 => Genshin.Ascension_BrilliantDiamond,
                                1 => Genshin.Ascension_AgnidusAgate,
                                2 => Genshin.Ascension_VarunadaLazurite,
                                3 => Genshin.Ascension_NagadusEmerald,
                                4 => Genshin.Ascension_VajradaAmethyst,
                                5 => Genshin.Ascension_VayudaTurquoise,
                                6 => Genshin.Ascension_ShivadaJade,
                                7 => Genshin.Ascension_PrithivaTopaz,
                                _ => Common.Unknown
                            };
                            break;
                        default:
                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            break;
                    }
                    e.CellStyle.BackColor = (int)row.Cells[nameof(AscensionTypeID)].Value switch
                    {
                        1 => Element.GetBackgroundColor(Element.ElementType.Pyro),
                        2 => Element.GetBackgroundColor(Element.ElementType.Hydro),
                        3 => Element.GetBackgroundColor(Element.ElementType.Dendro),
                        4 => Element.GetBackgroundColor(Element.ElementType.Electro),
                        5 => Element.GetBackgroundColor(Element.ElementType.Anemo),
                        6 => Element.GetBackgroundColor(Element.ElementType.Cryo),
                        7 => Element.GetBackgroundColor(Element.ElementType.Geo),
                        _ => Element.GetBackgroundColor(Element.ElementType.Unknown)
                    };
                    break;
                // 精鋭ボス
                case nameof(ViewBossItem):
                    switch (Table.Columns[e.ColumnIndex].Name)
                    {
                        case nameof(BossItemNum):
                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            break;
                    }
                    break;
                // 精鋭ドロップ素材
                case nameof(ViewEnemyItems):
                    switch (Table.Columns[e.ColumnIndex].Name)
                    {
                        case nameof(EnemyItemCharacterNum):
                        case nameof(EnemyItemTalentNum):
                        case nameof(EnemyItemTotalNum):
                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            if (e.Value == null || (int)e.Value == 0) e.Value = "";
                            break;
                    }
                    var id = (int)row.Cells[nameof(EnemyItemID)].Value - 112002;
                    e.CellStyle.BackColor = (id % 3) switch
                    {
                        0 => Rarity.GetBackgroundColor(Rarity.RarityType.Common),
                        1 => Rarity.GetBackgroundColor(Rarity.RarityType.Uncommon),
                        2 => Rarity.GetBackgroundColor(Rarity.RarityType.Rare),
                        _ => Rarity.GetBackgroundColor(Rarity.RarityType.Unknown)
                    };
                    break;
                // 特産素材
                case nameof(ViewLocalSpecialtyItem):
                    switch (Table.Columns[e.ColumnIndex].Name)
                    {
                        case nameof(LocalSpecialtyItemNum):
                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            break;
                    }
                    break;
                // 天賦素材
                case nameof(ViewTalentItems):
                    switch (Table.Columns[e.ColumnIndex].Name)
                    {
                        case nameof(TalentItemName):
                            e.CellStyle.BackColor = ((int)row.Cells[nameof(TalentItemID)].Value / 3) switch
                            {
                                0 => World.GetBackgroundColor(World.Country.Mondstadt),
                                1 => World.GetBackgroundColor(World.Country.Liyue),
                                2 => World.GetBackgroundColor(World.Country.Inazuma),
                                3 => World.GetBackgroundColor(World.Country.Sumeru),
                                4 => World.GetBackgroundColor(World.Country.Fontaine),
                                5 => World.GetBackgroundColor(World.Country.Natlan),
                                6 => World.GetBackgroundColor(World.Country.Nodkrai),
                                7 => World.GetBackgroundColor(World.Country.Snezhnaya),
                                _ => World.GetBackgroundColor(World.Country.Unknown)
                            };
                            if(Store.Misaki_chan.Data.Info?.Localize.Talent.TryGetValue(LocalizeManager.CurrentShort,out var talents)==true) {
                                if ((int)row.Cells[nameof(TalentItemID)].Value < talents.Count)
                                {
                                    e.Value = talents[(int)row.Cells[nameof(TalentItemID)].Value];
                                }
                                else e.Value = Common.Unknown;
                            }
                            break;
                        case nameof(TalentItemOpenDays):
                            if (e.Value == null) return;
                            var color = Color.FromArgb(0xFF, 0xFF, 0xCC, 0xCC);
                            var today = -1;
                            switch (Server.GameServerDate(Account.Server).DayOfWeek)
                            {
                                case DayOfWeek.Monday:
                                case DayOfWeek.Thursday:
                                    today = 0;
                                    break;
                                case DayOfWeek.Tuesday:
                                case DayOfWeek.Friday:
                                    today = 1;
                                    break;
                                case DayOfWeek.Wednesday:
                                case DayOfWeek.Saturday:
                                    today = 2;
                                    break;
                            }
                            if ((int)e.Value == today || today < 0) e.CellStyle.BackColor = color;

                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            e.Value = (int)e.Value switch
                            {
                                0 => Common.Week_Mon_Thu,
                                1 => Common.Week_Tue_Fri,
                                2 => Common.Week_Wed_Sat,
                                _ => Common.Unknown
                            };
                            break;
                        default:
                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            break;
                    }
                    break;
                // 週ボスドロップ素材
                case nameof(ViewWeeklyBossItems):

                    switch (Table.Columns[e.ColumnIndex].Name)
                    {
                        case nameof(WeeklyBossItemNum):
                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            break;
                    }
                    break;
                case nameof(CharacterView):
                    var character = characterDatas.Find(a=>a.id==(int)row.Cells[nameof(CharacterViewID)].Value);
                    if (character==null)
                    {
                        return;
                    }
                    switch (Table.Columns[e.ColumnIndex].Name)
                    {
                        case nameof(CharacterViewRarity):
                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            if ($"{e.Value}" == "5") e.CellStyle.BackColor = Color.FromArgb(0xFF, 0xEE, 0xAA);
                            else if ($"{e.Value}" == "4") e.CellStyle.BackColor = Color.FromArgb(0xCC, 0xAA, 0xFF);
                            break;
                        case nameof(CharacterViewName):
                            e.CellStyle.BackColor = Element.GetBackgroundColor(character.element);
                            break;
                        case nameof(CharacterViewLocalItemNum):
                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            if (character.localitem == null) e.Value = "";
                            break;
                        case nameof(CharacterViewTalentName):
                            if (character.talent.type == -1)
                            {
                                e.Value = "";
                                break;
                            }
                            e.CellStyle.BackColor = (character.talent.type / 3) switch
                            {
                                0 => World.GetBackgroundColor(World.Country.Mondstadt),
                                1 => World.GetBackgroundColor(World.Country.Liyue),
                                2 => World.GetBackgroundColor(World.Country.Inazuma),
                                3 => World.GetBackgroundColor(World.Country.Sumeru),
                                4 => World.GetBackgroundColor(World.Country.Fontaine),
                                5 => World.GetBackgroundColor(World.Country.Natlan),
                                6 => World.GetBackgroundColor(World.Country.Nodkrai),
                                7 => World.GetBackgroundColor(World.Country.Snezhnaya),
                                _ => World.GetBackgroundColor(World.Country.Unknown)
                            };
                            if (Store.Misaki_chan.Data.Info?.Localize.Talent.TryGetValue(LocalizeManager.CurrentShort, out var talents) == true)
                            {
                                if (character.talent.type < talents.Count)
                                {
                                    e.Value = talents[character.talent.type];
                                }
                                else e.Value = Common.Unknown;
                            }
                            break;
                        case nameof(CharacterViewTalentDay):
                            if (character.talent.type == -1) break;
                            var color = Color.FromArgb(0xFF, 0xFF, 0xCC, 0xCC);
                            var today = -1;
                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            switch (Server.GameServerDate(Account.Server).DayOfWeek)
                            {
                                case DayOfWeek.Monday:
                                case DayOfWeek.Thursday:
                                    today = 0;
                                    break;
                                case DayOfWeek.Tuesday:
                                case DayOfWeek.Friday:
                                    today = 1;
                                    break;
                                case DayOfWeek.Wednesday:
                                case DayOfWeek.Saturday:
                                    today = 2;
                                    break;
                            }
                            if (character.talent.type%3 == today || today < 0) e.CellStyle.BackColor = color;
                            e.Value = (character.talent.type % 3) switch
                            {
                                0 => Common.Week_Mon_Thu,
                                1 => Common.Week_Tue_Fri,
                                2 => Common.Week_Wed_Sat,
                                _ => Common.Unknown
                            };
                            break;
                        case nameof(CharacterViewTalentTNum):
                        case nameof(CharacterViewTalentGNum):
                        case nameof(CharacterViewTalentPNum):
                            if(e.Value==null||(int)e.Value==0)e.Value = "";
                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            break;
                        case nameof(CharacterViewHerosWitNum):
                        case nameof(CharacterViewMoraTotal):
                            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            e.Value = $"{e.Value:#,##0}";
                            break;
                    }
                    break;
            }
        }

        private void ViewLeave(object sender, EventArgs e)
        {
            if (sender is DataGridView view)
            {
                view.ClearSelection();
            }
        }
        class CharacterData
        {
            public int star;
            public int id;
            public Core.General.Convert.Element.ElementType element;
            public string name = string.Empty;
            public int talentcrown = 0;
            public Talent talent = new();
            public Ascension ascension = new();
            public List<Item> items = new();
            public class Talent
            {
                public int type=-1;
                public int teaching;
                public int guide;
                public int philosophies;
            }
            public class Ascension
            {
                public int Sliver;
                public int Fragment;
                public int Chunk;
                public int Gemstone;
            }
            public class Item
            {
                public string iconurl = string.Empty;
                public int id;
                public string name = string.Empty;
                public int num;
            }
            public Item? localitem;
            public Item? enemybossitem;
            public Item? weeklybossitem;
            public int Mora;
            public int HerosWit;
        }
    }
}
