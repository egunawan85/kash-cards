using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model
{
    public class KeyModel
    {
        //dev
        public static string PGCRYPTO_API_URL = "https://api.runegate.co";
        public static string PGCRYPTO_API_KEY = "ee02a74626a841358a9fdb4fa82c32f8";
        public static string PGCRYPTO_SECRET_KEY = "19073d27f59d49e285e53ab8938afd884e483075efe846bfb746ccf0c9a1e2ce";
        public static string QRYPTO_PAY_URL = "https://pay-otc.qrypto.trade/pay?id=";


        public static string WASABICARD_API_URL = "https://sandbox-api-merchant.wasabicard.com";
        public static string WASABICARD_API_KEY = "8128cdfe-24e8-48c9-aa3a-c093e6776924-8af5d88f-dc4f-4dc2-b05b-6174cb7dc20e";
        public static string WASABICARD_PUBLIC_KEY = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQC+f5GtV3k6tF0c76/2fGYvj1AXUYEjClHjgcBxvpG0KmdnwQgO6U4ljOzH1VZZgoACDPgZiLa4r1eBAk18YFr7cR6WX99VVuBDrujrWwEpnNb6uLs8Ht4KdIlIG55zljRbi+ABK3GCWDEMfUPkCbvkQiCzGh6fa882THK+bjKQ8wIDAQAB";
        public static string WASABICARD_PRIVATE_KEY = "MIICeQIBADANBgkqhkiG9w0BAQEFAASCAmMwggJfAgEAAoGBAL5/ka1XeTq0XRzvr/Z8Zi+PUBdRgSMKUeOBwHG+kbQqZ2fBCA7pTiWM7MfVVlmCgAIM+BmItrivV4ECTXxgWvtxHpZf31VW4EOu6OtbASmc1vq4uzwe3gp0iUgbnnOWNFuL4AErcYJYMQx9Q+QJu+RCILMaHp9rzzZMcr5uMpDzAgMBAAECgYEAtDVf7Rg74ZHwJ8iCsG08Ca/MN1LuE+TWVJ9RGwkJMuOOULNl2R1RxOoMsHobpq9yQv5b0WPoXsvYvn0cKhXI2kI/OrinvrxxY9+8uZ127E6q6pCZgYOX0/EEAluhCG9Rt3vIA79em2idCDJcGiLyyqNL9b4/UffqM7hnNO65yDkCQQD0xoEPCp0WDRhVuOzaHsXpoXeFBKgvxO7TNYsAo/mFz8+zNFPE2l/FG56kQhgRQLBRmBWQA4wahHwp+tNe7etlAkEAxzvjlXC5gfrBRaMgyaW+jqXcFONgHJi/oJ349LpMzmLoZI33IGiSFFS7IxSACKn9IcWz/T/HgP35dDPne+fBdwJBAOm3YCNsjvEvL70qBX1/RJn/go+QEscJ0r/4r/C8oNQTyM3jeNjNagRaiu9r7G8MxU4jWPNZb70iIywyQwCxS8ECQQDCyhgBhNuqbErIVex5mnYLq7fYKFJQwzsfwzOjuf3cDzHdFjvW0MZ54Dmy25kuX1ygx1XptZDN2gIpjZG2P+mTAkEAqukYhSY7U7LZIZ+t2ecjci8jgnYlbaJonS3f/v8hlAFUclKdHC2plGJ4dIshJEBrp9gGUN5TGWtRcMqIS+PJyg==";
        public static string WASABICARD_PRIVATE_KEY_XML = "<RSAKeyValue><Modulus>gjmPFXaURIZp2oKP9ej7vtPFRZY+pUb3fKVbFaIFkuMORru6kNpNeejUNtg2O4xr2l+6J8Yb2ABfwAcynx4mmiP90DQvrBo7YMJoJHBPmfx9ArKAEG5vxwVA2cTNdttSsn850CZ0/x9X1LL+Ld3wSfv/vGCOINYXJFR31pyqxU0=</Modulus><Exponent>AQAB</Exponent><P>z76mXtmjzomB0tc47H6SdNCtVrRtJhAh9Cidz/4KQV8FPBdycb0/knQkMcYb51WfS4/PVBjvmMQths0tAsPRew==</P><Q>oHlCiAlM/e7itUeF+FQpGmjEZNvKd+FCw07ZHAd/4KmoZdJ98iCa/k57X8WXC+5XiFI8hGLglc4xUZWt/HhV1w==</Q><DP>YH9kdGaQCl4hKbjDPkdE7HIKMl443RddTjaXp4ePZ/IlUlZp2J9ZqkO8lEo7p+dDySuR2LSEhueJZjZkFAa1hQ==</DP><DQ>T0QWbQPLGBOLwGeX8VYBB56AhCFdHWITjE3CSGob7GlhWQpkU9lvNfamUmRTe/07F4cnhW0h6l1zVw1MZ804+Q==</DQ><InverseQ>cfHBfwgIIBOy8frKyXYCtcsuzn8IVZ7SaY3FxbpRYrkehalwsR0G+b3BCA1XNAfXvMKR29Y9TNIyTKXdYsHS9g==</InverseQ><D>Fr3gi/oCWJk0oTFN3L8MP74R5F4hoJFtJPpnlraNzKIUWyvrn+JPx2turAlEJ1AVgbRX+Rwyvp0KG6nmID2OtfonJIauUMN6urufF7mtW9avBwDoz+9RMu+srLN47pFJKOJRUZ1U8dBopDesG2UiJP+Ld8m7kRRALpDFxU8wAy0=</D></RSAKeyValue>";
        public static string WASABICARD_WSBPUBLIC_KEY = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCyyPUFA1Zu2GFS2DdPL6xP5bGmPG9dFMSXpDy3GykW/lL4VR24UsjKs5h6g8OfSOuOze3yC4jWE/IBW5/L9xM9kGvFQK/S1zr44hMnDn+7S5MSi+jnZDRIrCRQJTqkwfpy39h2cQ4xlHKfbEjkkeo5ZQ0VFgRHYPr7b10l+3j8gwIDAQAB";

        public static string QRYPTO_URL_FORGOT_PASSWORD = "https://kash.cards/newpassword?id=";

        public static string QRYPTO_ENVIRONMENT = "dev";

        public static string EMAIL_ADDRESS = "no-reply@qrypto.trade";
        public static string EMAIL_PASSWORD = "olkebbjshtdbzcae";
        public static string EMAIL_SMTP_GATEWAY = "smtp.gmail.com";
        public static int EMAIL_SMTP_PORT = 587;




        //prod kashnow
        //public static string PGCRYPTO_API_URL = "https://api.runegate.co";
        //public static string PGCRYPTO_API_KEY = "ee02a74626a841358a9fdb4fa82c32f8";
        //public static string PGCRYPTO_SECRET_KEY = "19073d27f59d49e285e53ab8938afd884e483075efe846bfb746ccf0c9a1e2ce";
        //public static string QRYPTO_PAY_URL = "https://pay-otc.qrypto.trade/pay?id=";


        //public static string WASABICARD_API_URL = "https://api-merchant.wasabicard.com";
        //public static string WASABICARD_API_KEY = "d6246d5d-0804-42bc-acbb-19d23eed2272-23d0e8a4-420e-4397-a9c8-0dd3fcab23e1";
        //public static string WASABICARD_PUBLIC_KEY = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQC+f5GtV3k6tF0c76/2fGYvj1AXUYEjClHjgcBxvpG0KmdnwQgO6U4ljOzH1VZZgoACDPgZiLa4r1eBAk18YFr7cR6WX99VVuBDrujrWwEpnNb6uLs8Ht4KdIlIG55zljRbi+ABK3GCWDEMfUPkCbvkQiCzGh6fa882THK+bjKQ8wIDAQAB";
        //public static string WASABICARD_PRIVATE_KEY = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBALopRMF/aU1GI1tRhQB5rY0HEHbLFYGx37JfXDH5ouMpi0oU0jZQdzZHgYPDkrW2g31bUiFOUE4hhM6zM40y+7HxSigmdzg8utsw9b5MfJ6sD3dFJXwgwhA+N5R2AIXkztjvgG5Tja9eRUQI0QqJek2Z9RRzOG24tyoQFTe2lnM3AgMBAAECgYEAtV0PRxRCGq6CM60vzk687fA78f/YbApGzRhqUaXLM1R+ByZRxeiOu6reWuhmPfIaGD6nvRr20aeGI3oidyV8Xy4+QF+C7fGzRradKztnli9do4isAEc5jBgqkhA7sNeE6eiFeTODAS0Z+wWqB09BOQ9eHsMBVD2AIQnxjObVTkECQQDawloYy9kSjw3hZUOvpzTGjDV6Oo1ke8MYH59bCQNt3qwSix7yNTkpantOYwRQ8FuQKGM2qOTmiZ50hFVlyS4lAkEA2dpHPwV74uzNfuSr6VL0Fb1ue0Y/6Eab21yHAjaI9pQL9pbvX07R8aRyUQHX7gYUOqem3xcfnabUzBY6t073KwJAUYyoJ5w+VMPNadvlKqMLcoSsHt+a+/2DEggf0MEAbUHYJaWFKMecgor2YpdY8Y9YotnbenHluudMkaUPbL1dnQJAW1+SQnyqWaO5DWAcOuDwP64UiOAOLf5voLJObj8xczrlSahE/lSw+glfaVq8lrk2AuQOucOZHya6Wl94gSo9wQJAcNzDiLoHrpPHxK0bBK4bZYPWocdTU7T6RfZCi7IzySBFLRihTOOp5ecvn3EfjgvwPbHPz0Bk9lw+Ieo/n8gGzg==";
        //public static string WASABICARD_PRIVATE_KEY_XML = "<RSAKeyValue><Modulus>uilEwX9pTUYjW1GFAHmtjQcQdssVgbHfsl9cMfmi4ymLShTSNlB3NkeBg8OStbaDfVtSIU5QTiGEzrMzjTL7sfFKKCZ3ODy62zD1vkx8nqwPd0UlfCDCED43lHYAheTO2O+AblONr15FRAjRCol6TZn1FHM4bbi3KhAVN7aWczc=</Modulus><Exponent>AQAB</Exponent><P>2sJaGMvZEo8N4WVDr6c0xow1ejqNZHvDGB+fWwkDbd6sEose8jU5KWp7TmMEUPBbkChjNqjk5omedIRVZckuJQ==</P><Q>2dpHPwV74uzNfuSr6VL0Fb1ue0Y/6Eab21yHAjaI9pQL9pbvX07R8aRyUQHX7gYUOqem3xcfnabUzBY6t073Kw==</Q><DP>UYyoJ5w+VMPNadvlKqMLcoSsHt+a+/2DEggf0MEAbUHYJaWFKMecgor2YpdY8Y9YotnbenHluudMkaUPbL1dnQ==</DP><DQ>W1+SQnyqWaO5DWAcOuDwP64UiOAOLf5voLJObj8xczrlSahE/lSw+glfaVq8lrk2AuQOucOZHya6Wl94gSo9wQ==</DQ><InverseQ>cNzDiLoHrpPHxK0bBK4bZYPWocdTU7T6RfZCi7IzySBFLRihTOOp5ecvn3EfjgvwPbHPz0Bk9lw+Ieo/n8gGzg==</InverseQ><D>tV0PRxRCGq6CM60vzk687fA78f/YbApGzRhqUaXLM1R+ByZRxeiOu6reWuhmPfIaGD6nvRr20aeGI3oidyV8Xy4+QF+C7fGzRradKztnli9do4isAEc5jBgqkhA7sNeE6eiFeTODAS0Z+wWqB09BOQ9eHsMBVD2AIQnxjObVTkE=</D></RSAKeyValue>";
        //public static string WASABICARD_WSBPUBLIC_KEY = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCyyPUFA1Zu2GFS2DdPL6xP5bGmPG9dFMSXpDy3GykW/lL4VR24UsjKs5h6g8OfSOuOze3yC4jWE/IBW5/L9xM9kGvFQK/S1zr44hMnDn+7S5MSi+jnZDRIrCRQJTqkwfpy39h2cQ4xlHKfbEjkkeo5ZQ0VFgRHYPr7b10l+3j8gwIDAQAB";

        //public static string QRYPTO_URL_FORGOT_PASSWORD = "https://kash.now/newpassword?id=";

        //public static string QRYPTO_ENVIRONMENT = "prod";

        //public static string EMAIL_ADDRESS = "noreply@kash.now";
        //public static string EMAIL_PASSWORD = "3BE8AdF6-023c-4CE2-8993-6684fb975c8a";
        //public static string EMAIL_SMTP_GATEWAY = "mail.spacemail.com";
        //public static int EMAIL_SMTP_PORT = 465;
    }
}