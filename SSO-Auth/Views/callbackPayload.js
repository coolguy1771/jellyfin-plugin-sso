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

    var jfCredentials;
    try {
        jfCredentials = JSON.parse(jfCredentialsString);
    } catch (_) {
        return;
    }
    if (typeof jfCredentials !== 'object' || jfCredentials == null ||
        !Array.isArray(jfCredentials['Servers']) || jfCredentials['Servers'].length === 0) {
        return;
    }
    const jfUser = jfCredentials['Servers'][0]['UserId'];
    const jfToken = jfCredentials['Servers'][0]['AccessToken'];

    if (jfUser == null) return;
    if (jfToken == null) return;

    const url = '{{LINK_URL_PREFIX}}' + jfUser;
    const headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'X-Emby-Authorization': 'MediaBrowser Client="' + request.appName + '",Device="' + request.deviceName + '",DeviceId="' + request.deviceId + '",Version="' + request.appVersion + '",Token="' + jfToken + '"'
    };
    try {
        var res = await fetch(url, { method: 'POST', headers: headers, body: JSON.stringify(request) });
        if (!res.ok) return false;
        return await res.text();
    } catch (_) {
        return false;
    }
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
    var appVersion = "{{APP_VERSION}}";
    var deviceName = getDeviceName();

    var request = {deviceId, appName, appVersion, deviceName, data};

    var IS_LINKING_FLAG = {{IS_LINKING}};
    if (typeof IS_LINKING_FLAG === 'boolean' && IS_LINKING_FLAG) {
        var linkResult = await link(request);
        if (linkResult === false) {
            showLoginError();
            return;
        }
    }

    var url = '{{AUTH_URL}}';

    var response;
    try {
        var res = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
            body: JSON.stringify(request)
        });
        if (!res.ok) { showLoginError(); return; }
        response = await res.text();
    } catch (_) {
        showLoginError();
        return;
    }
    if (typeof response !== 'string' || response.length === 0) {
        showLoginError();
        return;
    }
    var responseJson;
    try {
        responseJson = JSON.parse(response);
    } catch (_) {
        showLoginError();
        return;
    }
    if (responseJson == null || responseJson['User'] == null ||
        responseJson['User']['Id'] == null || responseJson['User']['ServerId'] == null) {
        showLoginError();
        return;
    }
    var userId = 'user-' + responseJson['User']['Id'] + '-' + responseJson['User']['ServerId'];
    responseJson['User']['EnableAutoLogin'] = true;
    localStorage.setItem(userId, JSON.stringify(responseJson['User']));
    var jfCredsRaw = localStorage.getItem('jellyfin_credentials');
    try {
        var jfCreds = jfCredsRaw != null ? JSON.parse(jfCredsRaw) : {};
    } catch (_) {
        jfCreds = {};
    }
    if (typeof jfCreds !== 'object' || jfCreds == null) jfCreds = {};
    if (!Array.isArray(jfCreds['Servers']) || jfCreds['Servers'].length === 0) {
        jfCreds['Servers'] = [{}];
    }
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
