using Newtonsoft.Json;
using System.Globalization;
using Genshin_Checker.Core.General;
using static Genshin_Checker.Core.HoYoLab.Account;
using static System.Net.WebRequestMethods;

namespace Genshin_Checker.Core.HoYoLab
{
    public class ApiEndpoint
    {
        private readonly Account Account;
        internal ApiEndpoint(Account account) {
            Account = account;
        }
        /// <summary>
        /// アカウント情報を取得
        /// </summary>
        /// <param name="server">ゲームアカウントが所在しているサーバー</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<Model.HoYoLab.Account.Data> GetServerAccounts(Servers server)
        {
            var json = await GetJson.GetServerAccounts(Account, server);
            var root = JsonChecker<Model.HoYoLab.Account.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }
        /// <summary>
        /// 戦績情報
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public async Task<Model.HoYoLab.GameRecords.Data> GetGameRecords()
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetGameRecords(Account);
            var root = JsonChecker<Model.HoYoLab.GameRecords.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }
        /// <summary>
        /// キャラクター情報
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public async Task<Model.HoYoLab.Characters.Data> GetCharacters()
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetCharacters(Account);
            var root = JsonChecker<Model.HoYoLab.Characters.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }
        /// <summary>
        /// 深境螺旋情報
        /// </summary>
        /// <param name="current"><c>true</c> : 今月<br/><c>false</c> : 先月</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public async Task<Model.HoYoLab.SpiralAbyss.Data> GetSpiralAbyss(bool current, CultureInfo? culture = null)
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetSpiralAbyss(Account, current, culture);
            var root = JsonChecker<Model.HoYoLab.SpiralAbyss.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }
        /// <summary>
        /// リアルタイムノート
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public async Task<Model.HoYoLab.RealTimeNote.Data> GetRealTimeNote()
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetRealTimeNote(Account);
            var root = JsonChecker<Model.HoYoLab.RealTimeNote.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }
        /// <summary>
        /// 旅人手帳概要
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public async Task<Model.HoYoLab.TravelersDiary.Infomation.Data> GetTravelersDiaryInfo(int month = 0, CultureInfo? culture = null)
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetTravelersDiaryInfo(Account, month, culture);
            var root = JsonChecker<Model.HoYoLab.TravelersDiary.Infomation.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }
        /// <summary>
        /// 旅人手帳詳細
        /// </summary>
        /// <param name="type"><c>1</c> : 原石<br/><c>2</c> : モラ</param>
        /// <param name="month">既定値 : <c>当月</c><br/>直近3か月内のデータが取得可能</param>
        /// <param name="page">既定値 : <c>1</c><br/>1から始まる全てのページ<br/>1ページ当たり20件まで取得可能</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public async Task<Model.HoYoLab.TravelersDiary.Detail.Data> GetTravelersDiaryDetail(int type, int page = 1, int month = 0)
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetTravelersDiaryDetail(Account, type, page, month);
            var root = JsonChecker<Model.HoYoLab.TravelersDiary.Detail.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }
        /// <summary>
        /// キャラクター詳細情報<br/>ユーザーの天賦レベルも記載されている。
        /// </summary>
        /// <param name="characterID">キャラクター番号<br/>例 :10000089 = フリーナ</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<Model.HoYoLab.CharacterDetail.Data> GetCharacterDetail(int characterID)
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetCharacterDetail(Account, characterID);
            var root = JsonChecker<Model.HoYoLab.CharacterDetail.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }

        public async Task<Model.HoYoLab.CharacterDetailResult.Data> GetCharactersDetail(List<int> characterID)
        {

            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetCharactersDetail(Account, characterID);
            var root = JsonChecker<Model.HoYoLab.CharacterDetailResult.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }


        /// <summary>
        /// 幻想シアター情報
        /// </summary>
        /// <param name="IsNeedDetail">詳細情報が必要かどうか</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<Model.HoYoLab.RoleCombat.Data> GetRoleCombat(bool IsNeedDetail)
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetRoleCombat(Account, IsNeedDetail);
            var root = JsonChecker<Model.HoYoLab.RoleCombat.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }

        /// <summary>
        /// HoYoLabの情報を取得
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<Model.HoYoLab.MainMaterial.Data> GetHoYoLabMaterial()
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetHoYoLabMaterial(Account);
            var root = JsonChecker<Model.HoYoLab.MainMaterial.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }
        /// <summary>
        /// 旅の振り返りAPI
        /// </summary>
        /// <param name="since">該当DateTimeから今日までの差分を取得<br/>最大90日前まで利用可能</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<Model.HoYoLab.StellarJourney.Data> GetActiveQuery(DateTime since)
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetActiveQuery(Account, since);
            var root = JsonChecker<Model.HoYoLab.StellarJourney.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }
        /// <summary>
        /// 幽境の激戦情報を取得
        /// </summary>
        /// <param name="need_detail"></param>
        /// <returns></returns>
        /// <exception cref="UserNotAuthenticatedException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public async Task<Model.HoYoLab.HardChallenge.Data> GetHardChallenge(bool need_detail = true)
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetHardChallenge(Account, need_detail);
            var root = JsonChecker<Model.HoYoLab.HardChallenge.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }

        /// <summary>
        /// 幽境の激戦の「光を授けし者」を受けたキャラクターを取得
        /// </summary>
        /// <returns></returns>
        /// <exception cref="UserNotAuthenticatedException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public async Task<Model.HoYoLab.HardChallengePopularity.Data> GetHardChallengePopularity()
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.GetHardChallengePopularity(Account);
            var root = JsonChecker<Model.HoYoLab.HardChallengePopularity.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }

        /// <summary>
        /// 育成計算機
        /// </summary>
        /// <param name="data">キャラクターの計算変数</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<Model.HoYoLab.CalculatorComputeGet.Data> ComputeCalculate(Model.HoYoLab.CalculatorComputePost.Root data)
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.ComputeCalculate(Account, data);
            var root = JsonChecker<Model.HoYoLab.CalculatorComputeGet.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }

        /// <summary>
        /// 育成計算機(バッチ処理)
        /// </summary>
        /// <param name="data">キャラクターの計算変数</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<Model.HoYoLab.CalculatorComputeBatchGet.Data> ComputeBatchCalculate(Model.HoYoLab.CalculatorComputeBatchPost.Root data)
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.ComputeBatchCalculate(Account, data);
            var root = JsonChecker<Model.HoYoLab.CalculatorComputeBatchGet.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }

        /// <summary>
        /// 育成計算機のデータ取得
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="UserNotAuthenticatedException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public async Task<Model.HoYoLab.BatchAvaterListGet.Data> BatchAvaterList(int page = 1, int size = 200)
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var data = new Model.HoYoLab.BatchAvaterListPost.Root()
            {
                lang = Account.Culture.Name.ToLower().Split('-')[0],
                uid = Account.UID.ToString(),
                region = Account.Server.ToString(),
                page = page,
                size = size
            };
            var json = await GetJson.BatchAvaterList(Account, data);
            var root = JsonChecker<Model.HoYoLab.BatchAvaterListGet.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }


        /// <summary>
        /// ログボ受取
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<Model.HoYoLab.DailyBonusLogin.Data> LoginBonusSignIn()
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.LoginBonusSignIn(Account);
            var root = JsonChecker<Model.HoYoLab.DailyBonusLogin.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }

        /// <summary>
        /// ログボ受取
        /// </summary>
        /// <param name="campaignCode">キャラクターの計算変数</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<Model.HoYoLab.CodeExchange.Data> CodeExchange(string campaignCode)
        {
            if (!Account.IsAuthed) throw new UserNotAuthenticatedException(Account.UID);
            var json = await GetJson.ExchangeCode(Account,campaignCode);
            var root = JsonChecker<Model.HoYoLab.CodeExchange.Root>.Check(json);
            if (root.Data == null) throw new HoYoLabAPIException(root.Retcode, root.Message);
            return root.Data;
        }
        /// <summary>
        /// EnkaNetwork
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<Model.EnkaNetwork.ShowCase.Root> GetEnkaNetwork()
        {
            var json = await GetJson.GetEnkaNetwork(Account.UID);
            var root = JsonChecker<Model.EnkaNetwork.ShowCase.Root>.Check(json);
            return root;
        }
    }
    public static class GetJson
    {
        /// <summary>
        /// アカウント情報を取得
        /// </summary>
        /// <param name="server">ゲームアカウントが所在しているサーバー</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> GetServerAccounts(Account Account,Servers? server=null)
        {
            var region = Account.Server;
            if (server != null) region = (Servers)server;
            //ToDo : APIから取得するようにする
            var url = $"https://api-account-os.hoyolab.com/binding/api/getUserGameRolesByLtoken?game_biz=hk4e_global&region={region}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }
        /// <summary>
        /// 戦績情報
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public static async Task<string> GetGameRecords(Account Account)
        {
            var url = $"https://bbs-api-os.hoyolab.com/game_record/genshin/api/index?server={Account.Server}&role_id={Account.UID}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }
        /// <summary>
        /// キャラクター情報
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public static async Task<string> GetCharacters(Account Account)
        {
            var url = $"https://sg-public-api.hoyolab.com/event/game_record/genshin/api/character/list";
            string content = $"{{\"server\":\"{Account.Server}\",\"role_id\":\"{Account.UID}\"}}";
            var json = await WebRequest.HoYoPostRequest(url, Account.Cookie, content);
            return json ?? "";
        }
        /// <summary>
        /// 深境螺旋情報
        /// </summary>
        /// <param name="current"><c>true</c> : 今月<br/><c>false</c> : 先月</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public static async Task<string> GetSpiralAbyss(Account Account,bool current, CultureInfo? culture = null)
        {
            var cul = Account.Culture.Name.ToLower();
            if (culture != null) cul = culture.Name.ToLower();
            var url = $"https://bbs-api-os.hoyolab.com/game_record/genshin/api/spiralAbyss?server={Account.Server}&role_id={Account.UID}&lang={cul}&schedule_type={(current ? 1 : 2)}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie,new(cul));
            return json ?? "";
        }
        /// <summary>
        /// リアルタイムノート
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public static async Task<string> GetRealTimeNote(Account Account)
        {
            var url = $"https://bbs-api-os.hoyolab.com/game_record/genshin/api/dailyNote?server={Account.Server}&role_id={Account.UID}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }
        /// <summary>
        /// 旅人手帳概要
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public static async Task<string> GetTravelersDiaryInfo(Account Account, int month = 0, CultureInfo? culture = null)
        {
            var cul = Account.Culture.Name.ToLower();
            if (culture != null) cul = culture.Name.ToLower();
            var url = $"https://sg-hk4e-api.hoyolab.com/event/ysledgeros/month_info?region={Account.Server}&uid={Account.UID}&lang={cul}&month={month}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }
        /// <summary>
        /// 旅人手帳詳細
        /// </summary>
        /// <param name="type"><c>1</c> : 原石<br/><c>2</c> : モラ</param>
        /// <param name="month">既定値 : <c>当月</c><br/>直近3か月内のデータが取得可能</param>
        /// <param name="page">既定値 : <c>1</c><br/>1から始まる全てのページ<br/>1ページ当たり20件まで取得可能</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="HoYoLabAPIException"></exception>
        public static async Task<string> GetTravelersDiaryDetail(Account Account, int type, int page = 1, int month = 0)
        {
            var url = $"https://sg-hk4e-api.hoyolab.com/event/ysledgeros/month_detail?region={Account.Server}&uid={Account.UID}&lang={Account.Culture.Name.ToLower()}&month={month}&type={type}&current_page={page}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }
        /// <summary>
        /// キャラクター詳細情報<br/>ユーザーの天賦レベルも記載されている。
        /// </summary>
        /// <param name="characterID">キャラクター番号<br/>例 :10000089 = フリーナ</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> GetCharacterDetail(Account Account, int characterID)
        {
            var url = $"https://sg-public-api.hoyolab.com/event/calculateos/sync/avatar/detail?avatar_id={characterID}&uid={Account.UID}&region={Account.Server}&lang={Account.Culture.Name.ToLower()}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }

        /// <summary>
        /// 幻想シアター情報
        /// </summary>
        /// <param name="Account">アカウント情報</param>
        /// <param name="IsNeedDetail">詳細情報が必要か</param>
        /// <returns></returns>
        public static async Task<string> GetRoleCombat(Account Account, bool IsNeedDetail)
        {
            //以下のコメントは必要になったら。
            //&nickname={HttpUtility.UrlEncode(HttpUtility.UrlEncode(Account.Name)).ToUpper()}
            var url = $"https://bbs-api-os.hoyolab.com/game_record/genshin/api/role_combat?server={Account.Server}&role_id={Account.UID}&need_detail={(IsNeedDetail?"true":"false")}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }

        /// <summary>
        /// キャラクターの詳細情報
        /// </summary>
        /// <param name="Account">アカウント情報</param>
        /// <param name="characterID">キャラクターID</param>
        /// <returns></returns>
        public static async Task<string> GetCharactersDetail(Account Account,List<int> characters)
        {

            var url = $"https://bbs-api-os.hoyolab.com/game_record/app/genshin/api/character/detail";
            var data = new Model.HoYoLab.CharacterDetailPost.Root() { 
                role_id = Account.UID, 
                server = Account.Server.ToString(), 
                character_ids = characters 
            };
            var json = await WebRequest.HoYoPostRequest(url, Account.Cookie,JsonConvert.SerializeObject(data));
            return json ?? "";
        }




        /// <summary>
        /// HoYoLabのマテリアル情報取得
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> GetHoYoLabMaterial(Account Account)
        {
            var url = $"https://bbs-api-os.hoyolab.com/community/painter/wapi/circle/channel/guide/material?game_id=2";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }

        /// <summary>
        /// 交換コード引き換え
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> ExchangeCode(Account Account,string campaignCode)
        {
            long unixtime = DateTime.Now.Ticks / 10000;
            campaignCode = campaignCode.Trim();
            var url = $"https://sg-hk4e-api.hoyolab.com/common/apicdkey/api/webExchangeCdkeyHyl?cdkey={campaignCode}&game_biz=hk4e_global&lang={Account.Culture.Name.ToLower().Split('-')[0]}&region={Account.Server}&t={unixtime}&uid={Account.UID}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }
        


        /// <summary>
        /// 旅の振り返りAPI
        /// </summary>
        /// <param name="since">該当DateTimeから今日までの差分を取得<br/>最大90日前まで利用可能</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> GetActiveQuery(Account Account, DateTime since)
        {
            var url = $"https://bbs-api-os.hoyolab.com/game_record/genshin/wapi/query_tool?server={Account.Server}&role_id={Account.UID}&year={since.Year}&month={since.Month:00}&day={since.Day:00}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }

        /// <summary>
        /// 幽境の激戦情報を取得
        /// </summary>
        /// <param name="Account"></param>
        /// <param name="need_detail"></param>
        /// <returns></returns>
        public static async Task<string> GetHardChallenge(Account Account, bool need_detail = true)
        {
            var url = $"https://sg-public-api.hoyolab.com/event/game_record/genshin/api/hard_challenge?server={Account.Server}&role_id={Account.UID}&need_detail={need_detail.ToString().ToLower()}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }

        /// <summary>
        /// 「光を授けし者」を受けたキャラクターを取得
        /// </summary>
        /// <param name="Account"></param>
        /// <returns></returns>
        public static async Task<string> GetHardChallengePopularity(Account Account)
        {
            var url = $"https://sg-public-api.hoyolab.com/event/game_record/genshin/api/hard_challenge/popularity?server={Account.Server}&role_id={Account.UID}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }


        /// <summary>
        /// 育成計算機
        /// </summary>
        /// <param name="data">キャラクターの計算変数</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> ComputeCalculate(Account Account, Model.HoYoLab.CalculatorComputePost.Root data)
        {
            var url = $"https://sg-public-api.hoyolab.com/event/calculateos/compute";
            Log.Debug(JsonConvert.SerializeObject(data));
            var json = await WebRequest.HoYoPostRequest(url, Account.Cookie, JsonConvert.SerializeObject(data));
            return json ?? "";
        }

        /// <summary>
        /// 育成計算機(バッチ処理)
        /// </summary>
        /// <param name="data">キャラクターの計算変数</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> ComputeBatchCalculate(Account Account, Model.HoYoLab.CalculatorComputeBatchPost.Root data)
        {
            var url = $"https://sg-public-api.hoyolab.com/event/e20200928calculate/v3/batch_compute";
            Log.Debug(JsonConvert.SerializeObject(data));
            var json = await WebRequest.HoYoPostRequest(url, Account.Cookie, JsonConvert.SerializeObject(data));
            return json ?? "";
        }

        /// <summary>
        /// 育成計算機用キャラクター取得
        /// </summary>
        /// <param name="Account"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task<string> BatchAvaterList(Account Account, Model.HoYoLab.BatchAvaterListPost.Root data)
        {
            var url = $"https://sg-act-public-api.hoyolab.com/event/e20200928calculate/v1/sync/avatar/list";
            Log.Debug(JsonConvert.SerializeObject(data));
            var json = await WebRequest.HoYoPostRequest(url, Account.Cookie, JsonConvert.SerializeObject(data));
            return json ?? "";
        }



        /// <summary>
        /// ログインボーナスの情報
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> LoginBonusInfo(Account Account)
        {
            const string Act_ID = "e202102251931481";
            var url = $"https://sg-hk4e-api.hoyolab.com/event/sol/info?lang={Account.Culture.Name.ToLower()}&act_id={Act_ID}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }


        /// <summary>
        /// ログインボーナスのサインイン情報
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> LoginBonusResignInfo(Account Account)
        {
            const string Act_ID = "e202102251931481";
            var url = $"https://sg-hk4e-api.hoyolab.com/event/sol/resign_info?act_id={Act_ID}&lang={Account.Culture.Name.ToLower()}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }
        
        /// <summary>
        /// ログインボーナスの期間限定の追加報酬
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> LoginBonusExtraAward(Account Account)
        {
            const string Act_ID = "e202102251931481";
            var url = $"https://sg-hk4e-api.hoyolab.com/event/sol/extra_award?act_id={Act_ID}&region={Account.Server}&uid={Account.UID}";
            var json = await WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }
        

        /// <summary>
        /// ログインボーナスのサインイン
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> LoginBonusSignIn(Account Account)
        {
            const string Act_ID = "e202102251931481";
            var url = $"https://sg-hk4e-api.hoyolab.com/event/sol/sign?lang=ja-jp";
            var json = await WebRequest.HoYoPostRequest(url, Account.Cookie, $"{{\"act_id\": \"{Act_ID}\"}}");
            return json ?? "";
        }


        /// <summary>
        /// ログインボーナスのリワード一覧
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> LoginBonusRewards(Account Account)
        {
            const string Act_ID = "e202102251931481";
            var url = $"https://sg-hk4e-api.hoyolab.com/event/sol/home?lang=ja-jp&act_id={Act_ID}";
            var json = await Core.WebRequest.HoYoGetRequest(url, Account.Cookie);
            return json ?? "";
        }

        /// <summary>
        /// EnkaNetwork
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static async Task<string> GetEnkaNetwork(int uid)
        {
            var url = $"https://enka.network/api/uid/{uid}";
            var json = await WebRequest.GeneralGetRequest(url);
            return json ?? "";
        }
    }
}
