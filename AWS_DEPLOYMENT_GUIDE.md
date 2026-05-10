# AWS deployment — step-by-step

This guide walks through deploying **Consultancy Management** on AWS so **login**, **password reset**, and the SPA all work. Pick **one path**: **A** (single EC2 + Docker, simplest) or **B** (RDS + EC2 API + CloudFront/S3 UI, common for production).

**Terms**

| Name | Example | Role |
|------|---------|------|
| SPA / frontend | `https://app.example.com` | Angular app |
| API | `https://api.example.com` | ASP.NET Core |
| Organization code | `default` | Login field; seeded in DB |

**Secrets**

Never commit real passwords. Use **AWS Systems Manager Parameter Store** or **Secrets Manager** for production; below uses a root `.env` on the server for clarity.

---

## Before you start — checklist

- [ ] Domain registered (Route 53 or any registrar; DNS must be manageable).
- [ ] Decide: **Path A** (all-in-one Docker on EC2) vs **Path B** (RDS + split hosting).
- [ ] Install **AWS CLI** locally (`aws configure` with access key or SSO).

---

## Phase 1 — Region and naming

1. Choose an **AWS Region** (e.g. `us-east-1`) and use it consistently for RDS, EC2, SES, ACM, S3, CloudFront.
2. Pick hostnames, for example:
   - SPA: `app.example.com`
   - API: `api.example.com`

---

## Phase 2 — TLS certificates (ACM)

Browsers require **HTTPS** for production-grade behavior (cookies, mixed content, trust).

1. Open **AWS Certificate Manager (ACM)** in the **same region** as your load balancer / CloudFront.
2. **Request** a public certificate for:
   - `app.example.com`
   - `api.example.com`
   - (Optional) `www.app.example.com`
3. **Validate** via **DNS** (add CNAME records Route 53 can create automatically).
4. Wait until status is **Issued**.

**CloudFront** requires the **ACM certificate to be in `us-east-1`** if you attach it to CloudFront. API on ALB can use a cert in the **ALB’s region**.

---

## Phase 3 — PostgreSQL (RDS)

### Path A (DB on same VM as Docker)

You can skip RDS for a small setup and use the **Postgres container** in `docker-compose.prod.yml` (data in a Docker volume). **Back up the volume** or snapshot the EBS volume; not ideal for strict HA.

### Path B (recommended): Amazon RDS for PostgreSQL

1. **RDS → Create database**
   - Engine: **PostgreSQL** (16+ compatible).
   - Template: **Production** or **Dev/Test** as appropriate.
   - **VPC**: default VPC is OK to start; prefer private subnets + bastion for stricter setups.
   - **Public access**: **No** (recommended). EC2 in same VPC/security group access only.
   - **Security group**: inbound **5432** only from the **EC2/ECS security group** (or single IP if you must).
   - Master username/password: save securely.
   - Initial database name: e.g. `consultancy`.
2. After creation, note:
   - **Endpoint** host (e.g. `consultancy.xxxx.us-east-1.rds.amazonaws.com`)
   - **Port** `5432`
   - **Username / password**

**Connection string** for the API (SSL):

```text
Host=<RDS_ENDPOINT>;Port=5432;Database=consultancy;Username=<USER>;Password=<PASSWORD>;SSL Mode=Require;Trust Server Certificate=true
```

Store this as **`DATABASE_URL`** for the API container/service.

---

## Phase 4 — Email (Amazon SES)

Password reset emails require SMTP.

1. **SES** → **Verified identities**: verify your **domain** (recommended) or at least **From** email.
2. **SMTP settings**: create **SMTP credentials** (IAM user); note host like  
   `email-smtp.<region>.amazonaws.com`, port **587**, STARTTLS.
3. While account is in **SES sandbox**, you can only send **to verified recipient emails**. Request **production access** in SES to send to any address.
4. **Security group** for EC2/ECS: allow **outbound TCP 587** (and **443** for HTTPS/API calls).

Env vars for the API (example):

```bash
Smtp__Host=email-smtp.us-east-1.amazonaws.com
Smtp__Port=587
Smtp__Username=<SES_SMTP_USERNAME>
Smtp__Password=<SES_SMTP_PASSWORD>
Smtp__FromEmail=noreply@yourdomain.com
Smtp__FromName=ConsultancyManagement Solutions
Smtp__EnableSsl=true
```

---

## Phase 5 — EC2 (compute)

1. **Launch instance**
   - AMI: **Ubuntu 22.04 LTS** (or Amazon Linux 2023).
   - Instance type: **t3.small** or larger (API + optional Docker).
   - Key pair: create/download `.pem` for SSH.
2. **Security group (inbound)**
   - **22** — SSH from **your IP only** (not `0.0.0.0/0` long term).
   - **80** — HTTP (if nginx or ALB forwards HTTP).
   - **443** — HTTPS (if terminating TLS on this instance, or use ALB).
3. **Outbound**: default “all” is OK for SMTP/API; tighten in hardened environments.

4. **Elastic IP** (optional): associate a static IP if you point **api.example.com** directly at EC2.

---

## Phase 6 — Install Docker on EC2

SSH in:

```bash
ssh -i your-key.pem ubuntu@<EC2_PUBLIC_IP>
```

Install Docker + Compose ([Docker docs](https://docs.docker.com/engine/install/ubuntu/)):

```bash
sudo apt update && sudo apt install -y ca-certificates curl gnupg
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
sudo usermod -aG docker ubuntu
newgrp docker
```

---

## Phase 7 — Deploy application (Path A: all-in-one Docker)

On EC2:

```bash
cd ~
git clone <YOUR_REPO_URL> ConsultancyManagement
cd ConsultancyManagement
cp .env.deploy.example .env
nano .env
```

Fill **at minimum**:

| Variable | Purpose |
|----------|---------|
| `POSTGRES_PASSWORD` | Strong password (internal DB container). |
| `Jwt__Key` | Random string **≥ 32 characters**. |
| `Cors__AllowedOrigins__0` | Exact SPA origin, e.g. `https://app.example.com` (see Phase 9). |
| `PasswordReset__PublicResetUrlBase` | `https://app.example.com/reset-password` |
| `Smtp__*` | SES or other SMTP (Phase 4). |

Start:

```bash
docker compose -f docker-compose.prod.yml --env-file .env up -d --build
docker compose -f docker-compose.prod.yml logs -f consultancy-api
```

Verify locally on the server:

```bash
curl -s http://127.0.0.1/health
```

You should see JSON with `"database": "up"`.

**Notes**

- Stack = **postgres + consultancy-api + nginx** (Angular built with same-origin `/api`).
- Uploads persist in Docker volume **`api_uploads`**.
- DB data in **`postgres_data`** — **back up** for production.

---

## Phase 7b — Deploy application (Path B: RDS + API + separate SPA)

### API on EC2 (Docker without bundled Postgres)

1. Use **`docker-compose.prod.yml`** as a base but **remove** the `postgres` service **or** use a **compose override** that deletes `postgres` and sets only:

```yaml
environment:
  DATABASE_URL: "<RDS connection string from Phase 3>"
```

2. Run **only** `consultancy-api` (and optionally **nginx** if you serve the SPA from the same machine — otherwise build SPA separately).

### Build SPA for split hosting

On your laptop or CI:

```bash
cd consultancy-management-ui
npm ci
```

Edit **`src/environments/environment.prod.ts`**:

```typescript
apiBaseUrl: 'https://api.example.com/api',
```

Then:

```bash
npx ng build --configuration production
```

Upload **`dist/consultancy-management-ui/browser/`** to **S3** (see Phase 8).

### API `.env` highlights (Path B)

```bash
DATABASE_URL=Host=....rds.amazonaws.com;Port=5432;Database=consultancy;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
ASPNETCORE_ENVIRONMENT=Production
PORT=8000
DISABLE_HTTPS_REDIRECTION=true
Jwt__Key=<32+ chars>
Cors__AllowedOrigins__0=https://app.example.com
PasswordReset__PublicResetUrlBase=https://app.example.com/reset-password
# ... Smtp__* ...
```

Run API container binding **8000**; put **Nginx** or **ALB** in front with HTTPS.

---

## Phase 8 — Frontend on S3 + CloudFront (Path B)

1. **S3**: create bucket (private); upload **`browser/`** build output.
2. **CloudFront**
   - Origin: S3 (use origin access).
   - **Alternate domain names (CNAMEs)**: `app.example.com`.
   - **SSL certificate**: ACM in **us-east-1** for CloudFront.
   - **Default root object**: `index.html`
   - **Error responses**: **403** and **404** → `/index.html` with **200** (SPA routing, including `/reset-password`).
3. Invalidate cache after each deploy: `/*`.

---

## Phase 9 — DNS (Route 53)

Create records (adjust to your layout):

| Record | Target |
|--------|--------|
| `app.example.com` | CloudFront distribution domain (Path B) **or** EC2 / ALB (Path A). |
| `api.example.com` | EC2 Elastic IP, ALB, or NLB where the API listens. |

For **Path A** (single EC2 on port 80): **A record** to Elastic IP.

For **ALB**: **A (Alias)** to the load balancer; target group → EC2:80 or container port.

---

## Phase 10 — Reverse proxy and HTTPS (API)

- **Option 1**: **Nginx on EC2** with Let’s Encrypt (`certbot --nginx`) for `api.example.com` → proxy to `localhost:8000`.
- **Option 2**: **Application Load Balancer** with **HTTPS listener** + ACM cert → forward to target **HTTP:8000** on instances.

Enable **`X-Forwarded-For`** / **`X-Forwarded-Proto`** so the app sees HTTPS (already supported via **ForwardedHeaders** in the API).

Keep **`DISABLE_HTTPS_REDIRECTION=true`** on Kestrel when TLS terminates at nginx/ALB.

---

## Phase 11 — Final configuration matrix

| Item | Value |
|------|--------|
| Login API URL | `environment.prod.ts` → `apiBaseUrl` OR Docker same-origin `/api` |
| CORS | Must match SPA URL(s) exactly |
| JWT | Same secret on all API replicas |
| Reset email link | `PasswordReset__PublicResetUrlBase` = `https://app.../reset-password` |
| SES | Production access + verified domain |
| DB | RDS reachable from API security group |

---

## Phase 12 — Smoke tests

1. `curl https://api.example.com/health` → `"database":"up"`.
2. Open `https://app.example.com`, login (**organization** `default` + user from seed or created admin).
3. **Forgot password** → email arrives → link uses **`https://app.../reset-password?...`** → set new password → login.
4. Check **Docker logs** / **CloudWatch** if anything fails.

---

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| Login CORS error | `Cors__AllowedOrigins__*` doesn’t match browser URL (scheme/host/port). |
| Invalid credentials | Wrong org slug, user not in org, lockout, or wrong DB. |
| No reset email | SES sandbox, unverified sender, blocked outbound 587, wrong SMTP creds. |
| Reset link wrong host | Set **`PasswordReset__PublicResetUrlBase`**. |
| 404 on `/reset-password` | CloudFront/S3 error pages not rewriting to `index.html`. |
| Mixed content | SPA is HTTPS but `apiBaseUrl` is `http://` — use **https** for API URL. |

---

## Cost and ops tips

- Turn off unused dev RDS/EC2.
- Enable **RDS backups**; snapshot before major upgrades.
- Rotate **`Jwt__Key`** requires all users to log in again after deploy (tokens invalid).
- Do not expose PostgreSQL **publicly**; keep RDS private where possible.

For more context (Docker file layout, local compose), see **`DEPLOYMENT.md`**.
