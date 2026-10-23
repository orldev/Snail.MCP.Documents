# Documents: outline, read, table

## <code>documents_<wbr>read</code>

Read a document as Markdown: Word, Excel, PowerPoint, PDF, HTML, CSV, JSON, Markdown or plain text. Headings,
lists and tables keep their shape, and each page, sheet and slide begins with a marked heading; a sheet's
table carries the letters of its columns and the numbers of its rows, so a value is named by its cell. A long
text comes in pieces: next is the position to pass as from for the following piece, and null when the
document has ended. To start at a page, sheet or slide, pass its number as part instead of from. A PDF
page with no text layer is listed under scans: its words are not in the text.

*read-only · repeat-safe · stays on this machine.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `path` | `string` | yes | — | The file, relative to the working copy; attachments are under .swarm/attachments. |
| `from` | `integer` | — | `0` | Position in the text to start at: 0 by default, or the next of the previous piece. |
| `part` | `integer?` | — | `null` | Number of the page, sheet or slide to start at, as documents_outline lists them; when given, from is ignored. |
| `maxCharacters` | `integer` | — | `20000` | Characters of text to return, 500 to 100000; 20000 by default. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "path": {
      "description": "The file, relative to the working copy; attachments are under .swarm/attachments.",
      "type": "string"
    },
    "from": {
      "description": "Position in the text to start at: 0 by default, or the next of the previous piece.",
      "type": "integer",
      "default": 0
    },
    "part": {
      "description": "Number of the page, sheet or slide to start at, as documents_outline lists them; when given, from is ignored.",
      "type": [
        "integer",
        "null"
      ],
      "default": null
    },
    "maxCharacters": {
      "description": "Characters of text to return, 500 to 100000; 20000 by default.",
      "type": "integer",
      "default": 20000
    }
  },
  "required": [
    "path"
  ]
}
```

</details>

## <code>documents_<wbr>outline</code>

Say what a document holds without reading it: its format and length, its pages, sheets or slides with
their titles, its headings, its tables with their size, and which PDF pages are scans. Each part and
heading carries the position to pass to documents_read as from, and each table the number to pass to
documents_table. Each list stops at 300 entries and says how many it left out.

*read-only · repeat-safe · stays on this machine.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `path` | `string` | yes | — | The file, relative to the working copy. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "path": {
      "description": "The file, relative to the working copy.",
      "type": "string"
    }
  },
  "required": [
    "path"
  ]
}
```

</details>

## <code>documents_<wbr>table</code>

Read one table of a document as rows of cells: a sheet of a workbook, a table of a Word or HTML page,
of a slide or of a Markdown file, or the whole of a CSV. The first row is the header as the document
has it. A long table comes in pages of rows: next is the row to pass as fromRow, and null when the
table has ended.

*read-only · repeat-safe · stays on this machine.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `path` | `string` | yes | — | The file, relative to the working copy. |
| `table` | `integer` | — | `1` | Number of the table as documents_outline lists it; 1 by default. |
| `fromRow` | `integer` | — | `0` | Row to start at, counting the header as 0: 0 by default, or the next of the previous page. |
| `maxRows` | `integer` | — | `100` | Rows to return, 1 to 1000; 100 by default. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "path": {
      "description": "The file, relative to the working copy.",
      "type": "string"
    },
    "table": {
      "description": "Number of the table as documents_outline lists it; 1 by default.",
      "type": "integer",
      "default": 1
    },
    "fromRow": {
      "description": "Row to start at, counting the header as 0: 0 by default, or the next of the previous page.",
      "type": "integer",
      "default": 0
    },
    "maxRows": {
      "description": "Rows to return, 1 to 1000; 100 by default.",
      "type": "integer",
      "default": 100
    }
  },
  "required": [
    "path"
  ]
}
```

</details>

