using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace UWPLibrary.LearnMoreSplit
{
    class LearnMoreSplitGb : LearnMoreSplit
    {
        LearnMoreSplitGb(HtmlDocument bingMainPageHtmlDocument) : 
            base(bingMainPageHtmlDocument) 
        {
            Init();
        }

        private async void Init()
        {
            var nodes = _htmlDocument
                    .DocumentNode
                    .SelectNodes("//a[@class='learn_more']");
            if (nodes is null)
            {
                SetHrefWithMainPage();
                return;
            }

            var node = nodes
                .FirstOrDefault();
            if (node is null)
            {
                SetHrefWithMainPage();
                return;
            }

            var attribute = node
                .Attributes["href"];
            if (attribute is null)
            {
                SetHrefWithMainPage();
                return;
            }

            Href = attribute
                .Value
                .Replace("&amp;", "&")
                .Replace("&quot;", "\"");

            // if learn more href does not exist
            if (string.IsNullOrWhiteSpace(Href))
            {
                SetHrefWithMainPage();
                return;
            }

            try
            {
                // add country code at end "cc=gb"
                var uri = new Uri("https://www.bing.com" + Href);
                using (var httpClient = new Windows.Web.Http.HttpClient())
                {
                    string result = await httpClient.GetStringAsync(uri);

                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(result);

                    var documentNode = htmlDoc.DocumentNode;
                    if (documentNode is null)
                        return;

                    var tempNodes = documentNode.SelectNodes("//div[@class='ency_desc']");
                    if (tempNodes is null)
                        return;
                    
                    var tempNode = tempNodes.FirstOrDefault();
                    if (tempNode is null)
                        return;
                    Description = tempNode.InnerText;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private void SetHrefWithMainPage()
        {
            var nodes = _htmlDocument
                .DocumentNode
                .SelectNodes("//span[@class='text']");
            if (nodes is null)
                return;

            var node = nodes
                .FirstOrDefault();
            if (node is null)
                return;

            Description = node.InnerText;
        }

        public override string Title
        {
            get
            {
                var nodes = _htmlDocument
                    .DocumentNode
                    .SelectNodes("//div[@class='vs_bs_title']");
                if (nodes is null)
                    return null;

                var node = nodes.FirstOrDefault();
                if (node is null)
                    return null;

                return node.InnerText;
            }
        }

        public override string Credit
        {
            get
            {
                var nodes = _htmlDocument
                    .DocumentNode
                    .SelectNodes("//div[@class='vs_bs_credit']");
                if (nodes is null)
                    return null;

                var node = nodes.FirstOrDefault();
                if (node is null)
                    return null;

                return node.InnerText;
            }
        }
    }
}
