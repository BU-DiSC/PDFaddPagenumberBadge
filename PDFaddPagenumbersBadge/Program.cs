//Author: Manos Athanassoulis
//License: see LICENSE file

using System;
using iText.IO.Font;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.IO.Image;
using iText.Layout;
using iText.Pdfa;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using CommandLine;
using CommandLine.Text;
using iText.Pdfa.Exceptions;
using Org.BouncyCastle.Crypto;
using System.Diagnostics;

namespace pdfproject
{
    public sealed class Options
    {
        [Option('s', "source", Required = true, HelpText = "Input PDF file to be processed.")]
        public string InputPDF { get; set; } = default!;

        [Option('d', "dest", Required = true, HelpText = "Output PDF file to be processed.")]
        public string OuputPDF { get; set; } = default!;

        [Option('p', "page", Required = true, HelpText = "Starting page number.")]
        public int PageNumber { get; set; }

        [Option('b', "badge", Required = false, HelpText = "Path to badge to insert.")]
        public string BadgePath { get; set; } = default!;

        [Option('f', "font", Required = true, HelpText = "Path to font for page numbers.")]
        public string FontPath { get; set; } = default!;

        //Future options to consider:
        //pagenumber fontsize
        //badge size and location
        //URL of the badge
    }


    class Program
    {
        public static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
               .WithParsed(opts => RunProgram(opts))
               .WithNotParsed((errs) => HandleParseError(errs));
        }

        public static void TestMain(string[] args)
        {
            String input= "in.pdf";
            String output = "out.pdf";
            String badge = "badge.jpg";
            String font = "font.ttf";
            try
            {
                ManipulatePdf(input, output, badge, "https://mit.edu", font, 188);
            }
            catch (Exception e1)
            {
                Console.WriteLine("\t>>> Exception: " + e1.ToString() + "\n");
                Console.WriteLine("\t>>> Error message: " + e1.Message + "\n");
            }
        }

        private static void RunProgram(Options opts)
        {
            //handle options
            String badgeURI = "https://www.acm.org/publications/policies/artifact-review-and-badging-current";
            try
            {
                ManipulatePdf(opts.InputPDF.ToString(), opts.OuputPDF.ToString(), (opts.BadgePath == null) ? null : opts.BadgePath.ToString(), badgeURI, opts.FontPath.ToString(), opts.PageNumber);
            }
            catch (Exception e1)
            {
                Console.WriteLine("\t>>> Exception: " + e1.ToString() + "\n");
                Console.WriteLine("\t>>> Error message: " + e1.Message + "\n");
            }

        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
            Console.WriteLine("Please read the options carefully!");
        }

        private static void ManipulatePdf(String source_path, String dest_path, String? badge_path, String badge_URI, String font_path, int starting_page_number, bool isPDFA=true)
        {
            bool addBadge = false;
            bool addPagenumber = true;
            //bool notPDFA = false;
            if (source_path == null || dest_path == null)
            {
                Console.WriteLine("Source and destination PDF cannot be null!");
                System.Environment.Exit(-1);
            }
            if (starting_page_number < 1)
            {
                Console.WriteLine("Since start page number is < 1, no pagenumber are added!");
                addPagenumber = false;
            }
            if (badge_path != null)
            {
                addBadge = true;
                if (!File.Exists(badge_path))
                {
                    Console.WriteLine("Badge image does not exist!");
                    System.Environment.Exit(-1);
                }
            }
            if (!File.Exists(source_path))
            {
                Console.WriteLine("Source PDF does not exist!");
                System.Environment.Exit(-1);
            }

            if (!addBadge)
                Console.WriteLine("\tNo badge for " + source_path + " to " + dest_path + "!");
            if (!addPagenumber)
                Console.WriteLine("\tNo page numbers for " + source_path + " to " + dest_path + "!");

            PdfADocument pdfADoc = null; 
            PdfDocument pdfDoc = null;
            try
            {
                if (isPDFA)
                    pdfADoc = new PdfADocument(new PdfReader(source_path), new PdfWriter(dest_path));
                else
                    pdfDoc = new PdfDocument(new PdfReader(source_path), new PdfWriter(dest_path));
            }
            catch (Exception e)
            {
                if (e is PdfAConformanceException)
                {
                    //notPDFA = true;
                    Console.WriteLine("\t>> input file: "+source_path + " does not comform with PDF/A, reverting to simple PDF and starting over.");
                    //Console.WriteLine("\t>>> Error message: " + e.Message + "\n");
                    if (!isPDFA)
                        Console.WriteLine("Giving Up!\n");
                    else
                    {
                        try
                        {
                            ManipulatePdf(source_path, dest_path, badge_path, badge_URI, font_path, starting_page_number, false);
                            return;
                        }
                        catch (Exception e1)
                        {
                            Console.WriteLine("\t>>> Exception: " + e1.ToString() + "\n");
                            Console.WriteLine("\t>>> Error message: " + e1.Message + "\n");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Something is wrong with: "+source_path);
                    Console.WriteLine("\t>>> Error message: " + e.Message + "\n");
                    Console.WriteLine("Giving Up!\n");
                    System.Environment.Exit(-1);
                }
                //pdfDoc = new PdfDocument(new PdfReader(source_path), new PdfWriter(dest_path));
            }



            //Debug.Assert(isPDFA && pdfADoc != null);
            //Debug.Assert(!isPDFA && pdfDoc != null);
            Document doc;
            if (isPDFA == true)
                doc = new Document(pdfADoc);
            else
                doc = new Document(pdfDoc);

            PdfFont font = PdfFontFactory.CreateFont(font_path, PdfEncodings.WINANSI, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);

            int numberOfPages = doc.GetPdfDocument().GetNumberOfPages();

            if (addBadge)
            {
                PdfPage firstPage;
                ImageData img = ImageDataFactory.Create(badge_path);
                firstPage = doc.GetPdfDocument().GetFirstPage();
                float x = (float)(firstPage.GetPageSize().GetWidth()) * (float)0.756;
                float y = (float)(firstPage.GetPageSize().GetHeight()) * (float)0.901;
                float w = 72;
                float h = 72;
                AffineTransform affineTransform = AffineTransform.GetTranslateInstance(x, y);
                affineTransform.Concatenate(AffineTransform.GetScaleInstance(w, h));
                float[] matrix = new float[6];
                affineTransform.GetMatrix(matrix);

                if (isPDFA)
                    new PdfCanvas(firstPage.NewContentStreamAfter(), doc.GetPdfDocument().GetFirstPage().GetResources(), pdfADoc)
                            .AddImageWithTransformationMatrix(img, matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5], false);
                else
                    new PdfCanvas(firstPage.NewContentStreamAfter(), doc.GetPdfDocument().GetFirstPage().GetResources(), pdfDoc)
                        .AddImageWithTransformationMatrix(img, matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5], false);


                Rectangle linkLocation = new Rectangle(x, y, w, h);
                PdfAnnotation annotation = new PdfLinkAnnotation(linkLocation)
                    .SetHighlightMode(PdfAnnotation.HIGHLIGHT_OUTLINE)
                    .SetAction(PdfAction.CreateURI(badge_URI))
                    .SetBorder(new PdfArray(new float[] { 0, 0, 0 }));
                annotation.SetFlag(PdfAnnotation.PRINT);
                firstPage.AddAnnotation(annotation);
            }

            if (addPagenumber)
            {
                for (int i = 0; i < numberOfPages; i++)
                {
                    // Write aligned text to the specified by parameters point
                    float page_number_x_pos = doc.GetPdfDocument().GetPage(i + 1).GetPageSize().GetWidth() / 2;
                    float page_number_y_pos = 40;
                    Paragraph p = new Paragraph();
                    p.SetFont(font);
                    p.SetFontSize(10);
                    p.Add(new Text((i + starting_page_number).ToString()));
                    doc.ShowTextAligned(p, page_number_x_pos, page_number_y_pos, i + 1, TextAlignment.CENTER, VerticalAlignment.TOP, 0);
                }
            }

            bool closing_exception = false;
            try
            {
                doc.Close();
            }
            catch (Exception e)
            {
                closing_exception = true;
                if(e is PdfAConformanceException)
                {
                    //notPDFA = true;
                    Console.WriteLine("\t>>> when closing " + dest_path + " does not comform with PDF/A, reverting to simple PDF and starting over.");
                    //Console.WriteLine("\t>>> Error message: " + e.Message + "\n");
                    if (!isPDFA)
                        Console.WriteLine("Giving Up!\n");
                    else
                    {
                        try
                        {
                            ManipulatePdf(source_path, dest_path, badge_path, badge_URI, font_path, starting_page_number, false);
                            return;
                        }
                        catch (Exception e1)
                        {
                            Console.WriteLine("\t>>> Exception: " + e1.ToString() + "\n");
                            Console.WriteLine("\t>>> Error message: " + e1.Message + "\n");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("When closing something is wrong with: " + source_path);
                    Console.WriteLine("\t>>> Error message: " + e.Message + "\n");
                    Console.WriteLine("Giving Up!\n");
                    System.Environment.Exit(-1);
                }
                //pdfDoc = new PdfDocument(new PdfReader(source_path), new PdfWriter(dest_path));
            }
            finally
            {
                if (!closing_exception)
                    Console.WriteLine("... " + dest_path + " was written with SUCCESS!");
            }
        }
    }
}




