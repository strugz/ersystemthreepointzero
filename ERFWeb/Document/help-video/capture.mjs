// Sharp 1920x1080 screenshots for the Help videos (README.txt, "Sharp screenshots"). Drives a Chrome started with
// --remote-debugging-port=9222 --remote-debugging-address=127.0.0.1 and its own --user-data-dir; you sign in there.
// Needs Node 22+ (built-in WebSocket). Every command first sets the page to CSS 1280x720 at 1.5x.
// node capture.mjs nav <url>
// node capture.mjs click "<text>" [n]      clicks the n-th (default 0) visible button/link/tab with exactly that text
// node capture.mjs type "<css selector>" "<text>"
// node capture.mjs eval "<js>"
// node capture.mjs shot <file.png> ["<text>" [px]]   optionally scrolls that text to the top, px (default 90) below it
import { writeFileSync } from 'node:fs';

const [cmd, ...args] = process.argv.slice(2);
const tabs = await (await fetch('http://127.0.0.1:9222/json')).json();
const page = tabs.find((t) => t.type === 'page' && !t.url.startsWith('devtools'));
const ws = new WebSocket(page.webSocketDebuggerUrl);
await new Promise((r) => ws.addEventListener('open', r, { once: true }));
let id = 0;
const pending = new Map();
ws.addEventListener('message', (e) => {
  const m = JSON.parse(e.data);
  if (m.id && pending.has(m.id)) {
    pending.get(m.id)(m);
    pending.delete(m.id);
  }
});
const send = (method, params = {}) =>
  new Promise((resolve, reject) => {
    const n = ++id;
    pending.set(n, (m) => (m.error ? reject(new Error(m.error.message)) : resolve(m.result)));
    ws.send(JSON.stringify({ id: n, method, params }));
  });
const evaluate = async (expression) => {
  const r = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
  if (r.exceptionDetails) throw new Error(r.exceptionDetails.exception?.description ?? 'eval failed');
  return r.result.value;
};

// The override ends when this connection closes, so every command sets it first.
await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 720, deviceScaleFactor: 1.5, mobile: false });
await new Promise((r) => setTimeout(r, 400));
if (cmd === 'size') {
  console.log('1280x720 @1.5');
} else if (cmd === 'nav') {
  await send('Page.navigate', { url: args[0] });
  await new Promise((r) => setTimeout(r, 3000));
  console.log(await evaluate('location.href'));
} else if (cmd === 'eval') {
  console.log(JSON.stringify(await evaluate(args[0])));
} else if (cmd === 'click') {
  const text = args[0];
  const n = Number(args[1] ?? 0);
  const res = await evaluate(`(() => {
    const els = [...document.querySelectorAll('button, a, [role=tab], .v-list-item, label')]
      .filter((e) => e.offsetParent !== null && e.innerText.trim() === ${JSON.stringify(text)});
    if (!els[${n}]) return 'not found (' + els.length + ')';
    els[${n}].scrollIntoView({ block: 'center' });
    els[${n}].click();
    return 'clicked ' + els.length;
  })()`);
  await new Promise((r) => setTimeout(r, 1500));
  console.log(res);
} else if (cmd === 'type') {
  // focus the element matched by the CSS selector, then type like a keyboard
  await evaluate(`document.querySelector(${JSON.stringify(args[0])}).focus()`);
  await send('Input.insertText', { text: args[1] });
  await new Promise((r) => setTimeout(r, 500));
  console.log('typed');
} else if (cmd === 'shot') {
  // optional args[1]: text of an element to scroll to the top; args[2]: CSS px to leave above it (default 90)
  if (args[1]) {
    await evaluate(`(() => { const e = [...document.querySelectorAll('*')]
      .filter((x) => x.offsetParent !== null && x.innerText?.trim() === ${JSON.stringify(args[1])}).pop();
      if (e) { e.scrollIntoView({ block: 'start' }); window.scrollBy(0, -${Number(args[2] || 90)}); } })()`);
  }
  await new Promise((r) => setTimeout(r, 600));
  const r = await send('Page.captureScreenshot', { format: 'png' });
  writeFileSync(args[0], Buffer.from(r.data, 'base64'));
  console.log('saved', args[0]);
}
ws.close();
