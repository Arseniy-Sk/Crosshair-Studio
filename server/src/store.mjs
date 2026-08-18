import { mkdir, readFile, rename, writeFile } from "node:fs/promises";
import { dirname } from "node:path";
import { randomBytes } from "node:crypto";

const KINDS = new Set(["crosshair", "widget"]);

function clip(value, max) {
  return String(value ?? "").trim().slice(0, max);
}

function id() {
  return randomBytes(6).toString("hex");
}

export class Store {
  constructor(file) {
    this.file = file;
    this.items = [];
    this.queue = Promise.resolve();
  }

  async load() {
    try {
      const raw = await readFile(this.file, "utf8");
      const data = JSON.parse(raw);
      this.items = Array.isArray(data.items) ? data.items : [];
    } catch (err) {
      if (err.code !== "ENOENT")
        console.error("Failed to read store", err);
      this.items = [];
      await this.save();
    }
  }

  save() {
    this.queue = this.queue.then(async () => {
      await mkdir(dirname(this.file), { recursive: true });
      const tmp = this.file + ".tmp";
      await writeFile(tmp, JSON.stringify({ items: this.items }, null, 2));
      await rename(tmp, this.file);
    }).catch((err) => console.error("Failed to save store", err));
    return this.queue;
  }

  list({ kind, sort, q, clientId }) {
    const query = clip(q, 80).toLowerCase();
    let items = this.items.filter((item) => item.listed && (!kind || item.kind === kind));
    if (query) {
      items = items.filter((item) =>
        `${item.name} ${item.description} ${item.author}`.toLowerCase().includes(query));
    }
    items.sort((a, b) => {
      if (sort === "new")
        return Date.parse(b.createdAt) - Date.parse(a.createdAt);
      const likes = (b.likes?.length || 0) - (a.likes?.length || 0);
      return likes !== 0 ? likes : Date.parse(b.updatedAt) - Date.parse(a.updatedAt);
    });
    return items.slice(0, 120).map((item) => this.publicView(item, clientId));
  }

  get(itemId, clientId) {
    const item = this.items.find((entry) => entry.id === itemId);
    return item ? this.publicView(item, clientId) : null;
  }

  publish(authorId, body) {
    const kind = KINDS.has(body.kind) ? body.kind : null;
    if (!kind)
      throw Object.assign(new Error("Invalid kind"), { status: 400 });
    const name = clip(body.name, 48);
    if (!name)
      throw Object.assign(new Error("Name required"), { status: 400 });
    if (!body.payload || typeof body.payload !== "object")
      throw Object.assign(new Error("Payload required"), { status: 400 });

    const now = new Date().toISOString();
    let item = body.id ? this.items.find((entry) => entry.id === body.id) : null;
    if (item && item.authorId !== authorId)
      throw Object.assign(new Error("Forbidden"), { status: 403 });

    if (!item) {
      item = {
        id: id(),
        kind,
        authorId,
        likes: [],
        createdAt: now
      };
      this.items.push(item);
    }

    item.name = name;
    item.description = clip(body.description, 240);
    item.author = clip(body.author, 32) || "Player";
    item.listed = Boolean(body.listed);
    item.payload = body.payload;
    item.updatedAt = now;
    this.save();
    return item;
  }

  patch(itemId, authorId, body) {
    const item = this.items.find((entry) => entry.id === itemId);
    if (!item)
      return null;
    if (item.authorId !== authorId)
      throw Object.assign(new Error("Forbidden"), { status: 403 });
    if (typeof body.listed === "boolean")
      item.listed = body.listed;
    if (typeof body.name === "string")
      item.name = clip(body.name, 48) || item.name;
    if (typeof body.description === "string")
      item.description = clip(body.description, 240);
    item.updatedAt = new Date().toISOString();
    this.save();
    return item;
  }

  toggleLike(itemId, clientId) {
    const item = this.items.find((entry) => entry.id === itemId);
    if (!item)
      return null;
    item.likes ??= [];
    const index = item.likes.indexOf(clientId);
    if (index >= 0)
      item.likes.splice(index, 1);
    else
      item.likes.push(clientId);
    item.updatedAt = new Date().toISOString();
    this.save();
    return item;
  }

  publicView(item, clientId) {
    return {
      id: item.id,
      kind: item.kind,
      name: item.name,
      description: item.description,
      author: item.author,
      listed: item.listed,
      likeCount: item.likes?.length || 0,
      liked: Boolean(clientId && item.likes?.includes(clientId)),
      owned: item.authorId === clientId,
      createdAt: item.createdAt,
      payload: item.payload
    };
  }
}
