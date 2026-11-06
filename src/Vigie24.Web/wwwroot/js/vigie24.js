// Vigie24 — shared browser helpers.
window.vigie24 = window.vigie24 || {
    // Identité de voteur : cookie `voter_id` créé côté navigateur (lisible par le serveur
    // via HttpContext au prerender, et par le circuit Blazor via ce helper).
    voterId: function () {
        var m = document.cookie.match(/(?:^|;\s*)voter_id=([^;]+)/);
        if (m) return decodeURIComponent(m[1]);
        var bytes = new Uint8Array(16);
        (window.crypto || window.msCrypto).getRandomValues(bytes);
        var id = Array.prototype.map.call(bytes, function (b) {
            return ('0' + b.toString(16)).slice(-2);
        }).join('');
        var secure = location.protocol === 'https:' ? '; Secure' : '';
        document.cookie = 'voter_id=' + id + '; Path=/; Max-Age=31536000; SameSite=Lax' + secure;
        return id;
    },
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
    // Ascenseur vertical (chevron haut, piste + curseur, chevron bas) fixé à droite.
    // Toujours visible : les chevrons s'atténuent quand on est déjà en haut/en bas, et le
    // curseur suit la position de lecture. Pur client, aucun circuit Blazor.
    initScrollFabs: function () {
        var topBtn = document.getElementById('fab-top');
        var bottomBtn = document.getElementById('fab-bottom');
        if (!topBtn || !bottomBtn) return;
        // Blazor remplace les nœuds de la mise en page après l'hydratation : on rebinde
        // sur le nouveau bouton, et on ne double pas les écouteurs sur l'ancien.
        if (topBtn.dataset.v24Rail === '1') return;
        topBtn.dataset.v24Rail = '1';

        var THUMB_H = 16;
        var ticking = false;

        function update() {
            ticking = false;
            var doc = document.documentElement;
            var y = window.scrollY || doc.scrollTop || 0;
            var maxScroll = Math.max(0, doc.scrollHeight - window.innerHeight);
            // Requêtés à chaque passe : Blazor peut remplacer les nœuds de la mise en page.
            var thumb = document.querySelector('.scroll-thumb');
            var track = document.querySelector('.scroll-track');

            topBtn.classList.toggle('is-off', y <= 8);
            bottomBtn.classList.toggle('is-off', y >= maxScroll - 8);

            if (thumb && track) {
                // Piste verticale sur bureau, horizontale sur mobile : on suit le grand axe.
                var horizontal = track.clientWidth > track.clientHeight;
                var thumbSize = horizontal ? (thumb.offsetWidth || THUMB_H) : (thumb.offsetHeight || THUMB_H);
                var span = Math.max(0, (horizontal ? track.clientWidth : track.clientHeight) - thumbSize);
                var ratio = maxScroll > 0 ? Math.min(1, Math.max(0, y / maxScroll)) : 0;
                var pos = Math.round(ratio * span);
                thumb.style.transform = horizontal
                    ? 'translateX(' + pos + 'px)'
                    : 'translateY(' + pos + 'px)';
            }
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
        // Navigation améliorée de Blazor : la page change sans événement scroll.
        document.addEventListener('enhancedload', onScroll);
        update();
    },
    // Partage d'article. Le panneau est rendu côté serveur (liens + QR) : tout ce qui est
    // interactif est ici, en délégation sur document, pour survivre au remplacement du DOM
    // lors des navigations Blazor. Aucun aller-retour circuit dans le geste de clic —
    // indispensable pour que navigator.share reste autorisé sur iOS.
    initShare: function () {
        var MOBILE_MIN_WIDTH = 861;

        function panelOf(el) {
            return el ? el.closest('.share') : null;
        }

        function isMobile() {
            return window.matchMedia('(max-width: ' + (MOBILE_MIN_WIDTH - 1) + 'px)').matches;
        }

        function setOpen(root, open) {
            var panel = root.querySelector('.share-panel');
            var backdrop = root.querySelector('.share-backdrop');
            var trigger = root.querySelector('[data-share-open]');
            if (!panel) return;
            panel.hidden = !open;
            if (backdrop) backdrop.hidden = !open;
            if (trigger) trigger.setAttribute('aria-expanded', open ? 'true' : 'false');
            root.classList.toggle('share-open', open);
            document.body.classList.toggle('share-locked', open && isMobile());
            if (open) {
                var first = panel;
                if (first) first.focus({ preventScroll: true });
            } else if (trigger) {
                trigger.focus({ preventScroll: true });
            }
        }

        function status(root, message) {
            var node = root.querySelector('[data-share-status]');
            if (node) node.textContent = message;
        }

        function nativeShare(payload) {
            if (!navigator.share) return Promise.resolve('unavailable');
            return navigator.share(payload).then(function () {
                return 'shared';
            }).catch(function (err) {
                return err && err.name === 'AbortError' ? 'cancelled' : 'unavailable';
            });
        }

        document.addEventListener('click', function (e) {
            var target = e.target;
            if (!target || !target.closest) return;

            var openBtn = target.closest('[data-share-open]');
            if (openBtn) {
                e.preventDefault();
                var root = panelOf(openBtn);
                if (!root) return;
                // Second clic sur le déclencheur = fermer.
                if (root.classList.contains('share-open')) { setOpen(root, false); return; }
                // Bureau : panneau direct (il contient le QR, absent de la feuille système).
                // Mobile : feuille de partage native d'abord, panneau en repli.
                if (!isMobile()) { setOpen(root, true); return; }
                nativeShare({
                    title: root.getAttribute('data-share-title'),
                    text: root.getAttribute('data-share-text'),
                    url: root.getAttribute('data-share-url')
                }).then(function (result) {
                    if (result === 'unavailable') setOpen(root, true);
                });
                return;
            }

            var closeBtn = target.closest('[data-share-close]');
            if (closeBtn) {
                e.preventDefault();
                setOpen(panelOf(closeBtn), false);
                return;
            }

            var copyBtn = target.closest('[data-share-copy]');
            if (copyBtn) {
                e.preventDefault();
                var copyRoot = panelOf(copyBtn);
                window.vigie24.copyText(copyBtn.getAttribute('data-url') || '').then(function (ok) {
                    status(copyRoot, ok ? 'Lien copié.' : 'Copie impossible : sélectionnez le lien ci-dessus.');
                    if (ok) copyBtn.textContent = 'Copié';
                });
            }
        });

        document.addEventListener('keydown', function (e) {
            if (e.key !== 'Escape') return;
            var open = document.querySelector('.share.share-open');
            if (open) setOpen(open, false);
        });

        // FAB mobile : apparition après ~320 px de défilement, pour ne pas masquer le
        // début de l'article (le bouton en ligne reste la voie principale).
        var ticking = false;
        window.addEventListener('scroll', function () {
            if (ticking) return;
            ticking = true;
            window.requestAnimationFrame(function () {
                ticking = false;
                var fab = document.querySelector('[data-share-fab]');
                if (fab) fab.classList.toggle('visible', window.scrollY > 320);
            });
        }, { passive: true });
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
function bootVigie24() {
    window.vigie24.initScrollFabs();
    window.vigie24.initShare();
    window.vigie24.initAdminGate();
}
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bootVigie24);
} else {
    bootVigie24();
}

// Blazor remplace la mise en page après l'hydratation (et à chaque navigation améliorée) :
// on réinitialise à chaque fois que la barre d'ascenseur réapparaît dans le DOM.
document.addEventListener('enhancedload', bootVigie24);
(function watchRail() {
    if (!document.body || typeof MutationObserver === 'undefined') return;
    var obs = new MutationObserver(function () {
        var top = document.getElementById('fab-top');
        if (top && top.dataset.v24Rail !== '1') {
            window.vigie24.initScrollFabs();
            window.vigie24.initShare();
        }
    });
    obs.observe(document.body, { childList: true, subtree: true });
})();
