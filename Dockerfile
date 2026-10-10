# The server as a container: one per machine, standing behind every agent on it.
#
#   docker build -t snail/s3l-documents-mcp .
#   docker run -p 8823:8823 -v workspaces:/workspaces:ro snail/s3l-documents-mcp
#
# The volume is the point. This server reads the documents of the working copies, so it runs where the copies
# are: they are mounted here read-only, and each session names the one it reads through its address —
# …/mcp?root=/workspaces/<task>. A session naming a copy this container cannot see is refused at the door
# rather than answered about the wrong folder.

# Pinned to the band global.json asks for: on the floating tag a newer feature band refuses the build.
FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /source
COPY . .
RUN dotnet publish src/Snail.MCP.Documents.csproj -c Release -o /app --self-contained false -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11
WORKDIR /app
COPY --from=build /app .

# Every request at Information buries what matters in a container's log; the server's own warnings stay.
ENV SNAIL_MCP_DOCUMENTS_PORT=8823 \
    Logging__LogLevel__Default=Information \
    Logging__LogLevel__Microsoft.AspNetCore=Warning \
    Logging__LogLevel__ModelContextProtocol=Warning

EXPOSE 8823
USER app

# What the platform reads to fill in its registry entry: the port and path a session is opened at, and what
# an operator has to provide.
LABEL mcp.name="documents" \
      mcp.port="8823" \
      mcp.path="/mcp?root={workspace}" \
      mcp.secrets="" \
      mcp.description="Read the documents of the working copy as Markdown: Word, Excel, PowerPoint, the text of a PDF, HTML, CSV, JSON and text, in pieces, with an outline and tables row by row." \
      mcp.requires="The volume of the machine's working copies mounted at /workspaces, read-only."

ENTRYPOINT ["dotnet", "Snail.MCP.Documents.dll"]
