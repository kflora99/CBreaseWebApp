window.draftStorage = {
    save: function (key, value) {
        localStorage.setItem(key, value);
    },
    load: function (key) {
        return localStorage.getItem(key);
    },
    remove: function (key) {
        localStorage.removeItem(key);
    }
};