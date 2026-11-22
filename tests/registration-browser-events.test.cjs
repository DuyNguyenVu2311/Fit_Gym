const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const rules = require('../src/FitGym.Web/wwwroot/js/registration-flow.js');

// Minimal DOM adapter: production event handlers and async response order run unmodified.
function element(value = '') {
    return { value, checked: false, hidden: false, disabled: false, textContent: '', dataset: {}, selectedOptions: [{ textContent: 'Hội viên' }],
        listeners: {}, addEventListener(name, fn) { this.listeners[name] = fn; }, setAttribute() {}, removeAttribute() {},
        classList: { toggle() {} }, reportValidity() { return true; }, scrollIntoView() {} };
}
const tick = () => new Promise(resolve => setImmediate(resolve));
async function main() {
    const nodes = new Map();
    const get = selector => { if (!nodes.has(selector)) nodes.set(selector, element()); return nodes.get(selector); };
    const form = element();
    form.dataset.quoteUrl = '/Registrations/Quote';
    form.querySelector = get;
    form.querySelectorAll = () => [];
    get('#MaHoiVien').value = '7'; get('#NgayBatDau').value = '2026-11-20'; get('#PhuongThucThanhToan').value = 'TienMat';
    const requests = [];
    const context = { URL, Intl, console, window: { FitGymRegistrationRules: rules, location: { origin: 'http://localhost' } }, document: { querySelector: () => form, querySelectorAll: () => [] },
        fetch(url) { return Promise.resolve({ ok: true, status: 200, redirected: false, json: () => new Promise(resolve => requests.push({ url, resolve })) }); } };
    vm.runInNewContext(fs.readFileSync('src/FitGym.Web/wwwroot/js/registration-wizard.js', 'utf8'), context);
    get('#MaGoiTap').value = '9';
    get('#MaGoiTap').listeners.change(); await tick();
    assert.equal(requests.length, 1);
    get('#DongYSinhTracHoc').checked = true; get('#CashReceived').checked = true;
    get('#MaHoiVien').value = '8'; get('#MaHoiVien').listeners.change(); await tick();
    assert.equal(get('#DongYSinhTracHoc').checked, false, 'Changing member clears previous member consent');
    assert.equal(get('#CashReceived').checked, false, 'Changing member clears old cash acknowledgment');
    assert.equal(get('#QuoteToken').value, '', 'Changing member invalidates previous quote token');
    assert.equal(requests[1].url.searchParams.get('memberId'), '8');
    requests[0].resolve({ memberId: 7, packageId: 9, startDate: '2026-11-20', endDate: '2026-12-20', total: 100, packageName: 'OLD', quoteToken: 'old' }); await tick();
    assert.equal(get('#QuoteToken').value, '', 'Stale response body cannot reinstate another member quote');
    requests[1].resolve({ memberId: 8, packageId: 9, startDate: '2026-11-20', endDate: '2026-12-20', total: 200, packageName: 'CURRENT', quoteToken: 'current' }); await tick();
    assert.equal(get('#QuoteToken').value, 'current');
    assert.equal(get('[data-quote-name]').textContent, 'CURRENT');

    const renewal = element(); renewal.dataset.quoteUrl = '/Registrations/RenewalQuote'; renewal.querySelector = get;
    get('#MaDangKyGoc').value = '11';
    vm.runInNewContext(fs.readFileSync('src/FitGym.Web/wwwroot/js/renewal.js', 'utf8'), { URL, Intl, location: { origin:'http://localhost' }, document:{ querySelector: () => renewal }, fetch: context.fetch });
    await tick();
    requests[2].resolve({ startDate:'2026-12-21', endDate:'2027-01-21', total:300, quoteToken:'renewal' }); await tick();
    get('#CashReceived').checked = true; get('#PhuongThucThanhToan').listeners.change();
    assert.equal(get('#CashReceived').checked, false, 'Changing renewal payment method requires cash acknowledgment again');
    get('#CashReceived').checked = true; get('#MaGoiTap').value = '10'; get('#MaGoiTap').listeners.change();
    assert.equal(get('#CashReceived').checked, false, 'Changing renewal package clears cash acknowledgment');
    assert.equal(get('#QuoteToken').value, '', 'Changing renewal package invalidates signed terms');
    console.log('registration/renewal browser event regressions: PASS');
}
main().catch(error => { console.error(error); process.exitCode = 1; });
