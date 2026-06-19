using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Models
{
    public class KeyModel
    {
        //========= DEV =========//

        public static string APPKEY = "k3Yh45Hp9QrypT0C4rD";
        public static string USER_EMAIL = "syapril@qrypto.trade";
        public static string USER_PASSWORD = "B3wZnoBNJv7yjDN1rrCyyA==";

        ////local
        ////public static string API_URL = "http://localhost:54044";
        ////public static string PAYMENT_LINK = "https://pay-pg.octopath.cloud/payment.aspx?id=";

        ////local
        //public static string API_URL = "https://api-dev.qrypto.cards";
        ////public static string API_URL = "http://localhost:50405";
        //public static string PAYMENT_LINK = "https://pay.qrypto.trade/payment.aspx?id=";
        //public static string REFERRAL_URL = "http://localhost:50316/register?id=";
        //public static string DETAIL_URL = "http://localhost:50316/card/carddetail?id=";
        //public static string DETAIL_OWN_URL = "http://localhost:50316/card/mycarddetail?id=";
        //public static string TXCARD_URL = "http://localhost:50316/txcard?id=";
        //public static string WASABICARD_PRIVATE_KEY_XML = "<RSAKeyValue><Modulus>gjmPFXaURIZp2oKP9ej7vtPFRZY+pUb3fKVbFaIFkuMORru6kNpNeejUNtg2O4xr2l+6J8Yb2ABfwAcynx4mmiP90DQvrBo7YMJoJHBPmfx9ArKAEG5vxwVA2cTNdttSsn850CZ0/x9X1LL+Ld3wSfv/vGCOINYXJFR31pyqxU0=</Modulus><Exponent>AQAB</Exponent><P>z76mXtmjzomB0tc47H6SdNCtVrRtJhAh9Cidz/4KQV8FPBdycb0/knQkMcYb51WfS4/PVBjvmMQths0tAsPRew==</P><Q>oHlCiAlM/e7itUeF+FQpGmjEZNvKd+FCw07ZHAd/4KmoZdJ98iCa/k57X8WXC+5XiFI8hGLglc4xUZWt/HhV1w==</Q><DP>YH9kdGaQCl4hKbjDPkdE7HIKMl443RddTjaXp4ePZ/IlUlZp2J9ZqkO8lEo7p+dDySuR2LSEhueJZjZkFAa1hQ==</DP><DQ>T0QWbQPLGBOLwGeX8VYBB56AhCFdHWITjE3CSGob7GlhWQpkU9lvNfamUmRTe/07F4cnhW0h6l1zVw1MZ804+Q==</DQ><InverseQ>cfHBfwgIIBOy8frKyXYCtcsuzn8IVZ7SaY3FxbpRYrkehalwsR0G+b3BCA1XNAfXvMKR29Y9TNIyTKXdYsHS9g==</InverseQ><D>Fr3gi/oCWJk0oTFN3L8MP74R5F4hoJFtJPpnlraNzKIUWyvrn+JPx2turAlEJ1AVgbRX+Rwyvp0KG6nmID2OtfonJIauUMN6urufF7mtW9avBwDoz+9RMu+srLN47pFJKOJRUZ1U8dBopDesG2UiJP+Ld8m7kRRALpDFxU8wAy0=</D></RSAKeyValue>";


        //dev
        public static string API_URL = "https://api-app-dev.kash.cards";
        //public static string PAYMENT_LINK = "https://pay-pg.octopath.cloud/payment.aspx?id=";
        public static string REFERRAL_URL = "https://dash-dev.kash.cards/register?id=";
        public static string DETAIL_URL = "https://dash-dev.kash.cards/card/carddetail?id=";
        public static string DETAIL_OWN_URL = "https://dash-dev.kash.cards/card/mycarddetail?id=";
        public static string TXCARD_URL = "https://dash-dev.kash.cards/txcard?id=";
        public static string WASABICARD_PRIVATE_KEY_XML = "<RSAKeyValue><Modulus>gjmPFXaURIZp2oKP9ej7vtPFRZY+pUb3fKVbFaIFkuMORru6kNpNeejUNtg2O4xr2l+6J8Yb2ABfwAcynx4mmiP90DQvrBo7YMJoJHBPmfx9ArKAEG5vxwVA2cTNdttSsn850CZ0/x9X1LL+Ld3wSfv/vGCOINYXJFR31pyqxU0=</Modulus><Exponent>AQAB</Exponent><P>z76mXtmjzomB0tc47H6SdNCtVrRtJhAh9Cidz/4KQV8FPBdycb0/knQkMcYb51WfS4/PVBjvmMQths0tAsPRew==</P><Q>oHlCiAlM/e7itUeF+FQpGmjEZNvKd+FCw07ZHAd/4KmoZdJ98iCa/k57X8WXC+5XiFI8hGLglc4xUZWt/HhV1w==</Q><DP>YH9kdGaQCl4hKbjDPkdE7HIKMl443RddTjaXp4ePZ/IlUlZp2J9ZqkO8lEo7p+dDySuR2LSEhueJZjZkFAa1hQ==</DP><DQ>T0QWbQPLGBOLwGeX8VYBB56AhCFdHWITjE3CSGob7GlhWQpkU9lvNfamUmRTe/07F4cnhW0h6l1zVw1MZ804+Q==</DQ><InverseQ>cfHBfwgIIBOy8frKyXYCtcsuzn8IVZ7SaY3FxbpRYrkehalwsR0G+b3BCA1XNAfXvMKR29Y9TNIyTKXdYsHS9g==</InverseQ><D>Fr3gi/oCWJk0oTFN3L8MP74R5F4hoJFtJPpnlraNzKIUWyvrn+JPx2turAlEJ1AVgbRX+Rwyvp0KG6nmID2OtfonJIauUMN6urufF7mtW9avBwDoz+9RMu+srLN47pFJKOJRUZ1U8dBopDesG2UiJP+Ld8m7kRRALpDFxU8wAy0=</D></RSAKeyValue>";


        //prod
        //public static string API_URL = "https://api-app.kash.cards";
        ////public static string PAYMENT_LINK = "https://pay.qrypto.trade/payment.aspx?id=";
        //public static string REFERRAL_URL = "https://kash.cards/register?id=";
        //public static string DETAIL_URL = "https://kash.cards/card/carddetail?id=";
        //public static string DETAIL_OWN_URL = "https://kash.cards/card/mycarddetail?id=";
        //public static string TXCARD_URL = "https://kash.cards/txcard?id=";
        //public static string WASABICARD_PRIVATE_KEY_XML = "<RSAKeyValue><Modulus>uilEwX9pTUYjW1GFAHmtjQcQdssVgbHfsl9cMfmi4ymLShTSNlB3NkeBg8OStbaDfVtSIU5QTiGEzrMzjTL7sfFKKCZ3ODy62zD1vkx8nqwPd0UlfCDCED43lHYAheTO2O+AblONr15FRAjRCol6TZn1FHM4bbi3KhAVN7aWczc=</Modulus><Exponent>AQAB</Exponent><P>2sJaGMvZEo8N4WVDr6c0xow1ejqNZHvDGB+fWwkDbd6sEose8jU5KWp7TmMEUPBbkChjNqjk5omedIRVZckuJQ==</P><Q>2dpHPwV74uzNfuSr6VL0Fb1ue0Y/6Eab21yHAjaI9pQL9pbvX07R8aRyUQHX7gYUOqem3xcfnabUzBY6t073Kw==</Q><DP>UYyoJ5w+VMPNadvlKqMLcoSsHt+a+/2DEggf0MEAbUHYJaWFKMecgor2YpdY8Y9YotnbenHluudMkaUPbL1dnQ==</DP><DQ>W1+SQnyqWaO5DWAcOuDwP64UiOAOLf5voLJObj8xczrlSahE/lSw+glfaVq8lrk2AuQOucOZHya6Wl94gSo9wQ==</DQ><InverseQ>cNzDiLoHrpPHxK0bBK4bZYPWocdTU7T6RfZCi7IzySBFLRihTOOp5ecvn3EfjgvwPbHPz0Bk9lw+Ieo/n8gGzg==</InverseQ><D>tV0PRxRCGq6CM60vzk687fA78f/YbApGzRhqUaXLM1R+ByZRxeiOu6reWuhmPfIaGD6nvRr20aeGI3oidyV8Xy4+QF+C7fGzRradKztnli9do4isAEc5jBgqkhA7sNeE6eiFeTODAS0Z+wWqB09BOQ9eHsMBVD2AIQnxjObVTkE=</D></RSAKeyValue>";

    }
}