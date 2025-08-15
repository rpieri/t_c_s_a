#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE DATABASE cashflow_lancamentos;
    CREATE DATABASE cashflow_consolidado;
    CREATE DATABASE cashflow_lancamentos_dev;
    CREATE DATABASE cashflow_consolidado_dev;
    
    GRANT ALL PRIVILEGES ON DATABASE cashflow_lancamentos TO postgres;
    GRANT ALL PRIVILEGES ON DATABASE cashflow_consolidado TO postgres;
    GRANT ALL PRIVILEGES ON DATABASE cashflow_lancamentos_dev TO postgres;
    GRANT ALL PRIVILEGES ON DATABASE cashflow_consolidado_dev TO postgres;
EOSQL

echo "Multiple databases created successfully!"