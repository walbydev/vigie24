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
    // Admin gate (hidden by default): typing "vadmin" anywhere opens the admin
    // token prompt. On success the server issues an HttpOnly session cookie
    // (vigie24_admin, 7 days) which reveals admin actions on /sources — regular
    // visitors never see them. The token itself is never stored client-side.
    initAdminGate: function () {
        var buffer = '';
        document.addEventListener('keydown', function (e) {
            if (e.ctrlKey || e.metaKey || e.altKey) return;
            var target = e.target;
            if (target && (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable)) return;
            buffer = (buffer + (e.key || '')).toLowerCase().slice(-6);
            if (buffer !== 'vadmin') return;
            buffer = '';
            var token = window.prompt("Token d'administration Vigie24 :");
            if (!token) return;
            fetch('/api/admin/verify', {
                method: 'POST',
                headers: { 'X-Vigie24-Admin': token }
            }).then(function (r) {
                if (r.ok) { window.location.reload(); }
                else { window.alert("❌ Token invalide."); }
            }).catch(function () { window.alert("❌ Erreur réseau."); });
        });
    }
};

// Init when the DOM is ready (script is loaded at the end of <body>, but be safe
// if a layout change ever moves it earlier).
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
        window.vigie24.initScrollFabs();
        window.vigie24.initAdminGate();
    });
} else {
    window.vigie24.initScrollFabs();
    window.vigie24.initAdminGate();
}
