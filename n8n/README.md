# Overview

This is an n8n workflow that runs once a day, asks the ChatApp backend to summarize every conversation, and publishes the result two ways: a web page and a Google Sheet.

You do not need to know how the backend works. You only need three things from the backend owner:
- the backend's URL
- an `N8n` service key (a secret string)
- the username of an existing **Administrator** account on the backend

---

# Workflow

Every day at 00:00 (configurable), the workflow:

1. Asks the backend for the list of all conversations.
2. Asks the backend to summarize each conversation.
3. Asks the backend for one overall summary of everything from the last 24 hours.
4. Combines both into a single "digest".
5. Sends the digest back to the backend (so the app can notify users) and saves a copy the workflow itself serves as a web page.
6. Appends one row per conversation (plus one "overall" row) to a Google Sheet.

The web page is served by the workflow's own **Webhook** node — open its URL any time to see the latest digest, even outside the daily schedule.

---

# Local Installation

## Prerequisites

- [Node.js](https://nodejs.org) 20 or newer, and npm (comes with Node.js).
- The backend already running and reachable from your machine (ask the backend owner for its URL).
- From the backend owner: the `N8n` service key, and the username of an Administrator-role account.
- A Google account, if you want the Google Sheet step to work (optional — you can delete that node otherwise).

## Installation

1. Open a terminal in this folder (`n8n/`).
2. Install n8n:
   ```
   npm install
   ```
3. Start n8n:
   ```
   npm run dev
   ```
   Wait for it to print a URL (usually `http://localhost:5678`), then open that URL in your browser. First time only: n8n asks you to create a local account — any email/password works, it's just for your own login.
4. Import the workflow: top-left menu → **Import from File** → select `workflow.json` in this folder.
5. After import, open the workflow and read the yellow "Setup" note in the top-left corner — it lists the same steps below, directly on the canvas.
6. Create the credential the workflow needs:
   - Go to **Credentials** (left sidebar) → **Add credential** → **Header Auth**.
   - Name it exactly: `ChatApp N8n Key`
   - Header name: `X-Client-Key`
   - Header value: the `N8n` service key from the backend owner
   - Save.
   - Back in the workflow, open each HTTP Request node once (there are 4: "Get All Conversations", "Summarize Thread", "Get 24h Rollup", "Publish Digest To Backend") and confirm the credential dropdown shows `ChatApp N8n Key`. If it shows "credential not found", just pick it from the dropdown.
7. Open the **Config** node and fill in:
   - `baseUrl` — the backend's URL, no trailing slash (e.g. `https://your-backend.example.com`).
   - `onBehalfOfUsername` — the Administrator username the backend owner gave you.
8. Open the **Append To Google Sheet** node, connect your own Google account, and pick (or create) a spreadsheet with a sheet whose first row is: `Type | ConversationId | DisplayName | Summary | GeneratedAt`. Skip this step (and delete the node) if you don't need the spreadsheet.
9. Before relying on the daily schedule: click **Execute Workflow** once to run it manually and confirm every node turns green.
10. Turn on the **Active** toggle (top-right) so it runs automatically every day.

**To see the published digest page:** open the "Webhook: Digest Page" node, copy its **Production URL**, and open that URL in a browser. It shows "no digest yet" until the workflow runs once.

---

# References

- [docs/n8n-workflow.md](../docs/n8n-workflow.md) — the design behind this workflow, and the backend endpoints it calls.
- [docs/mcp-integration.md](../docs/mcp-integration.md) — the auth model shared with the MCP client (service key + on-behalf-of user).
- [n8n documentation](https://docs.n8n.io/)
- [n8n HTTP Request node](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.httprequest/)
- [n8n Google Sheets node](https://docs.n8n.io/integrations/builtin/app-nodes/n8n-nodes-base.googlesheets/)
