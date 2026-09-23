// Run against a fresh isolated demo database, never the live library database.
const {chromium} = require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const assert = require('node:assert/strict');
(async () => {
  const browser = await chromium.launch({channel:'msedge',headless:true});
  try {
    const base = process.env.BASE_URL || 'http://127.0.0.1:5267';
    async function login(username) {
      const context = await browser.newContext(); const page = await context.newPage();
      await page.goto(base+'/Account/Login');
      await page.locator('[name=username]').fill(username);
      await page.locator('[name=password]').fill('ThuVien@123');
      await page.getByRole('button',{name:/Đăng nhập/}).click();
      await page.waitForURL(username === 'docgia' ? base+'/Books' : base+'/');
      return page;
    }
    const reader = await login('docgia');
    await reader.goto(base+'/Reservations/Create');
    const token = await reader.locator('#loan-create input[name=__RequestVerificationToken]').inputValue();
    await reader.request.post(base+'/Reservations/Create', {form:{'Items[0].BookId':'6','Items[0].Quantity':'2','Items[1].BookId':'8','Items[1].Quantity':'1',__RequestVerificationToken:token}});
    await reader.goto(base+'/Reservations');
    assert.equal(await reader.getByRole('button',{name:'Xác nhận đặt trước',exact:true}).count(),0);
    assert.equal(await reader.getByRole('button',{name:'Xóa',exact:true}).count(),0);
    const readerId = (await reader.locator('form[action^="/Reservations/Cancel/"]').first().getAttribute('action')).split('/').pop();
    for(const action of ['Confirm','Delete']) {
      const response = await reader.request.post(base+`/Reservations/${action}/${readerId}`,{form:{__RequestVerificationToken:token}});
      assert.equal(response.status(),403);
    }
    const staff = await login('thuthu'); staff.on('dialog',d=>d.accept());
    await staff.goto(base+'/Loans'); const loansBefore = await staff.locator('tbody tr').count();
    await staff.goto(base+'/Reservations');
    const row = title => staff.locator('tbody tr').filter({hasText:title});
    const confirmPath = await row('Sapiens').locator('form[action^="/Reservations/Confirm/"]').getAttribute('action');
    const staffToken = await staff.locator('input[name=__RequestVerificationToken]').first().inputValue();
    const activeDelete = await staff.request.post(base+'/Reservations/Delete/'+confirmPath.split('/').pop(),{form:{__RequestVerificationToken:staffToken}});
    assert.match(await activeDelete.text(), /alert error/);
    const noCsrf = await staff.request.post(base+confirmPath,{form:{}}); assert.equal(noCsrf.status(),400);
    await Promise.all([staff.waitForResponse(r=>r.request().method()==='POST'),row('Sapiens').getByRole('button',{name:'Xác nhận đặt trước'}).click()]);
    await staff.waitForLoadState(); await staff.goto(base+'/Reservations');
    assert.match(await row('Sapiens').innerText(),/Đã nhận/);
    assert.equal(await row('Sapiens').getByRole('button',{name:'Xóa',exact:true}).count(),1);
    await staff.request.post(base+confirmPath,{form:{__RequestVerificationToken:staffToken}});
    await staff.goto(base+'/Loans'); assert.equal(await staff.locator('tbody tr').count(),loansBefore+2);
    await staff.goto(base+'/Reservations');
    await Promise.all([staff.waitForResponse(r=>r.request().method()==='POST'),row('Sapiens').getByRole('button',{name:'Xóa',exact:true}).click()]);
    await staff.goto(base+'/Reservations'); assert.equal(await row('Sapiens').count(),0);
    await staff.goto(base+'/Loans'); assert.equal(await staff.locator('tbody tr').count(),loansBefore+2);
    console.log('PASS staff confirms 2 copies once, deletes received request, preserves loans; reader and CSRF protection');
    await staff.goto(base+'/Reservations');
    await Promise.all([staff.waitForResponse(r=>r.request().method()==='POST'),row('Nhập môn').getByRole('button',{name:'Xác nhận đặt trước'}).click()]);
    await staff.goto(base+'/Reservations'); assert.match(await row('Nhập môn').innerText(),/Đang chờ/);
    await Promise.all([staff.waitForResponse(r=>r.request().method()==='POST'),row('Nhập môn').getByRole('button',{name:'Hủy đặt trước'}).click()]);
    await staff.goto(base+'/Reservations'); assert.match(await row('Nhập môn').innerText(),/Đã hủy/);
    await Promise.all([staff.waitForResponse(r=>r.request().method()==='POST'),row('Nhập môn').getByRole('button',{name:'Xóa',exact:true}).click()]);
    await staff.goto(base+'/Reservations'); assert.equal(await staff.locator('tbody tr').count(),0);
    console.log('PASS insufficient stock stays pending; librarian cancels and deletes canceled request');
  } finally { await browser.close(); }
})().catch(e=>{console.error(e);process.exit(1)});
