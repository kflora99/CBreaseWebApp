window.downloadFileFromText = (fileName, contentType, content) => {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.style.display = "none";

    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);

    URL.revokeObjectURL(url);
};

window.saveFileFromText = async (fileName, contentType, content) => {
    if (!window.showSaveFilePicker) {
        return false;
    }

    try {
        const handle = await window.showSaveFilePicker({
            id: "cbz-export",
            suggestedName: fileName,
            types: [
                {
                    description: "CBZ files",
                    accept: {
                        "text/plain": [".cbz"]
                    }
                }
            ]
        });

        const writable = await handle.createWritable();
        await writable.write(new Blob([content], { type: contentType }));
        await writable.close();
        return true;
    } catch (error) {
        if (error && error.name === "AbortError") {
            return true;
        }

        return false;
    }
};
