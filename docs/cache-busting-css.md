# Cache Busting CSS va tai nguyen tinh

Tai lieu nay giai thich cach tranh tinh trang nguoi dung da deploy ban CSS moi nhung trinh duyet van hien giao dien cu. No ap dung cho frontend Blazor WebAssembly trong repository nay va co the giao truc tiep cho mot AI khac thuc hien.

## Ket luan nhanh

Cache busting la viec thay doi URL cua tai nguyen moi moi khi **noi dung cua no thay doi**. Khi URL doi, browser va CDN xem do la mot file moi va bat buoc tai lai. Cach ben vung nhat la dua hash noi dung vao **ten file**; query string `?v=...` la giai phap nhanh va tam thoi.

Trong code hien tai, hai stylesheet tu tao dang duoc goi nhu sau trong `frontend/src/Frontend.App/wwwroot/index.html`:

```html
<link rel="stylesheet" href="css/app.css?v=20260828" />
<link href="Frontend.App.styles.css?v=20260828" rel="stylesheet" />
```

Neu CSS thay doi ma `20260828` khong doi, URL van giong nhau. Browser, CDN, reverse proxy hoac service worker co the tra lai ban cu. Neu co tang CDN duoc cau hinh bo qua query string, ke ca viec doi `?v=` cung khong du de khac phuc.

## Cache dang xuat hien o dau

```text
Browser memory/disk cache
          |
          v
CDN / Cloudflare cache (neu co)
          |
          v
Nginx / hosting proxy cache (neu co)
          |
          v
Web server / static hosting
```

Chi can mot tang tra file CSS cu la nguoi dung se thay giao dien cu. Vi vay viec sua CSS tren source khong dong nghia tat ca client se tai file moi ngay.

## Hash va version khac nhau nhu the nao

| Cach | Vi du URL | Uu diem | Han che |
| --- | --- | --- | --- |
| Version thu cong | `app.css?v=20260829.1` | Nhanh, de ap dung ngay | De quen tang version; CDN co the bo qua query string |
| Build version tu dong | `app.css?v=20260829.1742` | Khong can sua tay khi release | Van phu thuoc vao cach CDN xu ly query string |
| Content hash trong ten file | `app.3f8a2c19.css` | On dinh nhat, cache CDN tot, cache lau an toan | Can build sinh file va cap nhat `index.html` tu dong |

`3f8a2c19` la hash duoc tinh tu noi dung file. Neu file CSS khong doi, hash khong doi va browser duoc phep dung cache. Neu chi can sua mot ky tu, hash va URL moi duoc sinh ra. Khong dat hash bang tay.

## Cach trinh duyet quyet dinh dung file nao

1. Trinh duyet tai `index.html`.
2. File nay tham chieu `app.css` hoac `app.3f8a2c19.css`.
3. Neu URL CSS da tung tai va header cho phep cache, browser co the dung ban da luu.
4. Neu URL CSS moi, browser phai tai file moi, bat ke ban cu con trong cache.

Vi ly do do, `index.html` can duoc cap nhat nhanh, con cac asset co hash co the cache rat lau.

## Chinh sach header khuyen nghi

| Loai tai nguyen | Header khuyen nghi | Ly do |
| --- | --- | --- |
| `index.html` | `Cache-Control: no-cache, max-age=0, must-revalidate` | Browser luon kiem tra xem entry page moi hay chua |
| CSS/JS co hash trong ten file | `Cache-Control: public, max-age=31536000, immutable` | URL da doi khi noi dung doi, nen cache 1 nam an toan |
| CSS/JS chua co hash | `Cache-Control: no-cache` hoac TTL ngan | Tranh phuc vu file cu trong giai doan chuyen doi |
| Anh, font co hash | `Cache-Control: public, max-age=31536000, immutable` | Giam request va tang toc do tai trang |
| API du lieu | Cach header rieng, khong dung chung voi CSS | API co quy tac cache va phan quyen khac |

Khong nen dat `immutable` cho `index.html` hoac `app.css` khong co hash. Day la nguyen nhan pho bien khien deploy xong nhung CSS khong doi tren may nguoi dung.

## Phuong an cho repository hien tai

### Phuong an A: Sua ngay bang version query string

Moi lan CSS thay doi, tang cung luc ca hai version trong `index.html`:

```html
<link rel="stylesheet" href="css/app.css?v=20260829.1" />
<link href="Frontend.App.styles.css?v=20260829.1" rel="stylesheet" />
```

Khi release tiep theo, doi sang `20260829.2` hoac mot build id moi. Cach nay phu hop de xu ly su co ngay, nhung can dua vao checklist deploy de khong bi quen.

Dieu kien bat buoc: CDN/reverse proxy phai cache theo day du query string. Voi Cloudflare, kiem tra Cache Rules khong duoc cau hinh bo qua query string cho cac file CSS nay.

### Phuong an B: Ten file co content hash, nen dung cho production

Muc tieu sau build:

```text
wwwroot/css/app.3f8a2c19.css
wwwroot/Frontend.App.styles.91ce4a0b.css
```

Va `index.html` sau build phai tham chieu dung cac ten moi:

```html
<link rel="stylesheet" href="css/app.3f8a2c19.css" />
<link href="Frontend.App.styles.91ce4a0b.css" rel="stylesheet" />
```

Can co mot buoc build tu dong de:

1. Build frontend Blazor.
2. Tinh hash cua file CSS da sinh sau build, dac biet `Frontend.App.styles.css`.
3. Tao ban copy co ten gan hash hoac sinh asset bang bundler.
4. Cap nhat file HTML dau ra, khong sua `index.html` nguon bang hash co dinh.
5. Deploy dong bo `index.html` va toan bo asset hash moi.
6. Chi sau khi deploy thanh cong moi don dep asset hash cu theo chu ky an toan.

Khong duoc deploy `index.html` moi truoc asset hash moi. Neu xay ra, client se nhan 404 CSS tam thoi.

Voi Blazor WebAssembly, can giu cac file `_framework/*` theo co che build mac dinh cua .NET. Chi them hash cho tai nguyen tu quan ly nhu `css/app.css`, scoped stylesheet `Frontend.App.styles.css`, JavaScript tu viet, anh va font. Khong tu y doi ten file bootstrap `blazor.webassembly.js`.

## Quy trinh deploy an toan

1. Build frontend trong moi truong sach.
2. Kiem tra file HTML dau ra tham chieu dung CSS hash/version moi.
3. Upload asset CSS/JS moi truoc hoac cung luc voi `index.html`.
4. Cau hinh cache header theo bang o tren.
5. Neu dung CDN, purge chi `index.html` va URL asset cu/moi lien quan khi can; khong purge toan bo neu khong can thiet.
6. Mo cua so an danh, tai trang, kiem tra Network xem CSS co URL moi va status `200` hay `304` dung theo du kien.
7. Kiem tra tren mobile hoac mot mang khac neu CDN dang bat.

## Cach xu ly su co CSS cu da dang o production

1. Kiem tra source da co CSS moi hay chua.
2. Kiem tra file CSS tren origin co noi dung moi hay chua, bang cach mo dung URL tren server/deployment artifact.
3. Kiem tra `index.html` da tham chieu version/hash moi chua.
4. Trong DevTools > Network, bat `Disable cache`, reload trang va xem URL, response header `Cache-Control`, `Age`, `ETag`, `CF-Cache-Status` neu co Cloudflare.
5. Neu origin moi nhung CDN cu, purge dung URL tai Cloudflare/CDN va kiem tra Cache Rules.
6. Neu client dang dung service worker/PWA, cap nhat manifest/service worker va thong bao nguoi dung reload. Repository hien tai khong thay file PWA ro rang, nhung can kiem tra lai neu tinh nang nay duoc them sau.
7. Chi huong dan nguoi dung hard refresh la bien phap tam thoi, khong phai cach sua he thong.

## Nhung loi thuong gap

| Trieu chung | Nguyen nhan kha nang cao | Cach sua |
| --- | --- | --- |
| Chi mot so nguoi dung thay CSS cu | Browser/CDN cache theo URL cu | Doi hash/version va cau hinh header dung |
| Tat ca moi nguoi van thay CSS cu | Deploy khong cap nhat file origin hoac HTML | Kiem tra deployment artifact va `index.html` |
| Doi `?v=` nhung khong tac dung | CDN bo qua query string | Dung ten file co hash hoac sua Cache Rule |
| CSS trang bi mat sau deploy | HTML da tro toi hash moi nhung asset chua upload | Deploy asset truoc HTML; rollback HTML neu can |
| Chi scoped CSS trong `.razor.css` cu | Quen version/hash cho `Frontend.App.styles.css` | Cache bust ca `app.css` va stylesheet sinh boi Blazor |

## Checklist nghiem thu

- [ ] Sua mot quy tac CSS de nhin thay ro thay doi.
- [ ] Build va deploy release moi.
- [ ] URL cua `app.css` va `Frontend.App.styles.css` da doi khi noi dung cua chung doi.
- [ ] `index.html` khong co header `immutable` hoac TTL dai.
- [ ] Asset co hash co `public, max-age=31536000, immutable`.
- [ ] Cua so an danh tai duoc giao dien moi ma khong can hard refresh.
- [ ] DevTools Network khong co 404 cho CSS/JS moi.
- [ ] Neu co CDN, Cache Rule va purge da duoc kiem tra.

## Prompt giao cho AI khac

```text
Hay thiet ke va trien khai cache busting ben vung cho frontend Blazor WebAssembly trong repository nay.

Hien trang:
- File frontend/src/Frontend.App/wwwroot/index.html tham chieu:
  - css/app.css?v=20260828
  - Frontend.App.styles.css?v=20260828
- Du an la Microsoft.NET.Sdk.BlazorWebAssembly, target net9.0.
- Khong thay cau hinh Nginx, Docker hoac CDN trong repository; khong duoc tu gia dinh mot nha cung cap hosting cu the.

Yeu cau:
1. Phan tich pipeline build hien co va chon cach it rui ro de tu dong doi URL CSS/JS khi noi dung thay doi. Uu tien content hash trong ten file; chi dung build-version query string neu content hash khong the tich hop gon gang voi pipeline hien tai.
2. Khong hard-code hash cu the vao index.html nguon. Hash/version phai duoc sinh trong build/deploy.
3. Bao phu ca css/app.css va Frontend.App.styles.css, vi file thu hai la scoped CSS do Blazor sinh ra.
4. Khong doi ten hoac lam hu co che `_framework/blazor.webassembly.js` cua Blazor.
5. Tao tai lieu deploy trong docs/, bao gom header Cache-Control de xuat:
   - index.html: no-cache, max-age=0, must-revalidate
   - asset co hash: public, max-age=31536000, immutable
6. Neu khong the tu cau hinh header vi repository khong chua web server/CDN, chi ro chinh xac cau hinh nao can duoc them o ha tang; khong tao file Nginx/Cloudflare mau vo can cu.
7. Them cach kiem thu tu dong hoac script kiem tra build artifact, bao dam index.html tham chieu asset dang ton tai.
8. Giu nguyen cac thay doi khong lien quan trong worktree. Khong stage/commit file scratch o root.

Ket qua can bao gom:
- Danh sach file da sua va ly do.
- Lenh build/kiem thu da chay va ket qua.
- Cach rollback neu asset moi bi loi.
- Huong dan xu ly khi CDN cache van giu file cu.
```

## Quan he voi Redis cache

Cache busting giai quyet **tai nguyen tinh** nhu CSS, JavaScript, anh va font. Redis cache giai quyet **du lieu dong** nhu danh sach tu vung doc tu PostgreSQL. Day la hai bai toan khac nhau va nen trien khai doc lap:

```text
CSS hien cu  -> hash/version + Cache-Control + CDN purge khi can
Tu vung tai cham -> toi uu PostgreSQL + Redis cache-aside + pagination/virtualization
```

Khong nen dung Redis de sua loi CSS cache cu, va cung khong nen chi doi version CSS de sua van de API doc tu vung cham.
