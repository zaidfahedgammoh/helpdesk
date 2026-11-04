(() => {
    const root = document.getElementById("ticket-chat");
    if (!root) return;

    const ticketId = Number(root.dataset.ticketId);
    const apiBaseUrl = (root.dataset.apiBaseUrl || "").replace(/\/$/, "");
    const isAgent = root.dataset.isAgent === "true";
    const currentUserId = Number(root.dataset.currentUserId);
    const status = document.getElementById("chat-status");
    const messages = document.getElementById("chat-messages");
    const empty = document.getElementById("chat-empty");
    const form = document.getElementById("chat-form");
    const input = document.getElementById("chat-input");
    const error = document.getElementById("chat-error");
    const typingIndicator = document.getElementById("typing-indicator");
    const sendButton = document.getElementById("chat-send");

    const seen = new Set();
    let typingTimer = null;
    let isCurrentlyTyping = false;
    let hubConnection = null;

    const text = {
        waiting: root.dataset.waiting || "Waiting for agent…",
        agentConnected: root.dataset.agentConnected || "An agent has joined the chat.",
        customerConnected: root.dataset.customerConnected || "The customer is in the chat.",
        chatReady: root.dataset.chatReady || "You are connected to this ticket chat.",
        loadError: root.dataset.loadError || "Could not load chat messages.",
        sendError: root.dataset.sendError || "Message could not be sent.",
        you: root.dataset.you || "You",
        agentTyping: root.dataset.agentTyping || "Agent is typing…",
        customerTyping: root.dataset.customerTyping || "Customer is typing…",
        typing: root.dataset.typing || "is typing…"
    };

    function getCsrfToken() {
        return root.querySelector('input[name="__RequestVerificationToken"]')?.value ||
               document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    async function getChatToken() {
        const response = await fetch("/account/chat-token", {
            method: "POST",
            headers: {
                "Accept": "application/json",
                "RequestVerificationToken": getCsrfToken()
            }
        });
        if (!response.ok) {
            throw new Error("Unable to obtain authenticated chat session token.");
        }
        const data = await response.json();
        return data.token;
    }

    function renderMessage(message) {
        if (!message || seen.has(message.id)) return;
        seen.add(message.id);

        if (empty && empty.parentNode) {
            empty.remove();
        }

        const mine = Number(message.senderId) === Number(currentUserId);
        const wrapper = document.createElement("div");
        wrapper.className = "align-self-" + (mine ? "end" : "start") + " chat-message";

        const bubble = document.createElement("div");
        bubble.className = "rounded-3 px-3 py-2 " + (mine ? "text-white bg-primary chat-message-mine" : "bg-light text-dark");

        const author = document.createElement("div");
        author.className = "small fw-semibold mb-1";
        author.textContent = mine ? text.you : (message.senderName || "");

        const body = document.createElement("div");
        body.classList.add("chat-message-content");
        body.textContent = message.content;

        const time = document.createElement("div");
        time.className = "small mt-1 " + (mine ? "text-white-50" : "text-muted");
        time.textContent = message.sentAt ? new Date(message.sentAt).toLocaleTimeString() : "";

        bubble.append(author, body, time);
        wrapper.appendChild(bubble);
        messages.appendChild(wrapper);
        messages.scrollTop = messages.scrollHeight;
    }

    async function loadHistory() {
        try {
            const token = await getChatToken();
            const response = await fetch(`${apiBaseUrl}/api/chat/${ticketId}/messages`, {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!response.ok) {
                if (response.status === 404) return;
                throw new Error(text.loadError);
            }

            const data = await response.json();
            if (Array.isArray(data.messages)) {
                data.messages.forEach(renderMessage);
            }

            if (data.status === "Resolved" || data.status === "Closed") {
                onTicketCompleted(data.status);
            } else if (data.status === "Escalated") {
                onTicketEscalated();
            } else {
                status.textContent = isAgent ? text.chatReady : (data.waitingForAgent ? text.waiting : text.agentConnected);
            }
        } catch (err) {
            console.error("Load history error:", err);
            error.textContent = text.loadError;
        }
    }

    function onTicketCompleted(completedStatus) {
        status.textContent = completedStatus;
        if (input) input.disabled = true;
        if (sendButton) sendButton.disabled = true;

        if (!document.getElementById("status-notice-banner")) {
            const banner = document.createElement("div");
            banner.id = "status-notice-banner";
            banner.className = "alert alert-success d-flex align-items-center justify-content-between mt-3 mb-0 shadow-sm";
            banner.innerHTML = `
                <div class="d-flex align-items-center gap-2">
                    <i class="bi bi-check-circle-fill fs-5 text-success"></i>
                    <div>
                        <strong>${completedStatus === "Resolved" ? "This ticket has been marked as resolved by the agent." : "This ticket has been closed."}</strong>
                        <div class="small text-muted">The live chat session has concluded.</div>
                    </div>
                </div>
                <a href="${isAgent ? '/AgentTickets/Index' : '/Customer/Index'}" class="btn btn-sm btn-outline-success ms-3 text-nowrap">Back to Tickets</a>`;
            const cardBody = document.querySelector(".card-body");
            if (cardBody) cardBody.appendChild(banner);
        }
    }

    function onTicketEscalated(msg) {
        status.textContent = "Escalated";
        if (input) input.disabled = true;
        if (sendButton) sendButton.disabled = true;
        error.textContent = msg || "This ticket has been escalated. Chat is now in read-only mode.";
    }

    function triggerTyping() {
        if (!hubConnection || hubConnection.state !== signalR.HubConnectionState.Connected) return;

        if (!isCurrentlyTyping) {
            isCurrentlyTyping = true;
            hubConnection.invoke("UserTyping", ticketId).catch(() => {});
        }

        clearTimeout(typingTimer);
        typingTimer = setTimeout(() => {
            isCurrentlyTyping = false;
            if (hubConnection && hubConnection.state === signalR.HubConnectionState.Connected) {
                hubConnection.invoke("StopTyping", ticketId).catch(() => {});
            }
        }, 1500);
    }

    async function start() {
        if (!window.signalR) throw new Error(text.loadError);

        let builder = new signalR.HubConnectionBuilder()
            .withUrl(`${apiBaseUrl}/hubs/chat`, {
                accessTokenFactory: async () => await getChatToken()
            })
            .withAutomaticReconnect([0, 1000, 3000, 5000, 10000]);

        // Enable MessagePack Hub Protocol if available
        if (window.signalR.protocols && window.signalR.protocols.msgpack && window.signalR.protocols.msgpack.MessagePackHubProtocol) {
            builder = builder.withHubProtocol(new window.signalR.protocols.msgpack.MessagePackHubProtocol());
        }

        const connection = builder.build();
        hubConnection = connection;

        // SignalR Event Handlers
        connection.on("ReceiveMessage", renderMessage);

        connection.on("UserTyping", data => {
            if (data && Number(data.userId) !== currentUserId) {
                if (typingIndicator) {
                    typingIndicator.textContent = isAgent ? text.customerTyping : text.agentTyping;
                }
            }
        });

        connection.on("StopTyping", data => {
            if (data && Number(data.userId) !== currentUserId) {
                if (typingIndicator) {
                    typingIndicator.textContent = "";
                }
            }
        });

        connection.on("TicketEscalated", notification => {
            onTicketEscalated(notification?.message);
        });

        connection.on("TicketStatusChanged", update => {
            if (update.status === "Resolved" || update.status === "Closed") {
                onTicketCompleted(update.status);
            }
        });

        connection.on("ChatParticipantJoined", participant => {
            if (!isAgent && participant.role === "Agent") status.textContent = text.agentConnected;
            else if (isAgent && participant.role === "Customer") status.textContent = text.customerConnected;
        });

        connection.onreconnecting(() => {
            status.textContent = "Reconnecting to chat…";
        });

        connection.onreconnected(async () => {
            try {
                await connection.invoke("JoinTicket", ticketId);
                await loadHistory();
                status.textContent = isAgent ? text.chatReady : text.agentConnected;
            } catch (err) {
                console.error("Reconnected synchronization error:", err);
            }
        });

        connection.onclose(() => {
            status.textContent = "Chat disconnected.";
        });

        // Start connection
        await connection.start();
        await connection.invoke("JoinTicket", ticketId);
        await loadHistory();

        // Wire Typing event on input
        input.addEventListener("input", triggerTyping);
        input.addEventListener("blur", () => {
            if (isCurrentlyTyping) {
                isCurrentlyTyping = false;
                clearTimeout(typingTimer);
                if (connection.state === signalR.HubConnectionState.Connected) {
                    connection.invoke("StopTyping", ticketId).catch(() => {});
                }
            }
        });

        // Wire Send Message Form
        form.addEventListener("submit", async event => {
            event.preventDefault();
            const content = input.value.trim();
            if (!content) return;

            input.disabled = true;
            if (sendButton) sendButton.disabled = true;

            try {
                await connection.invoke("SendMessage", ticketId, content);
                input.value = "";
                error.textContent = "";
            } catch (err) {
                console.error("SendMessage error:", err);
                error.textContent = text.sendError;
            } finally {
                input.disabled = false;
                if (sendButton) sendButton.disabled = false;
                input.focus();
            }
        });

        window.addEventListener("beforeunload", () => {
            if (connection) {
                connection.stop().catch(() => {});
            }
        });
    }

    start().catch(err => {
        console.error("Ticket chat connection error:", err);
        status.textContent = text.waiting;
        error.textContent = text.loadError;
    });
})();
