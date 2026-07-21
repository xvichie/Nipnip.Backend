const express = require('express');
const puppeteer = require('puppeteer-extra');
const StealthPlugin = require('puppeteer-extra-plugin-stealth');

puppeteer.use(StealthPlugin());

const PORT = process.env.PORT || 8080;
const SCRAPER_SECRET = process.env.SCRAPER_SECRET;
// Real headless Chrome (with stealth patches) reliably passes MyMarket's Cloudflare
// managed challenge — a plain server-side fetch gets a 403 almost every time, this doesn't.
const MYMARKET_PRODUCT_URL = id => `https://api.mymarket.ge/api/ka/products/${id}/`;
// A lightweight, always-valid GET on the same host — used purely to land the page on
// api.mymarket.ge before firing an in-page fetch() POST, so the request's Origin header
// (and CORS preflight) look like real site traffic, and to pick up any existing
// cf_clearance cookie the browser already solved on a prior request.
const MYMARKET_WARMUP_URL = 'https://api.mymarket.ge/api/ka/category?CatID=0';
const MYMARKET_PRODUCTS_LIST_URL = 'https://api.mymarket.ge/api/ka/products';

// One browser instance shared across requests — launching Chrome is the slow part
// (multiple seconds), so keeping it warm makes every request after the first fast.
// Only one page navigation runs at a time; concurrent requests queue behind it rather
// than piling up pages, which would blow past this container's memory budget.
let browserPromise = null;
let queue = Promise.resolve();

async function getBrowser() {
  if (!browserPromise) {
    browserPromise = puppeteer.launch({
      headless: 'new',
      args: ['--no-sandbox', '--disable-setuid-sandbox'],
    });
    browserPromise.then(browser => {
      browser.on('disconnected', () => { browserPromise = null; });
    });
  }
  return browserPromise;
}

async function fetchProduct(id) {
  const browser = await getBrowser();
  const page = await browser.newPage();
  try {
    await page.setExtraHTTPHeaders({ Accept: 'application/json' });
    await page.goto(MYMARKET_PRODUCT_URL(id), { waitUntil: 'networkidle0', timeout: 25000 });
    // Give the challenge a moment to finish resolving/redirecting before reading the body.
    await new Promise(resolve => setTimeout(resolve, 2000));
    const text = await page.evaluate(() => document.body.innerText);
    return JSON.parse(text);
  } finally {
    await page.close().catch(() => {});
  }
}

// Mirrors the exact request MyMarket's own shop page fires (sniffed from
// mymarket.ge/shops/{id}/?Tab=products): a POST with CatID "0" (all categories) and
// ShopIDs as a string. Needs a real page navigation first (not just a bare fetch) so the
// POST's Origin header and CORS preflight match what their site actually sends.
async function fetchShopProducts(shopId, pageNum) {
  const browser = await getBrowser();
  const page = await browser.newPage();
  try {
    await page.setExtraHTTPHeaders({ Accept: 'application/json' });
    await page.goto(MYMARKET_WARMUP_URL, { waitUntil: 'networkidle0', timeout: 25000 });
    await new Promise(resolve => setTimeout(resolve, 1000));

    const text = await page.evaluate(
      async (url, body) => {
        const res = await fetch(url, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
          body: JSON.stringify(body),
        });
        return res.text();
      },
      MYMARKET_PRODUCTS_LIST_URL,
      { CatID: '0', Page: pageNum, Limit: 28, ShopIDs: shopId }
    );
    return JSON.parse(text);
  } finally {
    await page.close().catch(() => {});
  }
}

function runQueued(task) {
  const result = queue.then(task);
  // Swallow rejections here so one failed job doesn't wedge the queue for the next caller —
  // the actual error still propagates to whoever awaited `result` below.
  queue = result.catch(() => {});
  return result;
}

const app = express();

app.get('/health', (_req, res) => res.json({ ok: true }));

app.get('/mymarket/product/:id', async (req, res) => {
  if (SCRAPER_SECRET && req.get('x-scraper-secret') !== SCRAPER_SECRET) {
    return res.status(401).json({ error: 'Unauthorized' });
  }

  const { id } = req.params;
  if (!/^\d+$/.test(id)) {
    return res.status(400).json({ error: 'Invalid product id' });
  }

  try {
    const data = await runQueued(() => fetchProduct(id));
    res.json(data);
  } catch (err) {
    console.error(`mymarket-scraper: failed to fetch product ${id}:`, err.message);
    res.status(502).json({ error: 'Failed to fetch product from MyMarket.' });
  }
});

app.get('/mymarket/shop/:shopId/products', async (req, res) => {
  if (SCRAPER_SECRET && req.get('x-scraper-secret') !== SCRAPER_SECRET) {
    return res.status(401).json({ error: 'Unauthorized' });
  }

  const { shopId } = req.params;
  if (!/^\d+$/.test(shopId)) {
    return res.status(400).json({ error: 'Invalid shop id' });
  }
  const pageNum = Math.max(1, parseInt(req.query.page, 10) || 1);

  try {
    const data = await runQueued(() => fetchShopProducts(shopId, pageNum));
    res.json(data);
  } catch (err) {
    console.error(`mymarket-scraper: failed to fetch products for shop ${shopId} page ${pageNum}:`, err.message);
    res.status(502).json({ error: 'Failed to fetch products from MyMarket.' });
  }
});

app.listen(PORT, () => console.log(`mymarket-scraper listening on :${PORT}`));

async function shutdown() {
  if (browserPromise) {
    const browser = await browserPromise.catch(() => null);
    if (browser) await browser.close().catch(() => {});
  }
  process.exit(0);
}
process.on('SIGTERM', shutdown);
process.on('SIGINT', shutdown);
