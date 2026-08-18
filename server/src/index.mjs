import { createServer } from "node:http";
import { fileURLToPath } from "node:url";
import { Store } from "./store.mjs";

const PORT = Number(process.env.PORT || 8787);
const HOST = process.env.HOST || "0.0.0.0";
const defaultData = fileURLToPath(new URL("../data/workshop.json", import.meta.url));
const store = new Store(process.env.DATA_FILE || defaultData);

const hits = new Map();

function rateLimit(ip) {
  const now = Date.now();
  const windowMs = 10_000;
  const list = (hits.get(ip) || []).filter((t) => now - t < windowMs);
  list.push(now);
  hits.set(ip, list);
  return list.length <= 40;
}

function readBody(req, limit = 200_000) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;
    req.on("data", (chunk) => {
      size += chunk.length;
      if (size > limit) {
        reject(Object.assign(new Error("Payload too large"), { status: 413 }));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on("end", () => {
      if (chunks.length === 0) {
        resolve({});
        return;
      }
      try {
        resolve(JSON.parse(Buffer.concat(chunks).toString("utf8")));
      } catch {
        reject(Object.assign(new Error("Invalid JSON"), { status: 400 }));
      }
    });
    req.on("error", reject);
  });
}

function send(res, status, body) {
  const json = JSON.stringify(body);
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": Buffer.byteLength(json),
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Headers": "Content-Type, X-Client-Id",
    "Access-Control-Allow-Methods": "GET,POST,PATCH,OPTIONS"
  });
  res.end(json);
}

function clientId(req) {
  const id = req.headers["x-client-id"];
  return typeof id === "string" && id.length >= 8 && id.length <= 64 ? id : "";
}

function parseUrl(req) {
  return new URL(req.url || "/", `http://${req.headers.host || "localhost"}`);
}

const server = createServer(async (req, res) => {
  try {
    if (req.method === "OPTIONS") {
      res.writeHead(204, {
        "Access-Control-Allow-Origin": "*",
        "Access-Control-Allow-Headers": "Content-Type, X-Client-Id",
        "Access-Control-Allow-Methods": "GET,POST,PATCH,OPTIONS"
      });
      res.end();
      return;
    }

    const ip = req.socket.remoteAddress || "unknown";
    if (!rateLimit(ip)) {
      send(res, 429, { error: "Too many requests" });
      return;
    }

    const url = parseUrl(req);
    const uid = clientId(req);

    if (req.method === "GET" && url.pathname === "/api/health") {
      send(res, 200, { ok: true });
      return;
    }

    if (req.method === "GET" && url.pathname === "/api/workshop") {
      const kind = url.searchParams.get("kind") || "crosshair";
      const sort = url.searchParams.get("sort") || "likes";
      const q = url.searchParams.get("q") || "";
      send(res, 200, { items: store.list({ kind, sort, q, clientId: uid }) });
      return;
    }

    const itemMatch = url.pathname.match(/^\/api\/workshop\/([^/]+)(?:\/(like))?$/);
    if (itemMatch && req.method === "GET" && !itemMatch[2]) {
      const item = store.get(decodeURIComponent(itemMatch[1]), uid);
      if (!item) {
        send(res, 404, { error: "Not found" });
        return;
      }
      send(res, 200, item);
      return;
    }

    if (req.method === "POST" && url.pathname === "/api/workshop") {
      if (!uid) {
        send(res, 401, { error: "Missing X-Client-Id" });
        return;
      }
      const body = await readBody(req);
      const item = store.publish(uid, body);
      send(res, 200, store.publicView(item, uid));
      return;
    }

    if (itemMatch && req.method === "PATCH" && !itemMatch[2]) {
      if (!uid) {
        send(res, 401, { error: "Missing X-Client-Id" });
        return;
      }
      const body = await readBody(req);
      const item = store.patch(decodeURIComponent(itemMatch[1]), uid, body);
      if (!item) {
        send(res, 404, { error: "Not found" });
        return;
      }
      send(res, 200, store.publicView(item, uid));
      return;
    }

    if (itemMatch && itemMatch[2] === "like" && req.method === "POST") {
      if (!uid) {
        send(res, 401, { error: "Missing X-Client-Id" });
        return;
      }
      const item = store.toggleLike(decodeURIComponent(itemMatch[1]), uid);
      if (!item) {
        send(res, 404, { error: "Not found" });
        return;
      }
      send(res, 200, store.publicView(item, uid));
      return;
    }

    send(res, 404, { error: "Not found" });
  } catch (err) {
    send(res, err.status || 400, { error: err.message || "Bad request" });
  }
});

await store.load();
server.listen(PORT, HOST, () => {
  console.log(`Crosshair Studio workshop on http://${HOST}:${PORT}`);
});
