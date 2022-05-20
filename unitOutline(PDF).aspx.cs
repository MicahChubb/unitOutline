using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Web.UI.HtmlControls;
using System.Data.SqlClient;
using System.Data;

using System.IO;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using System.Text.RegularExpressions;

namespace WebApplication5
{
    public partial class unitOutline : System.Web.UI.Page
    {
        protected void btnExport_Click(object sender, EventArgs e)
        {
            /*
             * NOTE!
             * You need to go to Tools > NuGet Package Manager > Manage NuGet Packages for Solution
             *  Search for "iTextSharp" install version 5.5.13.3, newer versions (iText 7) will not work with
             *  VisualStudio 2015
             * 
             * Adapted from:
             * https://www.aspsnippets.com/Articles/Export-ASPNet-Web-Page-with-images-to-PDF-using-ITextsharp.aspx
             * 
             * 1. This code will output the aspx page as a PDF
             * 2. The stuff commented out below is my attempt to read in a PDF from my project and add it to our file stream (PDF writing part)
             */
            
            //Setup our response that lets us download the file
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", "attachment;filename=UnitOutline_" + Request.QueryString["ID"]  + ".pdf");
            Response.Cache.SetCacheability(HttpCacheability.NoCache);

            // This sets up our readers / writers to convert our webpage to a PDF
            StringWriter sw = new StringWriter();
            HtmlTextWriter hw = new HtmlTextWriter(sw);
            this.Page.RenderControl(hw);
            StringReader sr = new StringReader(sw.ToString());

            // This sets up our PDF doc
            Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 10f, 0f);
            HTMLWorker htmlparser = new HTMLWorker(pdfDoc);
            PdfWriter.GetInstance(pdfDoc, Response.OutputStream);

            // Server.MapPath() maps our computer location to where it would be on a server.
            // My Test.pdf is is in my "Files" folder in my solution

            // This is grabbing our "other pdf" you may want to add dynamic pdf uploads if you have time
            PdfReader pdfReader = new PdfReader(Server.MapPath("~/Files/Test.pdf"));

            // This is setting up a writer 
            PdfWriter writer = PdfWriter.GetInstance(pdfDoc, new FileStream(Server.MapPath("~/Files/OutPut.pdf"), FileMode.Create));

            //Opens our document and writes our html to our doc
            pdfDoc.Open();
            htmlparser.Parse(sr);
            
            // Do a thing for every page of our pdf
            for (int i = 1; i <= pdfReader.NumberOfPages; i++)
            {
                // Imports the page and adds to our doc
                PdfImportedPage page = writer.GetImportedPage(pdfReader, i);
                pdfDoc.Add(iTextSharp.text.Image.GetInstance(page));

                // Checks whether we need to rotate or not
                if (pdfReader.GetPageSize(i).Width > pdfReader.GetPageSize(i).Height)
                {
                    pdfDoc.SetPageSize(iTextSharp.text.PageSize.A4.Rotate());
                }
                else
                {
                    pdfDoc.SetPageSize(iTextSharp.text.PageSize.A4);
                }
            }

            // Close everything and save the file
            pdfDoc.Close();
            pdfReader.Close();
            Response.TransmitFile(Server.MapPath("~/Files/") + "OutPut.pdf");
            Response.End();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\dbTest.mdf;Integrated Security=True";

            SqlConnection con = new SqlConnection(connectionString);

            SqlCommand cmd = new SqlCommand("dbo.unitOut", con);
            cmd.CommandType = CommandType.StoredProcedure;

            // We are going to get the ID of the unit outline we want from a querystring
            // See: https://www.codeguru.com/dotnet/passing-data-between-pages-in-asp-net/
            cmd.Parameters.AddWithValue("ID", Request.QueryString["ID"]);

            con.Open();

            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                HtmlTableRow row = new HtmlTableRow();
                HtmlTableCell cell = new HtmlTableCell();

                // this array helps us make everything fairly optimised...
                string[] headers = { "Course", "Unit", "Unit Description", "Accreditation", "Unit Goals", "Content Descriptors" };

                for (int i = 0; i < 6; i++)
                {

                    // This is where we have normal breaks in the paragraph section
                    if (i == 2)
                    {
                        string[] points = Convert.ToString(dr[headers[i]]).Split('`');
                        cell.InnerHtml = string.Format("<b>{0}</b>", headers[i]);
                        foreach (string p in points)
                        {
                            cell.InnerHtml += string.Format("<br /><br />{0}", p);
                        }
                    }
                    else if (i == 4 || i == 5)
                    {
                        //Splitting on the ~ for each bullet point
                        //We set up a <ul> which is a bullet point list
                        string[] points = Convert.ToString(dr[headers[i]]).Split('~');
                        cell.InnerHtml = string.Format("<b>{0}</b><ul>", headers[i]);

                        foreach (string p in points)
                        {
                            //If it contains * we have a subheading
                            if (p.Contains("*"))
                            {
                                string[] subheading = p.Split('*');
                                cell.InnerHtml += string.Format("</ul><br /><i>{0}</i><ul>", subheading[1]);
                                cell.InnerHtml += string.Format("<li>{0}</li>", subheading[2]);
                            }
                            else
                            {
                                //li is list item
                                cell.InnerHtml += string.Format("<li>{0}</li>", p);
                           }

                        }

                        //Close the list
                        cell.InnerHtml += "</ul>";
                    }
                    else
                    {
                        // We are bolding our headings with the <b> tag, adding a break, then adding the content under
                        //We are getting the heading info from the array and the data from our datareader
                        cell.InnerHtml = string.Format("<b>{0}</b><br />{1}", headers[i], dr[headers[i]]);
                    }
                    
                    
                    row.Cells.Add(cell);
                    tableContent.Rows.Add(row);

                    cell = new HtmlTableCell();
                    row = new HtmlTableRow();
                }
            }

            con.Close();
        }
    }
}
