/* ============================================================
   Widget d'accessibilité — Vigie24
   Préférences : taille du texte, thème/contraste, hauteur de ligne,
   espacement des textes, mode dyslexie, masquage des images,
   saturation, liens soulignés, animations réduites, curseur agrandi.
   Stockage : localStorage["a11y.prefs"]. Synchronisé avec les
   attributs du site (data-font / data-theme sur <html>).
   API : window.A11yPrefs.{ mount, get, set, reset, open, close, subscribe }
   ============================================================ */
(function () {
    'use strict';

    var KEY = 'a11y.prefs';
    var VERSION = '2.0';
    var SIZES = ['50', '75', '100', '125', '150'];
    var THEMES = ['auto', 'light', 'dark', 'light-hc', 'dark-hc'];
    var LEVELS = { lh: ['0', '1', '2'], space: ['0', '1', '2'] };
    var FLAGS = ['dys', 'noimg', 'links', 'nomotion', 'cursor'];

    var DEFAULTS = {
        v: VERSION, size: '100', theme: 'auto',
        lh: '0', space: '0', sat: 100,
        dys: false, noimg: false, links: false, nomotion: false, cursor: false
    };

    var LABELS = {
        size: { '50': 'très petit', '75': 'petit', '100': 'normal', '125': 'grand', '150': 'très grand' },
        theme: { auto: 'automatique', light: 'clair', dark: 'sombre', 'light-hc': 'clair renforcé', 'dark-hc': 'sombre renforcé' },
        lh: { '0': 'valeur du site', '1': 'aérée', '2': 'très aérée' },
        space: { '0': 'normal', '1': 'moyen', '2': 'large' },
        dys: 'mode dyslexie', noimg: 'images masquées', links: 'liens soulignés',
        nomotion: 'animations réduites', cursor: 'curseur agrandi'
    };

    var state = null;
    var listeners = [];
    var root = null;
    var panel = null;
    var trigger = null;
    var statusEl = null;

    function media(q) {
        try { return window.matchMedia(q).matches; } catch (e) { return false; }
    }

    function sysPrefs() {
        return {
            dark: media('(prefers-color-scheme: dark)'),
            reduce: media('(prefers-reduced-motion: reduce)'),
            contrast: media('(prefers-contrast: more)')
        };
    }

    function load() {
        try {
            var raw = localStorage.getItem(KEY);
            if (raw) return JSON.parse(raw);
        } catch (e) { /* stockage indisponible : préférences par défaut */ }
        return null;
    }

    function level(value, allowed, fallback) {
        return allowed.indexOf(value) >= 0 ? value : fallback;
    }

    function sanitize(raw) {
        var p = {
            v: VERSION, size: DEFAULTS.size, theme: DEFAULTS.theme,
            lh: DEFAULTS.lh, space: DEFAULTS.space, sat: DEFAULTS.sat,
            dys: false, noimg: false, links: false, nomotion: false, cursor: false
        };
        if (!raw || typeof raw !== 'object') {
            var html = document.documentElement;
            p.size = level(html.getAttribute('data-font'), SIZES, DEFAULTS.size);
            p.theme = level(html.getAttribute('data-theme'), THEMES, DEFAULTS.theme);
            return p;
        }
        p.size = level(raw.size, SIZES, DEFAULTS.size);
        p.theme = level(raw.theme, THEMES, DEFAULTS.theme);
        p.lh = level(raw.lh, LEVELS.lh, DEFAULTS.lh);
        p.space = level(raw.space, LEVELS.space, DEFAULTS.space);
        if (typeof raw.sat === 'number' && raw.sat >= 0 && raw.sat <= 100) p.sat = Math.round(raw.sat);
        for (var i = 0; i < FLAGS.length; i++) {
            if (typeof raw[FLAGS[i]] === 'boolean') p[FLAGS[i]] = raw[FLAGS[i]];
        }
        // Migration v1 : l'ancien interrupteur « spacing » devient un niveau.
        if (raw.v === '1.0' && raw.spacing === true) p.space = '1';
        return p;
    }

    function save(p) {
        try { localStorage.setItem(KEY, JSON.stringify(copy(p))); }
        catch (e) { /* mode privé : la préférence vaut pour la session */ }
    }

    function resolvedTheme(p) {
        var s = sysPrefs();
        if (p.theme !== 'auto') return p.theme;
        if (s.dark) return s.contrast ? 'dark-hc' : 'dark';
        return s.contrast ? 'light-hc' : 'light';
    }

    function apply(p) {
        var html = document.documentElement;
        html.setAttribute('data-font', p.size);
        html.setAttribute('data-theme', resolvedTheme(p));

        html.classList.remove('a11y-lh-0', 'a11y-lh-1', 'a11y-lh-2');
        html.classList.remove('a11y-space-0', 'a11y-space-1', 'a11y-space-2');
        if (p.lh !== '0') html.classList.add('a11y-lh-' + p.lh);
        if (p.space !== '0') html.classList.add('a11y-space-' + p.space);

        html.classList.toggle('a11y-dys', !!p.dys);
        html.classList.toggle('a11y-no-img', !!p.noimg);
        html.classList.toggle('a11y-links', !!p.links);
        html.classList.toggle('a11y-cursor', !!p.cursor);
        html.classList.toggle('a11y-no-motion', !!p.nomotion || sysPrefs().reduce);

        // Saturation : filtre global (les éléments fixes restent fixes, vérifié).
        if (p.sat < 100) html.style.filter = 'saturate(' + p.sat + '%)';
        else html.style.removeProperty('filter');

        // Cohérence avec les valeurs du site.
        try {
            localStorage.setItem('vigie24-font', p.size);
            if (p.theme === 'auto') localStorage.removeItem('vigie24-theme');
            else localStorage.setItem('vigie24-theme', p.theme);
        } catch (e) { /* ignore */ }
    }

    function announce(text) {
        if (!statusEl) return;
        statusEl.textContent = '';
        window.setTimeout(function () { statusEl.textContent = text; }, 30);
    }

    function refresh() {
        if (!panel) return;
        var segs = panel.querySelectorAll('[data-a11y-set]');
        for (var i = 0; i < segs.length; i++) {
            var parts = segs[i].getAttribute('data-a11y-set').split(':');
            segs[i].setAttribute('aria-pressed', String(state[parts[0]]) === parts[1] ? 'true' : 'false');
        }
        var toggles = panel.querySelectorAll('[data-a11y-toggle]');
        for (var j = 0; j < toggles.length; j++) {
            var flag = toggles[j].getAttribute('data-a11y-toggle');
            if (toggles[j].checked !== !!state[flag]) toggles[j].checked = !!state[flag];
        }
        var ranges = panel.querySelectorAll('[data-a11y-range]');
        for (var k = 0; k < ranges.length; k++) {
            var name = ranges[k].getAttribute('data-a11y-range');
            if (String(ranges[k].value) !== String(state[name])) ranges[k].value = state[name];
            var out = ranges[k].parentNode.querySelector('.a11y-range-val');
            if (out) out.textContent = state[name] + ' %';
        }
    }

    function notify(source) {
        for (var i = 0; i < listeners.length; i++) {
            try { listeners[i](copy(state), source); } catch (e) { /* un abonné fautif ne bloque rien */ }
        }
    }

    function copy(p) {
        return {
            v: VERSION, size: p.size, theme: p.theme, lh: p.lh, space: p.space, sat: p.sat,
            dys: !!p.dys, noimg: !!p.noimg, links: !!p.links, nomotion: !!p.nomotion, cursor: !!p.cursor
        };
    }

    function describe(key, value) {
        if (key === 'size') return 'Taille du texte : ' + LABELS.size[value];
        if (key === 'theme') return 'Thème : ' + LABELS.theme[value];
        if (key === 'lh') return 'Hauteur de ligne : ' + LABELS.lh[value];
        if (key === 'space') return 'Espacement des textes : ' + LABELS.space[value];
        if (key === 'sat') return 'Saturation des couleurs : ' + value + ' %';
        if (FLAGS.indexOf(key) >= 0) return LABELS[key] + ' : ' + (value ? 'activé' : 'désactivé');
        return '';
    }

    function set(key, value, source) {
        if (!state) state = sanitize(load());
        if (key === 'size' && SIZES.indexOf(value) >= 0) state.size = value;
        else if (key === 'theme' && THEMES.indexOf(value) >= 0) state.theme = value;
        else if (key === 'lh' && LEVELS.lh.indexOf(value) >= 0) state.lh = value;
        else if (key === 'space' && LEVELS.space.indexOf(value) >= 0) state.space = value;
        else if (key === 'sat') {
            var n = parseInt(value, 10);
            if (isNaN(n)) return copy(state);
            state.sat = Math.max(0, Math.min(100, n));
        } else if (FLAGS.indexOf(key) >= 0) state[key] = !!value;
        else return copy(state);

        save(state);
        apply(state);
        refresh();
        announce(describe(key, state[key]));
        notify(source || 'api');
        return copy(state);
    }

    function reset(source) {
        state = {
            v: VERSION, size: '100', theme: 'auto', lh: '0', space: '0', sat: 100,
            dys: false, noimg: false, links: false, nomotion: false, cursor: false
        };
        save(state);
        apply(state);
        refresh();
        announce('Réglages d\'accessibilité réinitialisés');
        notify(source || 'api');
        return copy(state);
    }

    function openPanel() {
        if (!panel || panel.open) return;
        var html = document.documentElement;
        var size = html.getAttribute('data-font');
        if (size && SIZES.indexOf(size) >= 0) state.size = size;
        refresh();
        if (typeof panel.showModal === 'function') panel.showModal();
        else panel.setAttribute('open', '');
        if (trigger) trigger.setAttribute('aria-expanded', 'true');
        var first = panel.querySelector('.a11y-close');
        if (first) first.focus();
        notify('open');
    }

    function closePanel() {
        if (!panel || !panel.open) return;
        if (typeof panel.close === 'function') panel.close();
        else panel.removeAttribute('open');
        if (trigger) {
            trigger.setAttribute('aria-expanded', 'false');
            trigger.focus();
        }
        notify('close');
    }

    function bind() {
        trigger = root.querySelector('.a11y-trigger');
        panel = root.querySelector('.a11y-panel');
        statusEl = root.querySelector('.a11y-status');
        if (!trigger || !panel) return false;

        trigger.addEventListener('click', function () {
            if (panel.open) closePanel(); else openPanel();
        });

        root.addEventListener('click', function (ev) {
            var t = ev.target.closest ? ev.target.closest('[data-a11y-set]') : null;
            if (t) {
                var parts = t.getAttribute('data-a11y-set').split(':');
                set(parts[0], parts[1], 'panel');
                return;
            }
            if (ev.target.closest && ev.target.closest('.a11y-close')) { closePanel(); return; }
            if (ev.target.closest && ev.target.closest('.a11y-reset')) { reset('panel'); return; }
        });

        root.addEventListener('change', function (ev) {
            var t = ev.target;
            if (!t || !t.getAttribute) return;
            var flag = t.getAttribute('data-a11y-toggle');
            if (flag) set(flag, t.checked, 'panel');
            var range = t.getAttribute('data-a11y-range');
            if (range) set(range, t.value, 'panel');
        });

        // Retour immédiat pendant le glissement du curseur de saturation.
        root.addEventListener('input', function (ev) {
            var t = ev.target;
            if (!t || !t.getAttribute) return;
            var range = t.getAttribute('data-a11y-range');
            if (range) {
                var out = t.parentNode.querySelector('.a11y-range-val');
                if (out) out.textContent = t.value + ' %';
                apply(Object.assign(copy(state), { sat: parseInt(t.value, 10) }));
            }
        });

        // Échap + clic sur le fond : gérés nativement par <dialog>.
        panel.addEventListener('close', function () {
            if (trigger) {
                trigger.setAttribute('aria-expanded', 'false');
                trigger.focus();
            }
        });

        try {
            var mqDark = window.matchMedia('(prefers-color-scheme: dark)');
            var handler = function () { if (state && state.theme === 'auto') apply(state); };
            if (mqDark.addEventListener) mqDark.addEventListener('change', handler);
            else if (mqDark.addListener) mqDark.addListener(handler);
        } catch (e) { /* pas de media query API */ }

        return true;
    }

    function mount(selector) {
        root = document.querySelector(selector || '[data-a11y-root]');
        if (!root || root.getAttribute('data-a11y-mounted') === 'true') return false;
        if (!bind()) return false;
        state = sanitize(load());
        apply(state);
        root.removeAttribute('hidden');
        root.setAttribute('data-a11y-mounted', 'true');
        refresh();
        return true;
    }

    window.A11yPrefs = {
        version: VERSION,
        mount: mount,
        get: function () { return copy(state || sanitize(load())); },
        set: set,
        reset: reset,
        open: openPanel,
        close: closePanel,
        subscribe: function (fn) {
            if (typeof fn !== 'function') return function () {};
            listeners.push(fn);
            return function () {
                var i = listeners.indexOf(fn);
                if (i >= 0) listeners.splice(i, 1);
            };
        }
    };

    function boot() { mount(); }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }
})();
