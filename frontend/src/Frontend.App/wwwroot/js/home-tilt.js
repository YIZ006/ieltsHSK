// 3D Global Tilt & Mouse Tracking matching user React reference code
(function () {
    window.initHomeGlobalTilt = function (containerId) {
        const container = document.getElementById(containerId) || document.querySelector('.landing-container');
        if (!container) return;

        if (window._homeMouseMoveHandler) {
            window.removeEventListener('mousemove', window._homeMouseMoveHandler);
        }

        window._homeMouseMoveHandler = function (e) {
            const w = window.innerWidth;
            const h = window.innerHeight;
            if (!w || !h) return;
            const normX = (e.clientX - w / 2) / (w / 2);
            const normY = (e.clientY - h / 2) / (h / 2);

            // Clamped 3D tilt angles between -16deg and 16deg
            const rotX = Math.max(Math.min(-normY * 16, 16), -16);
            const rotY = Math.max(Math.min(normX * 16, 16), -16);

            container.style.setProperty('--rot-x', `${rotX.toFixed(2)}deg`);
            container.style.setProperty('--rot-y', `${rotY.toFixed(2)}deg`);
            container.style.setProperty('--mouse-x', `${(normX * 36).toFixed(2)}px`);
        };

        window.addEventListener('mousemove', window._homeMouseMoveHandler, { passive: true });
    };

    window.disposeHomeGlobalTilt = function () {
        if (window._homeMouseMoveHandler) {
            window.removeEventListener('mousemove', window._homeMouseMoveHandler);
            window._homeMouseMoveHandler = null;
        }
    };
})();
