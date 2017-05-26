var htmlElement = document.documentElement;
htmlElement.setAttribute('data-useragent', navigator.userAgent);

var versionOfInternetExplorer = +(/MSIE\s(\d+)/.exec(navigator.userAgent) || 0)[1];
if (!isNaN(versionOfInternetExplorer) && versionOfInternetExplorer < 11) {
    document.location = "browsernotsupported.html";
}
var versionOfFirefox = +(/Firefox\/(\d+)/.exec(navigator.userAgent) || 0)[1];
if (!isNaN(versionOfFirefox) && versionOfFirefox < 16) {
    document.location = "browsernotsupported.html";
}
