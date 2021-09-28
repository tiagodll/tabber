'use strict';

window.SetFocus = (element) => {
    console.log("### SetFocus ###: ")
    console.log(element)
    element.focus();
};

window.SetFocusById = (id) => {
    console.log("### SetFocusById ###: "+id);
    setTimeout(function(){ document.getElementById(id).focus(); }, 100);
};

window.Log = (obj) => {
    console.log(obj);
}

window.ToStorage = (obj) => {
    localStorage.setItem(obj.label, JSON.stringify(obj.value));
}

window.FromStorage = (label) => {
    console.log(JSON.parse(localStorage.getItem(label)))
    return JSON.parse(localStorage.getItem(label));
}

// navigator.webkitPersistentStorage.requestQuota(1024*1024, function() {
//     window.webkitRequestFileSystem(window.PERSISTENT , 1024*1024, SaveToFilesystem);
// })

function SaveToFilesystem(obj) {
    console.log("### Save file 1 ###: " + JSON.stringify(obj.data));
    let data = JSON.stringify(obj.data);
    let fileHandle = getNewFileHandle();
    console.log("### Save file 2 ###: " + fileHandle);
    // verifyPermission(fileHandle, true);
    writeFile(fileHandle, data);

    // obj.root.getFile("TheWebkitSavedFile.txt", {create: true}, function(DatFile) {
    //     DatFile.createWriter(function(DatContent) {
    //         var blob = new Blob([JSON.stringify(obj.data)], {type: "text/plain"});
    //         DatContent.write(blob);
    //     });
    // });
}

function getFileHandle() {
    // For Chrome 86 and later...
    if ('showOpenFilePicker' in window) {
        return window.showOpenFilePicker().then((handles) => handles[0]);
    }
    // For Chrome 85 and earlier...
    return window.chooseFileSystemEntries();
}

function getNewFileHandle() {
    // For Chrome 86 and later...
    if ('showSaveFilePicker' in window) {
        const opts = {
        types: [{
            description: 'Text file',
            accept: {'text/plain': ['.txt']},
        }],
        };
        return window.showSaveFilePicker(opts);
    }
    // For Chrome 85 and earlier...
    const opts = {
        type: 'save-file',
        accepts: [{
        description: 'Text file',
        extensions: ['txt'],
        mimeTypes: ['text/plain'],
        }],
    };
    return window.chooseFileSystemEntries(opts);
}
function readFile(file) {
    // If the new .text() reader is available, use it.
    if (file.text) {
        return file.text();
    }
    // Otherwise use the traditional file reading technique.
    return _readFileLegacy(file);
}

async function writeFile(fileHandle, contents) {
    // Support for Chrome 82 and earlier.
    if (fileHandle.createWriter) {
        const writer = await fileHandle.createWriter();
        await writer.write(0, contents);
        await writer.close();
        return;
    }
    // For Chrome 83 and later.
    const writable = await fileHandle.createWritable();
    await writable.write(contents);
    await writable.close();
}

async function verifyPermission(fileHandle, withWrite) {
    const opts = {};
    if (withWrite) {
        opts.writable = true;
        // For Chrome 86 and later...
        opts.mode = 'readwrite';
    }
    // Check if we already have permission, if so, return true.
    if (await fileHandle.queryPermission(opts) === 'granted') {
        return true;
    }
    // Request permission to the file, if the user grants permission, return true.
    if (await fileHandle.requestPermission(opts) === 'granted') {
        return true;
    }
    // The user did nt grant permission, return false.
    return false;
}