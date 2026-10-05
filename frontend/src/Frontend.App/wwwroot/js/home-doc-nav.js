/**
 * Home Doc Navigation & Scroll Spy
 * Smooth scroll tracking, active section highlight on minimalist TOC.
 */
(function () {
    let _activeDocNav = null;

    window.initHomeDocNav = function (sectionIds) {
        if (_activeDocNav) {
            _activeDocNav.destroy();
        }

        const ids = sectionIds || [
            'homeLandingContainer',
            'homeStatsSection',
            'homeCoreFeatures',
            'homeRoadmapSection',
            'homeReviewsSection',
            'homeFaqSection',
            'homeCtaSection'
        ];

        let ticking = false;

        const updateSpy = () => {
            const scrollY = window.scrollY || document.documentElement.scrollTop || 0;
            const docHeight = Math.max(1, document.documentElement.scrollHeight - window.innerHeight);
            const progress = Math.min(100, Math.max(0, Math.round((scrollY / docHeight) * 100)));

            let activeId = ids[0];
            const offset = Math.max(160, Math.floor(window.innerHeight * 0.35));

            for (let i = 0; i < ids.length; i++) {
                const el = document.getElementById(ids[i]);
                if (el) {
                    const rect = el.getBoundingClientRect();
                    if (rect.top <= offset) {
                        activeId = ids[i];
                    }
                }
            }

            if (progress >= 96 && ids.length > 0) {
                activeId = ids[ids.length - 1];
            }

            const items = document.querySelectorAll('.toc-item, .doc-toc-item');
            items.forEach((item) => {
                const target = item.getAttribute('data-target');
                if (target === activeId) {
                    item.classList.add('active');
                } else {
                    item.classList.remove('active');
                }
            });

            ticking = false;
        };

        const onScroll = () => {
            if (!ticking) {
                requestAnimationFrame(updateSpy);
                ticking = true;
            }
        };

        window.addEventListener('scroll', onScroll, { passive: true });
        window.addEventListener('resize', onScroll, { passive: true });

        setTimeout(updateSpy, 150);

        _activeDocNav = {
            destroy: function () {
                window.removeEventListener('scroll', onScroll);
                window.removeEventListener('resize', onScroll);
                _activeDocNav = null;
            }
        };
    };

    window.disposeHomeDocNav = function () {
        if (_activeDocNav) {
            _activeDocNav.destroy();
        }
    };

    window.docScrollToSection = function (elementId) {
        try {
            const el = document.getElementById(elementId);
            if (el) {
                const navHeight = 72;
                const rect = el.getBoundingClientRect();
                const scrollTop = window.scrollY || document.documentElement.scrollTop;
                const targetY = rect.top + scrollTop - navHeight;
                window.scrollTo({
                    top: Math.max(0, targetY),
                    behavior: 'smooth'
                });
            }
        } catch (e) {
            console.error('docScrollToSection error:', e);
        }
    };
})();
