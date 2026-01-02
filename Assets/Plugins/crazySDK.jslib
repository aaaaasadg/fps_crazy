mergeInto(LibraryManager.library, {
  InitSDK: function () {
    if (window.CrazyGamesScriptLoading) return;
    window.CrazyGamesScriptLoading = true;

    // Safety: ensure SDK is reported as failed if something goes wrong
    var failTimeout = setTimeout(function() {
        if (!window.CrazyGamesInitialized) {
            console.error("[CrazyGames] SDK Init Timeout");
            SendMessage('CrazyGamesHandler', 'OnSDKInitError', 'Timeout');
        }
    }, 10000);

    var script = document.createElement('script');
    script.src = 'https://sdk.crazygames.com/crazygames-sdk-v3.js';
    script.onload = function() {
        if (!window.CrazyGames || !window.CrazyGames.SDK) {
            SendMessage('CrazyGamesHandler', 'OnSDKInitError', 'SDK Object Missing');
            return;
        }
        window.CrazyGames.SDK.init().then(function() {
            window.CrazyGamesInitialized = true;
            clearTimeout(failTimeout);
            console.log("[CrazyGames] SDK Initialized Successfully");
            
            // Initial Adblock check
            window.CrazyGames.SDK.ad.hasAdblock().then(function(result) {
                SendMessage('CrazyGamesHandler', 'OnAdblockDetected', result ? 1 : 0);
            });

            // Auth Listener
            window.CrazyGames.SDK.user.addAuthListener(function(user) {
                SendMessage('CrazyGamesHandler', 'OnAuthListener', JSON.stringify(user));
            });

            SendMessage('CrazyGamesHandler', 'OnSDKInitSuccess');
        }).catch(function(e) {
            console.error("[CrazyGames] SDK Init Promise Rejected:", e);
            SendMessage('CrazyGamesHandler', 'OnSDKInitError', e.message || 'Unknown');
        });
    };
    script.onerror = function() {
        SendMessage('CrazyGamesHandler', 'OnSDKInitError', 'Network Error');
    };
    document.head.appendChild(script);
  },

  RequestAdSDK: function (adType) {
    if (!window.CrazyGamesInitialized) return;
    var type = UTF8ToString(adType);
    window.CrazyGames.SDK.ad.requestAd(type, {
        adStarted: function() { SendMessage('CrazyGamesHandler', 'OnAdStarted'); },
        adFinished: function() { SendMessage('CrazyGamesHandler', 'OnAdFinished'); },
        adError: function(e) { SendMessage('CrazyGamesHandler', 'OnAdError', JSON.stringify(e)); },
    });
  },

  PrefetchAdSDK: function (adType) {
    if (!window.CrazyGamesInitialized) return;
    try { window.CrazyGames.SDK.ad.prefetchAd(UTF8ToString(adType)); } catch (e) {}
  },

  RequestBannersSDK: function (bannersJSON) {
    if (!window.CrazyGamesInitialized) return;
    try {
        var banners = JSON.parse(UTF8ToString(bannersJSON));
        window.CrazyGames.SDK.banner.requestOverlayBanners(banners);
    } catch (e) {}
  },

  HappyTimeSDK: function () { 
    if (window.CrazyGamesInitialized) window.CrazyGames.SDK.game.happytime(); 
  },
  GameplayStartSDK: function () { 
    if (window.CrazyGamesInitialized) window.CrazyGames.SDK.game.gameplayStart(); 
  },
  GameplayStopSDK: function () { 
    if (window.CrazyGamesInitialized) window.CrazyGames.SDK.game.gameplayStop(); 
  },
  LoadingStartSDK: function () { 
    if (window.CrazyGamesInitialized) window.CrazyGames.SDK.game.loadingStart(); 
  },
  LoadingStopSDK: function () { 
    if (window.CrazyGamesInitialized) window.CrazyGames.SDK.game.loadingStop(); 
  },

  AddScoreSDK: function (score) { 
    if (window.CrazyGamesInitialized) window.CrazyGames.SDK.user.addScore(score); 
  },

  GetUserSDK: function () {
    if (!window.CrazyGamesInitialized) return;
    window.CrazyGames.SDK.user.getUser().then(function(u) {
        SendMessage('CrazyGamesHandler', 'OnGetUser', JSON.stringify(u));
    }).catch(function(e) {
        SendMessage('CrazyGamesHandler', 'OnGetUserError', JSON.stringify(e));
    });
  },

  ShowAuthPromptSDK: function () {
    if (!window.CrazyGamesInitialized) return;
    window.CrazyGames.SDK.user.showAuthPrompt().then(function(u) {
        SendMessage('CrazyGamesHandler', 'OnShowAuthPrompt', JSON.stringify(u));
    }).catch(function(e) {
        SendMessage('CrazyGamesHandler', 'OnShowAuthPromptError', JSON.stringify(e));
    });
  },

  IsUserAccountAvailableSDK: function () { 
    return (window.CrazyGamesInitialized && window.CrazyGames.SDK.user.isUserAccountAvailable) ? 1 : 0; 
  },

  DataSetItemSDK: function (k, v) { 
    if (!window.CrazyGamesInitialized) return;
    window.CrazyGames.SDK.data.setItem(UTF8ToString(k), UTF8ToString(v));
  },

  DataGetItemSDK: function (k) {
    if (!window.CrazyGamesInitialized) return "";
    var val = window.CrazyGames.SDK.data.getItem(UTF8ToString(k)) || "";
    var bufferSize = lengthBytesUTF8(val) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(val, buffer, bufferSize);
    return buffer;
  },

  SyncUnityGameDataSDK: function () { 
    if (window.CrazyGamesInitialized) window.CrazyGames.SDK.data.syncUnityGameData(); 
  }
});
