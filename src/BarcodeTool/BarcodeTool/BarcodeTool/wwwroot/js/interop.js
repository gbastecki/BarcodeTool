const blobUrls = new Map();
const dropzoneListeners = new Map();

export function createBlobUrl(elementId, bytes, mimeType) {
    if (blobUrls.has(elementId)) {
        URL.revokeObjectURL(blobUrls.get(elementId));
    }

    const blob = new Blob([bytes], { type: mimeType });
    const url = URL.createObjectURL(blob);
    blobUrls.set(elementId, url);

    const element = document.getElementById(elementId);
    if (element) {
        element.src = url;
    }
    return url;
}

export function revokeBlobUrl(elementId) {
    if (blobUrls.has(elementId)) {
        URL.revokeObjectURL(blobUrls.get(elementId));
        blobUrls.delete(elementId);
    }
}

export function revokeAllBlobUrls() {
    blobUrls.forEach((url) => URL.revokeObjectURL(url));
    blobUrls.clear();
}

export function downloadFileFromBytes(fileName, bytes, mimeType) {
    const blob = new Blob([bytes], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.download = fileName;
    link.href = url;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}

export function initializeDropzone(dropzoneId, inputId) {
    const dropzone = document.getElementById(dropzoneId);
    const input = document.getElementById(inputId);

    if (!dropzone || !input) {
        console.warn('Dropzone or input element not found:', dropzoneId, inputId);
        return false;
    }

    const preventDefaults = (e) => {
        e.preventDefault();
        e.stopPropagation();
    };

    const highlight = () => {
        dropzone.classList.add('dropzone-active');
    };

    const unhighlight = () => {
        dropzone.classList.remove('dropzone-active');
    };

    const handleDrop = (e) => {
        preventDefaults(e);
        unhighlight();

        const files = e.dataTransfer?.files;
        if (files && files.length > 0) {
            const dataTransfer = new DataTransfer();
            for (const file of files) {
                if (file.type.startsWith('image/') || file.name.toLowerCase().endsWith('.svg')) {
                    dataTransfer.items.add(file);
                }
            }

            if (dataTransfer.files.length > 0) {
                input.files = dataTransfer.files;
                input.dispatchEvent(new Event('change', { bubbles: true }));
            }
        }
    };

    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropzone.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    ['dragenter', 'dragover'].forEach(eventName => {
        dropzone.addEventListener(eventName, highlight, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropzone.addEventListener(eventName, unhighlight, false);
    });

    dropzone.addEventListener('drop', handleDrop, false);

    dropzoneListeners.set(dropzoneId, {
        preventDefaults,
        highlight,
        unhighlight,
        handleDrop
    });

    return true;
}

export function disposeDropzone(dropzoneId) {
    const listeners = dropzoneListeners.get(dropzoneId);
    if (!listeners) return;

    const dropzone = document.getElementById(dropzoneId);
    if (dropzone) {
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            dropzone.removeEventListener(eventName, listeners.preventDefaults, false);
        });
        ['dragenter', 'dragover'].forEach(eventName => {
            dropzone.removeEventListener(eventName, listeners.highlight, false);
        });
        ['dragleave', 'drop'].forEach(eventName => {
            dropzone.removeEventListener(eventName, listeners.unhighlight, false);
        });
        dropzone.removeEventListener('drop', listeners.handleDrop, false);
    }

    dropzoneListeners.delete(dropzoneId);
}

