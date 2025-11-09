using Genshin_Checker.Model.UserData.ImaginariumTheater.v2;
using Genshin_Checker.Model.UserData.ImaginariumTheater.v3;
using Detail = Genshin_Checker.Model.UserData.ImaginariumTheater.v3.Detail;
using Round = Genshin_Checker.Model.UserData.ImaginariumTheater.v3.Round;
using Avatar = Genshin_Checker.Model.UserData.ImaginariumTheater.v3.Avatar;
using Buff = Genshin_Checker.Model.UserData.ImaginariumTheater.v3.Buff;
using Enemy = Genshin_Checker.Model.UserData.ImaginariumTheater.v3.Enemy;
using Genshin_Checker.Model.HoYoLab.RoleCombat;
using ResultStatus = Genshin_Checker.Model.UserData.ImaginariumTheater.v3.ResultStatus;
using AvatarResult = Genshin_Checker.Model.UserData.ImaginariumTheater.v3.AvatarResult;


namespace Genshin_Checker.Model.UserData.ImaginariumTheater
{
    public static partial class Convert
    {
        public static V3 FromV2(V2 oldData)
        {
            V3 newData = new()
            {
                Version = 3,
                UID = oldData.UID,
                UpdateUTC = DateTime.UtcNow,
                Data = new()
                {

                    IsUnlock = oldData.Data.IsUnlock,
                    schedule_id = oldData.Data.schedule_id,
                    schedule_type = oldData.Data.schedule_type,
                    ScheduleTime = new()
                    {
                        start = oldData.Data.ScheduleTime.start,
                        end = oldData.Data.ScheduleTime.end
                    },
                    CurrentStats = new()
                    {
                        avatar_bonus_num = oldData.Data.CurrentStats.avatar_bonus_num,
                        difficulty_id = oldData.Data.CurrentStats.difficulty_id,
                        get_medal_round_list = oldData.Data.CurrentStats.get_medal_round_list.ToList(),
                        rent_cnt = oldData.Data.CurrentStats.rent_cnt,
                        medal_num = oldData.Data.CurrentStats.medal_num,
                        max_round_id = oldData.Data.CurrentStats.max_round_id,
                        tarot_finished_cnt = 0,
                        heraldry = oldData.Data.CurrentStats.heraldry,
                        coin_num = oldData.Data.CurrentStats.coin_num
                    },
                    Detail = oldData.Data.Detail.Select(d => new Detail
                    {
                        rounds_data = d.rounds_data.Select(r => new Round
                        {
                            avatars = r.avatars.Select(a => new Avatar
                            {
                                avatar_id = a.avatar_id,
                                avatar_type = a.avatar_type,
                                element = a.element,
                                image = a.image,
                                level = a.level,
                                rarity = a.rarity
                            }).ToList(),
                            choice_cards = r.choice_cards.Select(b => new Buff
                            {
                                icon = b.icon,
                                name = b.name,
                                desc = b.desc,
                                is_enhanced = b.is_enhanced,
                                id = b.id
                            }).ToList(),
                            buffs = new v3.Buffs()
                            {
                                ShiningBless = r.buffs.ShiningBless != null ? new()
                                {
                                    summary = new()
                                    {
                                        total_level = r.buffs.ShiningBless.summary.total_level,
                                        desc = r.buffs.ShiningBless.summary.desc
                                    },
                                    buffs = r.buffs.ShiningBless.buffs.Select(b => new SplendourBuffInfo
                                    {
                                        icon = b.icon,
                                        name = b.name,
                                        level = b.level,
                                        level_effect = b.level_effect.Select(le => new SplendourBuffEffect
                                        {
                                            icon = le.icon,
                                            name = le.name,
                                            desc = le.desc
                                        }).ToList(),
                                    }).ToList()
                                } : null,
                                WonderSupport = r.buffs.WonderSupport?.Select(b => new Buff
                                {
                                    icon = b.icon,
                                    name = b.name,
                                    desc = b.desc,
                                    is_enhanced = b.is_enhanced,
                                    id = b.id
                                }).ToList()
                            },
                            is_get_medal = r.is_get_medal,
                            round_id = r.round_id,
                            finish_time = r.finish_time,
                            enemy = r.enemy.Select(e => new Enemy()
                            {
                                id = e.id,
                                name = e.name,
                                icon = e.icon,
                                level = e.level
                            }).ToList(),
                            is_tarot = false,
                            tarot_serial_no = -1

                        }).ToList(),
                        Stats = new()
                        {
                            avatar_bonus_num = d.Stats.avatar_bonus_num,
                            difficulty_id = d.Stats.difficulty_id,
                            get_medal_round_list = d.Stats.get_medal_round_list.ToList(),
                            rent_cnt = d.Stats.rent_cnt,
                            medal_num = d.Stats.medal_num,
                            max_round_id = d.Stats.max_round_id,
                            heraldry = d.Stats.heraldry,
                            coin_num = d.Stats.coin_num,
                            tarot_finished_cnt = 0
                        },
                        backup_avatars = d.backup_avatars.Select(a => new Avatar
                        {
                            avatar_id = a.avatar_id,
                            avatar_type = a.avatar_type,
                            element = a.element,
                            image = a.image,
                            level = a.level,
                            rarity = a.rarity
                        }).ToList(),
                        result_status = d.result_status != null ? new ResultStatus
                        {
                            max_defeat_avatar = d.result_status.max_defeat_avatar.Select(ar => new AvatarResult
                            {
                                avatar_id = ar.avatar_id,
                                avatar_icon = ar.avatar_icon,
                                value = ar.value,
                                rarity = ar.rarity
                            }).ToList(),
                            max_damage_avatar = d.result_status.max_damage_avatar.Select(ar => new AvatarResult
                            {
                                avatar_id = ar.avatar_id,
                                avatar_icon = ar.avatar_icon,
                                value = ar.value,
                                rarity = ar.rarity
                            }).ToList(),
                            max_take_damage_avatar = d.result_status.max_take_damage_avatar.Select(ar => new AvatarResult
                            {
                                avatar_id = ar.avatar_id,
                                avatar_icon = ar.avatar_icon,
                                value = ar.value,
                                rarity = ar.rarity
                            }).ToList(),
                            shortest_avatar_list = d.result_status.shortest_avatar_list.Select(ar => new AvatarResult
                            {
                                avatar_id = ar.avatar_id,
                                avatar_icon = ar.avatar_icon,
                                value = ar.value,
                                rarity = ar.rarity
                            }).ToList(),
                            ButtleTime = d.result_status.ButtleTime
                        } : null,
                        UpdateAt = d.UpdateAt,
                        FirstRoundTime = d.FirstRoundTime,
                        FinalRoundTime = d.FinalRoundTime
                    }).ToList().FindAll(a=>a.FirstRoundTime != DateTime.MaxValue)
                }
            };

            return newData;
        }
    }
}