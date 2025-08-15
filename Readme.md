# Carrefour CaseFlow - Sistema de Gestão de Fluxo de Caixa

Sistema de controle de fluxo de caixa desenvolvido em .NET 9 com arquitetura de microsserviços, utilizando event-driven architecture com Apache Kafka.

## 🏗️ Arquitetura

O sistema é composto por dois principais microsserviços:

- **Lançamentos API**: Responsável pelo CRUD de lançamentos financeiros
- **Consolidado API/Worker**: Responsável pela consolidação dos saldos diários

### Stack Tecnológica

- **.NET 9** - Framework principal
- **PostgreSQL** - Banco de dados principal
- **Redis** - Cache para saldos consolidados
- **Apache Kafka** - Mensageria assíncrona
- **Docker & Docker Compose** - Containerização
- **Entity Framework Core** - ORM
- **MediatR** - Padrão mediator
- **FluentValidation** - Validações
- **Serilog** - Logging estruturado

## 🚀 Como Executar

### Pré-requisitos

- Docker e Docker Compose instalados
- .NET 9 SDK (opcional, para desenvolvimento local)

### Executando com Docker Compose (Recomendado)

1. **Clone o repositório**
```bash
git clone https://github.com/rpieri/t_c_s_a.git
cd t_c_s_a
```

2. **Suba primeiro a infraestrutura (PostgreSQL, Redis, Kafka)**
```bash
cd scripts/docker/devrun
docker-compose up -d postgres redis zookeeper kafka
```

3. **Aguarde o Kafka estar pronto e crie os tópicos**
```bash
# Aguarde ~30 segundos para o Kafka inicializar completamente
sleep 30

# Execute o script para criar os tópicos
cd scripts
chmod +x init-kafka-topics.sh
./init-kafka-topics.sh
```

4. **Agora suba as aplicações**
```bash
cd .. # Volta para t_c_s_a/scripts/docker/devrun
docker-compose up -d lancamentos-api consolidado-api consolidado-worker
```

5. **Verifique se todos os serviços estão rodando**
```bash
docker-compose ps
```

### Execução Completa (Alternativa)

Se preferir subir tudo de uma vez (tópicos serão criados automaticamente):
```bash
cd t_c_s_a/scripts/docker/devrun
docker-compose up -d
```

**Nota**: Com essa abordagem, pode levar alguns minutos para os workers se conectarem ao Kafka, pois eles aguardam os tópicos serem criados automaticamente.

### Interfaces Disponíveis

- **Lançamentos API**: http://localhost:5001/swagger
- **Consolidado API**: http://localhost:6006/swagger
- **Kafka UI**: http://localhost:8080 (com profile monitoring)

Para habilitar o Kafka UI:
```bash
docker-compose --profile monitoring up -d
```

## 📋 Endpoints Principais

### Lançamentos API

#### Criar Lançamento
```http
POST http://localhost:5001/api/v1/lancamentos
Content-Type: application/json

{
  "dataLancamento": "2025-08-15",
  "valor": 1000.00,
  "tipo": 2,
  "descricao": "Recebimento de vendas",
  "categoria": "Vendas"
}
```

#### Listar Lançamentos
```http
GET http://localhost:5001/api/v1/lancamentos?page=1&pageSize=10
```

#### Buscar por Período
```http
GET http://localhost:5001/api/v1/lancamentos/periodo?inicio=2025-08-01&fim=2025-08-31
```

#### Atualizar Lançamento
```http
PUT http://localhost:5001/api/v1/lancamentos/{id}
Content-Type: application/json

{
  "id": "uuid-here",
  "dataLancamento": "2025-08-15",
  "valor": 1500.00,
  "tipo": 2,
  "descricao": "Recebimento atualizado",
  "categoria": "Vendas"
}
```

#### Excluir Lançamento
```http
DELETE http://localhost:5001/api/v1/lancamentos/{id}
```

### Consolidado API

#### Saldo Atual
```http
GET http://localhost:6006/saldo/atual
```

#### Saldo por Data
```http
GET http://localhost:6006/saldo/data/2025-08-15
```

#### Relatório por Período
```http
GET http://localhost:6006/relatorio/periodo?inicio=2025-08-01&fim=2025-08-31
```

## 🗂️ Estrutura do Projeto

```
src/
├── Carrefour.CaseFlow.Lancamentos.API/          # API de lançamentos
├── Carrefour.CaseFlow.Lancamentos.Application/  # Lógica de aplicação
├── Carrefour.CaseFlow.Lancamentos.Domain/       # Domínio dos lançamentos
├── Carrefour.CaseFlow.Lancamentos.Infrastructure/ # Infraestrutura
├── Carrefour.CaseFlow.Consolidado.API/          # API de consolidação
├── Carrefour.CaseFlow.Consolidado.Service/      # Serviços de consolidação
├── Carrefour.CaseFlow.Consolidado.Domain/       # Domínio da consolidação
├── Carrefour.CaseFlow.Consolidado.Worker/       # Worker para eventos
├── Carrefour.CaseFlow.Shared.Events/            # Eventos compartilhados
└── Carrefour.CaseFlow.Shared.Kafka/             # Infraestrutura Kafka
```

## 🔧 Scripts Úteis

### Startup Recomendado (Passo a Passo)
```bash
cd t_c_s_a/scripts/docker/devrun

# 1. Suba infraestrutura
docker-compose up -d postgres redis zookeeper kafka

# 2. Aguarde e crie tópicos
sleep 30
cd scripts && chmod +x init-kafka-topics.sh && ./init-kafka-topics.sh && cd ..

# 3. Suba aplicações
docker-compose up -d lancamentos-api consolidado-api consolidado-worker
```

### Parar todos os serviços
```bash
cd scripts/docker/devrun
docker-compose down
```

### Parar e remover volumes (reset completo)
```bash
cd scripts/docker/devrun
docker-compose down -v
```

### Ver logs de um serviço específico
```bash
cd scripts/docker/devrun
docker-compose logs -f lancamentos-api
docker-compose logs -f consolidado-worker
```

### Rebuild e restart
```bash
cd scripts/docker/devrun
docker-compose down
docker-compose build --no-cache

# Startup passo a passo novamente
docker-compose up -d postgres redis zookeeper kafka
sleep 30
cd scripts && ./init-kafka-topics.sh && cd ..
docker-compose up -d lancamentos-api consolidado-api consolidado-worker
```

### Verificar tópicos criados
```bash
# Via script interno do container
docker exec cashflow-kafka kafka-topics --list --bootstrap-server localhost:9092

# Ou via Kafka UI (se habilitado)
docker-compose --profile monitoring up -d kafka-ui
# Acesse: http://localhost:8080
```

## 🏥 Health Checks

Todos os serviços possuem health checks configurados:

- **Lançamentos API**: http://localhost:5001/health
- **Consolidado API**: http://localhost:6006/health

## 📊 Monitoramento

### Logs

Os logs são estruturados usando Serilog e podem ser visualizados com:
```bash
docker-compose logs -f [service-name]
```

### Kafka UI

Para monitorar tópicos e mensagens:
```bash
docker-compose --profile monitoring up -d kafka-ui
```

Acesse: http://localhost:8080

## 🔀 Fluxo de Dados

1. **Criação de Lançamento**:
    - POST para `/api/v1/lancamentos`
    - Evento `LancamentoCriado` é publicado no Kafka
    - Worker de consolidação processa o evento
    - Saldo consolidado é atualizado

2. **Atualização de Lançamento**:
    - PUT para `/api/v1/lancamentos/{id}`
    - Evento `LancamentoAtualizado` é publicado
    - Worker recalcula o saldo do dia

3. **Exclusão de Lançamento**:
    - DELETE para `/api/v1/lancamentos/{id}`
    - Evento `LancamentoRemovido` é publicado
    - Worker ajusta o saldo consolidado

## 🐛 Troubleshooting

### Problema: Serviços não inicializam
- Verifique se as portas estão livres: `netstat -an | grep :5432`
- Execute reset completo: `docker-compose down -v && docker-compose up -d`

### Problema: Kafka não conecta ou tópicos não existem
```bash
# Verifique se Kafka está rodando
docker-compose ps kafka

# Verifique logs do Kafka
docker-compose logs kafka

# Recrie os tópicos manualmente
cd scripts
./init-kafka-topics.sh

# Liste tópicos existentes
docker exec cashflow-kafka kafka-topics --list --bootstrap-server localhost:9092
```

### Problema: Worker não processa eventos
```bash
# Verifique se o tópico existe
docker exec cashflow-kafka kafka-topics --describe --topic lancamento-events --bootstrap-server localhost:9092

# Verifique logs do worker
docker-compose logs consolidado-worker

# Verifique se há mensagens no tópico
docker exec cashflow-kafka kafka-console-consumer --topic lancamento-events --from-beginning --bootstrap-server localhost:9092 --max-messages 5
```

### Problema: Cache Redis não funciona
- Verifique se o Redis está rodando: `docker-compose ps redis`
- Teste conexão: `docker exec cashflow-redis redis-cli ping`

### Ordem de Inicialização Recomendada (para evitar problemas)
1. **Infraestrutura**: `docker-compose up -d postgres redis zookeeper kafka`
2. **Aguarde**: `sleep 30` (Kafka precisa estar completamente iniciado)
3. **Tópicos**: `cd scripts && ./init-kafka-topics.sh`
4. **Aplicações**: `docker-compose up -d lancamentos-api consolidado-api consolidado-worker`

## 🛠️ Desenvolvimento Local

Para desenvolver localmente sem containers:

1. **Configure o ambiente**:
```bash
# PostgreSQL, Redis e Kafka via Docker
docker-compose up -d postgres redis zookeeper kafka

# Configure as connection strings no appsettings.Development.json
```

2. **Execute as APIs**:
```bash
# Terminal 1 - Lançamentos API
cd src/Carrefour.CaseFlow.Lancamentos.API
dotnet run

# Terminal 2 - Consolidado API
cd src/Carrefour.CaseFlow.Consolidado.API
dotnet run

# Terminal 3 - Worker
cd src/Carrefour.CaseFlow.Consolidado.Worker
dotnet run
```
