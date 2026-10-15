// Мост к площадкам: VK Bridge и Яндекс Игры, сохранение прогресса, вознаграждаемая реклама.
// Ключи, которые игра сохраняет, читаются заранее: у обеих площадок чтение асинхронное,
// а игра запрашивает сохранение синхронно.
mergeInto(LibraryManager.library, {
  LoomfallInit: function () {
    var cache = (window.loomfallCache = window.loomfallCache || {});
    var keys = ['progress', 'codex'];
    var ready = function (name) { console.log('[Loomfall] площадка готова: ' + name); };

    try {
      if (typeof window.vkBridge !== 'undefined') {
        window.vkBridge.send('VKWebAppInit', {})
          .then(function () {
            return window.vkBridge.send('VKWebAppStorageGet', { keys: keys });
          })
          .then(function (data) {
            if (data && data.keys) {
              for (var i = 0; i < data.keys.length; i++) {
                var entry = data.keys[i];
                if (entry && entry.value !== undefined && entry.value !== '') cache[entry.key] = entry.value;
              }
            }
            ready('VK');
          })
          .catch(function (e) { console.warn('[Loomfall] VK: ' + e); ready('VK без сохранения'); });
      } else if (typeof window.YaGames !== 'undefined') {
        window.YaGames.init()
          .then(function (sdk) {
            window.loomfallSdk = sdk;

            // Яндексу обязательно нужно сообщить, что игра загрузилась, иначе
            // площадка не уберёт свой экран загрузки.
            try { sdk.features.LoadingAPI.ready(); } catch (e) { }

            return sdk.getPlayer({ scopes: false })
              .then(function (player) {
                window.loomfallPlayer = player;
                return player.getData(keys);
              })
              .then(function (data) {
                if (data) {
                  for (var key in data) {
                    if (data[key] !== undefined && data[key] !== '') cache[key] = data[key];
                  }
                }
              })
              .catch(function (e) { console.warn('[Loomfall] Яндекс, сохранение: ' + e); });
          })
          .then(function () { ready('Яндекс'); })
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
    try {
      if (window.loomfallCache && window.loomfallCache[key]) value = window.loomfallCache[key];
      if (!value) value = window.localStorage.getItem('loomfall.' + key) || '';
    } catch (e) { }
    var size = lengthBytesUTF8(value) + 1;
    var buffer = _malloc(size);
    stringToUTF8(value, buffer, size);
    return buffer;
  },

  LoomfallStorageSet: function (keyPtr, valuePtr) {
    var key = UTF8ToString(keyPtr);
    var value = UTF8ToString(valuePtr);
    try { window.localStorage.setItem('loomfall.' + key, value); } catch (e) { }
    if (window.loomfallCache) window.loomfallCache[key] = value;

    try {
      if (typeof window.vkBridge !== 'undefined') {
        window.vkBridge.send('VKWebAppStorageSet', { key: key, value: value }).catch(function () { });
      } else if (window.loomfallPlayer && window.loomfallPlayer.setData) {
        var bundle = {};
        bundle[key] = value;
        window.loomfallPlayer.setData(bundle, false).catch(function () { });
      }
    } catch (e) { }
  },

  LoomfallFree: function (pointer) {
    _free(pointer);
  },

  LoomfallShowAd: function () {
    var reward = function (name) { console.log('[Loomfall] награда за рекламу: ' + name); };
    try {
      if (typeof window.vkBridge !== 'undefined') {
        window.vkBridge.send('VKWebAppShowNativeAds', { ad_format: 'reward' })
          .then(function () { reward('VK'); })
          .catch(function (e) { console.warn('[Loomfall] реклама VK: ' + e); });
      } else if (window.loomfallSdk && window.loomfallSdk.adv) {
        window.loomfallSdk.adv.showRewardedVideo({
          callbacks: {
            onRewarded: function () { reward('Яндекс'); },
            onError: function (e) { console.warn('[Loomfall] реклама Яндекса: ' + e); }
          }
        });
      }
    } catch (e) {
      console.warn('[Loomfall] реклама недоступна: ' + e);
    }
  }
});
