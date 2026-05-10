# Consultancy Management - AWS Production Deployment Guide

This document prepares the current app for production deployment without changing business logic.

**End-to-end AWS walkthrough (RDS, SES, EC2, CloudFront, DNS):** see **[AWS_DEPLOYMENT_GUIDE.md](./AWS_DEPLOYMENT_GUIDE.md)**.

## 0) New server from scratch (Docker — API + Angular + PostgreSQL)

Use this on a fresh Linux VM with Docker Engine + Compose v2 installed.

1. Clone the repo and create env file:

```bash
git clone <your-repo-url> ConsultancyManagement && cd ConsultancyManagement
cp .env.deploy.example .env
nano .env   # set POSTGRES_PASSWORD, Jwt__Key (≥32 chars), Cors__AllowedOrigins__0, SMTP
```

2. **CORS:** `Cors__AllowedOrigins__0` must match how users open the SPA (scheme + host + port), e.g. `https://app.example.com` or `http://203.0.113.10` while testing without DNS/TLS.

3. Start everything:

```bash
docker compose -f docker-compose.prod.yml --env-file .env up -d --build
# or: ./scripts/deploy-docker-prod.sh
```

4. Verify:

```bash
curl -s http://127.0.0.1/health
docker compose -f docker-compose.prod.yml logs -f consultancy-api
```

What runs:

| Piece | Notes |
| --- | --- |
| **postgres** | Data in Docker volume `postgres_data` — back this up for production. |
| **consultancy-api** | ASP.NET on port **8000** inside the network only; uploads under volume `api_uploads`. |
| **web** | Nginx serves the Angular build and proxies **`/api`** and **`/health`** to the API. SPA uses **`apiBaseUrl: '/api'`** (`production-docker` Angular config). |

TLS: terminate HTTPS on the host (recommended) or put **Nginx/Caddy/Traefik** in front of port 80 with Let’s Encrypt; keep **`DISABLE_HTTPS_REDIRECTION=true`** on the API container when TLS is handled by the proxy.

### AWS: logins, JWT, forgot-password, and SMTP

For **login** and **reset password** to work end-to-end on AWS (EC2/ECS + RDS + CloudFront or ALB):

| Requirement | Why |
| --- | --- |
| **`Cors__AllowedOrigins__*`** | Must list the **exact** browser origins users use (e.g. `https://app.example.com`, `https://www.example.com`). Include **HTTPS** and omit trailing slashes. |
| **`Jwt__Key`** (≥32 chars, random) | Issued tokens fail validation if missing or rotated without redeploying both API instances with the same key. |
| **`PasswordReset__PublicResetUrlBase`** | Set to **`https://<your-spa-host>/reset-password`** so emails contain correct links when users might open the site via IP, `http`, or a non-canonical host. Without this, the link uses `window.location.origin` from the browser at request time. |
| **SMTP** | Password reset sends mail via **`Smtp__*`** env vars. On AWS, **Amazon SES** SMTP (`email-smtp.<region>.amazonaws.com`, port **587**) is typical: create SMTP credentials in SES, verify **sender domain/email**, allow **outbound TCP 587** from the API security group. **SES sandbox** only delivers to verified addresses until you move to production access in SES. |
| **HTTPS** | Serve the SPA over **HTTPS** (CloudFront + ACM). Mixed content (HTTPS page calling `http://` API) will be blocked by browsers — use **`https://…/api`** (split hosting) or same-origin **`/api`** (Docker/nginx stack). |
| **Angular routing on S3/CloudFront** | Configure **403/404 → `/index.html`** so `/reset-password` loads the SPA. |

**Smoke tests after deploy**

1. `GET https://api.<your-domain>/health` → `database: up`
2. Login from the **same URL** users will use in production (organization slug + seeded or known user).
3. Forgot password → receive mail → link opens **`/reset-password?...`** on HTTPS → submit new password → login again.

**External RDS instead of container Postgres:** omit the `postgres` service (use a custom compose override or run only `consultancy-api` + `web`) and set **`DATABASE_URL`** to your RDS connection string (SSL Mode `Require`). Apply the same **`Jwt__*`** and **`Cors__*`** variables.

**Split hosting (S3 + CloudFront + API subdomain):** build the SPA with `ng build --configuration production` and set **`environment.prod.ts`** `apiBaseUrl` to `https://api.yourdomain.com/api`. Use **`ConsultancyManagement.Api/.env`** / Compose API-only patterns from the sections below.

## 1) Project structure review

- Frontend (Angular): `consultancy-management-ui/`
- Backend (ASP.NET Core API): `ConsultancyManagement.Api/`
- Database layer + migrations: `ConsultancyManagement.Infrastructure/`
- Core domain/contracts: `ConsultancyManagement.Core/`
- Solution files: `ConsultancyManagement.DotNet.sln`, `ConsultancyManagement.slnx`

### Database connection files

- `ConsultancyManagement.Api/appsettings.json`
- `ConsultancyManagement.Api/appsettings.Development.json`
- `ConsultancyManagement.Infrastructure/DependencyInjection.cs`
- `ConsultancyManagement.Infrastructure/Data/ApplicationDbContextFactory.cs`

### Environment files

- Backend example env: `ConsultancyManagement.Api/.env.example`
- Frontend env files:
  - `consultancy-management-ui/src/environments/environment.ts`
  - `consultancy-management-ui/src/environments/environment.prod.ts`

### Build/package commands

#### Backend
```bash
dotnet restore
dotnet build ConsultancyManagement.Api/ConsultancyManagement.Api.csproj
dotnet run --project ConsultancyManagement.Api/ConsultancyManagement.Api.csproj
```

#### Frontend
```bash
cd consultancy-management-ui
npm install
npm run build
# or
npx ng build --configuration production
```

## 2) Backend production readiness checklist applied

Implemented in code:

- Environment-based DB support:
  - Uses `DATABASE_URL` first.
  - Falls back to `ConnectionStrings__DefaultConnection`.
- CORS now uses config/env via `Cors:AllowedOrigins`.
- Port configurable through `PORT` env var (`8000` target).
- Health endpoint added:
  - `GET /health`
- Added backend env template:
  - `ConsultancyManagement.Api/.env.example`

## 3) PostgreSQL -> AWS RDS migration steps

Current local DB name detected:

- `ConsultancyManagementDb`

Migration/ORM setup detected:

- Entity Framework Core + Npgsql provider
- Migrations folder:
  - `ConsultancyManagement.Infrastructure/Migrations/`

### Export local database (from your machine)

```bash
export PGPASSWORD='LOCAL_DB_PASSWORD'
pg_dump -h localhost -p 5432 -U postgres -d ConsultancyManagementDb -F c -f consultancy_local.dump
```

Alternative plain SQL export:
```bash
export PGPASSWORD='LOCAL_DB_PASSWORD'
pg_dump -h localhost -p 5432 -U postgres -d ConsultancyManagementDb > consultancy_local.sql
```

### Import into AWS RDS PostgreSQL

Create target DB/user first (if needed), then:

Custom format import:
```bash
export PGPASSWORD='RDS_PASSWORD'
pg_restore -h <rds-endpoint> -p 5432 -U <rds-username> -d <rds-db-name> --no-owner --no-privileges consultancy_local.dump
```

Plain SQL import:
```bash
export PGPASSWORD='RDS_PASSWORD'
psql -h <rds-endpoint> -p 5432 -U <rds-username> -d <rds-db-name> -f consultancy_local.sql
```

Backend env connection for RDS:
```env
DATABASE_URL=Host=<rds-endpoint>;Port=5432;Database=<rds-db-name>;Username=<rds-username>;Password=<rds-password>;SSL Mode=Require;Trust Server Certificate=true
```

## 4) Docker support (backend)

Added:

- Backend Dockerfile: `ConsultancyManagement.Api/Dockerfile`
- Compose file: `docker-compose.yml`
- Docker ignore: `.dockerignore`

### Build + run locally with Docker

```bash
cp ConsultancyManagement.Api/.env.example ConsultancyManagement.Api/.env
# edit ConsultancyManagement.Api/.env values

docker compose up -d --build
docker compose ps
curl http://localhost:8000/health
```

## 5) Angular production API config

Updated:

- `consultancy-management-ui/src/environments/environment.prod.ts`
  - `apiBaseUrl: 'https://api.mydomain.com/api'`

## 6) Frontend production build commands

```bash
cd consultancy-management-ui
npm install
npm run build
# or explicitly:
npx ng build --configuration production
```

Build output:

- `consultancy-management-ui/dist/consultancy-management-ui/browser/` (Angular v19 application builder output)

## 7) AWS EC2 deployment steps (backend API)

## 7.1 Create EC2 (Ubuntu)

- AMI: Ubuntu 22.04+ LTS
- Instance type: start with `t3.small` or `t3.medium`
- Security group inbound:
  - 22 (SSH) from your IP only
  - 80 from `0.0.0.0/0`
  - 443 from `0.0.0.0/0`

## 7.2 Connect and install packages

```bash
ssh -i <your-key>.pem ubuntu@<ec2-public-ip>
sudo apt update && sudo apt upgrade -y
sudo apt install -y ca-certificates curl gnupg lsb-release git nginx
```

## 7.3 Install Docker + Compose plugin

```bash
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker ubuntu
newgrp docker
docker --version
docker compose version
```

## 7.4 Clone project and configure env

```bash
cd /home/ubuntu
git clone https://github.com/karunakar8008/ConsultancyManagement.git
cd ConsultancyManagement

cp ConsultancyManagement.Api/.env.example ConsultancyManagement.Api/.env
nano ConsultancyManagement.Api/.env
```

Set at minimum in `.env`:

- `ASPNETCORE_ENVIRONMENT=Production`
- `PORT=8000`
- `DATABASE_URL=<RDS connection string>`
- `Jwt__Key=<strong-secret>`
- SMTP values
- `Cors__AllowedOrigins__0=https://mydomain.com`
- `Cors__AllowedOrigins__1=https://www.mydomain.com`

## 7.5 Run backend container

```bash
cd /home/ubuntu/ConsultancyManagement
docker compose up -d --build
docker compose ps
docker compose logs -f consultancy-api
curl http://localhost:8000/health
```

## 7.6 Restart backend when needed

```bash
cd /home/ubuntu/ConsultancyManagement
docker compose pull
docker compose up -d --build
docker compose logs -f consultancy-api
```

## 8) Nginx reverse proxy (backend)

Config file already added:

- `nginx/consultancy-api.conf`

Install on EC2:

```bash
sudo cp /home/ubuntu/ConsultancyManagement/nginx/consultancy-api.conf /etc/nginx/sites-available/consultancy-api.conf
sudo ln -s /etc/nginx/sites-available/consultancy-api.conf /etc/nginx/sites-enabled/consultancy-api.conf
sudo nginx -t
sudo systemctl restart nginx
```

## 9) HTTPS setup (Certbot + Nginx)

```bash
sudo apt install certbot python3-certbot-nginx -y
sudo certbot --nginx -d api.mydomain.com
```

Verify auto-renew:
```bash
sudo systemctl status certbot.timer
sudo certbot renew --dry-run
```

## 10) Frontend deployment to S3

## 10.1 Build frontend locally

```bash
cd consultancy-management-ui
npm install
npx ng build --configuration production
```

## 10.2 Create S3 bucket and upload

```bash
aws s3 mb s3://mydomain.com --region us-east-1
aws s3 sync dist/consultancy-management-ui/browser s3://mydomain.com --delete
```

You can keep the bucket private if serving through CloudFront OAC (recommended).

## 11) CloudFront setup (frontend)

Create a CloudFront distribution:

- Origin: S3 bucket (`mydomain.com`)
- Viewer protocol policy: Redirect HTTP to HTTPS
- Alternate domain (CNAME): `mydomain.com`, `www.mydomain.com` (if needed)
- Certificate: AWS ACM cert (us-east-1) for your domain
- Default root object: `index.html`
- Error responses for Angular routes:
  - `403 -> /index.html -> 200`
  - `404 -> /index.html -> 200`

After deployment, invalidate cache on new releases:

```bash
aws cloudfront create-invalidation --distribution-id <CF_DIST_ID> --paths "/*"
```

## 12) Route 53 / DNS setup

Create records:

- `A (Alias)` for `mydomain.com` -> CloudFront distribution
- `A (Alias)` for `www.mydomain.com` -> CloudFront distribution (optional)
- `A` record for `api.mydomain.com` -> EC2 public IP (or ALB DNS if using load balancer)

## 13) Security checklist

- Do not expose PostgreSQL publicly unless strictly required.
- Prefer RDS in private subnets.
- Allow RDS inbound only from EC2 security group.
- EC2 inbound:
  - `22` only from your IP
  - `80/443` public
- Store secrets only in `.env` / secure secret manager.
- Never commit `.env`.
- Enforce HTTPS on API and frontend.
- Enable RDS automated backups + retention.
- Set strict CORS origins to real frontend domains.
- Enable application logs:
  - `docker compose logs -f consultancy-api`
  - Nginx logs: `/var/log/nginx/access.log`, `/var/log/nginx/error.log`

## 14) Final expected live URLs

- Frontend: `https://mydomain.com`
- Backend API: `https://api.mydomain.com`
- Health: `https://api.mydomain.com/health`

## 15) Files added/updated for production preparation

Added:

- `ConsultancyManagement.Api/.env.example`
- `ConsultancyManagement.Api/Dockerfile`
- `docker-compose.yml`
- `.dockerignore`
- `nginx/consultancy-api.conf`
- `DEPLOYMENT.md`

Updated:

- `ConsultancyManagement.Api/Program.cs`
- `ConsultancyManagement.Api/appsettings.json`
- `ConsultancyManagement.Api/appsettings.Development.json`
- `ConsultancyManagement.Infrastructure/DependencyInjection.cs`
- `ConsultancyManagement.Infrastructure/Data/ApplicationDbContextFactory.cs`
- `consultancy-management-ui/src/environments/environment.prod.ts`

## Optional: Run EF migrations against RDS from EC2/app host

If schema changes are needed post-deploy:

```bash
dotnet ef database update \
  --project ConsultancyManagement.Infrastructure \
  --startup-project ConsultancyManagement.Api
```

