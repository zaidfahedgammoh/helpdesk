
document.addEventListener("DOMContentLoaded", () => {
    const passwordInput = document.getElementById("password");
    const toggleButton = document.getElementById("toggle-password");
    const loginForm = document.getElementById("login-form");
    const loginButton = document.getElementById("login-button");

    if (toggleButton && passwordInput) {
        const showText = toggleButton.dataset.showText;
        const hideText = toggleButton.dataset.hideText;

        toggleButton.addEventListener("click", () => {
            const isPassword = passwordInput.type === "password";

            passwordInput.type = isPassword ? "text" : "password";

            toggleButton.textContent = isPassword
                ? hideText
                : showText;
        });
    }

    if (loginForm) {
        loginForm.addEventListener("submit", () => {
            if (loginButton) {
                loginButton.disabled = true;

                loginButton.textContent =
                    loginButton.dataset.loadingText;
            }
        });
    }
});
