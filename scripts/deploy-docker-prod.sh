#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if [[ ! -f .env ]]; then
  echo "Creating .env from .env.deploy.example — edit required secrets before production."
  cp .env.deploy.example .env
fi

echo "Building and starting stack (docker-compose.prod.yml)..."
docker compose -f docker-compose.prod.yml --env-file .env up -d --build

echo "Waiting for health..."
sleep 3
if curl -sf "http://127.0.0.1:${HTTP_PORT:-80}/health" >/dev/null; then
  echo "Health OK at http://127.0.0.1:${HTTP_PORT:-80}/health"
else
  echo "Health check failed — run: docker compose -f docker-compose.prod.yml logs"
  exit 1
fi
