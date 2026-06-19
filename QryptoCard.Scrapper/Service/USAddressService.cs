using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Web;
using HtmlAgilityPack;

namespace QryptoCard.Scrapper.Service
{
    public class USAddressService
    {
        public static string getWebScrapper(string url)
        {
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(url);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));


                clients.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:78.0)   Gecko/20100101 Firefox/78.0");
                clients.DefaultRequestHeaders.Add("Referer", "https://www.google.com");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                
                var responses = clients.GetStringAsync("");
                return responses.ToString();
                
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}