window.breaseFocus = {
    moveTo: function (elementId) {
        try {
            const el = document.getElementById(elementId);
            if (el) {
                el.focus();
                if (typeof el.select === "function") {
                    el.select();
                }
            }
        } catch (e) {
            console.warn("breaseFocus.moveTo failed", e);
        }
    },

    focusElement: function (element) {
        try {
            if (element && typeof element.focus === "function") {
                element.focus();
                if (typeof element.select === "function") {
                    element.select();
                }
            }
        } catch (e) {
            console.warn("breaseFocus.focusElement failed", e);
        }
    }
};