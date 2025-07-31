#!/bin/bash

# This script performs the full MCP query and prints the final URL as its output.

# Combine issue title and body from environment variables provided by the workflow.
QUESTION="Title: $ISSUE_TITLE. Body: $ISSUE_BODY"

# The URL of the MCP tool endpoint.
url="https://mcp.deepwiki.com/mcp"

# Step 1: Initialize session and capture the session ID from server headers.
session_id=$(curl -s --dump-header - -X POST \
-H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
-d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","clientInfo":{"name":"GitHubActionClient","version":"1.0.2"},"capabilities":{}}}' \
"$url" -o /dev/null | grep -i '^mcp-session-id:' | cut -d ':' -f 2 | tr -d '[:space:]')

# Exit if the session ID could not be retrieved.
if [ -z "$session_id" ]; then
  exit 1
fi

# Step 2: Send the 'initialized' notification to the server.
curl -s -X POST \
-H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
-H "Mcp-Session-Id: $session_id" \
-d '{"jsonrpc":"2.0","method":"initialized","params":{}}' \
"$url" > /dev/null

# Step 3: Use jq to safely construct the JSON payload.
# We pass the question as an argument to jq to avoid shell interpretation issues.
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

# Step 4: Send the final query, extract the URL, and replace the domain.
curl -s -X POST \
-H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
-H "Mcp-Session-Id: $session_id" \
-d "$JSON_PAYLOAD" \
"$url" | grep -o 'https://deepwiki.com/search/[^"]*' | sed 's|https://deepwiki.com|https://wiki.bitplatform.dev|'