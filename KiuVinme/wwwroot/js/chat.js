class ChatApp {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.currentStatus = 'disconnected';
        this.stickerPacks = [];
        this.currentStickerPack = null;
        this.stickerPanelVisible = false;

        this.initializeElements();
        this.bindEvents();
        this.initializeSignalR();
        this.loadStickerPacks();
        this.startStatsUpdater();
    }

    initializeElements() {
        this.statusIndicator = document.getElementById('statusIndicator');
        this.statusText = document.getElementById('statusText');
        this.statsDisplay = document.getElementById('statsDisplay');
        this.messagesContainer = document.getElementById('messagesContainer');
        this.messageInput = document.getElementById('messageInput');
        this.sendButton = document.getElementById('sendButton');
        this.nextChatButton = document.getElementById('nextChatButton');
        this.stickerToggle = document.getElementById('stickerToggle');
        this.stickerPanel = document.getElementById('stickerPanel');
        this.stickerPackTabs = document.getElementById('stickerPackTabs');
        this.stickerGrid = document.getElementById('stickerGrid');
    }

    bindEvents() {
        this.messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });

        this.sendButton.addEventListener('click', () => this.sendMessage());
        this.nextChatButton.addEventListener('click', () => this.nextChat());
        this.stickerToggle.addEventListener('click', () => this.toggleStickerPanel());
    }

    async initializeSignalR() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/chatHub")
                .withAutomaticReconnect()
                .build();

            this.setupSignalREvents();

            await this.connection.start();
            this.updateConnectionStatus('waiting', 'Looking for someone to chat with...');
            this.enableControls();

        } catch (err) {
            this.updateConnectionStatus('disconnected', 'Connection Failed');
        }
    }

    setupSignalREvents() {
        this.connection.on('WaitingForMatch', (message) => {
            this.updateConnectionStatus('waiting', message);
            this.addSystemMessage(message);
            // Enable Next button while waiting (in case user wants to refresh their search)
            this.nextChatButton.disabled = false;
        });

        this.connection.on('Matched', (message) => {
            this.updateConnectionStatus('connected', 'Connected to someone!');
            this.addSystemMessage(message);
            // Enable all controls when matched
            this.enableControls();
        });

        this.connection.on('ReceiveMessage', (messageData) => {
            if (messageData && messageData.type === 'sticker') {
                this.addStickerMessage(messageData, false);
            } else if (messageData && messageData.content) {
                this.addTextMessage(messageData.content, false);
            }
        });

        this.connection.on('ChatEnded', (message) => {
            // Set status to 'ended' instead of 'waiting' - user must manually press Next
            this.updateConnectionStatus('ended', 'Chat ended. Click Next to find someone new.');
            this.addSystemMessage(message);

            // Disable chat controls but enable Next button
            this.disableChatControls();
            this.nextChatButton.disabled = false;

            // Make Next button more prominent
            this.nextChatButton.classList.add('btn-warning');
            this.nextChatButton.classList.remove('btn-danger');
        });

        this.connection.onreconnecting(() => {
            this.updateConnectionStatus('disconnected', 'Reconnecting...');
            this.disableControls();
        });

        this.connection.onreconnected(() => {
            this.updateConnectionStatus('waiting', 'Looking for someone to chat with...');
            this.enableControls();
        });
    }

    updateConnectionStatus(status, text) {
        this.currentStatus = status;
        this.statusIndicator.className = `status-indicator status-${status}`;
        this.statusText.textContent = text;
    }

    enableControls() {
        this.messageInput.disabled = false;
        this.sendButton.disabled = false;
        this.nextChatButton.disabled = false;
        this.stickerToggle.disabled = false;

        // Reset Next button styling
        this.nextChatButton.classList.remove('btn-warning');
        this.nextChatButton.classList.add('btn-danger');
    }

    disableControls() {
        this.messageInput.disabled = true;
        this.sendButton.disabled = true;
        this.nextChatButton.disabled = true;
        this.stickerToggle.disabled = true;
    }

    // New method to disable only chat controls but keep Next enabled
    disableChatControls() {
        this.messageInput.disabled = true;
        this.sendButton.disabled = true;
        this.stickerToggle.disabled = true;
        // Don't disable nextChatButton - user needs it to start new chat
    }

    async sendMessage() {
        const message = this.messageInput.value.trim();
        if (!message) return;

        // Only allow sending if connected to someone
        if (this.currentStatus !== 'connected') {
            this.addSystemMessage('You need to be connected to someone to send messages. Click Next to find someone!');
            return;
        }

        try {
            await this.connection.invoke('SendMessage', message);
            this.addTextMessage(message, true);
            this.messageInput.value = '';
        } catch (err) {
            console.error('Send message error:', err);
        }
    }

    async sendSticker(stickerId, stickerUrl, stickerName) {
        // Only allow sending if connected to someone
        if (this.currentStatus !== 'connected') {
            this.addSystemMessage('You need to be connected to someone to send stickers. Click Next to find someone!');
            return;
        }

        try {
            await this.connection.invoke('SendSticker', stickerId, stickerUrl, stickerName);
            this.addStickerMessage({
                stickerId: stickerId,
                stickerUrl: stickerUrl,
                content: stickerName
            }, true);
        } catch (err) {
            console.error('Sticker send error:', err);
        }
    }

    async nextChat() {
        try {
            await this.connection.invoke('NextChat');
            this.clearMessages();
            this.addSystemMessage('Looking for someone new to chat with...');

            // Reset button styling
            this.nextChatButton.classList.remove('btn-warning');
            this.nextChatButton.classList.add('btn-danger');

            // Update status
            this.updateConnectionStatus('waiting', 'Looking for someone to chat with...');
        } catch (err) {
            console.error('Next chat error:', err);
        }
    }

    addTextMessage(content, isSent) {
        if (!content && content !== 0) {
            content = '[Empty message]';
        }

        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${isSent ? 'sent' : 'received'}`;

        const bubbleDiv = document.createElement('div');
        bubbleDiv.className = 'message-bubble';
        bubbleDiv.textContent = String(content);

        messageDiv.appendChild(bubbleDiv);
        this.messagesContainer.appendChild(messageDiv);
        this.scrollToBottom();
    }

    addStickerMessage(messageData, isSent) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${isSent ? 'sent' : 'received'}`;

        const stickerDiv = document.createElement('div');
        stickerDiv.className = 'message-bubble sticker-message';

        const img = document.createElement('img');
        img.src = messageData.stickerUrl || messageData.StickerUrl;
        img.alt = messageData.content || messageData.Content || 'Sticker';
        img.title = messageData.content || messageData.Content || 'Sticker';

        stickerDiv.appendChild(img);
        messageDiv.appendChild(stickerDiv);
        this.messagesContainer.appendChild(messageDiv);
        this.scrollToBottom();
    }

    addSystemMessage(content) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'message system';

        const bubbleDiv = document.createElement('div');
        bubbleDiv.className = 'message-bubble';
        bubbleDiv.textContent = content;

        messageDiv.appendChild(bubbleDiv);
        this.messagesContainer.appendChild(messageDiv);
        this.scrollToBottom();
    }

    clearMessages() {
        this.messagesContainer.innerHTML = '';
    }

    scrollToBottom() {
        this.messagesContainer.scrollTop = this.messagesContainer.scrollHeight;
    }

    async loadStickerPacks() {
        try {
            const response = await fetch('/sticker/GetStickerPacks');
            if (response.ok) {
                this.stickerPacks = await response.json();
                this.renderStickerPackTabs();
            }
        } catch (err) {
            console.error('Failed to load sticker packs:', err);
        }
    }

    renderStickerPackTabs() {
        this.stickerPackTabs.innerHTML = '';

        this.stickerPacks.forEach((pack, index) => {
            const tab = document.createElement('button');
            tab.className = `sticker-pack-tab ${index === 0 ? 'active' : ''}`;
            tab.textContent = `${pack.name} (${pack.stickerCount})`;
            tab.addEventListener('click', () => this.selectStickerPack(pack, tab));
            this.stickerPackTabs.appendChild(tab);
        });

        if (this.stickerPacks.length > 0) {
            this.selectStickerPack(this.stickerPacks[0]);
        }
    }

    async selectStickerPack(pack, tabElement = null) {
        this.currentStickerPack = pack;

        if (tabElement) {
            this.stickerPackTabs.querySelectorAll('.sticker-pack-tab').forEach(t => t.classList.remove('active'));
            tabElement.classList.add('active');
        }

        try {
            const response = await fetch(`/sticker/GetStickersFromPack?packId=${pack.uid}`);
            if (response.ok) {
                const stickers = await response.json();
                this.renderStickers(stickers);
            }
        } catch (err) {
            console.error('Failed to load stickers:', err);
        }
    }

    renderStickers(stickers) {
        this.stickerGrid.innerHTML = '';

        stickers.forEach(sticker => {
            const stickerDiv = document.createElement('div');
            stickerDiv.className = 'sticker-item';
            stickerDiv.title = sticker.displayName;

            const img = document.createElement('img');
            img.src = sticker.imageUrl;
            img.alt = sticker.displayName;

            stickerDiv.appendChild(img);
            stickerDiv.addEventListener('click', () => {
                this.sendSticker(sticker.uid, sticker.imageUrl, sticker.displayName);
                this.toggleStickerPanel();
            });

            this.stickerGrid.appendChild(stickerDiv);
        });
    }

    toggleStickerPanel() {
        this.stickerPanelVisible = !this.stickerPanelVisible;
        this.stickerPanel.style.display = this.stickerPanelVisible ? 'block' : 'none';

        const icon = this.stickerToggle.querySelector('i');
        icon.className = this.stickerPanelVisible ? 'fas fa-times' : 'fas fa-smile';
    }

    async updateStats() {
        try {
            const response = await fetch('/sticker/GetStats');
            if (response.ok) {
                const stats = await response.json();
                this.statsDisplay.textContent =
                    `Active chats: ${stats.activeConnections} | Waiting: ${stats.waitingUsers}`;
            }
        } catch (err) {
            // Silently fail
        }
    }

    startStatsUpdater() {
        this.updateStats();
        setInterval(() => this.updateStats(), 5000);
    }
}

document.addEventListener('DOMContentLoaded', () => {
    window.contentManager = new ContentManager();
    window.chatApp = new ChatApp();
});