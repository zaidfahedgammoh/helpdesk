
document.addEventListener("DOMContentLoaded", () => {
    const passwordInput = document.getElementById("password");
    const toggleButton = document.getElementById("toggle-password");
    const loginForm = document.getElementById("login-form");
    const loginButton = document.getElementById("login-button");

    if (toggleButton && passwordInput) {
        toggleButton.addEventListener("click", () => {
            const isPassword = passwordInput.type === "password";

            passwordInput.type = isPassword ? "text" : "password";
            toggleButton.textContent = isPassword ? "Hide" : "Show";
        });
    }

    if (loginForm) {
        loginForm.addEventListener("submit", () => {
            if (loginButton) {
                loginButton.disabled = true;
                loginButton.textContent = "Signing in...";
            }
        });
    }
});
