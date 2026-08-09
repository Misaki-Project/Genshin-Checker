using Genshin_Checker.Resource.Languages;

namespace Genshin_Checker.Core.HoYoLab
{
    public class CharacterDetail:Base
    {
        //キャッシュ時間
        const int CacheSecond = 3600*8;
        public CharacterDetail(Account account) : base(account, 5000)
        {
            ServerUpdate.Elapsed += Timeout_Tick;
            ServerUpdate.Start();
        }
        Model.HoYoLab.CharacterDetailResult.Data? Cached;
        Model.HoYoLab.BatchAvaterListGet.Data? CachedAvater;
        public DateTime LatestUpdateTime = DateTime.MinValue;
        readonly SemaphoreSlim UpdateSemaphore = new(1, 1);

        public bool IsAvailableUpdate { get => UpdateSemaphore.CurrentCount == 1; }
        public Exception? LatestException { get; private set; } = null;
        /// <summary>
        /// 定期実行関数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Timeout_Tick(object? sender, EventArgs e)
        {
            Log.Debug($"天賦レベル取得");
            ServerUpdate.Stop();
            ServerUpdate.Interval = 60000*30;
            if (await UpdateGameData(true)) Log.Info("キャラクターの更新に成功");
            else
            {
                ServerUpdate.Interval = 60000 * 5;
                Log.Error("キャラクターの更新に失敗");
            }
            ServerUpdate.Start();
        }
        /// <summary>
        /// HoYoLabのサーバーからキャラクターの天賦情報を取得。<br/>
        /// 1分間以内に60回以上リクエストすると429エラーが返ってくる。
        /// </summary>
        /// <param name="characterID">キャラクター番号</param>
        /// <param name="Force">強制的にサーバーから取得</param>
        /// <returns></returns>
        public async Task<Model.HoYoLab.CharacterDetailResult.Character> GetData(int characterID, bool Force=false, int Timeout=-1)
        {

            Model.HoYoLab.CharacterDetailResult.Character? character = Cached?.list.Find(a => a.baseInfo.id == characterID);
            if (character==null)
            {
                var data = await account.Endpoint.GetCharactersDetail((await account.Characters.GetData()).list.Select(c => c.id).ToList());
                LatestUpdateTime = DateTime.UtcNow;
                Cached = data;

            }
            else
            {
                if (Force || LatestUpdateTime.AddSeconds(Timeout < 0 ? CacheSecond : Timeout) < DateTime.UtcNow)
                {
                    var data = await account.Endpoint.GetCharactersDetail((await account.Characters.GetData()).list.Select(c => c.id).ToList());
                    Cached = data;
                    LatestUpdateTime = DateTime.UtcNow;

                }
            }

            return Cached?.list.Find(a => a.baseInfo.id == characterID) ?? throw new ArgumentNullException("Character is not found.");
        }
        /// <summary>
        /// キャッシュの生成
        /// </summary>
        /// <param name="Force">キャッシュの再生成</param>
        /// <returns>成功したかどうか</returns>
        public async Task<bool> UpdateGameData(bool Force=false)
        {
            await UpdateSemaphore.WaitAsync();
            bool IsSuccessed = false;
            try
            {
                var characters = await account.Characters.GetData();
                List<int> charaids = new();
                foreach (var character in characters.list)
                {
                    charaids.Add(character.id);
                }
                var data2 = await account.Endpoint.GetCharactersDetail(charaids);
                Cached = data2;
                CachedAvater = await account.Endpoint.BatchAvaterList();
                foreach (var character in characters.list)
                {
                    for (int i = 0; i < 30; i++)
                        try
                        {
                            var data = await GetData(character.id);
                            break;
                        }
                        catch (Account.HoYoLabAPIException ex)
                        {
                            //TooManyRequestsが返ってきたら時間を空けて再リクエストを送る
                            if (ex.Retcode == 2000000429)
                            {
                                if (i == 29) throw new ArgumentException(Localize.Error_CharacterDetail_ReachedRetryCount,ex);
                                Log.Warn($"Ratelimit exceeded. please wait... {i}");
                                await Task.Delay(10000);
                            }
                            else
                                throw;
                        }
                        catch (Exception)
                        {
                            if (i == 9) throw;
                            continue;
                        }
                }
                LatestUpdateTime = DateTime.UtcNow;
                LatestException = null;
                IsSuccessed = true;
            }
            catch (Account.HoYoLabAPIException ex)
            {
                LatestException= ex;
                Log.Error($"アカウント検証エラー : {ex.Retcode} - {ex.APIMessage}");
            }
            catch (Exception ex)
            {
                LatestException = ex;
                Log.Error(ex.ToString());
            }
            finally
            {
                UpdateSemaphore.Release();
            }
            return IsSuccessed;
        }
        public List<Model.HoYoLab.CharacterDetailResult.Character> CachedCharacters()
        {
            return Cached?.list??new();
        }
        public List<Model.HoYoLab.BatchAvaterListGet.Character> CachedAvaterList()
        {
            return CachedAvater?.list ?? new();
        }
        public async Task<bool> IsReadyCacheData(int Timeout = -1)
        {
            var data = await account.Characters.GetData();
            foreach(var character in data.list)
            {
                var cache = Cached?.list.Find(a => a.baseInfo.id == character.id);
                if (cache==null)
                    return false;
                if (LatestUpdateTime.AddSeconds(Timeout < 0 ? CacheSecond : Timeout) < DateTime.UtcNow)
                    return false;
            }
            return true;
        }
    }
}
