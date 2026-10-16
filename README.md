# Snail.MCP.Documents

An MCP server that lets an agent read the documents of its working copy as Markdown: Word, Excel, PowerPoint,
the text layer of a PDF, saved web pages, CSV, JSON, Markdown and plain text.

It answers three questions and keeps them apart: *what does this document hold* — its pages, sheets, slides,
headings and tables, for almost nothing — *what does it say* — its text in pieces, every page, sheet and slide
marked — and *what does this table hold* — its rows as cells, for numbers that will be counted or compared. It
only reads, and only inside the folder a session names.

## Tools

| Tool | Question it answers |
| --- | --- |
| `documents_outline` | What does this document hold? Format, length, parts with titles, headings and tables, each with where it starts. |
| `documents_read` | What does it say? The text as Markdown, a piece at a time, from a position or from the start of a page, sheet or slide. |
| `documents_table` | What does this table hold? Rows of cells, a page of rows at a time, the header with every page. |

Every answer is JSON with an `ok` flag. A failure carries `error` and a `hint` saying what to do instead; in
the protocol it is flagged `isError`. A failure nobody foresaw names itself too — the tool, the kind of failure and
its message — rather than the protocol's bare «an error occurred».

Things about the answers that are deliberate:

- **A long document comes in pieces.** `next` is the position to pass as `from`; a document read in the last
  ten minutes and unchanged on disk is not parsed again.
- **A scan is said to be one.** A PDF page with pictures and no text layer is marked in the text and listed
  under `scans`: its words are not here, and reading them takes a model that sees pictures.
- **Dates and numbers read as the document shows them.** A spreadsheet date is a number in the file and is
  known by its cell's format; numbers are written to 15 significant digits, as Excel shows them.
- **Headings are found by the style's name**, not its id, so a document from a Russian or German Word has its
  headings too. A CSV in Windows-1251 with semicolons, as a Russian Excel saves one, reads as text, not noise.
- **What a file becomes is bounded, not only what it weighs.** A file that unpacks to more than 20 000 000
  characters of text, or to a table of more than 1 000 000 cells counting the padding of short rows, is refused as
  too large once read; reading one file stops after 30 seconds. A sparse workbook of 11 kB used to unpack to
  98 million characters.
- **The outline stays short.** Parts, headings and tables are each listed up to 300, with the count of the rest.
- **Only regular files are read, and never through a link that appeared after the path was checked.** A named pipe,
  a device or a path swapped for a link between the check and the open is refused with its reason.

| Format | Extensions | Parts | Tables |
| --- | --- | --- | --- |
| Word | `.docx`, `.docm` | — | the document's tables |
| Excel | `.xlsx`, `.xlsm` | sheets | one per sheet |
| PowerPoint | `.pptx`, `.pptm` | slides, titled; notes read as `Notes:` | the slides' tables |
| PDF | `.pdf` | pages; scans marked | — |
| HTML | `.html`, `.htm` | — | the page's tables |
| CSV | `.csv`, `.tsv` | — | the whole file |
| JSON | `.json` | — | a list of flat records; any other shape is kept as JSON |
| Markdown | `.md`, `.markdown` | — | pipe tables |
| Text | `.txt`, `.text`, `.log` | — | — |

## Run it

### In Docker, one per machine

```sh
docker build -t snail/s3l-documents-mcp .
docker run -p 127.0.0.1:8823:8823 -v workspaces:/workspaces:ro snail/s3l-documents-mcp
```

A session opens at `http://host:8823/mcp?root=/workspaces/<copy>` and reads that copy and nothing else. A
session naming a copy this container does not have is turned away with 400 before it starts: a worker sent to
the server of another machine hears about it at once instead of reading the wrong folder. So is a session naming
a folder outside `/workspaces`, the volume the image is configured with, and a request a browser makes (one that
carries `Origin`) is refused with 403. The server has no token: the network the container stands in is its access
control, so publish the port on loopback as above, not on every interface. `GET /healthz` answers 200 while the
server runs. `deploy/compose.yml` runs it alone over `./workspaces`.

### As a program

```sh
SNAIL_MCP_DOCUMENTS_ROOTS__0=/path/to/folder dotnet run --project src
```

Without a port it speaks stdio to the process that started it and reads inside the configured roots.

## Configuration

The settings come from the settings file and from the environment variables under `SNAIL_MCP_DOCUMENTS_`, and a
variable wins over the file; nothing else is read. The file is `$SNAIL_MCP_DOCUMENTS_CONFIG` when it is set, the only
one read then, or else the first of these that exists: `./.s3l-mcp-documents.json`, `./s3l-mcp-documents.json`,
`~/.config/s3l-mcp-documents/config.json`, `~/.s3l-mcp-documents.json`. A machine's settings belong in `~/.config/s3l-mcp-documents/config.json`
(`chmod 600`), never in a launchd job or in a client's registration, which travel wherever they are copied. Its fields
are the variables' names after the prefix, a double underscore nesting:

```json
{ "Roots": ["/path/to/folder"], "MemoryMinutes": 30 }
```

| Variable | Default | What it sets |
| --- | --- | --- |
| `SNAIL_MCP_DOCUMENTS_PORT` | `0` | Port to answer sessions on; unset — stdio. |
| `SNAIL_MCP_DOCUMENTS_ROOTS__0`, `__1`, … | — | Folders a session that names none may read; when set, a root a session names must lie inside one of them. |
| `SNAIL_MCP_DOCUMENTS_MAX_FILE_BYTES` | `52428800` | Bytes past which a file is not opened. |
| `SNAIL_MCP_DOCUMENTS_MEMORY_MINUTES` | `10` | Minutes a read document is remembered; `0` forgets at once. |
| `SNAIL_MCP_DOCUMENTS_MEMORY_DOCUMENTS` | `8` | Documents remembered at once. |

A setting out of its range stops the server at start-up rather than being repaired in silence.

## Snail.Swarm.Agent

`deploy/swarm/catalog-entry.json` is the platform's catalog entry and `deploy/swarm/compose.yml` the block for
its `deploy/compose.mcp.yml`: the server stands beside `mcp-filesystem` on the same volume of working copies,
mounted read-only. The platform puts the task's copy into the address (`{workspace}`), and the role's tools
into it as `&tool=…`; a tool the session was not granted is refused where calls arrive. Attachments of a task
are under `.swarm/attachments`.

## Developing

```sh
dotnet test
```

The tests build real files of every format, read them through the readers, the tools and the server started
as a process and at an address. Guard tests hold the descriptions the model reads to the code: every format
is named, every extension is listed in the refusal of an unknown one, and every range stated in prose is the
range the code enforces.

## License

MIT
