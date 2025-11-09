using Genshin_Checker.Core.General;
using Genshin_Checker.Model.UserData.ImaginariumTheater.v3;
using Genshin_Checker.Model.UserData.ImaginariumTheater.v2;
using Genshin_Checker.Model.UserData.ImaginariumTheater.v1;
using Genshin_Checker.Resource.Languages;
using Genshin_Checker.Core.General.Convert;
using Newtonsoft.Json;
using System.IO;

namespace Genshin_Checker.Core.HoYoLab
{

    public class ImaginariumTheater : Base
    {
        public ImaginariumTheater(Account account) : base(account, 5000)
        {
            ServerUpdate.Elapsed += Timeout_Tick;
        }
        private Model.HoYoLab.RoleCombat.Data? RoleCombat = null;
        private string REG_PATH { get => $"UserData\\{account.UID}\\ImaginariumTheater"; }
        public V3? Current { get; private set; }
        bool IsFirstCheck = false;
        internal async void Timeout_Tick(object? sender, EventArgs e)
        {
            ServerUpdate.Stop();
            Log.Debug("幻想シアターを取得");
            await ScheduleReload();
            ServerUpdate.Interval = (account.LatestActiveSession > DateTime.UtcNow.AddHours(-2) || account.LatestActivity == Game.ProcessTime.ProcessState.Foreground )? 600000 : 3600000 * 3;
            ServerUpdate.Start();
        }
        private async Task ScheduleReload()
        {
            try
            {
                var Data = await GetData();
                await SaveDatabase(Data);

            }catch(Exception ex)
            {
                Log.Error(ex);
            }
        }
        public async Task<V3> Load(int id)
        {
            var path = Registry.GetValue(REG_PATH, $"{id}", true) ?? throw new IOException(Localize.Error_SpiralAbyssFile_RegistryNotFound);
            var data = await AppData.LoadUserData(path);
            if (string.IsNullOrEmpty(data)) throw new InvalidDataException("Data is empty.");
            var ver = JsonChecker<Model.UserData.DatabaseRoot>.Check(data ?? "{}");
            V3? v3 = (ver?.Version) switch
            {
                null => throw new InvalidDataException(Localize.Error_SpiralAbyssFile_InvalidFileVersion),
                1 => Model.UserData.ImaginariumTheater.Convert.FromV2(Model.UserData.ImaginariumTheater.Convert.FromV1(JsonChecker<V1>.Check(data ?? ""))),
                2 => Model.UserData.ImaginariumTheater.Convert.FromV2(JsonChecker<V2>.Check(data ?? "")),
                3 => JsonChecker<V3>.Check(data ?? ""),
                _ => throw new InvalidDataException(string.Format(Localize.Error_SpiralAbyssFile_UnknownFileVersion, ver.Version)),
            } ?? throw new InvalidDataException(Localize.Error_SpiralAbyssFile_FailedConvert);
            if (ver?.Version < 3)
            {
                Log.Info("バージョンアップグレードします。");
                await Save(v3);
            }
            if (v3.UID != account.UID) throw new InvalidDataException(string.Format(Localize.Error_SpiralAbyssFile_DoesNotMatchUID, v3.UID, account.UID));
            return v3;
        }
        public async Task<Model.HoYoLab.RoleCombat.Data> GetData()
        {
            var data = await account.Endpoint.GetRoleCombat(true);
            RoleCombat = data;
            return data;
        }

        private async Task Save(V3 v3)
        {
            string? path = Registry.GetValue(REG_PATH, $"{v3.Data.schedule_id}", true);
            if (path == null)
            {
                path = AppData.GetRandomPath();
                Registry.SetValue(REG_PATH, $"{v3.Data.schedule_id}", path, true);

            }
            await AppData.SaveUserData(path, JsonConvert.SerializeObject(v3));
        }
        private async Task<int> SaveDatabase(Model.HoYoLab.RoleCombat.Data raw)
        {
            Log.Info("幻想シアターのデータを保存します。");
            int CountOfNewData = 0;
            foreach (var index in raw.data)
            {
                var userdata = new V3();
                #region ユーザーデータベースから過去の情報読み出し
                Log.Debug($"幻想シアター {index.schedule.schedule_id} 期");
                var path = Registry.GetValue(REG_PATH, $"{index.schedule.schedule_id}", true);
                if (path != null&& AppData.IsExistFile(path))
                {
                    Log.Debug($"→データが見つかりました。");
                    userdata = await Load(index.schedule.schedule_id);
                    if(userdata.UID != account.UID)
                        throw new InvalidDataException(
                            string.Format(Localize.Error_SpiralAbyssFile_DoesNotMatchUID, userdata.UID, account.UID));
                }
                #endregion
                #region 取得したデータが重複しているかチェック
                userdata.UID = account.UID;
                userdata.UpdateUTC = DateTime.UtcNow;
                userdata.Version = 3; //v1
                var starttime = DateTime.MaxValue;
                var endtime = DateTime.MinValue;
                foreach (var round in index.detail?.rounds_data ?? new())
                {
                    if(!long.TryParse(round.finish_time, out long t))continue;
                    var time = Time.GetUTCFromUnixTime(t);
                    if (starttime > time) starttime = time;
                    if (endtime < time) endtime = time;
                }

                Log.Debug($"取得したデータは {starttime} - {endtime} です。");
                if (Current == null || Current.Data.schedule_id <= userdata.Data.schedule_id) Current = userdata;
                bool IsNextMove = false;
                var game = userdata.Data.Detail.Find(a =>
                {
                    var start = a.FirstRoundTime;
                    var end = a.FinalRoundTime;
                    if (start == starttime)
                    {
                        if (end != endtime)
                        {
                            Log.Warn($"EndTime is Invalid {endtime} -> {end}");
                        }
                        else IsNextMove=true;
                        return true;
                    }
                    else return false;
                });
                #endregion
                if (IsNextMove&&IsFirstCheck)
                {
                    Log.Debug($"→データに更新が無さそうなのでスキップします。");
                    continue; //データに更新が無い場合はスキップ
                }
                else
                {
                    IsFirstCheck = true;
                }
                bool IsNewData = game == null;
                game ??= new();
                #region 内容のコピー

                //ここはスケジュール情報
                if(int.TryParse(index.schedule.start_time,out var start)) userdata.Data.ScheduleTime.start = Time.GetUTCFromUnixTime(start);
                if(int.TryParse(index.schedule.end_time,out var end)) userdata.Data.ScheduleTime.end = Time.GetUTCFromUnixTime(end);
                userdata.Data.schedule_id = index.schedule.schedule_id;
                userdata.Data.schedule_type = index.schedule.schedule_type;
                userdata.Data.IsUnlock = index.has_data;

                //ここは現時点でのデータ
                userdata.Data.CurrentStats.heraldry = index.stat.heraldry;
                userdata.Data.CurrentStats.rent_cnt = index.stat.rent_cnt;
                userdata.Data.CurrentStats.max_round_id = index.stat.max_round_id;
                userdata.Data.CurrentStats.medal_num = index.stat.medal_num;
                userdata.Data.CurrentStats.avatar_bonus_num = index.stat.avatar_bonus_num;
                userdata.Data.CurrentStats.coin_num = index.stat.coin_num;
                userdata.Data.CurrentStats.difficulty_id = index.stat.difficulty_id;
                userdata.Data.CurrentStats.get_medal_round_list.Clear();
                userdata.Data.CurrentStats.tarot_finished_cnt = index.stat.tarot_finished_cnt;
                foreach (var i in index.stat.get_medal_round_list) userdata.Data.CurrentStats.get_medal_round_list.Add(i);

                game.backup_avatars.Clear();
                game.rounds_data.Clear();
                game.Stats.heraldry = index.stat.heraldry;
                game.Stats.rent_cnt = index.stat.rent_cnt;
                game.Stats.avatar_bonus_num = index.stat.avatar_bonus_num;
                game.Stats.coin_num = index.stat.coin_num;
                game.Stats.difficulty_id = index.stat.difficulty_id;
                game.Stats.max_round_id = index.stat.max_round_id;
                game.Stats.medal_num = index.stat.medal_num;
                game.Stats.get_medal_round_list.Clear();
                if (index.detail != null && index.detail.fight_statisic.is_show_battle_stats)
                {
                    game.result_status = new();
                    var stats = index.detail.fight_statisic;
                    var data = stats.max_damage_avatar;
                    if (data != null)
                        game.result_status.max_damage_avatar.Add(new()
                        {
                            avatar_icon = data.avatar_icon,
                            avatar_id = data.avatar_id,
                            rarity = data.rarity,
                            value = data.value,
                        });
                    data = stats.max_take_damage_avatar;
                    if (data != null)
                        game.result_status.max_take_damage_avatar.Add(new()
                        {
                            avatar_icon = data.avatar_icon,
                            avatar_id = data.avatar_id,
                            rarity = data.rarity,
                            value = data.value,
                        });
                    data = stats.max_defeat_avatar;
                    if (data != null)
                        game.result_status.max_defeat_avatar.Add(new()
                        {
                            avatar_icon = data.avatar_icon,
                            avatar_id = data.avatar_id,
                            rarity = data.rarity,
                            value = data.value,
                        });
                    foreach (var data2 in stats.shortest_avatar_list)
                        game.result_status.shortest_avatar_list.Add(new()
                        {
                            avatar_icon = data2.avatar_icon,
                            avatar_id = data2.avatar_id,
                            rarity = data2.rarity,
                            value = data2.value,
                        });
                    game.result_status.ButtleTime = stats.total_use_time;
                }

                foreach (var i in index.stat.get_medal_round_list) game.Stats.get_medal_round_list.Add(i);
                if (index.detail != null)
                {
                    foreach(var backup in index.detail.backup_avatars)
                    {
                        game.backup_avatars.Add(new()
                        {
                            avatar_id = backup.avatar_id,
                            avatar_type = backup.avatar_type,
                            element = backup.element,
                            image = backup.image,
                            level = backup.level,
                            rarity = backup.rarity,
                        });
                    }
                    game.FirstRoundTime = DateTime.MaxValue;
                    game.FinalRoundTime = DateTime.MinValue;
                    game.UpdateAt = DateTime.UtcNow;
                    foreach(var round in index.detail.rounds_data)
                    {
                        if (long.TryParse(round.finish_time, out var f))
                        {
                            var finish = Time.GetUTCFromUnixTime(f);
                            if (game.FirstRoundTime > finish) game.FirstRoundTime = finish;
                            if (game.FinalRoundTime < finish) game.FinalRoundTime = finish;
                        }
                        long finishTime = 0;
                        if (!long.TryParse(round.finish_time, out finishTime)) finishTime = 0;
                        game.rounds_data.Add(new()
                        {
                            finish_time = Time.GetUTCFromUnixTime(finishTime),
                            round_id = round.round_id,
                            is_get_medal = round.is_get_medal,
                            is_tarot = round.is_tarot,
                            tarot_serial_no = round.talot_serial_no,
                        });
                        var roundsdata = game.rounds_data[^1];
                        foreach (var avatar in round.avatars)
                            roundsdata.avatars.Add(new()
                            {
                                avatar_id = avatar.avatar_id,
                                avatar_type = avatar.avatar_type,
                                element = avatar.element,
                                image = avatar.image,
                                level = avatar.level,
                                rarity = avatar.rarity,
                            });
                        //幻想シアターv1のみ
                        foreach (var buff in round.buffs)
                        {
                            if (roundsdata.buffs.WonderSupport == null) roundsdata.buffs.WonderSupport = new();
                            roundsdata.buffs.WonderSupport.Add(new()
                            {
                                id = buff.id,
                                name = buff.name,
                                icon = buff.icon,
                                desc = buff.desc,
                                is_enhanced = buff.is_enhanced,
                            });
                        }
                        //幻想シアターv2
                        if(round.splendour_buff != null)
                        {
                            roundsdata.buffs.ShiningBless = new();
                            var data = roundsdata.buffs.ShiningBless;
                            data.summary.total_level = round.splendour_buff.summary.total_level;
                            data.summary.desc = round.splendour_buff.summary.desc;

                            foreach (var buff in round.splendour_buff.buffs)
                                data.buffs.Add(new()
                                {
                                    icon = buff.icon,
                                    level = buff.level,
                                    level_effect = buff.level_effect,
                                    name = buff.name
                                });
                        }
                        foreach (var enemy in round.enemies)
                            roundsdata.enemy.Add(new()
                            {
                                icon = enemy.icon,
                                level = enemy.level,
                                name = enemy.name,
                                //ToDo: 将来的にIDを推測する
                                id = -1
                            });


                        foreach (var card in round.choice_cards)
                            roundsdata.choice_cards.Add(new()
                            {
                                id = card.id,
                                name = card.name,
                                icon = card.icon,
                                desc = card.desc,
                                is_enhanced = card.is_enhanced,
                            });
                    }
                }
                #endregion
                CountOfNewData++;
                Log.Debug($"データ : {game.FinalRoundTime.AddHours(9)} 難易度{game.Stats.difficulty_id} ☆{game.Stats.medal_num}");
                if (IsNewData && game.FirstRoundTime != DateTime.MaxValue && game.FinalRoundTime != DateTime.MinValue)
                {
                    Log.Debug($"→今回取得したデータは新しい為追加します。");
                    userdata.Data.Detail.Add(game);
                }
                userdata.Data.Detail.RemoveAll(a => a.FinalRoundTime == DateTime.MinValue && a.FirstRoundTime == DateTime.MaxValue);
                await Save(userdata);
                Log.Info($"第 {userdata.Data.schedule_id} 期保存しました。");
            }
            if (CountOfNewData == 0) Log.Info("今回は保存されませんでした。");
            return CountOfNewData;
        }
    }
}
