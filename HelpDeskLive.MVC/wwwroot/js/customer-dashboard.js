(() => {
        const issueTypeSelect = document.getElementById("IssueTypeId");
        const customTitleContainer = document.getElementById("customTitleContainer");
        const customTitleInput = document.getElementById("CustomTitle");

        if (!issueTypeSelect || !customTitleContainer || !customTitleInput) return;

        const updateCustomTitleVisibility = () => {
            const isCustomRequest = issueTypeSelect.value === "";
            customTitleContainer.hidden = !isCustomRequest;
            customTitleInput.required = isCustomRequest;

            if (!isCustomRequest) {
                customTitleInput.value = "";
            }
        };

        issueTypeSelect.addEventListener("change", updateCustomTitleVisibility);
        updateCustomTitleVisibility();
    })();
