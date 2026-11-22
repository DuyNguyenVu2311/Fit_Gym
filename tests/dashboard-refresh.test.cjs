const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync('src/FitGym.Web/wwwroot/js/dashboard-refresh.js', 'utf8');
const tick = () => new Promise(resolve => setImmediate(resolve));

async function scenario(live) {
    let instant = '2026-11-20T16:59:00Z', poll, replaced = 0, mode = 'ok', requested;
    const windowEvents = {};
    const snapshot = { dataset: { date: '2026-11-20', liveDay: String(live) } };
    const host = { dataset: { url: '/Home/Snapshot' }, isConnected: true, querySelector: () => snapshot, replaceChildren: () => replaced++ };
    const status = { textContent: '', classList: { toggle() {} } };
    const document = { hidden: false, querySelector: selector => selector === '[data-dashboard-host]' ? host : selector === '[data-dashboard-status]' ? status : null, addEventListener() {} };
    class Clock extends Date { constructor(...args) { super(...(args.length ? args : [instant])); } }
    const context = { URL, Intl, Date: Clock, AbortController, document, window: { location: { origin: 'http://localhost' }, addEventListener(name, fn) { windowEvents[name] = fn; } },
        setInterval(fn, ms) { assert.equal(ms, 60000); poll = fn; return 1; }, clearInterval() { poll = null; }, setTimeout, clearTimeout,
        DOMParser: class { parseFromString() { return { querySelector: () => mode === 'bad-html' ? null : ({ dataset: { date: requested } }) }; } },
        async fetch(url) { requested = url.searchParams.get('date'); if (mode === 'offline') throw new Error('offline'); return { ok: true, status: 200, redirected: mode === 'expired', text: async () => '<section></section>' }; } };
    vm.runInNewContext(source, context);
    assert.equal(typeof poll, 'function', 'Dashboard must schedule automatic refresh');
    await poll(); await tick();
    assert.equal(requested, '2026-11-20'); assert.equal(replaced, 1);
    instant = '2026-11-20T17:01:00Z';
    await poll(); await tick();
    assert.equal(requested, live ? '2026-11-21' : '2026-11-20', 'Only the live day follows Vietnam midnight');
    mode = 'offline'; await poll(); await tick();
    assert.equal(replaced, 2, 'Network failures retain the last successful snapshot');
    assert.match(status.textContent, /giữ nguyên/i);
    mode = 'bad-html'; await poll(); await tick(); assert.equal(replaced, 2, 'Malformed responses cannot replace dashboard data');
    mode = 'expired'; await poll(); await tick(); assert.equal(poll, null, 'Expired login must stop polling');
    assert.equal(replaced, 2);
    mode = 'ok';
    windowEvents.pagehide();
    assert.equal(poll, null);
    assert.equal(typeof windowEvents.pageshow, 'function', 'Back/forward cache restoration must resume automatic updates');
    windowEvents.pageshow({ persisted: true }); await tick();
    assert.equal(typeof poll, 'function'); assert.equal(replaced, 3);
}
(async () => { await scenario(true); await scenario(false); console.log('dashboard refresh behavior: PASS'); })().catch(error => { console.error(error); process.exitCode = 1; });
