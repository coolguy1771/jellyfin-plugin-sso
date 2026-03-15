var _baseUrl = '{{BASE_URL}}';
function showLoginError() {
    while (document.body.lastChild) {
        document.body.removeChild(document.body.lastChild);
    }
    var p1 = document.createElement('p');
    p1.textContent = 'Login failed. Please try again or contact your administrator.';
    var p2 = document.createElement('p');
    var a = document.createElement('a');
    a.href = _baseUrl + '/web/index.html';
    a.textContent = 'Back to login';
    p2.appendChild(a);
    document.body.appendChild(p1);
    document.body.appendChild(p2);
}

async function link(request) {
    const jfCredentialsString = localStorage.getItem("jellyfin_credentials");

    if (jfCredentialsString == null) return;

    const jfCredentials = JSON.parse(jfCredentialsString);
    const jfUser = jfCredentials['Servers'][0]['UserId'];
    const jfToken = jfCredentials['Servers'][0]['AccessToken'];

    if (jfUser == null) return;
    if (jfToken == null) return;

    const url = '{{LINK_URL_PREFIX}}' + jfUser;

    return new Promise(resolve => {
       var xhr = new XMLHttpRequest();
       xhr.open('POST', url, true);
       xhr.setRequestHeader('Content-Type', 'application/json');
       xhr.setRequestHeader('Accept', 'application/json');
       xhr.setRequestHeader(
           'X-Emby-Authorization',
           "MediaBrowser Client=\"" + request.appName + "\",Device=\"" + request.deviceName + "\",DeviceId=\"" + request.deviceId + "\",Version=\"" + request.appVersion + "\",Token=\"" + jfToken + "\"");
       xhr.onload = function(e) {
         resolve(xhr.response);
       };
       xhr.onerror = function (e) {
         resolve(undefined);
       };
       xhr.send(JSON.stringify(request));
    });
}

async function main() {
    try {
    localStorage.removeItem('jellyfin_credentials');
    document.getElementById('iframe-main').src = '{{BASE_URL}}/web/index.html';

    var data = '{{DATA}}';
    var deadline = Date.now() + 30000;
    while (localStorage.getItem("_deviceId2") == null ||
        localStorage.getItem("jellyfin_credentials") == null) {
        if (Date.now() > deadline) {
            showLoginError();
            return;
        }
        await sleep(100);
    }
    var deviceId = localStorage.getItem("_deviceId2");
    var appName = "Jellyfin Web";
    var appVersion = "10.8.0";
    var deviceName = getDeviceName();

    var request = {deviceId, appName, appVersion, deviceName, data};

    if ({{IS_LINKING}}) await link(request);

    var url = '{{AUTH_URL}}';

    let response = await new Promise(resolve => {
       var xhr = new XMLHttpRequest();
       xhr.open('POST', url, true);
       xhr.setRequestHeader('Content-Type', 'application/json');
       xhr.setRequestHeader('Accept', 'application/json');
       xhr.onload = function(e) {
         resolve(xhr.response);
       };
       xhr.onerror = function () {
         resolve(undefined);
       };
       xhr.send(JSON.stringify(request));
    });
    var responseJson = JSON.parse(response);
    var userId = 'user-' + responseJson['User']['Id'] + '-' + responseJson['User']['ServerId'];
    responseJson['User']['EnableAutoLogin'] = true;
    localStorage.setItem(userId, JSON.stringify(responseJson['User']));
    var jfCreds = JSON.parse(localStorage.getItem('jellyfin_credentials'));
    jfCreds['Servers'][0]['AccessToken'] = responseJson['AccessToken'];
    jfCreds['Servers'][0]['UserId'] = responseJson['User']['Id'];
    localStorage.setItem('jellyfin_credentials', JSON.stringify(jfCreds));
    localStorage.setItem('enableAutoLogin', 'true');
    window.location.replace('{{BASE_URL}}/web/index.html');
    } catch (e) {
        showLoginError();
    }
}

document.addEventListener('DOMContentLoaded', function () {
    main();
});
