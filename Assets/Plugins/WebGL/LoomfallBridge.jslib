// Мост к площадкам: VK Bridge и Яндекс Игры, сохранение прогресса, реклама.
mergeInto(LibraryManager.library, {
  LoomfallInit: function () {
    var ready = function (name) { console.log('[Loomfall] площадка готова: ' + name); };
    try {
      if (typeof window.vkBridge !== 'undefined') {
        window.vkBridge.send('VKWebAppInit', {}).then(function () { ready('VK'); })
          .catch(function (e) { console.warn('[Loomfall] VK: ' + e); });
      } else if (typeof window.YaGames !== 'undefined') {
        window.YaGames.init().then(function (sdk) { window.loomfallSdk = sdk; ready('Яндекс'); })
          .catch(function (e) { console.warn('[Loomfall] Яндекс: ' + e); });
      } else {
        ready('локально');
      }
    } catch (e) {
      console.warn('[Loomfall] площадка недоступна: ' + e);
    }
  },

  LoomfallStorageGet: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    var value = '';
    try { value = window.localStorage.getItem('loomfall.' + key) || ''; } catch (e) { }
    var size = lengthBytesUTF8(value) + 1;
    var buffer = _malloc(size);
    stringToUTF8(value, buffer, size);
    return buffer;
  },

  LoomfallStorageSet: function (keyPtr, valuePtr) {
    var key = UTF8ToString(keyPtr);
    var value = UTF8ToString(valuePtr);
    try { window.localStorage.setItem('loomfall.' + key, value); } catch (e) { }
    try {
      if (typeof window.vkBridge !== 'undefined') {
        window.vkBridge.send('VKWebAppStorageSet', { key: key, value: value }).catch(function () { });
      }
    } catch (e) { }
  },

  LoomfallFree: function (pointer) {
    _free(pointer);
  },

  LoomfallShowAd: function () {
    try {
      if (typeof window.vkBridge !== 'undefined') {
        window.vkBridge.send('VKWebAppShowNativeAds', { ad_format: 'reward' }).catch(function () { });
      } else if (window.loomfallSdk && window.loomfallSdk.adv) {
        window.loomfallSdk.adv.showFullscreenAdv();
      }
    } catch (e) {
      console.warn('[Loomfall] реклама недоступна: ' + e);
    }
  }
});
