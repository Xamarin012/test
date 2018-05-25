using Entities.Fonts;
using Entities.StaticData;
using Entities.Ticket;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Font = iTextSharp.text.Font;

namespace NovasTiffBL.PdfCellFunctionality
{
    public static class CellProcessing
    {
        public static PdfPCell GetBarcodeTicketData(Ticket pTicet, Font pLargeFont)
        {
            PdfPCell lPdfCell = new PdfPCell { Colspan = 2 };
            PdfPCell lLeftCell = new PdfPCell();
            PdfPCell lRightCell = new PdfPCell();

            lRightCell.Border = 0;
            lRightCell.HorizontalAlignment = Element.ALIGN_RIGHT;

            lRightCell = GetBarcode(pTicet);

            Phrase lPhrase = new Phrase(pTicet.EcbNovNumber, pLargeFont);
            Paragraph lParagrapth = new Paragraph(lPhrase);
            lLeftCell.AddElement(lParagrapth);
            lLeftCell.Border = 0;

            PdfPTable lBarcodeTable = new PdfPTable(2);
            lBarcodeTable.HorizontalAlignment = 0; //left align
            lBarcodeTable.AddCell(lLeftCell);
            lBarcodeTable.AddCell(lRightCell);
            lPdfCell.AddElement(lBarcodeTable);
            lPdfCell.Border = 0;
            return lPdfCell;
        }

        public static PdfPCell GetSignature(XMLTicketRow pData, Ticket pTicet, Font lLargeFontLine)
        {
            List<PdfPCell> lList = new List<PdfPCell>();
            PdfPCell lInnerCell = new PdfPCell() { Colspan = 2 };
            PdfPCell lCell = new PdfPCell();
            if (pTicet.SignatureBitmap.Length > 0)
            {
                Bitmap _image1 = (Bitmap)System.Drawing.Image.FromStream(new System.IO.MemoryStream(pTicet.SignatureBitmap));
                Bitmap _image2 = new Bitmap(new Bitmap(_image1));
                int width = _image2.Width + _image1.Width + 15;
                int height = Math.Max(_image2.Height, 71);
                Bitmap fullBmp = new Bitmap(width, height);
                Graphics gr = Graphics.FromImage(fullBmp);
                System.Drawing.Brush brushBlack = new SolidBrush(System.Drawing.Color.White);
                gr.FillRectangle(brushBlack, 0, 0, _image2.Width + 15, 71);
                gr.FillRectangle(brushBlack, _image2.Width + 15, 0, _image1.Width, 71);
                //gr.DrawImage(img, 0, 0, img.Width + 15, 0);
                gr.DrawImage(_image1, _image2.Width + 15, 0);
                fullBmp.Save("FullImage1", ImageFormat.Jpeg);
                iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(fullBmp, System.Drawing.Imaging.ImageFormat.Jpeg);
                pdfImage.WidthPercentage = 70;
                pdfImage.Alignment = iTextSharp.text.Image.ALIGN_LEFT;

                lCell.AddElement(pdfImage);
                lCell.HorizontalAlignment = Element.ALIGN_LEFT;
                lCell.Border = 0;

                //lCell.HorizontalAlignment = (int)HorizontalAlignment.Center;
                lCell.HorizontalAlignment = Element.ALIGN_CENTER;

                lList.Add(lCell);
            }
            else
            {
                PdfPCell lCellEmptyReplace = new PdfPCell();
                lCellEmptyReplace.Border = 0;

                lList.Add(lCellEmptyReplace);
                lList.Add(GetHorizontalLineTicketData(pData, lLargeFontLine));
            }

            PdfPCell lCellEmpty = new PdfPCell();
            lCellEmpty.Border = 0;

            lList.Add(lCellEmpty);
            lList.Add(GetHorizontalLineTicketData(pData, lLargeFontLine));
            PdfPTable lFinalTable = new PdfPTable(2);
            //  lFinalTable.WidthPercentage = 120f;
            lFinalTable.HorizontalAlignment = 0; //left align
                                                 // float[] sglTblHdWidths = new float[2];
                                                 //sglTblHdWidths[0] = 50f; //column width
                                                 //sglTblHdWidths[1] = 50f;
                                                 //lFinalTable.SetWidths(sglTblHdWidths); // set width to the table when it is created and not to columns

            lFinalTable.AddCell(lCell);
            lFinalTable.AddCell(lCellEmpty);
            lInnerCell.AddElement(lFinalTable);
            lInnerCell.Border = 0;

            return lInnerCell;
        }

        internal static PdfPCell GetHorizontalLineTicketData(XMLTicketRow row, Font printLargeFontLine)
        {
            Paragraph p2 = new Paragraph(row.HTMLData, printLargeFontLine);
            PdfPCell cell = new PdfPCell(p2) { Colspan = 2 };
            cell.Border = 0;
            //cell.HorizontalAlignment = (int)HorizontalAlignment.Center;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            return cell;
        }

        public static PdfPCell GetBarcode(Ticket pTicket)
        {
            Barcode128 code128 = new Barcode128();
            code128.CodeType = iTextSharp.text.pdf.Barcode.EAN8;
            code128.ChecksumText = true;
            code128.GenerateChecksum = true;
            code128.StartStopText = false;
            code128.Code = pTicket.EcbNovNumber;

            // Create a blank image
            System.Drawing.Bitmap bmpimg = new Bitmap(250, 25); // provide width and height based on the barcode image to be generated. harcoded for sample purpose

            Graphics bmpgraphics = Graphics.FromImage(bmpimg);
            bmpgraphics.Clear(System.Drawing.Color.White); // Provide this, else the background will be black by default

            // generate the code128 barcode
            bmpgraphics.DrawImage(code128.CreateDrawingImage(System.Drawing.Color.Black, System.Drawing.Color.White), new System.Drawing.Point(0, 0));
            // Save the output stream as gif. You can also save it to external file
            bmpimg.Save("testB", System.Drawing.Imaging.ImageFormat.Tiff);
            iTextSharp.text.Image pdfImage1 = iTextSharp.text.Image.GetInstance(bmpimg, System.Drawing.Imaging.ImageFormat.Tiff);
            pdfImage1.WidthPercentage = 150;
            // pdfImage1.WidthPercentage = 50;

            PdfPCell lCell = new PdfPCell();
            lCell.AddElement(pdfImage1);
            lCell.Border = 0;
            lCell.HorizontalAlignment = (int)HorizontalAlignment.Left;

            //List<PdfPCell> lList = new List<PdfPCell>();
            //lList.Add(lCell);
            //lList.Add(GetHorizontalLineTicketData(pData));

            return lCell;
        }

        public static PdfPCell GetFormatedTextTicketData(XMLTicketRow pData, Font pFont, Font pBoldFont)
        {
            Dictionary<string, string> lDataDictionary = new Dictionary<string, string>();
            Dictionary<string, string> lDisplayTextDictionary = new Dictionary<string, string>();
            string[] lDBData = pData.DBData.Split('|');

            for (int i = 0; i < lDBData.Length; i++)
            {
                lDataDictionary.Add("D" + i, lDBData[i]);
            }

            string[] lDisplayText = pData.DisplayText.Split('|');

            for (int i = 0; i < lDisplayText.Length; i++)
            {
                lDisplayTextDictionary.Add("T" + i, lDisplayText[i]);
            }

            //PdfPTable lTempTable = null;
            string[] lMap = pData.Map.Split('|');
            Phrase lPhrase = new Phrase();
            Paragraph lParagraph = new Paragraph();

            foreach (var lItem in lMap)
            {
                if (lItem.Substring(0, 1) == "T")
                {
                    lPhrase = new Phrase(lDisplayTextDictionary[lItem], pBoldFont);
                    lPhrase.Add(" ");
                }
                else
                {
                    lPhrase = new Phrase(lDataDictionary[lItem], pFont);
                    lPhrase.Add(" ");
                }
                lParagraph.Add(lPhrase);
            }

            PdfPCell cell = new PdfPCell(lParagraph) { Colspan = 2 };
            cell.Border = 0;
            switch (pData.HTMLConfiguration)
            {
                case "Center":
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    break;

                case "Left":
                    cell.HorizontalAlignment = Element.ALIGN_LEFT;
                    break;
            }
            return cell;
        }

        public static PdfPCell GetTextTicketData(XMLTicketRow pData, Ticket pTicket, Font pFont, Font pBoldFont)
        {
            Dictionary<string, string> lDataDictionary = new Dictionary<string, string>();
            Dictionary<string, string> lDisplayTextDictionary = new Dictionary<string, string>();
            string[] lDBData = pData.DBData.Split('|');

            for (int i = 0; i < lDBData.Length; i++)
            {
                lDataDictionary.Add("D" + i, lDBData[i]); // separate data points from data map
            }

            string[] lDisplayText = pData.DisplayText.Split('|');

            for (int i = 0; i < lDisplayText.Length; i++)
            {
                lDisplayTextDictionary.Add("T" + i, lDisplayText[i]); //separate text points from data map
            }

            List<string> htmlTexts = pData.HTMLData.Split('|').ToList();

            // PdfPTable lTempTable = null;
            string[] lMap = pData.Map.Split('|');
            //  Phrase lPhrase = new Phrase();
            Paragraph lParagraph = new Paragraph();
            // PdfPCell cell = new PdfPCell(lParagraph) { Colspan = 2 };

            foreach (var lItem in lMap)
            {
                if (lItem.Substring(0, 1) == "T")
                {
                    Phrase lPhrase = new Phrase(lDisplayTextDictionary[lItem], pBoldFont);
                    lPhrase.Add(" ");
                    lParagraph.Add(lPhrase);
                }
                else
                {
                    Phrase lPhrase = new Phrase();
                    switch (lDataDictionary[lItem])
                    {
                        case "NovNumber":
                            lPhrase = new Phrase(pTicket.NovNumber.ToString(), pFont);
                            break;

                        case "AgencyId":
                            lPhrase = new Phrase(pTicket.AgencyId.Trim().Length == 0 ? "_________NA_________" : pTicket.AgencyId, pFont);
                            break;

                        case "ReportLevel":
                            lPhrase = new Phrase(pTicket.ReportLevel.Trim().Length == 0 ? "_________NA_________" : pTicket.ReportLevel, pFont);
                            break;

                        case "Resp1HouseNo":
                            lPhrase = new Phrase(pTicket.Resp1HouseNo.Trim().Length == 0 ? "_________NA_________" : pTicket.Resp1HouseNo, pFont);
                            break;

                        case "Resp1Address":
                            lPhrase = new Phrase(pTicket.ReportLevel.Trim().Length == 0 ? "_________NA_________" : pTicket.ReportLevel, pFont);
                            break;

                        case "Resp1Address1":
                            lPhrase = new Phrase(pTicket.Resp1Address1.Trim().Length == 0 ? "_________NA_________" : pTicket.Resp1Address1, pFont);
                            break;

                        case "Resp1City":
                            lPhrase = new Phrase(pTicket.Resp1City.Trim().Length == 0 ? "_________NA_________" : pTicket.Resp1City, pFont);
                            break;

                        case "Resp1FirstName":
                            lPhrase = new Phrase(pTicket.Resp1FirstName, pFont);
                            break;

                        case "Resp1LastName":
                            lPhrase = new Phrase(pTicket.Resp1LastName, pFont);
                            break;

                        case "Resp1MiddleInitial":
                            lPhrase = new Phrase(pTicket.Resp1MiddleInitial, pFont);
                            break;

                        case "Resp1Sex":
                            lPhrase = new Phrase(pTicket.Resp1Sex.Trim().Length == 0 ? "_________NA_________" : pTicket.Resp1Sex, pFont);
                            break;

                        case "Resp1State":
                            lPhrase = new Phrase(pTicket.Resp1State.Trim().Length == 0 ? "_________NA_________" : pTicket.Resp1State, pFont);
                            break;

                        case "Resp1Zip":
                            lPhrase = new Phrase(pTicket.Resp1Zip.Trim().Length == 0 ? "_________NA_________" : pTicket.Resp1Zip, pFont);
                            break;

                        case "LicenseNumber":
                            var text = pTicket.LicenseNumber.Trim().Length == 0 ? "_________NA_________" : pTicket.LicenseNumber;
                            text = GetStringBaseOnDisplayLabel(pBoldFont, pFont, text, htmlTexts, lMap.ToList().IndexOf(lItem));
                            lPhrase = new Phrase(text, pFont);
                            break;

                        case "LicenseExpDate":
                            lPhrase = new Phrase(pTicket.LicenseExpDate.Trim().Length == 0 ? "_________NA_________" : pTicket.LicenseExpDate, pFont);
                            break;

                        case "LicenseTypeDesc":
                            lPhrase = new Phrase(pTicket.LicenseTypeDesc.Trim().Length == 0 ? "_________NA_________" : pTicket.LicenseTypeDesc, pFont);
                            break;

                        case "LicenseAgency":
                            lPhrase = new Phrase(pTicket.LicenseAgency.Trim().Length == 0 ? "_________NA_________" : pTicket.LicenseAgency, pFont);
                            break;

                        case "IssuedTimeStamp":
                            lPhrase = new Phrase(pTicket.IssuedTimeStamp.Trim().Length == 0 ? "_________NA_________" : pTicket.IssuedTimeStamp, pFont);
                            break;

                        case "TicketPrintingType":
                            lPhrase = new Phrase(pTicket.TicketPrintingType.Trim().Length == 0 ? "_________NA_________" : pTicket.TicketPrintingType, pFont); //todo need to be fiugred out probalby will not be needed this is type of print like void....
                            break;

                        case "PlaceBoroCode":
                            lPhrase = new Phrase(pTicket.PlaceBoroCode.Trim().Length == 0 ? "_________NA_________" : pTicket.PlaceBoroCode, pFont);
                            break;

                        case "CBNO":
                            lPhrase = new Phrase(pTicket.CBNO.Trim().Length == 0 ? "_________NA_________" : pTicket.CBNO, pFont);
                            break;

                        case "PlaceAddressDescriptor":
                            lPhrase = new Phrase(pTicket.PlaceAddressDescriptor.Trim().Length == 0 ? "_________NA_________" : pTicket.PlaceAddressDescriptor, pFont); //todo need to be fiugred out probalby will not be needed this is type of print like void....
                            break;

                        case "PlaceBBL":
                            lPhrase = new Phrase(pTicket.PlaceBBL.Trim().Length == 0 ? "_________NA_________" : pTicket.PlaceBBL, pFont);
                            break;

                        case "DisplayAddress":
                            lPhrase = new Phrase(pTicket.DisplayAddress.Trim().Length == 0 ? "_________NA_________" : pTicket.DisplayAddress, pFont);
                            break;

                        case "EcbNovNumber":
                            lPhrase = new Phrase(pTicket.EcbNovNumber.Trim().Length == 0 ? "_________NA_________" : pTicket.EcbNovNumber, NovasFonts.PrintBoldFontECBNOV); //Requires bigger fonts for ECB nov number
                            break;

                        case "HearingDate":
                            lPhrase = new Phrase(pTicket.HearingTimestamp.Trim().Length == 0 ? "_________NA_________" : String.Format("{0:MMM d, yyyy}", DateTime.Parse(pTicket.HearingTimestamp)), pFont);
                            break;

                        case "HearingTime":
                            lPhrase = new Phrase(pTicket.HearingTimestamp.Trim().Length == 0 ? "_________NA_________" : String.Format("{0:t}", DateTime.Parse(pTicket.HearingTimestamp)), pFont);
                            break;

                        case "Comments":
                            lPhrase = new Phrase(pTicket.Comments.Trim().Length == 0 ? "_________NA_________" : pTicket.Comments, pFont);
                            break;

                        case "CheckSum":
                            lPhrase = new Phrase(pTicket.CheckSum.Trim().Length == 0 ? "_________NA_________" : pTicket.CheckSum, pFont);
                            break;

                        case "CodeLawDescription":
                            lPhrase = new Phrase(pTicket.CodeLawDescription.Trim().Length == 0 ? "_________NA_________" : pTicket.CodeLawDescription, pFont);
                            break;

                        case "LawSection":
                            lPhrase = new Phrase(pTicket.LawSection.Trim().Length == 0 ? "_________NA_________" : pTicket.LawSection, pFont);
                            break;

                        case "HHTIdentifier":
                            lPhrase = new Phrase(pTicket.HHTIdentifier.Trim().Length == 0 ? "_________NA_________" : pTicket.HHTIdentifier, pFont);
                            break;

                        case "PrintViolationCode":
                            lPhrase = new Phrase(pTicket.PrintViolationCode.Trim().Length == 0 ? "_________NA_________" : pTicket.PrintViolationCode, pFont);
                            break;

                        case "MailableAmount":
                            lPhrase = new Phrase(pTicket.MailableAmount.Trim().Length == 0 ? "_________NA_________" : Math.Round(Convert.ToDecimal(pTicket.MailableAmount), 2).ToString(), pFont);
                            break;

                        case "MaximumAmount":
                            lPhrase = new Phrase(pTicket.MaximumAmount.Trim().Length == 0 ? "_________NA_________" : Math.Round(Convert.ToDecimal(pTicket.MaximumAmount), 2).ToString(), pFont);
                            break;

                        case "ViolationScript":
                            lPhrase = new Phrase(pTicket.ViolationScript.Trim().Length == 0 ? "_________NA_________" : pTicket.ViolationScript, pFont);
                            break;

                        case "PropertyType":
                            lPhrase = new Phrase(pTicket.PropertyType.Trim().Length == 0 ? "_________NA_________" : pTicket.PropertyType, pFont);
                            break;

                        case "IsPetitionerCourtAppear":
                            lPhrase = new Phrase(pTicket.IsPetitionerCourtAppear.Trim().Length == 0 ? "_________NA_________" : pTicket.IsPetitionerCourtAppear, pFont);
                            break;

                        case "OfficerName":
                            lPhrase = new Phrase(pTicket.OfficerName.Trim().Length == 0 ? "_________NA_________" : pTicket.OfficerName, pFont);
                            break;

                        case "AbbrevName":
                            lPhrase = new Phrase(pTicket.AbbrevName.Trim().Length == 0 ? "_________NA_________" : pTicket.AbbrevName, pFont);
                            break;

                        case "Title":
                            lPhrase = new Phrase(pTicket.Title.Trim().Length == 0 ? "_________NA_________" : pTicket.Title, pFont);
                            break;

                        case "InternalShortDescription":
                            lPhrase = new Phrase(pTicket.InternalShortDescription.Trim().Length == 0 ? "_________NA_________" : pTicket.InternalShortDescription, pFont);
                            break;

                        case "LawDescription":
                            lPhrase = new Phrase(pTicket.LawDescription.Trim().Length == 0 ? "_________NA_________" : pTicket.LawDescription, pFont);
                            break;

                        default:
                            break;
                    }
                    lPhrase.Add(" "); //add blank space to separate elements

                    lParagraph.Add(lPhrase);
                }
            }
            lParagraph.Alignment = 0;
            PdfPCell cell = new PdfPCell(lParagraph) { Colspan = 2 };
            cell.Border = 0;

            switch (pData.HTMLConfiguration)
            {
                case "Center":
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    //(int)HorizontalAlignment.Center;
                    break;

                case "Left":
                    cell.HorizontalAlignment = Element.ALIGN_LEFT;
                    break;

                case "Right":
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    break;
            }

            return cell;
        }

        private static string GetStringBaseOnDisplayLabel(Font pBoldFont, Font pFont, string value, List<string> htmlTexts, int index)
        {
            var displayLabel = htmlTexts.Count > index ? htmlTexts[index] : "";
            if (string.IsNullOrEmpty(displayLabel))
                return value;

            var boldFont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, pBoldFont.Size, pBoldFont.Style);
            var rFont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, pFont.Size, pFont.Style);
            while (rFont.BaseFont.GetWidth(value) < (boldFont.BaseFont.GetWidth(displayLabel)))
            {
                value += " ";
            }

            return value;
        }

        public static PdfPTable CreatePdfTable()
        {
            PdfPTable lTable = new PdfPTable(2);
            lTable.WidthPercentage = PdfStaticData.WidthPercentage; //widht of actual table on the page
            lTable.HorizontalAlignment = PdfStaticData.HorizontalAlignmentLeft; //left align
            float[] sglTblHdWidths = new float[2];
            sglTblHdWidths[0] = PdfStaticData.sglTblHdWidths1; //column width
            sglTblHdWidths[1] = PdfStaticData.sglTblHdWidths2;
            lTable.SetWidths(sglTblHdWidths); // set width to the table when it is created and not to columns
            return lTable;
        }

        public static List<PdfPCell> GetImageTicketData(XMLTicketRow pData, Font pFont)
        {
            string[] lImageData = pData.DisplayText.Split('|');

            List<PdfPCell> lPdfPCell = new List<PdfPCell>();
            iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(lImageData[0]);

            PdfPCell lCell0 = new PdfPCell();
            lCell0.Border = 0;
            lCell0.AddElement(image);

            Paragraph p1 = new Paragraph();
            Phrase pp1 = new Phrase(lImageData[1] + "\n", pFont);
            Phrase pp2 = new Phrase(lImageData[2], pFont);
            Phrase pp3 = new Phrase(pData.DBData, pFont);
            p1.Add(pp1);
            p1.Add(pp2);
            p1.Add(pp3);

            PdfPCell lCell1 = new PdfPCell(p1);
            lCell1.Border = 0; //remove border around image
            lCell1.HorizontalAlignment = (int)HorizontalAlignment.Left;
            lPdfPCell.Add(lCell0);
            lPdfPCell.Add(lCell1);

            return lPdfPCell;
        }
    }
}