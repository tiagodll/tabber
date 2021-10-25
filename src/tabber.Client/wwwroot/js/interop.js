'use strict';

window.initOnKeyDownCallback = (onKeyDown) => {
    window.onkeydown = (ev) => {
        onKeyDown.invokeMethodAsync('Invoke', ev.key);
    };
}

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
    // console.log(JSON.parse(localStorage.getItem(label)))
    return JSON.parse(localStorage.getItem(label));
}
