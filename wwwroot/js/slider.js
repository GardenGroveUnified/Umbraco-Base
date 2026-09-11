// Slider autoplay control. Swiper (v6) is initialised from the vendor bundle,
// so the instance on el.swiper is not ready until after load. This wires the
// pause / play toggle and honours the OS "reduce motion" setting.
(function () {
    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

    function playing(swiper) {
        return !!(swiper.autoplay && swiper.autoplay.running && !swiper.autoplay.paused);
    }

    function reflect(toggle, swiper) {
        var isPlaying = playing(swiper);
        toggle.setAttribute('aria-pressed', isPlaying ? 'false' : 'true');
        toggle.setAttribute('aria-label', isPlaying ? 'Pause slideshow' : 'Play slideshow');
        toggle.dataset.state = isPlaying ? 'playing' : 'paused';
    }

    function wire(toggle) {
        var block = toggle.closest('.block-slider');
        var container = block ? block.querySelector('.swiper-container') : null;
        var swiper = container && container.swiper;
        if (!swiper || !swiper.autoplay) { return false; }

        toggle.addEventListener('click', function () {
            if (playing(swiper)) { swiper.autoplay.stop(); }
            else { swiper.autoplay.start(); }
            reflect(toggle, swiper);
        });

        if (reduceMotion.matches) { swiper.autoplay.stop(); }
        reflect(toggle, swiper);
        toggle.hidden = false;
        return true;
    }

    function init() {
        document.querySelectorAll('[data-slider-autoplay-toggle]').forEach(function (toggle) {
            var tries = 0;
            (function attempt() {
                if (wire(toggle) || tries++ > 20) { return; }
                setTimeout(attempt, 150);
            })();
        });
    }

    window.addEventListener('load', init);
})();
