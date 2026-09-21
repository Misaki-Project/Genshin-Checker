using Genshin_Checker.Model.Misaki_chan.HoYoContent;
using HarfBuzzSharp;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Genshin_Checker.GUI.Window
{
    public partial class HoYoContentViewer : System.Windows.Window
    {
        public HoYoContentViewer()
        {
            InitializeComponent();
            InitializeWebView2();
            LoadArticles();
        }

        private async void InitializeWebView2()
        {
            HtmlViewer.CoreWebView2InitializationCompleted += HtmlViewer_CoreWebView2InitializationCompleted;
            await HtmlViewer.EnsureCoreWebView2Async(null);
        }

        private void HtmlViewer_CoreWebView2InitializationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {

            HtmlViewer.CoreWebView2.NavigationStarting += (sender, args) =>
            {
                if (args.IsUserInitiated)
                {
                    args.Cancel = true;
                }
            };
            HtmlViewer.CoreWebView2.NewWindowRequested += (sender, args) =>
            {
                args.Handled = true;
                Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true });
            };


            HtmlViewer.CoreWebView2.ContextMenuRequested += (sender, args) =>
            {
                args.Handled = true;
            };

            const string appReferer = "https://misaki-chan.world/";

            HtmlViewer.CoreWebView2.AddWebResourceRequestedFilter(
                "https://www.youtube.com/embed/*",
                CoreWebView2WebResourceContext.Document
            );

            HtmlViewer.CoreWebView2.WebResourceRequested += (sender, e) =>
            {
                if (e.Request.Uri.StartsWith(
                    "https://www.youtube.com/embed/",
                    StringComparison.OrdinalIgnoreCase))
                {
                    e.Request.Headers.SetHeader(
                        "Referer",
                        appReferer
                    );
                }

            };
                if (ArticleList.Items.Count > 0) ArticleList.SelectedIndex = 0;
#if !DEBUG
                HtmlViewer.CoreWebView2.Settings.AreDevToolsEnabled = false;
#endif
        }

        private async void LoadArticles()
        {
            ArticleList.ItemsSource = new List<Article> { new Article { Title = "Loading...", Summary = "" } };

            var articles = await FetchArticlesAsync();
            ArticleList.ItemsSource = articles;
            if (ArticleList.Items.Count > 0) ArticleList.SelectedIndex = 0;
        }

        private async Task<List<Article>> FetchArticlesAsync()
        {
            var articles = new List<Article>();
            try
            {
                using (var client = new HttpClient())
                {
                    var responseBytes = await client.GetByteArrayAsync("https://dynamic-api.misaki-chan.world/genshin/bbs/ja");
                    var responseString = System.Text.Encoding.UTF8.GetString(responseBytes);
                    var root = JsonSerializer.Deserialize<Root>(responseString);

                    if (root != null)
                    {
                        foreach (var result in root.Results)
                        {
                            string AddContent = "";
                            if (result.Content.IsSimple)
                            {
                                foreach (var img in result.Content.Images)
                                {
                                    AddContent += $"<br/><img src=\"{img}\"/>";
                                }
                            }
                            var preview = "";
                            foreach (var a in result.Content.Images)
                            {
                                if (a.Contains("hyl-static-res-prod.hoyolab.com")) continue;
                                preview = a;
                                break;
                            }
                            articles.Add(new Article
                            {
                                Title = result.Subject,
                                Summary = result.Content.IsSimple ? result.Content.HTMLText.Replace("<br/>", "\n") : result.Description,
                                Content = result.Content.HTMLText + AddContent,
                                PreviewImage = preview
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Dialog.Error("取得エラー", $"{ex}");
                Close();
            }
            return articles;
        }

        private void ArticleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            const string scripts = "document.addEventListener('contextmenu', e => e.preventDefault()); window.addEventListener('popstate', e => { history.pushState(null, null, document.URL); e.preventDefault(); });";
            try
            {
                if (ArticleList.SelectedItem is Article selectedArticle)
                {
                    HtmlViewer.NavigateToString("<meta charset=\"UTF-8\"><script>" + scripts + "</script><style>img { width: 100%; height: auto; }\niframe { width: 100%; aspect-ratio: 16 / 9;}</style>" + selectedArticle.Content);
                    Title = selectedArticle.Title;
                }
            }
            catch (InvalidOperationException)
            {
                HtmlViewer.CoreWebView2InitializationCompleted += (s, e) =>
                {
                    if (ArticleList.SelectedItem is Article selectedArticle)
                    {
                        HtmlViewer.NavigateToString("<meta charset=\"UTF-8\"><script>" + scripts + "</script><style>img { width: 100%; height: auto; }\niframe { width: 100%; aspect-ratio: 16 / 9;}</style>" + selectedArticle.Content);
                        Title = selectedArticle.Title;
                    }
                };
            }
            catch (Exception ex)
            {

                Log.Error(ex);
            }
        }

    }

    public class Article
    {
        public string Title { get; set; } = "";
        public string Summary { get; set; } = "";
        public string Content { get; set; } = "";
        public string PreviewImage { get; set; } = "";
    }
}