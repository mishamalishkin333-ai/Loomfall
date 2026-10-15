// Проверка моста к площадкам без браузера: подставляем VK Bridge и Яндекс Игры
// и смотрим, какие вызовы уходят в SDK. Запуск: node tools/bridge-test.mjs
import fs from 'node:fs';
import vm from 'node:vm';

const path = process.argv[2] || 'C:/Unity/Loomfall/Assets/Plugins/WebGL/LoomfallBridge.jslib';
const source = fs.readFileSync(path, 'utf8');

let passed = 0;
let failed = 0;
const check = (name, ok, detail) => {
  if (ok) { passed++; console.log('  ок   ' + name); }
  else { failed++; console.log('  СБОЙ ' + name + (detail ? ' — ' + detail : '')); }
};
const tick = async () => { for (let i = 0; i < 8; i++) await new Promise((r) => setImmediate(r)); };

// Собираем библиотеку из jslib в подставном окружении.
function loadLibrary(windowMock) {
  const sandbox = {
    window: windowMock,
    console: { log: () => {}, warn: () => {} },
    LibraryManager: { library: {} },
    mergeInto: (target, library) => Object.assign(target, library),
    UTF8ToString: (pointer) => (pointer && pointer.value !== undefined ? pointer.value : String(pointer)),
    stringToUTF8: (text, buffer) => { buffer.value = text; },
    lengthBytesUTF8: (text) => Buffer.byteLength(text, 'utf8') + 1,
    _malloc: () => ({ value: '' }),
    _free: () => {}
  };
  vm.createContext(sandbox);
  vm.runInContext(source, sandbox);
  return sandbox.LibraryManager.library;
}

// --- Сценарий 1: VK Bridge ---
{
  console.log('VK Bridge:');
  const calls = [];
  const storage = { progress: 'ярус-3', codex: 'запись-7' };
  const windowMock = {
    localStorage: {
      store: {},
      getItem(k) { return this.store[k] || null; },
      setItem(k, v) { this.store[k] = v; }
    },
    vkBridge: {
      send(method, params) {
        calls.push({ method, params });
        if (method === 'VKWebAppStorageGet') {
          return Promise.resolve({ keys: Object.keys(storage).map((key) => ({ key, value: storage[key] })) });
        }
        return Promise.resolve({});
      }
    }
  };

  const library = loadLibrary(windowMock);
  library.LoomfallInit();
  await tick();

  const methods = calls.map((c) => c.method);
  check('подключение VKWebAppInit', methods[0] === 'VKWebAppInit', methods.join(','));
  check('прогресс читается из хранилища VK', methods[1] === 'VKWebAppStorageGet', methods.join(','));

  const read = library.LoomfallStorageGet({ value: 'progress' });
  check('чтение сохранения из VK', read.value === 'ярус-3', JSON.stringify(read.value));

  calls.length = 0;
  library.LoomfallStorageSet({ value: 'progress' }, { value: 'ярус-4' });
  check('запись сохранения в VK', calls[0] && calls[0].method === 'VKWebAppStorageSet' && calls[0].params.value === 'ярус-4');
  check('запись сохранения в браузер', windowMock.localStorage.getItem('loomfall.progress') === 'ярус-4');

  calls.length = 0;
  library.LoomfallShowAd();
  await tick();
  check('вознаграждаемая реклама VK', calls[0] && calls[0].method === 'VKWebAppShowNativeAds' && calls[0].params.ad_format === 'reward');
}

// --- Сценарий 2: Яндекс Игры ---
{
  console.log('Яндекс Игры:');
  const calls = [];
  const playerData = { progress: 'яндекс-ярус-2' };
  const windowMock = {
    localStorage: { store: {}, getItem() { return null; }, setItem() {} },
    YaGames: {
      init() {
        calls.push({ method: 'YaGames.init' });
        return Promise.resolve({
          features: { LoadingAPI: { ready() { calls.push({ method: 'LoadingAPI.ready' }); } } },
          getPlayer(options) {
            calls.push({ method: 'getPlayer', params: options });
            return Promise.resolve({
              getData(keys) { calls.push({ method: 'player.getData', params: keys }); return Promise.resolve(playerData); },
              setData(bundle, flush) { calls.push({ method: 'player.setData', params: { bundle, flush } }); return Promise.resolve({}); }
            });
          },
          adv: {
            showRewardedVideo(options) {
              calls.push({ method: 'adv.showRewardedVideo', params: options });
              options.callbacks.onRewarded();
            }
          }
        });
      }
    }
  };

  const library = loadLibrary(windowMock);
  library.LoomfallInit();
  await tick();

  const methods = calls.map((c) => c.method);
  check('инициализация Яндекс Игр', methods.includes('YaGames.init'), methods.join(','));
  check('прогресс читается из профиля игрока', methods.includes('player.getData'), methods.join(','));

  // Готовность отправляется отдельным вызовом после первого кадра, а не при запуске.
  check('о загрузке не сообщается раньше первого кадра', !methods.includes('LoadingAPI.ready'), methods.join(','));
  library.LoomfallReady();
  check('готовность отправляется отдельным вызовом', calls.filter((c) => c.method === 'LoadingAPI.ready').length === 1,
    calls.map((c) => c.method).join(','));
  library.LoomfallReady();
  check('повторная готовность не отправляется второй раз', calls.filter((c) => c.method === 'LoadingAPI.ready').length === 1,
    calls.map((c) => c.method).join(','));

  const read = library.LoomfallStorageGet({ value: 'progress' });
  check('чтение сохранения из профиля', read.value === 'яндекс-ярус-2', JSON.stringify(read.value));

  calls.length = 0;
  library.LoomfallStorageSet({ value: 'progress' }, { value: 'яндекс-ярус-3' });
  await tick();
  const save = calls.find((c) => c.method === 'player.setData');
  check('запись сохранения в профиль', !!save && save.params.bundle.progress === 'яндекс-ярус-3');

  calls.length = 0;
  library.LoomfallShowAd();
  await tick();
  const ad = calls.find((c) => c.method === 'adv.showRewardedVideo');
  check('вознаграждаемая реклама Яндекса', !!ad && typeof ad.params.callbacks.onRewarded === 'function');
}

// --- Сценарий 3: обычный браузер без площадки ---
{
  console.log('Без площадки:');
  const windowMock = {
    localStorage: {
      store: {},
      getItem(k) { return this.store[k] || null; },
      setItem(k, v) { this.store[k] = v; }
    }
  };
  const library = loadLibrary(windowMock);
  let crashed = false;
  try { library.LoomfallInit(); library.LoomfallReady(); library.LoomfallStorageSet({ value: 'progress' }, { value: 'локально' }); library.LoomfallShowAd(); }
  catch (e) { crashed = true; }
  check('не падает без SDK', !crashed);
  check('сохранение в localStorage', windowMock.localStorage.getItem('loomfall.progress') === 'локально');
  check('чтение из localStorage', library.LoomfallStorageGet({ value: 'progress' }).value === 'локально');
}

console.log('');
console.log('итог: успешно ' + passed + ', сбоев ' + failed);
process.exit(failed === 0 ? 0 : 1);
