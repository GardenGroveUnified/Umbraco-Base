/*
 * Main navigation behaviour.
 *
 * Mouse users get the desktop dropdowns from a :hover rule in core.css.
 * styles.css adds :focus-within so keyboard focus opens the same menu,
 * and an .is-open class for the tap path (touch, or click with no
 * hover/focus). This file supplies:
 *   - click / tap / Enter / Space toggles .is-open (the tap path)
 *   - Escape adds .nav-collapsed, which suppresses the :focus-within
 *     reveal while focus stays on the toggle; the class is cleared when
 *     focus leaves, on the next key, or on a pointer press
 *   - a click or tap outside closes everything
 * It also keeps the responsive class swap that used to be inline in
 * header.cshtml.
 *
 * This same toggle/close/Escape/focus logic runs at every width. Below
 * 992px styles.css hides each .dropdownMenu until its .nav-item gets
 * .is-open, so the accordion behaviour is identical to desktop - only
 * the reveal is a tap on the toggle instead of a :hover.
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

    // Ancestor toggles of `toggle` (its own item's toggle, and that
    // item's parents' toggles) - these must stay open when `toggle`
    // itself is the one being opened, or opening a nested submenu would
    // immediately collapse the parent section around it.
    function ancestorToggles(toggle) {
        var result = [];
        var item = owningItem(toggle) && owningItem(toggle).parentElement
            ? owningItem(toggle).parentElement.closest('.nav-item, .dropdown-submenu')
            : null;
        while (item) {
            var t = item.querySelector(':scope > [data-nav-toggle]');
            if (t) { result.push(t); }
            item = item.parentElement ? item.parentElement.closest('.nav-item, .dropdown-submenu') : null;
        }
        return result;
    }

    function closeAll(except) {
        var spared = except ? ancestorToggles(except) : [];
        toggles.forEach(function (toggle) {
            if (toggle !== except && spared.indexOf(toggle) === -1) { setOpen(toggle, false); }
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
            event.preventDefault();
            var item = owningItem(toggle);
            // Read the state from is-open, not aria-expanded: clicking a button
            // also focuses it in Chromium, and focusin (below) sets
            // aria-expanded=true as part of that same click before this handler
            // runs - reading aria-expanded here would see its own click's
            // side effect and immediately close what it just opened.
            var willOpen = !item || !item.classList.contains('is-open');
            closeAll(willOpen ? toggle : null);
            setOpen(toggle, willOpen);
            // Closing with focus still on the toggle: add nav-collapsed so the
            // :focus-within rule in styles.css stops holding the menu open.
            if (!willOpen && item) { item.classList.add('nav-collapsed'); }
        });
    });

    // Escape closes the top-level menu it is inside and holds focus on
    // that toggle. .nav-collapsed keeps the menu shut while focus is still
    // on the toggle (which would otherwise re-open it via :focus-within).
    nav.addEventListener('keydown', function (event) {
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

    // Focus entering an item keeps aria-expanded in step with the
    // :focus-within reveal in styles.css. Walk every enclosing item so a
    // deep link marks both its submenu toggle and the top-level toggle open.
    nav.addEventListener('focusin', function (event) {
        var item = event.target.closest('.nav-item, .dropdown-submenu');
        while (item) {
            if (!item.classList.contains('nav-collapsed')) {
                var toggle = item.querySelector(':scope > [data-nav-toggle]');
                if (toggle) { toggle.setAttribute('aria-expanded', 'true'); }
            }
            item = item.parentElement ? item.parentElement.closest('.nav-item, .dropdown-submenu') : null;
        }
    });

    // Focus leaving an item closes it and clears the Escape guard.
    nav.addEventListener('focusout', function (event) {
        var item = event.target.closest('.nav-item, .dropdown-submenu');
        if (!item || item.contains(event.relatedTarget)) { return; }
        item.classList.remove('nav-collapsed');
        var toggle = item.querySelector('[data-nav-toggle]');
        if (toggle) { setOpen(toggle, false); }
    });

    // Click or tap outside the nav closes everything.
    document.addEventListener('pointerdown', function (event) {
        if (nav.contains(event.target)) { return; }
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
