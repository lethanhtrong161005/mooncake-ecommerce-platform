# Staging containers

This Compose stack runs the Vite frontend through Nginx and the ASP.NET Core WebApi. It does not start a database. The WebApi reads the existing `src/Mooncake.EcommercePlatform.WebApi/appsettings.json` through a read-only bind mount. The image itself contains only the non-secret sample configuration.

1. On the deployment host, place the backend repository and `mooncake-fe` in the same parent directory. Put your existing `appsettings.json` at `src/Mooncake.EcommercePlatform.WebApi/appsettings.json` inside the backend repository. Its `ConnectionStrings:DefaultConnection` can be the **same PostgreSQL connection string used locally**. Keep this file private; it is ignored by Git and excluded from both Docker build contexts.
2. Copy `staging.env.example` to `.env` in this directory and set `STG_PUBLIC_ORIGIN` to the staging HTTPS origin. Compose loads `.env` automatically when run from this directory. It uses the origin to override the local Google callback and CORS origin without editing `appsettings.json`. Register `<STG_PUBLIC_ORIGIN>/auth/google/callback` with Google. TLS should terminate at the public reverse proxy, which forwards to `127.0.0.1:5173` by default.
3. From `docker/stg`, run:

   ```powershell
   Copy-Item staging.env.example .env
   # Edit .env and set STG_PUBLIC_ORIGIN to your actual HTTPS staging origin.
   docker compose config --quiet
   docker compose up --build -d
   ```

4. Check `docker compose ps`. The frontend container listens on port `5173` and the backend container listens on port `8089`; both ports are published to the host at `127.0.0.1:5173` and `127.0.0.1:8089` by default. Open the staging origin. `/api/` and `/hubs/` are proxied by the frontend container to the separate WebApi container, including WebSocket upgrade requests. If you already use `staging.env`, keep it and run `docker compose --env-file staging.env up --build -d` from this directory.

`VITE_*` values are embedded when the frontend image is built. Rebuild the frontend image after changing them. The backend image contains only `appsettings.sample.json`; the real `appsettings.json` is mounted read-only as `/app/appsettings.json` when the container starts. ASP.NET Core environment variables override individual settings from that file, so `STG_PUBLIC_ORIGIN` supplies the staging callback and CORS origin while the database and other credentials remain in your existing file.

The stack does not apply migrations or run `--bootstrap-platform`. Keep the shared database schema compatible with both local and staging binaries before deployment. Both WebApi instances register recurring jobs; running local and staging concurrently against the same database may execute jobs twice unless the jobs use distributed coordination. Check this before enabling staging workers alongside local.

When local development and staging run on the same host, stop the local WebApi on port `8089` before starting this Compose stack. They can share the database, but two processes cannot bind the same host port simultaneously.
