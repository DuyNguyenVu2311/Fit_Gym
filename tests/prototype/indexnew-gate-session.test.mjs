import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

const prototypePath = 'D:/flutter_projects/index.html';
const plain = (value) => JSON.parse(JSON.stringify(value));

function loadFactory() {
  const html = readFileSync(prototypePath, 'utf8');
  const match = html.match(/\/\* gate-session:start \*\/([\s\S]*?)\/\* gate-session:end \*\//);
  assert.ok(match, 'index.html must expose the gate-session controller block');

  const context = {};
  vm.createContext(context);
  vm.runInContext(`${match[1]};globalThis.__factory=createGateRecognitionSession;`, context);
  return context.__factory;
}

test('success before the third attempt ends the session without support', () => {
  const createSession = loadFactory();
  const session = createSession({ gateId: 'G-01', now: () => '2026-11-12T14:30:00+07:00' });

  session.start();
  session.fail('Kính phản sáng');
  session.succeed();

  assert.deepEqual(plain(session.snapshot()), {
    gateId: 'G-01',
    status: 'success',
    attempts: 2,
    failures: ['Kính phản sáng'],
    supportRequest: null,
  });
});

test('the third consecutive failure creates exactly one support request', () => {
  const createSession = loadFactory();
  const session = createSession({
    gateId: 'G-01',
    sessionId: () => 'GS-143000',
    now: () => '2026-11-12T14:30:00+07:00',
  });

  session.start();
  session.fail('Khuôn mặt bị che');
  session.fail('Kính phản sáng');
  session.fail('Độ tin cậy thấp');
  session.fail('Không được tạo lần thứ tư');

  const state = plain(session.snapshot());
  assert.equal(state.status, 'awaiting-clear');
  assert.equal(state.attempts, 3);
  assert.deepEqual(state.failures, ['Khuôn mặt bị che', 'Kính phản sáng', 'Độ tin cậy thấp']);
  assert.deepEqual(state.supportRequest, {
    id: 'GS-143000',
    gateId: 'G-01',
    createdAt: '2026-11-12T14:30:00+07:00',
    reasons: ['Khuôn mặt bị che', 'Kính phản sáng', 'Độ tin cậy thấp'],
  });
});

test('the next person cannot start until the scan zone is clear', () => {
  const createSession = loadFactory();
  const session = createSession({ gateId: 'G-01' });

  session.start();
  session.fail('Lỗi 1');
  session.fail('Lỗi 2');
  session.fail('Lỗi 3');

  assert.equal(session.start(), false);
  assert.equal(session.markZoneClear(), true);
  assert.equal(session.start(), true);
  assert.deepEqual(plain(session.snapshot()), {
    gateId: 'G-01',
    status: 'scanning',
    attempts: 0,
    failures: [],
    supportRequest: null,
  });
});

