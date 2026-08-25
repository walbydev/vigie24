// Vigie24 — shared browser helpers.
window.vigie24 = window.vigie24 || {
    // Copies text to the clipboard, with a fallback for non-secure contexts
    // (http:// on staging) where navigator.clipboard does not exist.
    copyText: async function (text) {
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
                return true;
            }
        } catch (e) {
            // repli ci-dessous
        }
        try {
            var ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.focus();
            ta.select();
            var ok = document.execCommand('copy');
            document.body.removeChild(ta);
            return ok;
        } catch (e) {
            return false;
        }
    },
    // Floating scroll buttons (top/bottom of page), right side of the content.
    // Pure client-side: no Blazor circuit needed, works during prerender too.
    initScrollFabs: function () {
        var topBtn = document.getElementById('fab-top');
        var bottomBtn = document.getElementById('fab-bottom');
        if (!topBtn || !bottomBtn) return;

        var ticking = false;
        function update() {
            ticking = false;
            var y = window.scrollY || document.documentElement.scrollTop;
            var maxScroll = document.documentElement.scrollHeight - window.innerHeight;
            topBtn.classList.toggle('visible', y > 400);
            bottomBtn.classList.toggle('visible', maxScroll > 150 && y < maxScroll - 150);
        }
        function onScroll() {
            if (!ticking) { ticking = true; requestAnimationFrame(update); }
        }

        var reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        var behavior = reduce ? 'auto' : 'smooth';

        topBtn.addEventListener('click', function (e) {
            e.preventDefault();
            window.scrollTo({ top: 0, behavior: behavior });
        });
        bottomBtn.addEventListener('click', function (e) {
            e.preventDefault();
            window.scrollTo({ top: document.documentElement.scrollHeight, behavior: behavior });
        });
        window.addEventListener('scroll', onScroll, { passive: true });
        window.addEventListener('resize', onScroll);
        update();
    },
    // Vertical article timeline (drag-to-navigate). Pure client-side during the
    // drag; one interop call on pointerup. DotNet reference from the component.
    timelineDragging: false,
    initTimeline: function (dotNetRef, totalPages, pageSize, totalCount, dates, sortField) {
        var track = document.getElementById('tl-track');
        var thumb = document.getElementById('tl-thumb');
        var tooltip = document.getElementById('tl-tooltip');
        if (!track || !thumb) return;

        var pageAt = function (clientY) {
            var rect = track.getBoundingClientRect();
            var frac = Math.min(1, Math.max(0, (clientY - rect.top) / rect.height));
            return Math.floor(frac * totalPages) + 1;
        };
        var labelFor = function (page) {
            if (sortField === 'date' || sortField === 'dateAsc') {
                var idx = Math.min(dates.length - 1, Math.max(0, (page - 1) * pageSize));
                var d = dates[idx];
                return d ? '📅 ' + d : 'Page ' + page + '/' + totalPages;
            }
            return 'Page ' + page + '/' + totalPages;
        };
        var moveThumb = function (page) {
            var pct = totalPages <= 1 ? 0 : (page - 1) * 100 / totalPages;
            thumb.style.top = pct + '%';
            if (tooltip) { tooltip.textContent = labelFor(page); tooltip.style.opacity = '1'; }
        };
        var jump = function (page) {
            page = Math.min(totalPages, Math.max(1, page));
            moveThumb(page);
            window.vigie24.timelineDragging = false;
            if (tooltip) {
                var tip = tooltip;
                setTimeout(function () {
                    if (!window.vigie24.timelineDragging) tip.style.opacity = '0';
                }, 1500);
            }
            if (dotNetRef) dotNetRef.invokeMethodAsync('OnJump', page);
        };

        track.addEventListener('pointerdown', function (e) {
            if (e.target === thumb) return;
            e.preventDefault();
            jump(pageAt(e.clientY));
        });
        thumb.addEventListener('pointerdown', function (e) {
            e.preventDefault();
            window.vigie24.timelineDragging = true;
            thumb.setPointerCapture(e.pointerId);
            var onMove = function (ev) {
                if (!window.vigie24.timelineDragging) return;
                moveThumb(pageAt(ev.clientY));
                ev.preventDefault();
            };
            var onUp = function (ev) {
                thumb.releasePointerCapture(ev.pointerId);
                thumb.removeEventListener('pointermove', onMove);
                thumb.removeEventListener('pointerup', onUp);
                thumb.removeEventListener('pointercancel', onUp);
                if (window.vigie24.timelineDragging) jump(pageAt(ev.clientY));
            };
            thumb.addEventListener('pointermove', onMove);
            thumb.addEventListener('pointerup', onUp);
            thumb.addEventListener('pointercancel', onUp);
        });
        thumb.addEventListener('keydown', function (e) {
            var page = parseInt(thumb.getAttribute('aria-valuenow'), 10) || 1;
            if (e.key === 'ArrowRight' || e.key === 'ArrowDown') { e.preventDefault(); jump(page + 1); }
            else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') { e.preventDefault(); jump(page - 1); }
            else if (e.key === 'Home') { e.preventDefault(); jump(1); }
            else if (e.key === 'End') { e.preventDefault(); jump(totalPages); }
        });
    },
    updateTimeline: function (currentPage, totalPages) {
        var thumb = document.getElementById('tl-thumb');
        var tooltip = document.getElementById('tl-tooltip');
        if (!thumb) return;
        thumb.setAttribute('aria-valuenow', String(currentPage));
        thumb.setAttribute('aria-valuemax', String(totalPages));
        if (!window.vigie24.timelineDragging) {
            var pct = totalPages <= 1 ? 0 : (currentPage - 1) * 100 / totalPages;
            thumb.style.top = pct + '%';
            if (tooltip) tooltip.style.opacity = '0';
        }
    }
};

// Init when the DOM is ready (script is loaded at the end of <body>, but be safe
// if a layout change ever moves it earlier).
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () { window.vigie24.initScrollFabs(); });
} else {
    window.vigie24.initScrollFabs();
}
