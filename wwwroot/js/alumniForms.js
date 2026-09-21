// wwwroot/js/alumniForms.js
// Handles the Alumni Sign Up form, the Alumni Directory's per-card
// "Send a message" contact form, and the "Post a Memoir" form, all posted
// via fetch to AlumniSurfaceController (see
// Core/Controllers/AlumniSurfaceController.cs).
(function () {
    function showResult(form, message, isError) {
        var result = form.querySelector('[data-alumni-form-result], [data-memoir-form-result]');
        if (!result) { return; }
        result.textContent = message;
        result.classList.toggle('alumni-signup__result--error', !!isError);
    }

    function submitForm(form, url, extraFields) {
        var formData = new FormData(form);
        if (extraFields) {
            Object.keys(extraFields).forEach(function (key) {
                formData.set(key, extraFields[key]);
            });
        }

        var submitButton = form.querySelector('button[type="submit"]');
        if (submitButton) { submitButton.disabled = true; }

        fetch(url, { method: 'POST', body: formData })
            .then(function (response) { return response.json(); })
            .then(function (data) {
                showResult(form, data.message, !data.success);
                if (data.success) { form.reset(); }
            })
            .catch(function () {
                showResult(form, 'Something went wrong. Please try again later.', true);
            })
            .finally(function () {
                if (submitButton) { submitButton.disabled = false; }
            });
    }

    function initSignupForm() {
        var form = document.querySelector('[data-alumni-signup-form]');
        if (!form) { return; }

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            submitForm(form, '/umbraco/surface/AlumniSurface/SignUp');
        });
    }

    function initContactModal() {
        var modal = document.querySelector('[data-alumni-contact-modal]');
        if (!modal) { return; }

        var form = modal.querySelector('[data-alumni-contact-form]');
        var memberIdField = modal.querySelector('[data-alumni-contact-member-id]');
        var titleEl = modal.querySelector('[data-alumni-contact-title]');
        var closeButton = modal.querySelector('[data-alumni-contact-close]');
        var lastTrigger = null;

        function open(memberId, memberName, trigger) {
            memberIdField.value = memberId;
            titleEl.textContent = 'Send a message to ' + memberName;
            modal.hidden = false;
            lastTrigger = trigger || null;
            if (closeButton) { closeButton.focus(); }
        }

        function close() {
            modal.hidden = true;
            form.reset();
            if (lastTrigger) { lastTrigger.focus(); }
            lastTrigger = null;
        }

        document.addEventListener('click', function (event) {
            var trigger = event.target.closest('[data-alumni-contact-trigger]');
            if (trigger) {
                open(trigger.getAttribute('data-member-id'), trigger.getAttribute('data-member-name') || 'this alumnus', trigger);
                return;
            }

            if (event.target.closest('[data-alumni-contact-close]') || event.target === modal) {
                close();
            }
        });

        document.addEventListener('keydown', function (event) {
            if (!modal.hidden && event.key === 'Escape') {
                close();
            }
        });

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            submitForm(form, '/umbraco/surface/AlumniSurface/SendMessage');
        });
    }

    function initMemoirSubmitForm() {
        var form = document.querySelector('[data-memoir-submit-form]');
        if (!form) { return; }

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            submitForm(form, '/umbraco/surface/AlumniSurface/SubmitMemoir');
        });
    }

    initSignupForm();
    initContactModal();
    initMemoirSubmitForm();
})();
