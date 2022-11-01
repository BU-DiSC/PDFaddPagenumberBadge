// See https://aka.ms/new-console-template for more information
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
//using static System.Net.Mime.MediaTypeNames;
//using System.Drawing;
//using System.Reflection.PortableExecutable;
//using System.Runtime.InteropServices;


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

    }


    class Program
    {
        public static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
               .WithParsed(opts => RunProgram(opts))
               .WithNotParsed((errs) => HandleParseError(errs));
        }

        private static void RunProgram(Options opts)
        {
            //handle options
            ManipulatePdf(opts.InputPDF.ToString(), opts.OuputPDF.ToString(), (opts.BadgePath == null) ? null : opts.BadgePath.ToString(), opts.FontPath.ToString(), opts.PageNumber);
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
            Console.WriteLine("Error in options!");
        }

        private static void ManipulatePdf(String source_path, String dest_path, String badge_path, String font_path, int starting_page_number)
        {
            bool addBadge = false;
            if (source_path == null || dest_path == null)
            {
                Console.WriteLine("Source and destination PDF cannot be null!");
                System.Environment.Exit(-1);
            }
            if (starting_page_number < 1)
            {
                Console.WriteLine("Starting page number should be >= 1!");
                System.Environment.Exit(-1);
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

            PdfADocument pdfDoc = new PdfADocument(new PdfReader(source_path), new PdfWriter(dest_path));
            Document doc = new Document(pdfDoc);
            PdfFont font = PdfFontFactory.CreateFont(font_path, PdfEncodings.WINANSI, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);

            int numberOfPages = pdfDoc.GetNumberOfPages();

            if (addBadge)
            {
                PdfPage firstPage;
                ImageData img = ImageDataFactory.Create(badge_path);
                firstPage = pdfDoc.GetFirstPage();
                float x = (float)(firstPage.GetPageSize().GetWidth()) * (float)0.756;
                float y = (float)(firstPage.GetPageSize().GetHeight()) * (float)0.901;
                float w = 72;
                float h = 72;
                AffineTransform affineTransform = AffineTransform.GetTranslateInstance(x, y);
                affineTransform.Concatenate(AffineTransform.GetScaleInstance(w, h));
                float[] matrix = new float[6];
                affineTransform.GetMatrix(matrix);

                new PdfCanvas(firstPage.NewContentStreamAfter(), pdfDoc.GetFirstPage().GetResources(), pdfDoc)
                        .AddImageWithTransformationMatrix(img, matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5], false);
                Rectangle linkLocation = new Rectangle(x, y, w, h);
                PdfAnnotation annotation = new PdfLinkAnnotation(linkLocation)
                    .SetHighlightMode(PdfAnnotation.HIGHLIGHT_OUTLINE)
                    .SetAction(PdfAction.CreateURI("https://www.acm.org/publications/policies/artifact-review-and-badging-current"))
                    .SetBorder(new PdfArray(new float[] { 0, 0, 0 }));
                annotation.SetFlag(PdfAnnotation.PRINT);
                firstPage.AddAnnotation(annotation);
            }

            for (int i = 0; i < numberOfPages; i++)
            {
                // Write aligned text to the specified by parameters point
                float pos = doc.GetPdfDocument().GetPage(i + 1).GetPageSize().GetWidth() / 2;
                Paragraph p = new Paragraph();
                p.SetFont(font);
                p.SetFontSize(10);
                p.Add(new Text((i + starting_page_number).ToString()));
                doc.ShowTextAligned(p, pos, 40, i + 1, TextAlignment.CENTER, VerticalAlignment.TOP, 0);
            }

            doc.Close();
        }
    }
}




