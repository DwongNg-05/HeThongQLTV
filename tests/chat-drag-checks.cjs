const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs = require('node:fs');
const assert = require('node:assert/strict');
(async () => {
 const browser = await chromium.launch({channel:'msedge',headless:true});
 try {
 const page = await browser.newPage({viewport:{width:1440,height:1000},hasTouch:true});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const source=fs.readFileSync('HeThongQLTV/Views/Shared/_Chat.cshtml','utf8');
 await page.setContent(source.slice(source.indexOf('<button id="chat-launch"'),source.lastIndexOf('<script')).replace('@Html.AntiForgeryToken()',''));
 await page.addStyleTag({content:fs.readFileSync('HeThongQLTV/wwwroot/css/chat.css','utf8')});
 await page.addScriptTag({content:fs.readFileSync('HeThongQLTV/wwwroot/js/chat.js','utf8')});
 const panel=page.locator('#library-chat'), handle=page.locator('.chat-drag-label');
 const launcher=page.locator('#chat-launch'), initial=await launcher.boundingBox();
 await page.mouse.move(initial.x+30,initial.y+20);await page.mouse.down();await page.mouse.move(initial.x-270,initial.y-180,{steps:10});await page.mouse.up();
 const relocated=await launcher.boundingBox();assert(relocated.x<initial.x-250);assert(await panel.isHidden());
 await page.locator('#chat-launch').click();
 const before=await panel.boundingBox(), grip=await handle.boundingBox();
 await page.mouse.move(grip.x+20,grip.y+8);await page.mouse.down();await page.mouse.move(grip.x-280,grip.y-142,{steps:8});await page.mouse.up();
 const moved=await panel.boundingBox();assert(moved.x<before.x-200&&moved.y<before.y-100);
 await page.locator('#chat-close').click();await page.locator('#chat-launch').click();assert.deepEqual(await panel.boundingBox(),moved);
 await handle.focus();await page.keyboard.press('ArrowLeft');assert.equal(Math.round((await panel.boundingBox()).x),Math.round(moved.x)-10);
 async function inBounds(){const r=await panel.boundingBox(), v=page.viewportSize();assert(r.x>=9&&r.y>=9&&r.x+r.width<=v.width-9&&r.y+r.height<=v.height-9,JSON.stringify(r));}
 await page.locator('#chat-expand').click();await inBounds();await page.locator('#chat-expand').click();
 await page.setViewportSize({width:390,height:844});await page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));await inBounds();
 // Use a real touch sequence through Chromium so pointer capture and touch-action are exercised.
 const cdp=await page.context().newCDPSession(page), h=await handle.boundingBox();
 await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:h.x+15,y:h.y+10}]});
 await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:380,y:820}]});
 await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await inBounds();
 assert.equal(await panel.evaluate(e=>e.classList.contains('dragging')),false);
 await page.locator('#chat-reset').click();await page.locator('#chat-input').fill('Tra cứu sách');assert.equal(await page.locator('#chat-input').inputValue(),'Tra cứu sách');
 await page.locator('#chat-close').click();
 const touchBefore=await launcher.boundingBox();
 await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:touchBefore.x+20,y:touchBefore.y+15}]});
 await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:30,y:50}]});
 await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
 assert(await panel.isHidden());assert((await launcher.boundingBox()).y<touchBefore.y);
 await page.waitForTimeout(350);await launcher.tap();await page.waitForFunction(()=>!document.querySelector("#library-chat").hidden);
 assert.deepEqual(errors,[]);console.log('PASS mouse drag, keyboard movement, reopen position, expand/resize bounds, touch drag, controls and input');
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
