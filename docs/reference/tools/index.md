# Tools

3 tools in 1 family.
The names, descriptions and schemas are generated from the server itself, so they are exactly what the model receives in `tools/list`.

Reads the documents of a working copy as Markdown: Word, Excel, PowerPoint, the text layer of a PDF, saved web pages, CSV, JSON, Markdown and plain text. Three questions kept apart: what does this document hold (`documents_outline`), what does it say (`documents_read`, a piece at a time), and what does this table hold (`documents_table`, row by row). It only reads, and only inside the folder a session names.

| Tool | Family | What it does |
| --- | --- | --- |
| [`documents_read`](documents.md#documents_read) | Documents: outline, read, table | Read a document as Markdown: Word, Excel, PowerPoint, PDF, HTML, CSV, JSON, Markdown or plain text. |
| [`documents_outline`](documents.md#documents_outline) | Documents: outline, read, table | Say what a document holds without reading it: its format and length, its pages, sheets or slides with their titles, its headings, its tables with their size,… |
| [`documents_table`](documents.md#documents_table) | Documents: outline, read, table | Read one table of a document as rows of cells: a sheet of a workbook, a table of a Word or HTML page, of a slide or of a Markdown file, or the whole of a CSV. |
