import { router } from './router.mjs';
import * as crypto from 'crypto';
import config from './config.mjs';
import Koa from 'koa';
import logger from 'koa-logger';
import serve from 'koa-static';
import session from 'koa-session';
import { fileURLToPath } from 'url';
import path from 'path';

function createInMemorySessionStore() {
  const map = new Map();
  return {
    get: map.get.bind(map),
    set: map.set.bind(map),
    destroy: map.delete.bind(map),
  };
}

const __filename = fileURLToPath(import.meta.url);
const frontendPath = path.join(path.dirname(__filename), 'frontend');

const app = new Koa();

// (Optional) Log all requests to this server
app.use(logger());

// Serve the static frontend
app.use(serve(frontendPath));

// Manage sessions using an in-memory session store and signed, SameSite=Lax, HttpOnly cookies
app.keys = [crypto.randomBytes(8).toString('hex')];
app.use(session({ store: createInMemorySessionStore(), sameSite: 'lax', httpOnly: true }, app));

// Serve the backend routes
app.use(router.routes()).listen(config.SERVER_PORT);

console.log(`[INFO]: Server started at http://localhost:${config.SERVER_PORT}\n`);
