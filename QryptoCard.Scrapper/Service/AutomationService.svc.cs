using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace QryptoCard.Scrapper.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "AutomationService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select AutomationService.svc or AutomationService.svc.cs at the Solution Explorer and start debugging.
    public class AutomationService : IAutomationService
    {
        public void USAddressGenerator()
        {
            int record = 0;

            HttpClient httpClient = new HttpClient();

            var url = "https://www.fakexy.com/us-fake-address-generator-ca";
            string html = USAddressService.getWebScrapper(url);

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            string xpath = ".//table[@class='table']";
            HtmlNodeCollection data = htmlDocument.DocumentNode.SelectNodes(xpath);

            return;
        }
        public async Task<int> AddressGenerator()
        {
            int record = 0;

            HttpClient httpClient = new HttpClient();

            var url = "https://www.fakexy.com/";
            string html = await httpClient.GetStringAsync(url);

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            string xpath = ".//table[@class='table']";
            HtmlNodeCollection data = htmlDocument.DocumentNode.SelectNodes(xpath);

            return record;
        }
    }
}
