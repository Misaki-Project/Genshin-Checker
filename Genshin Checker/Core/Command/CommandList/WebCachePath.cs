﻿using Genshin_Checker.Core.Game;
using Genshin_Checker.Core.General;
using Genshin_Checker.Window;

namespace Genshin_Checker.Core.Command.CommandList
{

    public class GetPath : Command
    {
        public override string Name => "getpath";
        public override string Description => "ゲーム内パスを取得します";

        public override async Task Execute(params string[] parameters)
        {
            if(parameters.Length == 1)
            {
                Console("GameDir -> ゲーム内ディレクトリの位置");
                Console("WebCache -> ゲーム内ウェブキャッシュの位置");
                Console("WebLinks -> ゲーム内からアカウントに関連するURLの取得");
                Console("AuthKey -> アカウントセンターの認証キーを取得");
                return;
            }
            switch (parameters[1].ToLower())
            {
                case "gamedir":
                    Console($"{await GameApp.WhereGameDir()??"(null)"}");
                    break;
                case "webcache":
                    Console($"{await GameApp.WhereWebCacheFilePath()??"(null)"}");
                    break;
                case "weblinks":
                    {
                        var links = await WebViewWatcher.GetLinks();
                        if (links == null)
                        {
                            Console("(null)");
                            break;
                        }
                        else
                        {
                            foreach (var l in links)
                            {
                                Console($"{l}");
                                Console("-------------------------");
                            }
                            Console("");
                            Console($"リンクは {links.Count} 件見つかりました。");
                        }
                    }
                    break;
                case "authkey":
                    Console($"{await WebViewWatcher.GetServiceCenterAuthKey()}");
                    break;
                default:
                    Console($"{parameters[1]} は不明なパラメータです。");
                    break;
            }
            return;
        }
    }
}
