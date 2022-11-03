# PDFtools

A command line .NET application using [iText7](https://github.com/itext/itext7-dotnet) that:
(i) can add page numbers and a badge to a PDF, and (ii) can merge two PDFs. Both can happen while maintaining PDFa compliance.

Packages used: iText7 and CommandLineParser

Install packages using NuGet for [iText 7](https://www.nuget.org/packages/itext7) and [CommandLineParser](https://www.nuget.org/packages/CommandLineParser):
```
Install-Package itext7
Install-Package CommandLineParser
```

Usage:
```
$bin/PDFtools --help
PDFtools 1.0.0
Copyright (C) 2022 PDFtools
  overlay    Add pagenumbers and a badge.
  merge      Merge two documents.
  help       Display more information on a specific command.
  version    Display version information.
```

Overlay:
```
bin/PDFtools overlay --help
PDFtools 1.0.0
Copyright (C) 2022 PDFtools
  -s, --source    Required. Input PDF file to be processed.
  -d, --dest      Required. Output PDF file.
  -p, --page      Required. Starting page number.
  -b, --badge     Path to badge to insert (has to be an image with no transparency).
  -f, --font      Required. Path to font for page numbers.
  --help          Display this help screen.
  --version       Display version information.
```
  
  Merge:
  ```
  $ bin/PDFtools merge --help
PDFtools 1.0.0
Copyright (C) 2022 PDFtools
  -f, --first             Required. The first input PDF file to be merged.
  -s, --second            Required. The second input PDF file to be merged.
  -d, --dest              Required. Output PDF file.
  -N, --force-not-PdfA    Assume files are not PDFA (only use if it complains at closing time).
  --help                  Display this help screen.
  --version               Display version information.
```
