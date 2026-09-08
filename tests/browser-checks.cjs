let playwright;
try { playwright=require(process.env.PLAYWRIGHT_MODULE || 'playwright'); }
catch { playwright=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'); }
const {chromium}=playwright;
const fs=require('fs');
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 const context=await browser.newContext({viewport:{width:1440,height:1100}});
 const page=await context.newPage(); const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const base=process.env.BASE_URL||'http://localhost:5256';
 await page.goto(base+'/'); await page.locator('[name=username]').fill('admin');await page.locator('[name=password]').fill('ThuVien@123');await page.getByRole('button',{name:/Đăng nhập/}).click();await page.waitForURL(base+'/');
 fs.mkdirSync('docs/screenshots',{recursive:true});await page.screenshot({path:'docs/screenshots/dashboard.png',fullPage:true,animations:"disabled"});
 for(const path of ['/Books','/Books/Details/1','/Books/Edit','/Members','/Members/Edit','/Loans','/Loans/Create','/Reservations','/Reports','/Users','/Users/Edit']){
  const response=await page.goto(base+path);if(response.status()!==200)throw new Error(path+' HTTP '+response.status());console.log('PASS admin page '+path);
 }
 await page.goto(base+'/Books'); await page.screenshot({path:'docs/screenshots/books.png',fullPage:true,animations:"disabled"});
 await page.setViewportSize({width:390,height:844});await page.emulateMedia({reducedMotion:'reduce'});await page.screenshot({path:'docs/screenshots/mobile.png',fullPage:true,animations:"disabled"});
 if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw new Error('Mobile overflow');console.log('PASS mobile no horizontal overflow');
 await page.setViewportSize({width:1440,height:1000});
 await page.goto(base+'/Books/Edit');await page.locator('#Title').fill('Sách kiểm thử CRUD');await page.locator('#Author').fill('Tác giả kiểm thử');await page.locator('#Category').fill('Kiểm thử');await page.locator('#Quantity').fill('2');await page.getByRole('button',{name:'Lưu thông tin'}).click();await page.waitForURL(/\/Books$/,{timeout:10000}).catch(async e=>{console.error(await page.locator('body').innerText());throw e});
 await page.locator('[name=q]').fill('Sách kiểm thử CRUD');await page.getByRole('button',{name:'Tìm kiếm'}).click();await page.waitForURL(/q=/);if(await page.locator('.book-card').count()!==1)throw new Error('Search failed');
 await page.locator('.book-card h3').click();const id=page.url().split('/').pop();await page.getByRole('link',{name:'Chỉnh sửa sách'}).click();await page.locator('#Title').fill('Sách kiểm thử đã sửa');await page.getByRole('button',{name:'Lưu thông tin'}).click();await page.waitForURL(/\/Books$/,{timeout:10000}).catch(async e=>{console.error(await page.locator('body').innerText());throw e});await page.goto(base+'/Books/Details/'+id);if(!await page.getByRole('heading',{name:'Sách kiểm thử đã sửa'}).count())throw new Error('Edit failed');
 page.on('dialog',d=>d.accept());await page.getByRole('button',{name:'Xóa sách',exact:true}).click();await page.waitForURL(/\/Books$/);console.log('PASS book create search update delete');
 await page.goto(base+'/Members/Edit');await page.locator('#FullName').fill('Độc giả kiểm thử');await page.locator('#Email').fill('crud-check@example.com');await page.getByRole('button',{name:'Lưu độc giả'}).click();await page.waitForURL(/\/Members$/);
 await page.locator('[name=q]').fill('crud-check@example.com');await page.getByRole('button',{name:'Tìm kiếm'}).click();await page.waitForURL(/q=/);if(await page.locator('tbody tr').count()!==1)throw new Error('Member search failed');await page.getByRole('link',{name:'Sửa',exact:true}).click();await page.locator('#FullName').fill('Độc giả đã sửa');await page.getByRole('button',{name:'Lưu độc giả'}).click();await page.waitForURL(/\/Members$/);const row=page.locator('tr').filter({hasText:'crud-check@example.com'});if(!await row.innerText().then(t=>t.includes('Độc giả đã sửa')))throw new Error('Member update failed');await row.getByRole('button',{name:'Xóa',exact:true}).click();await page.waitForURL(/\/Members$/);console.log('PASS member create search update delete');
 await page.goto(base+'/Users/Edit/1');await page.locator('#Active').uncheck();await page.getByRole('button',{name:'Lưu tài khoản'}).click();if(!await page.locator('.validation').innerText().then(t=>t.includes('Không thể tự khóa')))throw new Error('Self-lock allowed');console.log('PASS admin self-lock rejected');
 const notFound=await page.goto(base+'/Books/Details/999999');if(notFound.status()!==404)throw new Error('404 handler failed');console.log('PASS friendly 404 page');
 const badCsrf=await context.request.post(base+'/Books/Delete/1',{form:{id:'1'}});if(badCsrf.status()!==400)throw new Error('CSRF not rejected: '+badCsrf.status()+' '+(await badCsrf.text()).slice(0,400));console.log('PASS CSRF rejected');
 const exportResponse=await context.request.get(base+'/Reports/Export?kind=books');if(exportResponse.status()!==200 || !(await exportResponse.text()).includes('Tên sách'))throw new Error('CSV invalid');console.log('PASS UTF-8 CSV export');
 await page.getByRole('button',{name:'Đăng xuất',exact:true}).click();await page.waitForURL(/Login/);
 await page.locator('[name=username]').fill('docgia');await page.locator('[name=password]').fill('ThuVien@123');await page.getByRole('button',{name:/Đăng nhập/}).click();await page.waitForURL(/Books/);
 for(const path of ['/Members','/Users','/Books/Edit','/Loans/Create','/Reports']){const r=await page.goto(base+path);if(r.status()!==403)throw new Error('Reader allowed '+path);console.log('PASS reader denied '+path);}
 await page.goto(base+'/Loans');const body=await page.locator('tbody').innerText();if(body.includes('Trần Hoàng Nam'))throw new Error('Reader data leak');console.log('PASS reader only own loans');
 await page.getByRole('button',{name:'Đăng xuất',exact:true}).click();await page.waitForURL(/Login/);await page.locator('[name=username]').fill('thuthu');await page.locator('[name=password]').fill('ThuVien@123');await page.getByRole('button',{name:/Đăng nhập/}).click();await page.waitForURL(base+'/');
 const denied=await page.goto(base+'/Users');if(denied.status()!==403)throw new Error('Librarian allowed users');await page.goto(base+'/Members');console.log('PASS librarian member access and admin denial');
 if(errors.length)throw new Error(errors.join('\n'));console.log('PASS no browser JS errors');
 await browser.close();
})().catch(e=>{console.error(e);process.exit(1);});
