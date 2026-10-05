/**
 * Droplet Scrollbar - Liquid Water Droplet Interactive Scrollbar
 * Spring-physics simulation with bi-directional fluid deformation and dragging.
 */
(function () {
    const R = 12.5;                // Bán kính tĩnh ban đầu (hình tròn hoàn hảo d = 25px)
    const k = 0.55228475 * R;      // Hằng số Bezier xấp xỉ đường tròn chính xác
    const Lmax = 24.0;             // Độ vươn kéo dãn tối đa khi cuộn nhanh (px)
    const RAIL_PAD = 18;           // Khoảng đệm đầu/cuối của ray trượt (px)

    /**
     * Tạo đường cong Bezier biến dạng chất lỏng 2 chiều (Bidirectional Fluid Morphing)
     * - s = 0: Hình tròn hoàn hảo 100% (trạng thái nghỉ / ban đầu)
     * - s > 0: Cuộn xuống -> quán tính kéo đỉnh nhọn vươn LÊN TRÊN, đáy bầu tròn
     * - s < 0: Cuộn lên -> quán tính kéo đỉnh nhọn vươn XUỐNG DƯỚI, đỉnh bầu tròn
     */
    function getFluidPath(s) {
        if (s >= 0) {
            const t = Math.min(1, s);
            const yTop = -R - t * Lmax;
            const yBottom = R + t * 1.5;
            const ySide = t * 3.5;
            const rx = R * (1 - 0.22 * t);

            const cpTopR = [k * (1 - 0.88 * t), yTop + t * Lmax * 0.38];
            const cpTopL = [-k * (1 - 0.88 * t), yTop + t * Lmax * 0.38];
            const pR = [rx, ySide];
            const cpInR = [R * (1 - 0.05 * t), -k * (1 - 0.45 * t) + ySide * 0.6];
            const cpOutR = [R * (1 - 0.12 * t), k + ySide * 0.8];
            const pB = [0, yBottom];
            const cpBR = [k * (1 - 0.05 * t), yBottom];
            const cpBL = [-k * (1 - 0.05 * t), yBottom];
            const pL = [-rx, ySide];
            const cpInL = [-R * (1 - 0.12 * t), k + ySide * 0.8];
            const cpOutL = [-R * (1 - 0.05 * t), -k * (1 - 0.45 * t) + ySide * 0.6];

            return "M 0 " + yTop.toFixed(2) + " " +
                "C " + cpTopR[0].toFixed(2) + " " + cpTopR[1].toFixed(2) + ", " + cpInR[0].toFixed(2) + " " + cpInR[1].toFixed(2) + ", " + pR[0].toFixed(2) + " " + pR[1].toFixed(2) + " " +
                "C " + cpOutR[0].toFixed(2) + " " + cpOutR[1].toFixed(2) + ", " + cpBR[0].toFixed(2) + " " + cpBR[1].toFixed(2) + ", 0 " + pB[1].toFixed(2) + " " +
                "C " + cpBL[0].toFixed(2) + " " + cpBL[1].toFixed(2) + ", " + cpInL[0].toFixed(2) + " " + cpInL[1].toFixed(2) + ", " + pL[0].toFixed(2) + " " + pL[1].toFixed(2) + " " +
                "C " + cpOutL[0].toFixed(2) + " " + cpOutL[1].toFixed(2) + ", " + cpTopL[0].toFixed(2) + " " + cpTopL[1].toFixed(2) + ", 0 " + yTop.toFixed(2) + " Z";
        } else {
            const t = Math.min(1, -s);
            const yBottom = R + t * Lmax;
            const yTop = -R - t * 1.5;
            const ySide = -t * 3.5;
            const rx = R * (1 - 0.22 * t);

            const cpBR = [k * (1 - 0.88 * t), yBottom - t * Lmax * 0.38];
            const cpBL = [-k * (1 - 0.88 * t), yBottom - t * Lmax * 0.38];
            const pR = [rx, ySide];
            const cpInR = [R * (1 - 0.12 * t), -k + ySide * 0.8];
            const cpOutR = [R * (1 - 0.05 * t), k * (1 - 0.45 * t) + ySide * 0.6];
            const pTop = [0, yTop];
            const cpTopR = [k * (1 - 0.05 * t), yTop];
            const cpTopL = [-k * (1 - 0.05 * t), yTop];
            const pL = [-rx, ySide];
            const cpInL = [-R * (1 - 0.05 * t), k * (1 - 0.45 * t) + ySide * 0.6];
            const cpOutL = [-R * (1 - 0.12 * t), -k + ySide * 0.8];

            return "M 0 " + yTop.toFixed(2) + " " +
                "C " + cpTopR[0].toFixed(2) + " " + cpTopR[1].toFixed(2) + ", " + cpInR[0].toFixed(2) + " " + cpInR[1].toFixed(2) + ", " + pR[0].toFixed(2) + " " + pR[1].toFixed(2) + " " +
                "C " + cpOutR[0].toFixed(2) + " " + cpOutR[1].toFixed(2) + ", " + cpBR[0].toFixed(2) + " " + cpBR[1].toFixed(2) + ", 0 " + yBottom.toFixed(2) + " " +
                "C " + cpBL[0].toFixed(2) + " " + cpBL[1].toFixed(2) + ", " + cpInL[0].toFixed(2) + " " + cpInL[1].toFixed(2) + ", " + pL[0].toFixed(2) + " " + pL[1].toFixed(2) + " " +
                "C " + cpOutL[0].toFixed(2) + " " + cpOutL[1].toFixed(2) + ", " + cpTopL[0].toFixed(2) + " " + cpTopL[1].toFixed(2) + ", 0 " + yTop.toFixed(2) + " Z";
        }
    }

    let _activeInstance = null;

    window.initDropletScrollbar = function (wrapEl) {
        if (!wrapEl && !(wrapEl = document.getElementById("dropletScrollWrap"))) {
            return;
        }

        if (_activeInstance) {
            _activeInstance.destroy();
        }

        document.documentElement.classList.add("has-droplet-scrollbar");

        const reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        let y = 0, v = 0, s = 0, sv = 0;
        let last = window.scrollY || document.documentElement.scrollTop || 0;
        let raf = 0;
        let isRunning = false;
        let dragging = false;
        let lastPointerY = 0;
        let dragVelocity = 0;
        let hover = false;
        let idle = 0;

        const el = wrapEl;
        const d = el.querySelector(".drop-scroll__drop") || document.getElementById("dropletScrollDrop");
        const p = el.querySelector(".drop-path") || document.getElementById("dropletScrollPath");
        const caustic = el.querySelector(".drop-caustic") || document.getElementById("dropletScrollCaustic");
        const specular = el.querySelector(".drop-specular") || document.getElementById("dropletSpecular");
        const bottomGlint = el.querySelector(".drop-bottom-glint") || document.getElementById("dropletBottomGlint");

        if (!d || !p) return;

        // Đặt hình dáng ban đầu: hình tròn hoàn hảo
        const initialCirclePath = getFluidPath(0);
        p.setAttribute("d", initialCirclePath);
        if (caustic) caustic.setAttribute("d", initialCirclePath);

        const maxScroll = () => Math.max(0, document.documentElement.scrollHeight - window.innerHeight);
        const travel = () => Math.max(0, el.clientHeight - RAIL_PAD * 2);

        const show = () => {
            const m = maxScroll();
            if (m <= 5) {
                el.style.opacity = "0";
                el.style.pointerEvents = "none";
                return;
            }
            el.style.opacity = "1";
            el.style.pointerEvents = "auto";
            startLoop();
            clearTimeout(idle);
            idle = window.setTimeout(() => {
                if (!hover && !dragging) {
                    el.style.opacity = "0.78";
                }
            }, 1500);
        };

        const tick = () => {
            const m = maxScroll();
            const tr = travel();

            if (m <= 5 || tr <= 0) {
                el.style.opacity = "0";
                el.style.pointerEvents = "none";
                isRunning = false;
                return;
            }

            const currentScroll = window.scrollY || document.documentElement.scrollTop || 0;
            const target = (currentScroll / m) * tr;

            // Vật lý lò xo vị trí giọt nước
            v = (v + (target - y) * 0.18) * 0.72;
            y += v;

            // Tốc độ cuộn tính theo độ biến thiên trong frame
            let speed = currentScroll - last;
            last = currentScroll;

            if (dragging) {
                speed = dragVelocity * 32;
                dragVelocity *= 0.65;
            }

            // Mục tiêu kéo dãn (clamped trong khoảng -1 đến 1)
            const goal = reduce ? 0 : Math.max(-1, Math.min(1, speed / 26));

            // Lò xo đàn hồi co giãn (Spring damping with jiggle)
            sv = (sv + (goal - s) * 0.24) * 0.76;
            s += sv;

            // Cập nhật đường cong SVG giọt nước
            const pathD = getFluidPath(s);
            p.setAttribute("d", pathD);
            if (caustic) caustic.setAttribute("d", pathD);

            // Tinh chỉnh phản xạ quang học theo độ dãn
            if (specular) {
                specular.setAttribute("transform", "translate(0, " + (s * 2.2).toFixed(1) + ") rotate(-26 -3.6 -3.6)");
            }
            if (bottomGlint) {
                bottomGlint.setAttribute("transform", "translate(0, " + (s * 1.6).toFixed(1) + ")");
            }

            // Đặt vị trí giọt nước trên rãnh trượt
            const kScale = hover || dragging ? 1.12 : 1.0;
            d.style.transform = "translate3d(0, " + (RAIL_PAD + y).toFixed(2) + "px, 0) scale(" + kScale.toFixed(3) + ")";

            // Kiểm tra trạng thái dừng (settle)
            const isSettled = Math.abs(target - y) < 0.05 &&
                Math.abs(v) < 0.03 &&
                Math.abs(s) < 0.005 &&
                Math.abs(sv) < 0.005;

            if (isSettled && !dragging && !hover) {
                // Khi dừng hẳn, chắc chắn trở về hình tròn tĩnh lặng hoàn hảo
                if (s !== 0) {
                    s = 0;
                    sv = 0;
                    const finalCircle = getFluidPath(0);
                    p.setAttribute("d", finalCircle);
                    if (caustic) caustic.setAttribute("d", finalCircle);
                    if (specular) specular.setAttribute("transform", "rotate(-26 -3.6 -3.6)");
                    if (bottomGlint) bottomGlint.setAttribute("transform", "");
                }
                isRunning = false;
                return;
            }

            raf = requestAnimationFrame(tick);
        };

        const startLoop = () => {
            if (!isRunning) {
                isRunning = true;
                raf = requestAnimationFrame(tick);
            }
        };

        const scrollToPointer = (clientY) => {
            const m = maxScroll();
            const tr = travel();
            if (m <= 0 || tr <= 0) return;
            const r = el.getBoundingClientRect();
            const relativeY = clientY - r.top - RAIL_PAD;
            const ratio = relativeY / tr;
            const clamped = Math.max(0, Math.min(1, ratio));
            window.scrollTo({ top: clamped * m, behavior: "auto" });
        };

        const onDown = (e) => {
            dragging = true;
            el.classList.add("is-dragging");
            lastPointerY = e.clientY;
            dragVelocity = 0;
            try {
                if (el.setPointerCapture) el.setPointerCapture(e.pointerId);
            } catch (err) { }
            scrollToPointer(e.clientY);
            show();
        };

        const onMove = (e) => {
            if (dragging) {
                const dy = e.clientY - lastPointerY;
                lastPointerY = e.clientY;
                dragVelocity = Math.max(-1, Math.min(1, dy / 16));
                scrollToPointer(e.clientY);
                show();
            }
        };

        const onUp = (e) => {
            if (dragging) {
                dragging = false;
                dragVelocity = 0;
                el.classList.remove("is-dragging");
                try {
                    if (el.hasPointerCapture && el.hasPointerCapture(e.pointerId)) {
                        el.releasePointerCapture(e.pointerId);
                    }
                } catch (err) { }
                show();
            }
        };

        const onEnter = () => {
            hover = true;
            show();
        };

        const onLeave = () => {
            hover = false;
            show();
        };

        const onWindowScroll = () => {
            show();
        };

        const onWindowResize = () => {
            show();
        };

        window.addEventListener("scroll", onWindowScroll, { passive: true });
        window.addEventListener("resize", onWindowResize, { passive: true });
        window.addEventListener("pointerup", onUp, { passive: true });

        el.addEventListener("pointerdown", onDown);
        el.addEventListener("pointermove", onMove);
        el.addEventListener("pointerup", onUp);
        el.addEventListener("pointercancel", onUp);
        el.addEventListener("pointerenter", onEnter);
        el.addEventListener("pointerleave", onLeave);

        startLoop();
        show();

        _activeInstance = {
            destroy: function () {
                document.documentElement.classList.remove("has-droplet-scrollbar");
                cancelAnimationFrame(raf);
                clearTimeout(idle);
                window.removeEventListener("scroll", onWindowScroll);
                window.removeEventListener("resize", onWindowResize);
                window.removeEventListener("pointerup", onUp);
                el.removeEventListener("pointerdown", onDown);
                el.removeEventListener("pointermove", onMove);
                el.removeEventListener("pointerup", onUp);
                el.removeEventListener("pointercancel", onUp);
                el.removeEventListener("pointerenter", onEnter);
                el.removeEventListener("pointerleave", onLeave);
                _activeInstance = null;
            }
        };
    };

    window.disposeDropletScrollbar = function () {
        if (_activeInstance) {
            _activeInstance.destroy();
        }
    };
})();

