using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard.Admin.Models
{
    public class Common
    {
        private static string loadRsaPrivateKeyPem()
        {
            return KeyModel.WASABICARD_PRIVATE_KEY_XML;
        }

        public static string decrypt(string strText)
        {
            var testData = Encoding.UTF8.GetBytes(strText);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    var base64Encrypted = strText;

                    // server decrypting data with private key                    
                    rsa.FromXmlString(loadRsaPrivateKeyPem());

                    var resultBytes = Convert.FromBase64String(base64Encrypted);
                    var decryptedBytes = rsa.Decrypt(resultBytes, RSAEncryptionPadding.Pkcs1);
                    var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                    return decryptedData.ToString();
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }


        public static bool checkID()
        {
            //return true;
            if (SessionLib.Current.AdminID != "")
                return true;
            else
                return false;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        //public static SetupCode generate2FA(string email, string key)
        //{
        //    try
        //    {
        //        TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
        //        SetupCode setupInfo = tfa.GenerateSetupCode("Qrypto Payment Gateway", email, key, false, 3);

        //        string qrCodeImageUrl = setupInfo.QrCodeSetupImageUrl;
        //        string manualEntrySetupCode = setupInfo.ManualEntryKey;

        //        return setupInfo;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        public static void ExportExcel(GridView gv, string title)
        {
            //IWorkbook workbook = new XSSFWorkbook();

            //ISheet sheet1 = workbook.CreateSheet("Sheet 1");

            ////make a header row
            //IRow row1 = sheet1.CreateRow(0);
            //row1.Height = (short)600;


            //// create font style
            //XSSFFont myFont = (XSSFFont)workbook.CreateFont();
            //myFont.FontHeightInPoints = (short)13;
            ////myFont.FontName = "Tahoma";
            //myFont.IsBold = true;

            //// create bordered cell style
            //XSSFCellStyle headerStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            //headerStyle.SetFont(myFont);
            //headerStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Medium;
            //headerStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Medium;
            //headerStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Medium;
            //headerStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Medium;
            //headerStyle.Alignment = HorizontalAlignment.Left;
            //headerStyle.VerticalAlignment = VerticalAlignment.Center;

            //// create bordered cell style
            //XSSFCellStyle valueStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            //valueStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Medium;
            //valueStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Medium;
            //valueStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Medium;
            //valueStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Medium;
            //valueStyle.Alignment = HorizontalAlignment.Left;
            //valueStyle.VerticalAlignment = VerticalAlignment.Center;


            //int[] maxNumCharacters = new int[gv.Columns.Count];

            //for (int j = 0; j < gv.Columns.Count; j++)
            //{
            //    if (j == 0)
            //        maxNumCharacters[j] = 0;

            //    ICell cell = row1.CreateCell(j);
            //    String columnName = gv.Columns[j].ToString();
            //    cell.SetCellValue(columnName);
            //    cell.CellStyle = headerStyle;

            //    if (columnName.Count() > maxNumCharacters[j])
            //        maxNumCharacters[j] = columnName.Count();
            //}


            ////loops through data
            //for (int i = 0; i < gv.Rows.Count; i++)
            //{
            //    IRow row = sheet1.CreateRow(i + 1);
            //    row.Height = (short)400;
            //    for (int j = 0; j < gv.Columns.Count; j++)
            //    {
            //        ICell cell = row.CreateCell(j);
            //        String columnName = gv.Columns[j].ToString();
            //        if (j == 0)
            //            cell.SetCellValue((i + 1).ToString());
            //        else
            //            cell.SetCellValue(gv.Rows[i].Cells[j].Text.ToString().Replace("&nbsp;", ""));
            //        cell.CellStyle = valueStyle;

            //        if (gv.Rows[i].Cells[j].Text.Count() > maxNumCharacters[j])
            //            maxNumCharacters[j] = gv.Rows[i].Cells[j].Text.Count();
            //    }
            //}

            //for (int i = 0; i < maxNumCharacters.Count(); i++)
            //{
            //    //sheet1.SetColumnWidth(i, (columns[columnNames[i]].ToString().Length) * 2 * 256);

            //    //int width = ((int)(maxNumCharacters[i] * 2)) * 256;
            //    //if (width > 256)
            //    //    sheet1.SetColumnWidth(i, 256);
            //    //else
            //    //    sheet1.SetColumnWidth(i, width);

            //    sheet1.AutoSizeColumn(i);
            //    GC.Collect();
            //}

            //using (var exportData = new MemoryStream())
            //{
            //    System.Web.HttpContext.Current.Response.Clear();
            //    workbook.Write(exportData);
            //    System.Web.HttpContext.Current.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            //    System.Web.HttpContext.Current.Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}", title + ".xlsx"));
            //    System.Web.HttpContext.Current.Response.BinaryWrite(exportData.ToArray());
            //    System.Web.HttpContext.Current.Response.End();
            //}
        }
    }
}