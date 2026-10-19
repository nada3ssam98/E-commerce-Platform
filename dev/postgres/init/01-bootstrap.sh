#!/usr/bin/env bash
# Creates the platform database and its two login roles (milestone-m0-m1 §2, M0-11; implementation-plan §6.4):
#   platform_migrator  owns the database (and so its public schema); runs migrations (DDL).
#   platform_app       used by the API, CLI, and worker; can connect and use the schema but cannot run DDL.
# Table privileges for platform_app are granted by the initial migration (default privileges).
#
# Runs as the PostgreSQL superuser: automatically on first start of the local container (Aspire, via
# /docker-entrypoint-initdb.d) and from the integration-test fixture. Passwords come from the environment;
# none is stored in the repository.

if [ -z "${PLATFORM_MIGRATOR_PASSWORD:-}" ] || [ -z "${PLATFORM_APP_PASSWORD:-}" ]; then
  echo "PLATFORM_MIGRATOR_PASSWORD and PLATFORM_APP_PASSWORD must be set." >&2
  exit 1
fi

psql -v ON_ERROR_STOP=1 \
  --username "${POSTGRES_USER:-postgres}" \
  --dbname postgres \
  -v migrator_password="$PLATFORM_MIGRATOR_PASSWORD" \
  -v app_password="$PLATFORM_APP_PASSWORD" <<'SQL'
CREATE ROLE platform_migrator LOGIN PASSWORD :'migrator_password';
CREATE ROLE platform_app LOGIN PASSWORD :'app_password';
CREATE DATABASE platform OWNER platform_migrator;

\connect platform

-- Nobody but the owner may create objects; the application role may only connect and use the schema.
REVOKE ALL ON DATABASE platform FROM PUBLIC;
GRANT CONNECT ON DATABASE platform TO platform_app;
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO platform_app;
SQL
