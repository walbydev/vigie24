// Vigie24 — helpers navigateur partagés.
window.vigie24 = window.vigie24 || {
    // Copie du texte dans le presse-papiers, avec repli pour les contextes non sécurisés
    // (http:// sur le staging) où navigator.clipboard n'existe pas.
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
    }
};
