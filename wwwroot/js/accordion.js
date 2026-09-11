// Style 2 ("accordion-css") collapsible groups. The Default Bootstrap
// accordion needs no script. This runs once for the page; each container
// binds a single click listener, so two widgets no longer double-toggle.
(function () {
    function setItem(item, active) {
        item.setAttribute('data-accordion-status', active ? 'active' : 'not-active');

        var button = item.querySelector('[aria-controls]');
        if (button) {
            button.setAttribute('aria-expanded', active ? 'true' : 'false');
        }

        // A collapsed panel is clipped by CSS but stays in the DOM; inert keeps
        // keyboard focus and screen readers out of it.
        var panel = item.querySelector('.accordion-css__item-bottom');
        if (panel) {
            if (active) {
                panel.removeAttribute('inert');
            } else {
                panel.setAttribute('inert', '');
            }
        }
    }

    function toggleItem(container, item) {
        var closeSiblings = container.getAttribute('data-accordion-close-siblings') === 'true';
        var isActive = item.getAttribute('data-accordion-status') === 'active';

        setItem(item, !isActive);

        if (closeSiblings && !isActive) {
            container.querySelectorAll('[data-accordion-status="active"]').forEach(function (sibling) {
                if (sibling !== item) { setItem(sibling, false); }
            });
        }
    }

    function init() {
        document.querySelectorAll('[data-accordion-css-init]').forEach(function (container) {
            if (container.dataset.accordionBound === 'true') { return; }
            container.dataset.accordionBound = 'true';

            container.addEventListener('click', function (event) {
                var toggle = event.target.closest('[data-accordion-toggle]');
                if (!toggle || !container.contains(toggle)) { return; }

                var item = toggle.closest('[data-accordion-status]');
                if (item) { toggleItem(container, item); }
            });
        });
    }

    init();
})();
