/*
 * Main navigation behaviour.
 *
 * Mouse users get the desktop dropdowns from a :hover rule in core.css.
 * styles.css adds :focus-within so keyboard focus opens the same menu,
 * and an .is-open class for the touch path (tap, no hover, no focus).
 * This file supplies:
 *   - click / tap / Enter / Space toggles .is-open (the touch path)
 *   - Escape adds .nav-collapsed, which suppresses the :focus-within
 *     reveal while focus stays on the toggle; the class is cleared when
 *     focus leaves, on the next key, or on a pointer press
 *   - a click or tap outside closes everything
 * It also keeps the responsive class swap that used to be inline in
 * header.cshtml: below 992px the menus expand in the flow.
 *
 * The open/close handling only runs at desktop width. Below 992px the
 * hamburger menu and inline expansion already work.
 */
(function () {
    'use strict';

    var nav = document.getElementById('navbarMainNav');
    if (!nav) { return; }

    var desktopQuery = window.matchMedia('(min-width: 992px)');
    function isDesktop() { return desktopQuery.matches; }

    var toggles = Array.prototype.slice.call(nav.querySelectorAll('[data-nav-toggle]'));

    function owningItem(toggle) {
        return toggle.closest('.nav-item, .dropdown-submenu');
    }

    function setOpen(toggle, open) {
        var item = owningItem(toggle);
        if (!item) { return; }
        item.classList.toggle('is-open', open);
        if (open) { item.classList.remove('nav-collapsed'); }
        toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    function closeAll(except) {
        toggles.forEach(function (toggle) {
            if (toggle !== except) { setOpen(toggle, false); }
        });
    }

    function clearCollapsed() {
        nav.querySelectorAll('.nav-collapsed').forEach(function (el) {
            el.classList.remove('nav-collapsed');
        });
    }

    // Touch / mouse / Enter / Space: explicit toggle.
    toggles.forEach(function (toggle) {
        toggle.addEventListener('click', function (event) {
            if (!isDesktop()) { return; }
            event.preventDefault();
            var item = owningItem(toggle);
            var willOpen = !(item && item.classList.contains('is-open'));
            closeAll(willOpen ? toggle : null);
            setOpen(toggle, willOpen);
        });
    });

    // Escape closes the top-level menu it is inside and holds focus on
    // that toggle. .nav-collapsed keeps the menu shut while focus is still
    // on the toggle (which would otherwise re-open it via :focus-within).
    nav.addEventListener('keydown', function (event) {
        if (!isDesktop()) { return; }
        if (event.key !== 'Escape') {
            if (event.key !== 'Tab' && event.key !== 'Shift') { clearCollapsed(); }
            return;
        }
        var navItem = event.target.closest('.navbar-nav > .nav-item');
        if (!navItem) { return; }
        navItem.classList.add('nav-collapsed');
        navItem.classList.remove('is-open');
        navItem.querySelectorAll('.dropdown-submenu.is-open').forEach(function (sub) {
            sub.classList.remove('is-open');
            var st = sub.querySelector('[data-nav-toggle]');
            if (st) { st.setAttribute('aria-expanded', 'false'); }
        });
        var toggle = navItem.querySelector(':scope > [data-nav-toggle]');
        if (toggle) {
            toggle.setAttribute('aria-expanded', 'false');
            toggle.focus();
        }
    });

    // Focus leaving an item closes it and clears the Escape guard.
    nav.addEventListener('focusout', function (event) {
        if (!isDesktop()) { return; }
        var item = event.target.closest('.nav-item, .dropdown-submenu');
        if (!item || item.contains(event.relatedTarget)) { return; }
        item.classList.remove('nav-collapsed');
        var toggle = item.querySelector('[data-nav-toggle]');
        if (toggle) { setOpen(toggle, false); }
    });

    // Click or tap outside the nav closes everything.
    document.addEventListener('pointerdown', function (event) {
        if (!isDesktop() || nav.contains(event.target)) { return; }
        clearCollapsed();
        closeAll(null);
    });

    // Responsive class swap (previously inline in header.cshtml).
    var menuWidth = document.getElementById('menuWidth');
    var dropdownMenus = Array.prototype.slice.call(nav.querySelectorAll('.dropdownMenu'));

    function syncResponsiveClasses() {
        var desktop = isDesktop();
        if (menuWidth) { menuWidth.classList.toggle('w-100', desktop); }
        dropdownMenus.forEach(function (menu) {
            menu.classList.toggle('dropdown-menu', desktop);
            menu.classList.toggle('animate-slideup', desktop);
        });
        if (!desktop) { clearCollapsed(); closeAll(null); }
    }

    syncResponsiveClasses();
    window.addEventListener('resize', syncResponsiveClasses);
})();
