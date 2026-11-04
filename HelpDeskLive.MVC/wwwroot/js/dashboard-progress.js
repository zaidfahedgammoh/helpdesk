document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll(".progress-bar[data-progress]").forEach(bar => {
        const value = Number(bar.dataset.progress);
        bar.style.width = Math.max(0, Math.min(100, Number.isFinite(value) ? value : 0)) + "%";
    });
});
