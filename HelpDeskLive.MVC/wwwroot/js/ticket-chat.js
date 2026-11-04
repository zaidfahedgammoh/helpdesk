(() => {
    const root = document.getElementById("ticket-chat");
    if (!root) return;
    const ticketId = Number(root.dataset.ticketId);
    const token = root.dataset.token;
    const apiBaseUrl = root.dataset.apiBaseUrl;
    const isAgent = root.dataset.isAgent === "true";
    const currentUserId = Number(root.dataset.currentUserId);
    const status = document.getElementById("chat-status");
    const messages = document.getElementById("chat-messages");
    const empty = document.getElementById("chat-empty");
    const form = document.getElementById("chat-form");
    const input = document.getElementById("chat-input");
    const error = document.getElementById("chat-error");
    const seen = new Set();
    const text = {
        waiting: root.dataset.waiting,
        agentConnected: root.dataset.agentConnected,
        customerConnected: root.dataset.customerConnected,
        chatReady: root.dataset.chatReady,
        loadError: root.dataset.loadError,
        sendError: root.dataset.sendError,
        you: root.dataset.you
    };

    function renderMessage(message) {
        if (seen.has(message.id)) return;
        seen.add(message.id);
        if (empty) empty.remove();
        const mine = Number(message.senderId) === Number(currentUserId);
        const wrapper = document.createElement("div");
        wrapper.className = "align-self-" + (mine ? "end" : "start");
        wrapper.classList.add("chat-message");
        const bubble = document.createElement("div");
        bubble.className = "rounded-3 px-3 py-2 " + (mine ? "text-white" : "bg-light");
        if (mine) bubble.classList.add("chat-message-mine");
        const author = document.createElement("div");
        author.className = "small fw-semibold mb-1";
        author.textContent = mine ? text.you : (message.senderName || "");
        const body = document.createElement("div");
        body.classList.add("chat-message-content");
        body.textContent = message.content;
        const time = document.createElement("div");
        time.className = "small mt-1 " + (mine ? "text-white-50" : "text-muted");
        time.textContent = new Date(message.sentAt).toLocaleTimeString();
        bubble.append(author, body, time);
        wrapper.appendChild(bubble);
        messages.appendChild(wrapper);
        messages.scrollTop = messages.scrollHeight;
    }

    async function loadHistory() {
        const response = await fetch(apiBaseUrl + "/api/chat/" + ticketId + "/messages", {
            headers: { Authorization: "Bearer " + token }
        });
        if (!response.ok) throw new Error(text.loadError);
        const data = await response.json();
        data.messages.forEach(renderMessage);
        status.textContent = isAgent ? text.chatReady : (data.waitingForAgent ? text.waiting : text.agentConnected);
    }

    async function start() {
        if (!window.signalR) throw new Error(text.loadError);
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(apiBaseUrl + "/hubs/chat", { accessTokenFactory: () => token })
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveMessage", renderMessage);
        connection.on("ChatParticipantJoined", participant => {
            if (!isAgent && participant.role === "Agent") status.textContent = text.agentConnected;
            else if (isAgent && participant.role === "Customer") status.textContent = text.customerConnected;
        });
        connection.onreconnected(async () => {
            await connection.invoke("JoinTicket", ticketId);
            status.textContent = isAgent ? text.chatReady : text.agentConnected;
        });

        await connection.start();
        await connection.invoke("JoinTicket", ticketId);
        await loadHistory();

        form.addEventListener("submit", async event => {
            event.preventDefault();
            const content = input.value.trim();
            if (!content) return;
            input.disabled = true;
            try {
                await connection.invoke("SendMessage", ticketId, content);
                input.value = "";
                error.textContent = "";
            } catch {
                error.textContent = text.sendError;
            } finally {
                input.disabled = false;
                input.focus();
            }
        });
    }

    start().catch(() => {
        status.textContent = text.waiting;
        error.textContent = text.loadError;
    });
})();
