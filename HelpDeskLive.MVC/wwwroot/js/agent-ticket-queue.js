(() => {
    const queueRoot = document.getElementById("agent-queue");
    if (!queueRoot) return;

    const apiBaseUrl = queueRoot.dataset.apiBaseUrl;
    const alertContainer = document.getElementById("queue-alert-container");
    const liveBadge = document.getElementById("queue-live-badge");

    async function getChatToken() {
        const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
        const response = await fetch("/account/chat-token", {
            method: "POST",
            headers: {
                "Accept": "application/json",
                "RequestVerificationToken": csrfToken
            }
        });
        if (!response.ok) throw new Error("Could not acquire token");
        const data = await response.json();
        return data.token;
    }

    function showUpdateNotice(message) {
        if (!alertContainer) {
            window.location.reload();
            return;
        }
        alertContainer.innerHTML = `
            <div class="alert alert-info alert-dismissible fade show" role="alert">
                <i class="bi bi-bell-fill me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>`;
        setTimeout(() => window.location.reload(), 1500);
    }

    async function connectDepartmentHub() {
        if (!window.signalR || !apiBaseUrl) {
            window.setTimeout(() => window.location.reload(), 20000);
            return;
        }

        try {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl(apiBaseUrl.replace(/\/$/, "") + "/hubs/chat", {
                    accessTokenFactory: async () => await getChatToken()
                })
                .withAutomaticReconnect()
                .build();

            connection.on("TicketAssigned", data => {
                showUpdateNotice(`Ticket #${data.ticketId} assigned to agent #${data.agentId}. Updating queue...`);
            });

            connection.on("EscalatedTicketAvailable", data => {
                showUpdateNotice(`Escalated ticket #${data.ticketId} is now available in your department queue. Refreshing...`);
            });

            connection.on("TicketStatusChanged", data => {
                showUpdateNotice(`Ticket #${data.ticketId} status changed to ${data.status}. Updating queue...`);
            });

            connection.onreconnected(async () => {
                try {
                    await connection.invoke("JoinMyDepartment");
                    if (liveBadge) liveBadge.className = "badge bg-success-subtle text-success border border-success-subtle px-2 py-1 small";
                } catch { }
            });

            connection.onclose(() => {
                if (liveBadge) liveBadge.className = "badge bg-warning-subtle text-warning border border-warning-subtle px-2 py-1 small";
            });

            await connection.start();
            await connection.invoke("JoinMyDepartment");

            if (liveBadge) {
                const liveConnectedText = liveBadge.dataset.liveConnected || "Live Connected";
                liveBadge.className = "badge bg-success-subtle text-success border border-success-subtle px-2 py-1 small";
                liveBadge.innerHTML = `<i class="bi bi-broadcast me-1"></i>${liveConnectedText}`;
            }
        } catch {
            window.setTimeout(() => window.location.reload(), 15000);
        }
    }

    connectDepartmentHub();
})();
