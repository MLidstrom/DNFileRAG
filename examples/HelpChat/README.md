# HelpChat Example

A static HTML/TypeScript chat interface that connects to DNFileRAG API.

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
- Node.js (for TypeScript compilation)
- Documents indexed in Qdrant

## Setup

### 1. Start DNFileRAG

```bash
cd src/DNFileRAG
dotnet run
```

### 2. Compile TypeScript

```bash
cd examples/HelpChat
npx tsc
```

Or install TypeScript globally:

```bash
npm install -g typescript
tsc
```

### 3. Serve the Page

Using Python:
```bash
python -m http.server 3000
```

Using Node.js:
```bash
npx serve .
```

Or simply open `index.html` in a browser (note: some browsers block local file CORS).

### 4. Open in Browser

Navigate to `http://localhost:3000`

## API Connection

The chat connects to DNFileRAG at `http://localhost:8181`. To change this, edit `chat.ts`:

```typescript
new HelpChat('http://your-api-host:port');
```

## Features

- Real-time connection status indicator
- Typing indicator while waiting for response
- Source citations with relevance scores
- Response metadata (model, latency)
- Error handling with user-friendly messages
- Fully typed TypeScript client

## API Endpoints Used

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/health` | GET | Check API connectivity |
| `/api/query` | POST | Send RAG queries |

## Customization

### Change the Company Name

Edit `index.html`:
```html
<h1>Your Company Help Center</h1>
```

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
