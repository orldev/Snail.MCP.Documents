# The server as a container: one per machine, standing behind every agent on it.
#
#   docker build -t snail/s3l-documents-mcp .
#   docker run -p 127.0.0.1:8823:8823 -v workspaces:/workspaces:ro snail/s3l-documents-mcp
#
# The volume is the point. This server reads the documents of the working copies, so it runs where the copies
# are: they are mounted here read-only, and each session names the one it reads through its address —
# …/mcp?root=/workspaces/<task>. A session naming a copy this container cannot see is refused at the door
# rather than answered about the wrong folder.
#
# The SDK is pinned to the band global.json asks for: on the floating sdk:10.0 a newer feature band would one day
# refuse the build. The server is native: published for linux-x64 the SDK compiles it ahead of time (Native AOT), which
# needs clang and zlib in the build stage, and the image runs the one executable on runtime-deps, with no .NET runtime
# in it — a smaller image and a faster start.
#
# VERSION is the release's when the shared mcp-image.yml builds the image (0.0.0-<branch>.<commit> from a branch)
# and 0.0.0-local otherwise: the server reports it in initialize and the image carries it as its OCI version.
ARG VERSION=0.0.0-local

FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
ARG VERSION
RUN apt-get update \
    && apt-get install --yes --no-install-recommends clang zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /source
COPY . .
RUN dotnet publish src/Snail.MCP.Documents.csproj -c Release -r linux-x64 -o /app -p:Version=$VERSION

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0
ARG VERSION
LABEL org.opencontainers.image.version=$VERSION
WORKDIR /app
COPY --from=build /app .

# The volume is the server's root: a session names one copy inside it and is turned away at the door when it names
# anything else. Every request at Information buries what matters in a container's log; the server's own warnings stay.
ENV SNAIL_MCP_DOCUMENTS_PORT=8823 \
    SNAIL_MCP_DOCUMENTS_ROOTS__0=/workspaces \
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

ENTRYPOINT ["/app/Snail.MCP.Documents"]
