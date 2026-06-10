(() => {
    const header = document.getElementById("siteHeader");
    const mobileButton = document.getElementById("mobileMenuButton");
    const mobileClose = document.getElementById("mobileMenuClose");
    const mobileMenu = document.getElementById("mobileMenu");
    const mobileOverlay = document.getElementById("mobileOverlay");

    const setHeaderState = () => {
        if (!header) {
            return;
        }

        header.classList.toggle("is-scrolled", window.scrollY > 24);
    };

    const openMenu = () => {
        mobileOverlay?.classList.remove("hidden");
        requestAnimationFrame(() => {
            mobileOverlay?.classList.add("is-open");
            mobileMenu?.classList.add("is-open");
        });
        mobileButton?.setAttribute("aria-expanded", "true");
        mobileMenu?.setAttribute("aria-hidden", "false");
        document.body.style.overflow = "hidden";
    };

    const closeMenu = () => {
        mobileOverlay?.classList.remove("is-open");
        mobileMenu?.classList.remove("is-open");
        mobileButton?.setAttribute("aria-expanded", "false");
        mobileMenu?.setAttribute("aria-hidden", "true");
        document.body.style.overflow = "";

        window.setTimeout(() => {
            if (!mobileOverlay?.classList.contains("is-open")) {
                mobileOverlay?.classList.add("hidden");
            }
        }, 300);
    };

    const initTabs = () => {
        const buttons = Array.from(document.querySelectorAll("[data-tab-target]"));
        const panels = Array.from(document.querySelectorAll("[data-tab-panel]"));

        buttons.forEach((button) => {
            button.addEventListener("click", () => {
                const target = button.getAttribute("data-tab-target");

                buttons.forEach((item) => {
                    const isActive = item === button;
                    item.classList.toggle("is-active", isActive);
                    item.setAttribute("aria-selected", String(isActive));
                });

                panels.forEach((panel) => {
                    panel.classList.toggle("is-active", panel.getAttribute("data-tab-panel") === target);
                });

                window.lucide?.createIcons();
            });
        });
    };

    const animateValue = (element, target, duration = 1400) => {
        const start = performance.now();

        const tick = (now) => {
            const progress = Math.min((now - start) / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3);
            element.textContent = Math.round(target * eased).toLocaleString("fr-FR");

            if (progress < 1) {
                requestAnimationFrame(tick);
            }
        };

        requestAnimationFrame(tick);
    };

    const initCountUp = () => {
        const root = document.querySelector("[data-countup-root]");
        const counters = Array.from(document.querySelectorAll("[data-countup]"));

        if (!root || counters.length === 0) {
            return;
        }

        const observer = new IntersectionObserver(
            (entries, activeObserver) => {
                if (!entries.some((entry) => entry.isIntersecting)) {
                    return;
                }

                counters.forEach((counter) => {
                    const target = Number(counter.getAttribute("data-countup"));
                    animateValue(counter, target);
                });
                activeObserver.disconnect();
            },
            { threshold: 0.35 }
        );

        observer.observe(root);
    };

    const initPartnerLogos = () => {
        document.querySelectorAll("[data-logo-file]").forEach((card) => {
            const file = card.getAttribute("data-logo-file");
            const label = card.textContent.trim();

            if (!file) {
                return;
            }

            const image = new Image();
            image.onload = () => {
                image.alt = label;
                image.loading = "lazy";
                card.replaceChildren(image);
            };
            image.src = `/${file}`;
        });
    };

    setHeaderState();
    window.addEventListener("scroll", setHeaderState, { passive: true });
    mobileButton?.addEventListener("click", openMenu);
    mobileClose?.addEventListener("click", closeMenu);
    mobileOverlay?.addEventListener("click", closeMenu);
    mobileMenu?.querySelectorAll("a").forEach((link) => link.addEventListener("click", closeMenu));
    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            closeMenu();
        }
    });

    initTabs();
    initCountUp();
    initPartnerLogos();
    window.lucide?.createIcons();
})();
