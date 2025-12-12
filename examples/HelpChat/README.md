# HelpChat Example

A static mock company landing page with a popup support chat that connects to the DNFileRAG API.

## Files

```
HelpChat/
├── assets/         # Static assets (logo, images)
├── index.html      # Chat UI
├── chat.ts         # TypeScript client for DNFileRAG API
├── chat.js         # Compiled JavaScript (generated)
├── tsconfig.json   # TypeScript configuration
└── README.md
```

## Prerequisites

- DNFileRAG API running on `http://localhost:8181`
- (Optional) Node.js (only if you want to modify `chat.ts` and recompile)

## Setup

### 1. Start DNFileRAG

```bash
cd src/DNFileRAG
dotnet run
```

### 2. Serve the page

Using Python:
```bash
cd examples/HelpChat
python -m http.server 3000
```

Using Node.js:
```bash
cd examples/HelpChat
npx serve .
```

Or open `index.html` directly (note: some browsers block `file://` requests to `http://localhost`).

### 3. Open in browser

Navigate to `http://localhost:3000`

## API Connection

The chat connects to DNFileRAG at `http://localhost:8181`. To change this, edit `chat.ts`:

```typescript
new HelpChat('http://your-api-host:port');
```

## Features

- Real-time connection status indicator
- Typing indicator while waiting for response
- Response metadata (model, latency)
- Error handling with user-friendly messages
- Pop-up chat widget + resizable window (upper-left handle)
- Fully typed TypeScript client (optional; `chat.js` is already included)

## API Endpoints Used

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/health` | GET | Check API connectivity |
| `/api/query` | POST | Send RAG queries |

## Customization

### Change the Company Name

Edit `index.html` (topbar + hero copy).

### Adjust Query Parameters

Edit `chat.ts`:
```typescript
const request: QueryRequest = {
    query: message,
    topK: 10,           // More context
    temperature: 0.5,   // More creative
    maxTokens: 1000     // Longer responses
};
```

### If you change `chat.ts`

Recompile:

```bash
cd examples/HelpChat
npx tsc
```
