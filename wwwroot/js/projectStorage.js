window.projectStorage = {
    saveCbz: function (fileName, cbzText) {
        localStorage.setItem("cbrease_cbz_filename", fileName || "");
        localStorage.setItem("cbrease_cbz_text", cbzText || "");
    },

    getCbzFileName: function () {
        return localStorage.getItem("cbrease_cbz_filename");
    },

    getCbzText: function () {
        return localStorage.getItem("cbrease_cbz_text");
    },

    clearCbz: function () {
        localStorage.removeItem("cbrease_cbz_filename");
        localStorage.removeItem("cbrease_cbz_text");
    }
};