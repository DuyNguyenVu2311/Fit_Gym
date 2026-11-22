const assert = require('node:assert/strict');
const rules = require('../src/FitGym.Web/wwwroot/js/registration-flow.js');

const valid = { memberId: 7, packageId: 9, startDate: '2026-11-20', quote: { memberId: 7, packageId: 9, startDate: '2026-11-20', endDate: '2026-12-20', total: 250000 }, consent: true, method: 'TienMat', cashReceived: true };
assert.equal(rules.canProceed(1, { ...valid, memberId: 0 }), false, 'Select a member before moving forward');
assert.equal(rules.canProceed(1, valid), true);
assert.equal(rules.canProceed(2, { ...valid, quote: null }), false, 'A quote must be loaded before advancing');
assert.equal(rules.canProceed(2, { ...valid, packageId: 10 }), false, 'Changing package invalidates the old quote');
assert.equal(rules.canProceed(2, { ...valid, startDate: '2026-11-21' }), false, 'Changing date invalidates the old quote');
assert.equal(rules.canProceed(2, { ...valid, memberId: 8 }), false, 'Changing member invalidates another member quote');
assert.equal(rules.canProceed(2, valid), true);
assert.equal(rules.canProceed(3, { ...valid, consent: false }), false, 'Consent must be explicitly acknowledged');
assert.equal(rules.canProceed(4, { ...valid, cashReceived: false }), false, 'Unreceived cash cannot activate a registration');
assert.equal(rules.canProceed(4, { ...valid, method: 'QR', cashReceived: false }), true, 'QR may create a pending payment request');
assert.equal(rules.canProceed(4, { ...valid, method: 'POS' }), false, 'Unsupported payment method must be rejected');
assert.equal(rules.canSubmit(valid), true);
assert.equal(rules.canSubmit({ ...valid, quote: { ...valid.quote, packageId: 8 } }), false, 'Direct submit must also reject stale quote');
console.log('registration flow behavior: PASS (13 scenarios)');
