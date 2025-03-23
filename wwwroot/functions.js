function fullscreen() {
    var el = document.getElementById("esp-canvas")
    el.requestFullscreen()
}

function saveSetting(key, value) {
    localStorage.setItem(key, JSON.stringify(value));
}

function loadSetting(key) {
    return JSON.parse(localStorage.getItem(key));
}