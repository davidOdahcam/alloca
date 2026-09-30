#!/usr/bin/env bash
# Espera o schema (criado pelas migrations da API) e aplica os seeds idempotentes.
set -euo pipefail

SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
SERVER="sqlserver,1433"
PASS="${MSSQL_SA_PASSWORD:-Alloca@Dev123!}"
DB="AllocaDb"

echo "Aguardando o schema do banco (migrations da API)..."
until "$SQLCMD" -S "$SERVER" -U sa -P "$PASS" -C -d "$DB" -h -1 \
      -Q "SET NOCOUNT ON; IF OBJECT_ID('dbo.Users') IS NOT NULL SELECT 1 ELSE SELECT 0" 2>/dev/null \
      | grep -q 1; do
    echo "  schema ainda nao pronto; nova tentativa em 5s..."
    sleep 5
done

echo "Schema pronto. Aplicando seeds (idempotentes)..."
for f in seed.sql BCP01.sql BCP02.sql; do
    echo "  -> $f"
    "$SQLCMD" -S "$SERVER" -U sa -P "$PASS" -C -d "$DB" -b -i "/sql/$f"
done

echo "Seed concluido."
