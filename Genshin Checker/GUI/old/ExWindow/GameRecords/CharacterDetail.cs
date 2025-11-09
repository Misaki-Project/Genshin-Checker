using Genshin_Checker.Core.HoYoLab;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Genshin_Checker.UI.Control.GameRecord.CharacterDetail;
using LiveChartsCore.Drawing;
using static Genshin_Checker.Core.HoYoLab.Account;
using Genshin_Checker.Window.Popup;
using Genshin_Checker.Resource.Languages;
using Genshin_Checker.Core.General.UI;
using Genshin_Checker.Store;
using System.Diagnostics;
using Genshin_Checker.Model.Misaki_chan.Character;
using Genshin_Checker.Core.General.Music;
using Genshin_Checker.Core;
using Genshin_Checker.Model.HoYoLab;
using Genshin_Checker.Model.EnkaNetwork.ShowCase;

namespace Genshin_Checker.Window.ExWindow.GameRecords
{
    public partial class CharacterDetail : Form
    {
        PictureBox picture;
        List<TalentInfo> MainTalent;
        List<TalentInfo> SubTalent;
        WeaponDetail WeaponDetail;
        List<ConstellationInfo> Constellation;
        List<ArtifactInfo> ArtifactInfos;
        List<UI.Control.GameRecord.CharacterDetail.CharacterStory> CharacterStories;
        Image? CharacterBanner;
        List<Button> TrailerVideoButtons;
        private CancellationTokenSource? cts;
        private SemaphoreSlim _semaphore;
        bool IsLoading = false;
        int TempCharacterID = 0;
        public CharacterDetail()
        {
            InitializeComponent();
            Icon = Icon.FromHandle(resource.icon.Battle_Chronicle.GetHicon());
            picture = new();
            panel1.Resize += pictureBox1_Resize;
            panel1.Controls.Add(picture);
            picture.Location = new(0, 0);
            picture.SizeMode = PictureBoxSizeMode.AutoSize;
            MainTalent = new();
            SubTalent = new();
            Constellation = new();
            ArtifactInfos = new();
            TrailerVideoButtons = new();
            CharacterStories = new();
            _semaphore = new SemaphoreSlim(1, 1);
            WeaponDetail = new WeaponDetail() { Dock = DockStyle.Top };
            groupBox2.Controls.Add(WeaponDetail);
        }
        public async void DataUpdate(Account account, int characterID, string name)
        {

            await _semaphore.WaitAsync(); // ロックを取得する
            try
            {
                if (TempCharacterID == characterID) return;
                TempCharacterID = characterID;
                if (cts != null) cts.Cancel(true);
                cts = new();
                if (Store.EnkaData.Data.Characters == null) return;

                //キャラクター情報の取得
                var Detail = await account.Characters.GetData();
                var CharacterInfo = await account.CharacterDetail.GetData(characterID);
                var staticinfo = Misaki_chan.Data.Characters?.Data.Find(a => a.Id == characterID);
                if (CharacterInfo == null) throw new ArgumentNullException(Localize.Error_CharacterDetail_DontHaveCharacter);


                string gacha = "";
                try
                {
                    var character = Store.EnkaData.Data.Characters[$"{characterID}"];
                    gacha = character.SideIconName.Replace("UI_AvatarIcon_Side_", "UI_Gacha_AvatarImg_");
                }
                catch (Exception)
                {
                    Dialog.Error(Localize.Error_CharacterDetail_CharacterDetailIsMissing, string.Format(Localize.Error_CharacterDetail_CharacterDetailIsMissing_Message, CharacterInfo.baseInfo.name));
                }

                Text = $"{Localize.WindowName_CharacterDetail} - {CharacterInfo.baseInfo.name} (UID:{account.UID})";
                IsLoading = true;
                ImageReload(true);
                //概要
                label1.Text = $"{name}";
                label2.Text = string.Format(Localize.UI_Character_Level, CharacterInfo.baseInfo.level);
                label4.Text = CharacterInfo.baseInfo.fetter > 0 ? string.Format(Localize.UI_FriendshipLevel, CharacterInfo.baseInfo.fetter) : "";
                if (staticinfo?.Profile.Description.TryGetValue(LocalizeManager.CurrentShort, out var desc) == true)
                {
                    LabelCharacterDescription.Text = desc;
                    LabelCharacterDescription.Visible = true;
                }
                else
                {
                    LabelCharacterDescription.Visible = false;
                }
                int constellation = CharacterInfo.constellations.FindAll(a => a.is_actived).Count;
                if (constellation == 0) label5.Text = "";
                else if (constellation == 6) label5.Text = Localize.UI_ConstellationLevelMax;
                else label5.Text = string.Format(Localize.UI_ConstellationLevel, constellation);
                pictureBox1.Image = await Core.WebRequest.ImageGetRequest($"https://static-api.misaki-chan.world/genshin-checker/asset/element/type-1/{CharacterInfo.baseInfo.element.ToLower()}.png");

                //武器
                var weapon = CharacterInfo.weapon;
                WeaponDetail.UpdateData(weapon.rarity, weapon.icon, weapon.name, weapon.level, weapon.affix_level);

                //天賦
                Panel_MainTalent.SuspendLayout();
                Panel_SubTalent.SuspendLayout();
                foreach (var control in MainTalent)
                {
                    Panel_MainTalent.Controls.Remove(control);
                    control.Dispose();
                }
                MainTalent.Clear();
                foreach (var control in SubTalent)
                {
                    Panel_SubTalent.Controls.Remove(control);
                    control.Dispose();
                }
                SubTalent.Clear();
                panel5.Visible = true;
                try
                {
                    var data = await account.CharacterDetail.GetData(characterID);
                    var list = data.skills.FindAll(a => Core.General.Convert.Character.GetSkillGrowthable(a.skill_id) && a.skill_type == 1);
                    list.Reverse();
                    int cnt = 0;
                    foreach (var main in list)
                    {
                        int? triggerConstellations = null;
                        int add_levels = 0;
                        bool enabled = false;
                        switch (cnt)
                        {
                            case 2:
                                triggerConstellations = staticinfo?.Skills.Upgrade_skills.Normal?.Constellations;
                                //add_levels = staticinfo?.Skills.Upgrade_skills.Normal?.Add_level ?? 0;
                                break;
                            case 1:
                                triggerConstellations = staticinfo?.Skills.Upgrade_skills.Skill?.Constellations;
                                //add_levels = staticinfo?.Skills.Upgrade_skills.Skill?.Add_level ?? 0;
                                break;
                            case 0:
                                triggerConstellations = staticinfo?.Skills.Upgrade_skills.Burst?.Constellations;
                                //add_levels = staticinfo?.Skills.Upgrade_skills.Burst?.Add_level ?? 0;
                                break;
                        }
                        if (triggerConstellations != null && CharacterInfo.baseInfo.actived_constellation_num >= triggerConstellations)
                        {
                            enabled = true;
                        }
                        var info = new TalentInfo(main.icon, main.name, string.Format(Localize.UI_Talent_Level, main.level + (enabled ? add_levels : 0)), enabled, "概要をここに(未実装)");
                        info.Dock = DockStyle.Top;
                        Panel_MainTalent.Controls.Add(info);
                        MainTalent.Add(info);
                        cnt++;
                    }
                    var li = data.skills.FindAll(a => { foreach (var b in list) if (b.skill_id == a.skill_id) return false; return true; });
                    li.Reverse();
                    foreach (var sub in li)
                    {
                        var level = "";
                        if (sub.level > 1) level = string.Format(Localize.UI_Talent_Level, sub.level);
                        var info = new TalentInfo(sub.icon, sub.name, level, false, "概要をここに(未実装)");
                        info.Dock = DockStyle.Top;
                        Panel_SubTalent.Controls.Add(info);
                        SubTalent.Add(info);
                    }
                    Error_TalentPanel.Visible = false;
                    Button_TalentHideShow.Visible = true;
                }
                catch (HoYoLabAPIException ex)
                {
                    string hint = "";
                    switch (ex.Retcode)
                    {
                        case -502002:
                            hint = HoYoLabAPIRetcode._502002;
                            break;
                    }
                    textBox1.Lines = string.Format(Localize.Error_CharacterDetail_HoYoLabAPIException, ex.Retcode, ex.APIMessage, hint).Split('\n');
                    Error_TalentPanel.Visible = true;
                    Button_TalentHideShow.Visible = false;

                }
                catch (Exception ex)
                {

                    textBox1.Lines = $"{ex.GetType()}\n{ex.Message}".Split('\n');
                    Error_TalentPanel.Visible = true;
                    Button_TalentHideShow.Visible = false;
                    Dialog.Error(Localize.Error_CharacterDetail_FailToLoadTalentInfomation, ex.ToString());
                }

                Panel_MainTalent.ResumeLayout(true);
                Panel_SubTalent.ResumeLayout(true);
                //命ノ星座
                ConstellationPanel.SuspendLayout();
                foreach (var control in Constellation)
                {
                    ConstellationPanel.Controls.Remove(control);
                    control.Dispose();
                }
                Constellation.Clear();
                for (int i = 6; i >= 1; i--)
                {
                    var data = CharacterInfo.constellations.Find(a => a.pos == i);
                    if (data == null) continue;
                    var info = new ConstellationInfo(data.name, data.icon, data.is_actived)
                    {
                        Dock = DockStyle.Top
                    };
                    ConstellationPanel.Controls.Add(info);
                    Constellation.Add(info);
                }
                ConstellationPanel.ResumeLayout(true);
                //聖遺物
                ArtifactLayout.SuspendLayout();
                foreach (var control in ArtifactInfos)
                {
                    ArtifactLayout.Controls.Remove(control);
                    control.Dispose();
                }
                ArtifactInfos.Clear();
                for (int i = 1; i <= 5; i++)
                {
                    var data = CharacterInfo.relics.Find(a => a.pos == i);
                    ArtifactInfo? control = null;
                    if (data == null)
                    {
                        control = new(i, null, 0, Localize.WindowName_CharacterDetail_NoArtifact);
                    }
                    else
                    {
                        control = new(i, data.icon, data.rarity, $"+{data.level}");
                    }
                    ArtifactLayout.Controls.Add(control);
                    ArtifactInfos.Add(control);
                }
                ArtifactLayout.ResumeLayout(true);
                //トレーラービデオ
                VideoListPanel.SuspendLayout();
                foreach (var control in TrailerVideoButtons)
                {
                    VideoListPanel.Controls.Remove(control);
                    control.Dispose();
                }
                TrailerVideoButtons.Clear();
                groupBox5.Visible = false;
                if (staticinfo?.Wiki.Video != null)
                {
                    var addbutton = new Action<string, string, string?>((string controlname, string ytid, string? title) =>
                    {
                        var b = new Button();
                        b.Text = controlname;
                        b.Click += (s, e) => { new WebMiniBrowser(new($"https://static-api.misaki-chan.world/embed/youtube.html?v={ytid}&t={System.Web.HttpUtility.UrlEncode(title)}"), size: new(1280, 720 + SystemInformation.CaptionHeight + 7), urlboxshow: false).Show(); };
                        b.AutoSize = true;
                        VideoListPanel.Controls.Add(b);
                        TrailerVideoButtons.Add(b);
                        groupBox5.Visible = true;
                    });
                    var addsong = new Action<string, string, string>((string controlname, string url, string title) =>
                    {
                        var b = new Button();
                        b.Text = controlname;
                        b.Click += (s, e) =>
                        {
                            Player.Instance.AddQueue($"https://static-api.misaki-chan.world/{url}", title);
                            Genshin_Checker.Core.General.ManageWindow.OpenWindow(null, nameof(MusicPlayer));
                        };
                        b.AutoSize = true;
                        VideoListPanel.Controls.Add(b);
                        TrailerVideoButtons.Add(b);
                        groupBox5.Visible = true;
                    });
                    foreach (var video in staticinfo.Wiki.Video)
                    {
                        string title = video.Key;
                        if (Misaki_chan.Data.Info?.Localize.Wiki.TryGetValue("video", out var LocalizeVideo) == true &&
                            LocalizeVideo.TryGetValue(video.Key, out var Video) &&
                            Video.TryGetValue(LocalizeManager.CurrentShort, out var text))
                        {
                            title = text;
                        }
                        foreach (var lang in video.Value)
                        {
                            string langname;
                            if (lang.Key == "none")
                            {
                                langname = "";
                            }
                            else if (Misaki_chan.Data.Info?.Localize.Lang.TryGetValue(LocalizeManager.CurrentShort, out var LocalizeLang) == true &&
                            LocalizeLang.TryGetValue(lang.Key, out var languageText))
                            {
                                langname = $" ({languageText})";
                            }
                            else
                            {
                                langname = $" ({lang.Key})";
                            }
                            addbutton($"{title}{langname}", lang.Value.Ytid, lang.Value.Title);
                        }
                    }
                    foreach (var music in staticinfo.Wiki.Music)
                    {
                        string MusicText = music.Key;
                        if (Misaki_chan.Data.Info?.Localize.Wiki.TryGetValue("music", out var Localize_music) == true &&
                            Localize_music.TryGetValue(music.Key, out var music_type) &&
                            music_type.TryGetValue(LocalizeManager.CurrentShort, out var text))
                        {
                            MusicText = text;
                        }
                        addsong(MusicText, music.Value.Path,
                            music.Value.Title.TryGetValue(LocalizeManager.CurrentShort, out var lng) ? lng :
                            music.Value.Title.TryGetValue("en", out var lng2) ? lng2 : Common.Unknown);
                    }

                }

                VideoListPanel.ResumeLayout(true);
                //キャラクターストーリー
                GroupCharacterStory.SuspendLayout();
                foreach (var ui in CharacterStories)
                {
                    GroupCharacterStory.Controls.Remove(ui);
                    ui.Dispose();
                }
                CharacterStories.Clear();
                var charastory = Misaki_chan.Data.CharacterStory?.Data.Find(a => a.ID == characterID);
                if (charastory != null)
                {
                    GroupCharacterStory.Visible = true;
                    for (int i = charastory.Story.Count - 1; i >= 0; i--)
                    {
                        var story = charastory.Story.ElementAt(i);
                        string lang = story.Key;
                        if (Store.Misaki_chan.Data.Info?.Localize.Wiki.TryGetValue("character-story", out var localize_story) == true)
                            if (localize_story.TryGetValue(story.Key, out var langs))
                                if (langs.TryGetValue(LocalizeManager.CurrentShort, out var outlang))
                                {

                                    lang = outlang;
                                }
                        string? UnlockInfomation = null;
                        string title = story.Value.Title ?? lang;
                        if (staticinfo != null && staticinfo.Unlocks.Story.TryGetValue(story.Key, out var unlocks))
                        {
                            bool IsUnlocked = false;
                            if (unlocks.Conditions.friendship != null && CharacterInfo.baseInfo.fetter >= unlocks.Conditions.friendship) IsUnlocked = true;
                            else UnlockInfomation = $"キャラクターの好感度がLv. {unlocks.Conditions.friendship} で解禁";
                            if (unlocks.Conditions.Never)
                            {
                                UnlockInfomation = "このストーリーが解禁される時はまだ訪れていません。";
                                IsUnlocked = false;
                            }
                            if (!IsUnlocked && unlocks.IsHiddenTitle) title = "???";
                        }
                        ;
                        var ui = new CharacterStory(title, UnlockInfomation, story.Value.Text, UnlockInfomation != null);
                        ui.Dock = DockStyle.Top;
                        ui.BorderStyle = BorderStyle.FixedSingle;
                        GroupCharacterStory.Controls.Add(ui);
                        CharacterStories.Add(ui);
                    }
                }
                else
                    GroupCharacterStory.Visible = false;
                GroupCharacterStory.ResumeLayout(true);


                if (!string.IsNullOrEmpty(gacha))
                {
                    CharacterBanner = await Core.WebRequest.ImageGetRequest($"https://enka.network/ui/{gacha}.png", cts.Token);
                    IsLoading = false;
                }
                ImageReload(true);
                Application.DoEvents();
                AutoScrollPosition = new(0, -100000);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void pictureBox1_Resize(object? sender, EventArgs e)
        {
            ImageReload();
        }
        void ImageReload(bool reload = false)
        {
            if (IsLoading)
            {
                var old = picture.Image;
                picture.Image = resource.GenshinAsset.loading;
                picture.Dock = DockStyle.Fill;
                picture.SizeMode = PictureBoxSizeMode.CenterImage;
                old?.Dispose();
            }
            else
            {
                if (CharacterBanner != null && (panel1.Height != picture.Height || reload))
                {
                    var old = picture.Image;
                    double zoom = (double)panel1.Height / CharacterBanner.Height;
                    Size size = new((int)(CharacterBanner.Width * zoom), (int)(CharacterBanner.Height * zoom));
                    if (size.Width <= 0 || size.Height <= 0) return;
                    picture.Dock = DockStyle.None;
                    picture.SizeMode = PictureBoxSizeMode.AutoSize;
                    picture.Image = DrawControl.BitmapInterpolation(CharacterBanner, (int)(CharacterBanner.Width * zoom), (int)(CharacterBanner.Height * zoom));
                    old?.Dispose();
                }
                picture.Location = new(-picture.Width / 2 + panel1.Width / 2, 0);
            }
        }

        private void Button_TalentHideShow_Click(object sender, EventArgs e)
        {
            Panel_SubTalent.Visible = !Panel_SubTalent.Visible;
            if (Panel_SubTalent.Visible)
            {
                Button_TalentHideShow.Text = Localize.WindowName_CharacterDetail_Talent_Hide;
            }
            else
            {
                Button_TalentHideShow.Text = Localize.WindowName_CharacterDetail_Talent_ShowMore;
            }
        }

        private void CharacterDetail_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void panel6_Resize(object sender, EventArgs e)
        {
            LabelCharacterDescription.MaximumSize = new(panel6.Size.Width, 0);
        }
    }
}
