#!/bin/bash
echo "🎯 Criando tópicos do Kafka..."

# Aguardar Kafka ficar pronto
echo "⏳ Aguardando Kafka estar pronto..."
sleep 30

# Criar tópicos
docker exec cashflow-kafka kafka-topics --create \
  --topic lancamento-events \
  --bootstrap-server localhost:9092 \
  --partitions 3 \
  --replication-factor 1 \
  --if-not-exists

docker exec cashflow-kafka kafka-topics --create \
  --topic consolidado-events \
  --bootstrap-server localhost:9092 \
  --partitions 1 \
  --replication-factor 1 \
  --if-not-exists

# Listar tópicos criados
echo "📋 Tópicos criados:"
docker exec cashflow-kafka kafka-topics --list --bootstrap-server localhost:9092

echo "✅ Tópicos Kafka prontos!"