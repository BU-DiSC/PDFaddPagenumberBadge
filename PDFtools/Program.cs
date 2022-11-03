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
using iText.Pdfa.Checker;
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
using iText.Kernel.Utils;

namespace pdfproject
{
    [Verb("overlay", HelpText = "Add pagenumbers and a badge.")]
    public sealed class OverlayOptions
    {
        [Option('s', "source", Required = true, HelpText = "Input PDF file to be processed.")]
        public string InputPDF { get; set; } = default!;

        [Option('d', "dest", Required = true, HelpText = "Output PDF file.")]
        public string OuputPDF { get; set; } = default!;

        [Option('p', "page", Required = true, HelpText = "Starting page number.")]
        public int StartingPageNumber { get; set; }

        [Option('b', "badge", Required = false, HelpText = "Path to badge to insert (has to be an image with no transparency).")]
        public string BadgePath { get; set; } = default!;

        [Option('f', "font", Required = true, HelpText = "Path to font for page numbers.")]
        public string FontPath { get; set; } = default!;

        //Future options to consider:
        //pagenumber fontsize
        //badge size and location
        //URL of the badge
    }

    [Verb("merge", HelpText = "Merge two documents.")]
    public sealed class MergeOptions
    {
        [Option('f', "first", Required = true, HelpText = "The first input PDF file to be merged.")]
        public string InputPDF1 { get; set; } = default!;

        [Option('s', "second", Required = true, HelpText = "The second input PDF file to be merged.")]
        public string InputPDF2 { get; set; } = default!;

        [Option('d', "dest", Required = true, HelpText = "Output PDF file.")]
        public string OuputPDF { get; set; } = default!;

        [Option('N', "force-not-PdfA", HelpText = "Assume files are not PDFA (only use if it complains at closing time).")]
        public bool notPDFa { get; set; } = false;
    }

    public class BadgeOptions
    {
        public bool isValid;
        public String badgePath;
        public String badgeURI;
        public float x_ratio;
        public float y_ratio;
        public float width;
        public float height;

        public BadgeOptions(bool isValid, string badgePath, string badgeURI, float x_ratio, float y_ratio, float width, float height)
        {
            this.isValid = isValid;
            this.badgePath = badgePath;
            this.badgeURI = badgeURI;
            this.x_ratio = x_ratio;
            this.y_ratio = y_ratio;
            this.width = width;
            this.height = height;
        }
    }

    public class PageNumberOptions
    {
        public bool isValid;
        public String fontPath;
        public int fontSize;
        public int starting_page;
        public int y_pos;

        public PageNumberOptions(bool isValid, string fontPath, int fontSize, int starting_page, int y_pos)
        {
            this.isValid = isValid;
            this.fontPath = fontPath;
            this.fontSize = fontSize;
            this.starting_page = starting_page;
            this.y_pos = y_pos;
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<OverlayOptions, MergeOptions>(args)
               .WithParsed<OverlayOptions>(opts => RunOverlay(opts))
               .WithParsed<MergeOptions>(opts => RunMerge(opts))
               .WithNotParsed((errs) => HandleParseError(errs));
        }

        public static void RunMerge(MergeOptions opts)
        {
            try
            {
                MergeTwoPdfAFiles(opts.InputPDF1.ToString(), opts.InputPDF2.ToString(), opts.OuputPDF.ToString(), opts.notPDFa);
            }
            catch (Exception e)
            {
                //Console.WriteLine("\t>>> Exception: " + e.ToString() + "\n");
                Console.WriteLine("\t>>> Error message: " + e.Message + "\n");
            }
        }

        public static void MergeTwoPdfAFiles(String file1, String file2, String file_out, bool notPDFa=false)
        {
            if (notPDFa)
            {
                PdfDocument pdfDocument = new PdfDocument(new PdfReader(file1), new PdfWriter(file_out));
                PdfDocument pdfDocument2 = new PdfDocument(new PdfReader(file2));

                PdfMerger merger = new PdfMerger(pdfDocument);
                merger.Merge(pdfDocument2, 1, pdfDocument2.GetNumberOfPages());

                pdfDocument2.Close();
                pdfDocument.Close();
            }
            else
            { 
                PdfADocument pdfDocument = new PdfADocument(new PdfReader(file1), new PdfWriter(file_out));
                PdfDocument pdfDocument2 = new PdfDocument(new PdfReader(file2));

                PdfMerger merger = new PdfMerger(pdfDocument);
                merger.Merge(pdfDocument2, 1, pdfDocument2.GetNumberOfPages());

                pdfDocument2.Close();
                pdfDocument.Close();
            }
        }


        public static void TestMain(string[] args)
        {
            String input= "in.pdf";
            String output = "out.pdf";
            String badge = "badge.jpg";
            String font = "font.ttf";
            PageNumberOptions page_number_info = new PageNumberOptions(true, font, 10, 188, 40);
            BadgeOptions badge_info = new BadgeOptions(true, badge, "https://www.mit.edu", 0.756F, 0.901F, 72, 72);

            try
            {
                AddPageNumberBadgeToPdf(input, output, badge_info, page_number_info);
            }
            catch (Exception e1)
            {
                Console.WriteLine("\t>>> Exception: " + e1.ToString() + "\n");
                Console.WriteLine("\t>>> Error message: " + e1.Message + "\n");
            }
        }

        private static void RunOverlay(OverlayOptions opts)
        {
            //handle options
            bool valid = (opts.StartingPageNumber >= 1);
            PageNumberOptions page_number_info = new PageNumberOptions(valid,opts.FontPath.ToString(),10,opts.StartingPageNumber,40);
            valid = (opts.BadgePath != null);
            BadgeOptions badge_info = new BadgeOptions(valid, (opts.BadgePath == null) ? "" : opts.BadgePath.ToString(), "https://www.acm.org/publications/policies/artifact-review-and-badging-current", 0.756F, 0.901F, 72, 72);

            try
            {
                AddPageNumberBadgeToPdf(opts.InputPDF.ToString(), opts.OuputPDF.ToString(), badge_info, page_number_info);
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

        private static void AddPageNumberBadgeToPdf(String source_path, String dest_path, BadgeOptions badge_info, PageNumberOptions page_number_info, bool isPDFA=true)
        {
            bool addBadge = false;
            bool addPagenumber = true;
            //bool notPDFA = false;
            if (source_path == null || dest_path == null)
            {
                Console.WriteLine("Source and destination PDF cannot be null!");
                System.Environment.Exit(-1);
            }
            if (page_number_info.isValid && page_number_info.starting_page < 1)
            {
                Console.WriteLine("Since start page number is < 1, no pagenumber are added!");
                addPagenumber = false;
            }
            if (badge_info.isValid)
            {
                addBadge = true;
                if (!File.Exists(badge_info.badgePath))
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
                    Console.WriteLine("  >> input file: "+source_path + " is not PDF/A, reverting to simple PDF and restarting.");
                    //Console.WriteLine("\t>>> Error message: " + e.Message + "\n");
                    if (!isPDFA)
                        Console.WriteLine("Giving Up!\n");
                    else
                    {
                        try
                        {
                            AddPageNumberBadgeToPdf(source_path, dest_path, badge_info, page_number_info, false);
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

            PdfFont font = PdfFontFactory.CreateFont(page_number_info.fontPath, PdfEncodings.WINANSI, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);

            int numberOfPages = doc.GetPdfDocument().GetNumberOfPages();

            if (addBadge)
            {
                PdfPage firstPage;
                ImageData img = ImageDataFactory.Create(badge_info.badgePath);
                firstPage = doc.GetPdfDocument().GetFirstPage();
                float x = (float)(firstPage.GetPageSize().GetWidth()) * badge_info.x_ratio;
                float y = (float)(firstPage.GetPageSize().GetHeight()) * badge_info.y_ratio;
                AffineTransform affineTransform = AffineTransform.GetTranslateInstance(x, y);
                affineTransform.Concatenate(AffineTransform.GetScaleInstance(badge_info.width, badge_info.height));
                float[] matrix = new float[6];
                affineTransform.GetMatrix(matrix);

                if (isPDFA)
                    new PdfCanvas(firstPage.NewContentStreamAfter(), doc.GetPdfDocument().GetFirstPage().GetResources(), pdfADoc)
                            .AddImageWithTransformationMatrix(img, matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5], false);
                else
                    new PdfCanvas(firstPage.NewContentStreamAfter(), doc.GetPdfDocument().GetFirstPage().GetResources(), pdfDoc)
                        .AddImageWithTransformationMatrix(img, matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5], false);


                Rectangle linkLocation = new Rectangle(x, y, badge_info.width, badge_info.height);
                PdfAnnotation annotation = new PdfLinkAnnotation(linkLocation)
                    .SetHighlightMode(PdfAnnotation.HIGHLIGHT_OUTLINE)
                    .SetAction(PdfAction.CreateURI(badge_info.badgeURI))
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
                    float page_number_y_pos = page_number_info.y_pos;
                    Paragraph p = new Paragraph();
                    p.SetFont(font);
                    p.SetFontSize(page_number_info.fontSize);
                    p.Add(new Text((i + page_number_info.starting_page).ToString()));
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
                    Console.WriteLine("  >>> when closing " + dest_path + " is not PDF/A, reverting to simple PDF and restarting.");
                    //Console.WriteLine("\t>>> Error message: " + e.Message + "\n");
                    if (!isPDFA)
                        Console.WriteLine("Giving Up!\n");
                    else
                    {
                        try
                        {
                            AddPageNumberBadgeToPdf(source_path, dest_path, badge_info, page_number_info, false);
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




