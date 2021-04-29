﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using HtmlAgilityPack;
using Telegram.Bot.Args;

namespace lab4
{
    internal partial class Program
    {
        private class Share
        {
            private sealed class NameCostEqualityComparer : IEqualityComparer<Share>
            {
                public bool Equals(Share x, Share y)
                {
                    if (ReferenceEquals(x, y)) return true;
                    if (ReferenceEquals(x, null)) return false;
                    if (ReferenceEquals(y, null)) return false;
                    if (x.GetType() != y.GetType()) return false;
                    return x.Name == y.Name && x.Cost == y.Cost;
                }

                public int GetHashCode(Share obj)
                {
                    return HashCode.Combine(obj.Name, obj.Cost);
                }
            }

            public static IEqualityComparer<Share> NameCostComparer { get; } = new NameCostEqualityComparer();

            public string Name { get; set; }
            public string Cost { get; set; }
            public string Val { get; set; }
            public string Opened { get; set; }
            public string Closed { get; set; }
            public string Costdif { get; set; }

            public static HtmlDocument GetUrl(MessageEventArgs e, string shareCode)
            {
                var url = "https://finance.yahoo.com/quote/";
                var userMessWord = e.Message.Text.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var count = userMessWord.Length;

                if (shareCode == null) shareCode = count > 1 ? userMessWord[1] : userMessWord[0];

                url = url + shareCode + '/';


                var handler = new HttpClientHandler { AllowAutoRedirect = true };
                var httpClient = new HttpClient(handler);
                var response = httpClient.GetAsync(url).Result;
                Console.WriteLine(response);


                var redirectUri = response.RequestMessage.RequestUri;

                var web = new HtmlWeb();
                var doc = web.Load(redirectUri);
                return doc;
            }

            public static List<HtmlNode> ParseDoc(HtmlDocument doc)
            {
                var elements = doc.DocumentNode.Descendants("div")
                    .Where(x => x.Attributes["class"] != null)
                    .Where(x => x.Attributes["class"].Value == "D(ib) Mend(20px)" ||
                                x.Attributes["class"].Value == "D(ib) " ||
                                x.Attributes["class"].Value == "C($tertiaryColor) Fz(12px)").ToList();
                var elements2 = doc.DocumentNode.Descendants("span")
                    .Where(x => x.Attributes["class"] != null)
                    .Where(x => x.Attributes["class"].Value == "Trsdu(0.3s) " &&
                                (x.Attributes["data-reactid"].Value == "44" ||
                                 x.Attributes["data-reactid"].Value == "49")).ToList();
                elements.Add(elements2[0]);
                elements.Add(elements2[1]);
                return elements;
            }

            public void ParsEl(List<HtmlNode> elements)
            {
                var descHtml = elements.SelectMany(x => x.Descendants("h1"))
                    .Where(x => x.Attributes["class"] != null)
                    .FirstOrDefault(x => x.Attributes["class"].Value == "D(ib) Fz(18px)");
                if (descHtml?.InnerText != null)
                    Name = descHtml.InnerText;

                descHtml = elements.SelectMany(x => x.Descendants("span"))
                    .Where(x => x.Attributes["class"] != null)
                    .FirstOrDefault(x => x.Attributes["class"].Value == "Trsdu(0.3s) Fw(b) Fz(36px) Mb(-4px) D(ib)");
                if (descHtml?.InnerText != null)
                    Cost = Math.Round(double.Parse(descHtml.InnerText[Range.EndAt(descHtml.InnerText.Length)]), 2)
                        .ToString(CultureInfo.InvariantCulture);

                descHtml = elements.SelectMany(x => x.DescendantsAndSelf("div"))
                    .Where(x => x.Attributes["class"] != null)
                    .FirstOrDefault(x => x.Attributes["class"].Value == "C($tertiaryColor) Fz(12px)");
                if (descHtml?.InnerText != null)
                {
                    var sharePlat = descHtml.InnerText.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    Val = sharePlat[^1]; // Takes last word in string, usual that is "USD"
                }

                descHtml = elements[3];
                Closed = descHtml.InnerText;

                descHtml = elements[4];
                Opened = descHtml.InnerText;
            }

            public void ShareComparison(Share compShare)
            {
                if (Cost == null)
                {
                    Costdif = "0.00";
                    Val = compShare.Val;
                }

                else
                    Costdif = (Math.Round(double.Parse(Cost)
                                          - double.Parse(compShare.Cost)), 2).ToString();

                Cost = compShare.Cost;
                Name = compShare.Name;
            }


        }
    }
}