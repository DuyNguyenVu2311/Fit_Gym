import assert from 'node:assert/strict';

const targets = await (await fetch('http://127.0.0.1:9223/json')).json();
const target = targets.find((item) => item.url === 'file:///D:/flutter_projects/index.html');
assert.ok(target, 'headless Edge must have index.html open');

const socket = new WebSocket(target.webSocketDebuggerUrl);
await new Promise((resolve, reject) => {
  socket.addEventListener('open', resolve, { once: true });
  socket.addEventListener('error', reject, { once: true });
});

let sequence = 0;
const pending = new Map();
socket.addEventListener('message', ({ data }) => {
  const message = JSON.parse(data);
  if (!message.id || !pending.has(message.id)) return;
  const { resolve, reject } = pending.get(message.id);
  pending.delete(message.id);
  if (message.error) reject(new Error(message.error.message));
  else resolve(message.result);
});

function evaluate(expression) {
  const id = ++sequence;
  socket.send(JSON.stringify({ id, method: 'Runtime.evaluate', params: { expression, returnByValue: true } }));
  return new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
}

await evaluate(`document.querySelector('#gateSessionButton').click()`);
let result = await evaluate(`({
  open: document.querySelector('#gateSessionDialog').open,
  badge: document.querySelector('#gateSessionBadge').textContent,
  attempts: document.querySelector('#gateAttemptText').textContent
})`);
assert.deepEqual(result.result.value, { open: true, badge: 'Đang quét', attempts: 'Đã dùng 0/3 lần' });

await evaluate(`document.querySelector('#simulateFaceFailure').click();document.querySelector('#simulateFaceFailure').click();document.querySelector('#simulateFaceFailure').click()`);
result = await evaluate(`({
  badge: document.querySelector('#gateSessionBadge').textContent,
  attempts: document.querySelector('#gateAttemptText').textContent,
  supportVisible: !document.querySelector('#gateSupportCode').hidden,
  clearVisible: !document.querySelector('#markScanZoneClear').hidden,
  queued: [...document.querySelectorAll('.incident-copy strong')].filter(x => x.textContent === 'Nhận diện thất bại sau 3 lần').length
})`);
assert.deepEqual(result.result.value, {
  badge: 'Chuyển hỗ trợ',
  attempts: 'Đã dùng 3/3 lần',
  supportVisible: true,
  clearVisible: true,
  queued: 1,
});

await evaluate(`document.querySelector('#markScanZoneClear').click()`);
result = await evaluate(`({
  badge: document.querySelector('#gateSessionBadge').textContent,
  attempts: document.querySelector('#gateAttemptText').textContent,
  failVisible: !document.querySelector('#simulateFaceFailure').hidden
})`);
assert.deepEqual(result.result.value, { badge: 'Đang quét', attempts: 'Đã dùng 0/3 lần', failVisible: true });

socket.close();
console.log('browser smoke: PASS');

