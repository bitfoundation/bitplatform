#!/bin/bash

# This script performs the full MCP query and prints the final URL as its output.

set -euo pipefail

# Combine issue title and body from environment variables provided by the workflow.
QUESTION="Title: $ISSUE_TITLE. Body: $ISSUE_BODY"

# The URL of the MCP tool endpoint.
url="https://mcp.deepwiki.com/mcp"

# The server runs in stateless mode, so no initialize/initialized handshake is needed.
# Use jq to safely construct the JSON payload, passing the question as an argument
# to avoid shell interpretation issues.
JSON_PAYLOAD=$(jq -n \
  --arg question "$QUESTION" \
  '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "ask_question",
      "arguments": {
        "repoName": "bitfoundation/bitplatform",
        "question": $question
      }
    }
  }')

# Send the query. A transport level failure here fails the whole step.
RESPONSE=$(curl -s --fail-with-body -X POST \
  -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
  -d "$JSON_PAYLOAD" \
  "$url")

# Extract the first URL and replace the domain.
# The URL is embedded in a JSON string, so stop at the first quote or backslash
# (the response terminates the URL with an escaped newline).
# An empty result is not an error, it just means no comment gets posted.
# The trailing `|| true` keeps a no-match grep from tripping `pipefail`.
echo "$RESPONSE" \
  | { grep -o 'https://deepwiki\.com/search/[^"\\]*' || true; } \
  | head -n 1 \
  | sed 's|https://deepwiki.com|https://wiki.bitplatform.dev|'
