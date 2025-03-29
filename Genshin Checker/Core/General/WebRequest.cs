using Genshin_Checker.Core.General;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace Genshin_Checker.Core
{
    public class WebRequest
    {
        /// <summary>
        /// 基本的なユーザーエージェント
        /// </summary>
        const string WebUserAgent = "Mozilla/5.0 (X11; Linux x86_64; rv:100.0) Gecko/20100101 Firefox/100.0";
        /// <summary>
        /// ランダムな文字列
        /// </summary>
        /// <param name="n">文字数</param>
        /// <returns>文字数分のランダムな文字列</returns>
        static private string RandomString(int n)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            string result = "";
            Random random = new((int)DateTime.UtcNow.Ticks);
            for (int i = 0; i < n; i++)
                result += chars[random.Next(0, chars.Length)];
            return result;
        }
        /// <summary>
        /// ハッシュ情報
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static private string MD5Hash(string input)
        {
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var builder = new StringBuilder();
            foreach (var b in hashBytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
        /// <summary>
        /// アプリ内のキャッシュファイルパス取得
        /// </summary>
        /// <param name="filename">ファイル名</param>
        /// <returns>フルパス</returns>
        public static string GetCachePath(string filename)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Genshin Checker", "CacheFiles", filename); //水咲原神チェッカー
        }
        /// <summary>
        /// DS認証情報
        /// </summary>
        /// <returns></returns>
        static public string DS(long? time=null,string? random=null)
        {
            //Todo: saltはstatic APIに投げて動的に変更させる
            const string salt = "6s25p5ox5y14umn1p61aqyyvbvvl3lrt";
            if(random==null)random = RandomString(6);

            if(time==null)time = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            var input = $"salt={salt}&t={time}&r={random}";
            return $"{time},{random},{MD5Hash(input)}";
        }
        /// <summary>
        /// HoYoLab内GETリクエスト
        /// </summary>
        /// <param name="url">リクエスト先</param>
        /// <param name="cookie">クッキー情報</param>
        /// <param name="culture">言語</param>
        /// <returns>JSON</returns>
        public static async Task<string> HoYoGetRequest(string url, string cookie, CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;
            var uri = new Uri(url);
            var root = $"{uri.Scheme}://{uri.Host}";
            Dictionary<string, string> headers = new()
                {
                    { "Origin", root },
                    { "Referer", root },
                    { "Accept", "application/json, text/plain, */*" },
                    { "Accept-Encoding", "None" },
                    { "Accept-Language", $"{culture.Name};q=0.5" },
                    { "x-rpc-app_version", "1.5.0" },
                    { "x-rpc-client_type", "5" },
                    { "x-rpc-language", culture.Name.ToLower() },
                    { "User-Agent", WebUserAgent },
                    { "Cookie", cookie },
                    { "Ds", DS() }
                };
            HttpClient client = new();
            client.DefaultRequestHeaders.Clear();
            foreach (KeyValuePair<string, string> header in headers)
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            HttpResponseMessage response = await client.GetAsync(url);
            Log.Debug($"HoYoGet - {url}");
            if (!response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                try
                {
                    var json = JsonChecker<Model.HoYoLab.Root<object>>.Check(data);
                    return data;
                }
                catch
                {
                    return $"{{\"retcode\":{(int)response.StatusCode + 2000000000},\"message\":{Newtonsoft.Json.JsonConvert.SerializeObject($"This error is generated from Genshin Checker.\nHTTP status code is failure.\nStatus Code : {(int)response.StatusCode} - {response.StatusCode}\nMessage : {data}")}}}";
                }
            }
            return await response.Content.ReadAsStringAsync();
        }
        /// <summary>
        /// HoYoLab内Postリクエスト
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="cookie">クッキー</param>
        /// <param name="data">データ</param>
        /// <param name="culture">言語</param>
        /// <returns>JSON</returns>
        public static async Task<string> HoYoPostRequest(string url, string cookie, string data, CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;
            var uri = new Uri(url);
            var root = $"{uri.Scheme}://{uri.Host}";
            Dictionary<string, string> headers = new()
                {
                    { "Origin", root },
                    { "Referer", root },
                    { "Accept", "application/json, text/plain, */*" },
                    { "Accept-Encoding", "None" },
                    { "Accept-Language", $"{culture.Name};q=0.5" },
                    { "x-rpc-app_version", "1.5.0" },
                    { "x-rpc-client_type", "5" },
                    { "x-rpc-language", culture.Name.ToLower() },
                    { "User-Agent", WebUserAgent },
                    { "Cookie", cookie },
                    { "DS", DS() }
                };
            HttpClient client = new();
            client.DefaultRequestHeaders.Clear();
            foreach (KeyValuePair<string, string> header in headers)
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            var content = new StringContent(data, Encoding.UTF8, @"application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            Log.Debug($"HoYoPost - {url}");
            if (!response.IsSuccessStatusCode)
            {
                var data2 = await response.Content.ReadAsStringAsync();
                try
                {
                    var json = JsonChecker<Model.HoYoLab.Root<object>>.Check(data2);
                    return data2;
                }
                catch
                {
                    return $"{{\"retcode\":{(int)response.StatusCode + 2000000000},\"message\":{Newtonsoft.Json.JsonConvert.SerializeObject($"This error is generated from Genshin Checker.\nHTTP status code is failure.\nStatus Code : {(int)response.StatusCode} - {response.StatusCode}\nMessage : {data}")}}}";
                }
            }
            return await response.Content.ReadAsStringAsync();
        }
        /// <summary>
        /// 一般的なGETリクエスト
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="HideUserAgent">一般的なユーザーエージェントを使用するか</param>
        /// <returns>JSON</returns>
        public static async Task<string> GeneralGetRequest(string url, bool HideUserAgent = false)
        {
            var sw = Stopwatch.StartNew();
            var uri = new Uri(url);
            var root = $"{uri.Scheme}://{uri.Host}";
            Dictionary<string, string> headers = new()
                {
                    { "Origin", root },
                    { "Referer", root },
                    { "Accept", "application/json, text/plain, */*" },
                    { "Accept-Encoding", "None" },
                    { "Accept-Language", "en-US;q=0.5" },
                    { "x-rpc-app_version", "1.5.0" },
                    { "x-rpc-client_type", "5" },
                    { "x-rpc-language", "en-us" },
                    { "User-Agent", HideUserAgent?WebUserAgent:$"Genshin Checker/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}" },
                };
            HttpClient client = new();
            client.DefaultRequestHeaders.Clear();
            foreach (KeyValuePair<string, string> header in headers)
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            HttpResponseMessage response = await client.GetAsync(url);
            Log.Debug($"GeneralGet - {response.StatusCode} ({sw.Elapsed.TotalSeconds:0.000}s) {url}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        /// <summary>
        /// ファイルのダウンロード
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="token">キャンセルトークン</param>
        /// <returns>ファイル</returns>
        public static async Task<byte[]?> GetRequest(string? url, CancellationToken? token = null)
        {
            try
            {
                if (url == null) return null;
                var uri = new Uri(url);
                bool IsQuery = url.Contains('?');
                var filename = GetCachePath(MD5Hash(url));
                var directory = Path.GetDirectoryName(filename);
                if (directory != null && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
                if (!IsQuery && File.Exists(filename))
                {
                    try
                    {
                        using FileStream fs = new(filename, FileMode.Open);
                        using GZipStream zipStream = new(fs, CompressionMode.Decompress);
                        using MemoryStream cacheout = new();
                        zipStream.CopyTo(cacheout);
                        Log.Debug($"Cache ({fs.Length}->{cacheout.Length}) : {url}");
                        return cacheout.ToArray();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"Failed to load cache from {filename}\nReason : {ex.Message}");
                    }
                }
                Stopwatch stopwatch = new();
                stopwatch.Start();
                var root = $"{uri.Scheme}://{uri.Host}";
                Dictionary<string, string> headers = new()
                {
                    { "Origin", root },
                    { "Referer", root },
                    { "Accept", "application/json, text/plain, */*" },
                    { "Accept-Encoding", "None" },
                    { "Accept-Language", "en-US;q=0.5" },
                    { "x-rpc-app_version", "1.5.0" },
                    { "x-rpc-client_type", "5" },
                    { "x-rpc-language", "en-us" },
                    { "User-Agent", $"Genshin Checker/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}" },
                };
                HttpClient client = new();
                client.DefaultRequestHeaders.Clear();
                foreach (KeyValuePair<string, string> header in headers)
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                HttpResponseMessage response = new();
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        if (token == null) response = await client.GetAsync(url);
                        else response = await client.GetAsync(url, (CancellationToken)token);
                        if (!response.IsSuccessStatusCode)
                        {
                            Log.Error($"Fetch Error: {response.StatusCode} - {url}");
                            throw new ArgumentException($"Error: {response.StatusCode} - {url}");
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.Message);
                        if (i == 9)
                        {
                            return null;
                        }
                        continue;
                    }
                }
                var stream = await response.Content.ReadAsStreamAsync();
                if (!IsQuery)
                {
                    using FileStream stream2 = new(filename, FileMode.Create);
                    using GZipStream zipStream = new(stream2, CompressionMode.Compress);
                    stream.CopyTo(zipStream);
                    zipStream.Close();
                }
                Log.Debug($"Downloaded ({stream.Length:#,##0} Bytes / {stopwatch.ElapsedMilliseconds / 1000.0:0.00} sec) : {url}");
                byte[] buffer = new byte[16 * 1024];
                using MemoryStream ms = new();
                stream.Position = 0;
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }catch(Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }
        /// <summary>
        /// 画像の取得リクエスト
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="token">キャンセルトークン</param>
        /// <returns>画像データ</returns>
        public static async Task<Image> ImageGetRequest(string? url, CancellationToken? token = null)
        {
            if (url == null) return resource.icon.fail;
            var stream = await GetRequest(url, token);
            if (stream == null) return resource.icon.fail;
            MemoryStream ms = new(stream);
            var bitmap = new Bitmap(ms);
            return bitmap;
        }
        public static async Task<System.Windows.Media.ImageSource> ImageSourceGetRequest(string? url, CancellationToken? token = null)
        {
            if (url == null) return new BitmapImage(new Uri("pack://application:,,,/Genshin_Checker;component/Resources/fail.png"));
            var stream = await GetRequest(url, token);
            if (stream == null) return new BitmapImage(new Uri("pack://application:,,,/Genshin_Checker;component/Resources/fail.png"));
            MemoryStream ms = new(stream);
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); // To make it cross-thread accessible
            return bitmap;
        }

    }
}
