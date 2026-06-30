using Newtonsoft.Json;
using QryptoCard.INT.Model.WasabiCard;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // Root-cause guard: WasabiCard's openCard / openCardWithHolder responses carry the issued cardNo,
    // but our models used to drop it (no property) — so the buy path could never stamp the card and
    // every purchase stranded waiting on a webhook. These pin that the cardNo now deserializes, so the
    // synchronous finalize can read res.data[0].cardNo.
    public class OpenCardResponseCardNoTests
    {
        [Fact]
        public void OpenCardWithHolder_Response_DeserializesCardNo()
        {
            // Shape taken from a real prd openCardWithHolder success response.
            const string json = "{\"success\":true,\"code\":200,\"msg\":\"SUCCESS\",\"data\":[{" +
                "\"cardNo\":\"WD202606302071906404663844864\",\"orderNo\":\"2071906403950813184\"," +
                "\"merchantOrderNo\":\"QRYCRDBUY000000001002\",\"currency\":\"USD\",\"amount\":\"1\"," +
                "\"fee\":\"0\",\"receivedAmount\":\"0\",\"receivedCurrency\":\"USD\",\"type\":\"create\"," +
                "\"status\":\"success\",\"transactionTime\":1782815956000}]}";

            var model = JsonConvert.DeserializeObject<WCOpenCardWithHolderResponseModel>(json);

            Assert.NotNull(model);
            Assert.Equal(200, model.code);
            Assert.Single(model.data);
            Assert.Equal("WD202606302071906404663844864", model.data[0].cardNo);
        }

        [Fact]
        public void OpenCard_Response_DeserializesCardNo()
        {
            const string json = "{\"success\":true,\"code\":200,\"msg\":\"SUCCESS\",\"data\":[{" +
                "\"cardNo\":\"WB202606292071542934600704001\",\"orderNo\":\"2071542933807980544\"," +
                "\"merchantOrderNo\":\"QRYCRDBUY000000001001\",\"currency\":\"USD\",\"amount\":\"1\"," +
                "\"fee\":\"0\",\"receivedAmount\":\"0\",\"receivedCurrency\":\"USD\",\"type\":\"create\"," +
                "\"status\":\"success\",\"transactionTime\":1782729297000}]}";

            var model = JsonConvert.DeserializeObject<WCOpenCardResponseModel>(json);

            Assert.NotNull(model);
            Assert.Single(model.data);
            Assert.Equal("WB202606292071542934600704001", model.data[0].cardNo);
        }
    }
}
