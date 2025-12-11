// DNFileRAG Help Chat Client
// Connects to DNFileRAG API running on localhost:8181

interface Source {
    filePath: string;
    fileName: string;
    chunkIndex: number;
    pageNumber: number | null;
    score: number;
    snippet: string;
}

interface QueryMetadata {
    model: string;
    latencyMs: number;
    guardrailsApplied: boolean;
    conversationId: string | null;
}

interface QueryResponse {
    answer: string;
    sources: Source[];
    metadata: QueryMetadata;
}

interface QueryRequest {
    query: string;
    topK?: number;
    temperature?: number;
    maxTokens?: number;
}

class HelpChat {
    private readonly apiBaseUrl: string;
    private readonly chatMessages: HTMLElement;
    private readonly messageInput: HTMLInputElement;
    private readonly sendButton: HTMLButtonElement;
    private readonly apiStatus: HTMLElement;
    private isFirstMessage: boolean = true;

    constructor(apiBaseUrl: string = 'http://localhost:8181') {
        this.apiBaseUrl = apiBaseUrl;
        this.chatMessages = document.getElementById('chatMessages')!;
        this.messageInput = document.getElementById('messageInput') as HTMLInputElement;
        this.sendButton = document.getElementById('sendButton') as HTMLButtonElement;
        this.apiStatus = document.getElementById('apiStatus')!;

        this.init();
    }

    private init(): void {
        this.sendButton.addEventListener('click', () => this.sendMessage());
        this.messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.sendMessage();
        });

        this.messageInput.focus();
        this.checkApiHealth();
    }

    private async checkApiHealth(): Promise<void> {
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/health`);
            if (response.ok) {
                this.apiStatus.textContent = 'Connected to DNFileRAG';
                this.apiStatus.className = 'api-status connected';
            } else {
                throw new Error('API not healthy');
            }
        } catch {
            this.apiStatus.textContent = 'API Disconnected';
            this.apiStatus.className = 'api-status disconnected';
        }
    }

    private escapeHtml(text: string): string {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    private addMessage(content: string, isUser: boolean, sources: Source[] = [], metadata?: QueryMetadata): void {
        if (this.isFirstMessage) {
            this.chatMessages.innerHTML = '';
            this.isFirstMessage = false;
        }

        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${isUser ? 'user' : 'bot'}`;

        let html = `<div class="message-content">${this.escapeHtml(content)}</div>`;

        if (!isUser && sources.length > 0) {
            const sourceList = sources
                .map(s => `${s.fileName}${s.pageNumber ? ` (p.${s.pageNumber})` : ''} [${(s.score * 100).toFixed(0)}%]`)
                .join(', ');
            html += `<div class="message-sources"><strong>Sources:</strong> ${sourceList}</div>`;
        }

        if (!isUser && metadata) {
            html += `<div class="message-meta">${metadata.model} | ${metadata.latencyMs}ms</div>`;
        }

        messageDiv.innerHTML = html;
        this.chatMessages.appendChild(messageDiv);
        this.chatMessages.scrollTop = this.chatMessages.scrollHeight;
    }

    private addTypingIndicator(): void {
        const indicator = document.createElement('div');
        indicator.className = 'message bot';
        indicator.id = 'typingIndicator';
        indicator.innerHTML = `
            <div class="typing-indicator">
                <span>.</span><span>.</span><span>.</span> Thinking
            </div>
        `;
        this.chatMessages.appendChild(indicator);
        this.chatMessages.scrollTop = this.chatMessages.scrollHeight;
    }

    private removeTypingIndicator(): void {
        const indicator = document.getElementById('typingIndicator');
        if (indicator) indicator.remove();
    }

    private addErrorMessage(message: string): void {
        const errorDiv = document.createElement('div');
        errorDiv.className = 'error-message';
        errorDiv.textContent = message;
        this.chatMessages.appendChild(errorDiv);
        this.chatMessages.scrollTop = this.chatMessages.scrollHeight;
    }

    private async sendMessage(): Promise<void> {
        const message = this.messageInput.value.trim();
        if (!message) return;

        this.addMessage(message, true);
        this.messageInput.value = '';
        this.sendButton.disabled = true;
        this.addTypingIndicator();

        try {
            const request: QueryRequest = {
                query: message,
                topK: 5,
                temperature: 0.3,
                maxTokens: 500
            };

            const response = await fetch(`${this.apiBaseUrl}/api/query`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            });

            if (!response.ok) {
                throw new Error(`API error: ${response.status}`);
            }

            const data: QueryResponse = await response.json();
            this.removeTypingIndicator();
            this.addMessage(data.answer, false, data.sources, data.metadata);

        } catch (error) {
            this.removeTypingIndicator();
            const errorMessage = error instanceof Error ? error.message : 'Unknown error';
            this.addErrorMessage(`Failed to get response: ${errorMessage}`);
        } finally {
            this.sendButton.disabled = false;
            this.messageInput.focus();
        }
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new HelpChat('http://localhost:8181');
});
