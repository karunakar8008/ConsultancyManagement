# Consultancy Management - AWS Production Deployment Guide

This document prepares the current app for production deployment without changing business logic.

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

