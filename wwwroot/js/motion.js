// Respect the OS "reduce motion" setting for things CSS cannot reach.
// The reduced-motion @media block in styles.css collapses transitions,
// animations and AOS; an autoplaying <video> needs script to stop.
(function () {
    var query = window.matchMedia('(prefers-reduced-motion: reduce)');

    function apply() {
        if (!query.matches) { return; }
        var videos = document.querySelectorAll('video[autoplay]');
        for (var i = 0; i < videos.length; i++) {
            videos[i].removeAttribute('autoplay');
            videos[i].removeAttribute('loop');
            try { videos[i].pause(); } catch (e) { /* not ready yet, harmless */ }
        }
    }

    apply();
    // matchMedia fires when the user changes the setting mid-session.
    if (query.addEventListener) {
        query.addEventListener('change', apply);
    } else if (query.addListener) {
        query.addListener(apply);
    }
})();
