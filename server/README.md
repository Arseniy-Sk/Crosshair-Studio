# Crosshair Studio Workshop

Node 18+. Extra npm packages are not required.

From the **server** folder:

```bash
cd ~/Crosshair-Studio/server
node src/index.mjs
```

or:

```bash
cd ~/Crosshair-Studio/server
chmod +x start.sh
./start.sh
```

If you are already in `server/src`:

```bash
node index.mjs
```

Do not run `node src/index.mjs` from inside `src` — that looks for `src/src/index.mjs` by mistake.

Listens on `0.0.0.0:8787`. Data: `server/data/workshop.json`.

```bash
PORT=8787 HOST=0.0.0.0 node src/index.mjs
```

systemd:

```
[Service]
WorkingDirectory=/root/Crosshair-Studio/server
ExecStart=/usr/bin/node src/index.mjs
Restart=always
Environment=PORT=8787
```

Open port **8787**. The app uses `http://150.251.152.203:8787` by default.
