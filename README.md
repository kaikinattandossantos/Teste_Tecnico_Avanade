# Teste Técnico Avanade

## 📝 Descrição
Sistema de microserviços para **Gestão de Estoque** e **Gestão de Vendas**, desenvolvido em **.NET 9 (C#)**.  
Usa **Entity Framework Core**, **RabbitMQ** para comunicação assíncrona e **JWT** para autenticação.  

O projeto contém um **API Gateway** que direciona as requisições para os microserviços corretos.

---

## 🏗️ Arquitetura de Microserviços

### 1️⃣ StockService (Gestão de Estoque)
Responsável por:
- CRUD de produtos
- Consulta de estoque
- Atualização de quantidades
- Autenticação de usuários
- Receber mensagens do SalesService via RabbitMQ para reduzir estoque

**Endpoints principais:**

| Método | Endpoint                  | Descrição                         |
|--------|---------------------------|----------------------------------|
| GET    | `/api/Product`            | Listar produtos                  |
| GET    | `/api/Product/{id}`       | Consultar produto por ID         |
| POST   | `/api/Product`            | Criar produto                    |
| PUT    | `/api/Product/{id}`       | Atualizar estoque                |
| DELETE | `/api/Product/{id}`       | Remover produto                  |
| POST   | `/api/Auth/register`      | Registrar usuário                |
| POST   | `/api/Auth/login`         | Login e gerar JWT                |
| GET    | `/api/Auth/token`         | Gerar token de teste             |
| GET    | `/api/Product/test`       | Teste da API                     |

---

### 2️⃣ SalesService (Gestão de Vendas)
Responsável por:
- Criar pedidos
- Validar disponibilidade de produtos
- Reduzir estoque via StockService (mensagens RabbitMQ)
- Publicar mensagens de atualização de estoque

**Endpoints principais:**

| Método | Endpoint        | Descrição                     |
|--------|----------------|-------------------------------|
| POST   | `/api/Order`   | Criar pedido                  |
| GET    | `/api/Order/{id}` | Consultar pedido por ID     |
| GET    | `/api/Order`   | Listar pedidos                |
| PUT    | `/api/Order/{id}` | Atualizar pedido            |
| DELETE | `/api/Order/{id}` | Remover pedido              |
| GET    | `/api/Order/test` | Teste da API                |

---

### 3️⃣ API Gateway
Responsável por:
- Encaminhar requisições externas para os microserviços corretos
- Centralizar autenticação JWT
- Simplificar comunicação do cliente com os microserviços

---

## 🐇 RabbitMQ
- Fila principal: `stock_queue`
- **SalesService** envia mensagens para reduzir o estoque
- **StockService** consome mensagens e atualiza produtos
- Comunicação assíncrona entre microserviços

**Executando RabbitMQ com Docker:**
```bash
docker run -d --hostname my-rabbit --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```
A interface web do RabbitMQ fica disponível em: http://localhost:15672

Usuário padrão: guest, senha: guest

# Teste Técnico Avanade

Este é um guia sobre como configurar e executar o projeto de Teste Técnico da Avanade. O projeto consiste em múltiplos serviços que se comunicam via RabbitMQ e são protegidos por JWT.

## Pré-requisitos

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/downloads)
- Um cliente HTTP como [Postman](https://www.postman.com/downloads/) ou [Insomnia](https://insomnia.rest/download)

## ⚙️ Configuração do `appsettings.json`

Antes de executar os serviços, é crucial configurar corretamente o arquivo `appsettings.json` em cada projeto. O arquivo principal de configuração deve se parecer com o seguinte:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TesteTecnico;User Id=sa;Password=YourPassword;"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "Port": "5672"
  },
  "Jwt": {
    "Key": "SuaChaveSecretaAqui",
    "Issuer": "TesteTecnicoAvanade",
    "Audience": "TesteTecnicoAvanade"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Detalhes da Configuração:

-   **`ConnectionStrings`**:
    -   `DefaultConnection`: String de conexão para o seu banco de dados SQL Server. Substitua `YourPassword` pela senha do seu usuário `sa` ou ajuste conforme sua configuração de banco de dados.

-   **`RabbitMQ`**:
    -   As configurações padrão (`localhost`, `guest`/`guest`, porta `5672`) funcionarão perfeitamente com a instância Docker executada no próximo passo.

-   **`Jwt`**:
    -   `Key`: Defina uma chave secreta forte e única para a geração e validação dos tokens JWT.
    -   `Issuer` / `Audience`: Emissor e audiência do token, que podem ser mantidos como no exemplo para este projeto.

## 🚀 Como Executar

Siga os passos abaixo para colocar a aplicação em funcionamento.

### 1. Clonar o repositório

Abra seu terminal e execute os seguintes comandos para clonar o projeto e navegar para o diretório raiz:

```bash
git clone [https://github.com/kaikinattandossantos/Teste_Tecnico_Avanade.git](https://github.com/kaikinattandossantos/Teste_Tecnico_Avanade.git)
cd Teste_Tecnico_Avanade
```

### 2. Executar RabbitMQ via Docker

Para a comunicação entre os serviços, é necessário ter uma instância do RabbitMQ em execução. Use o Docker para iniciar um contêiner facilmente:

```bash
docker run -d --hostname my-rabbit --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

-   A interface de gerenciamento do RabbitMQ estará disponível em: **http://localhost:15672**
-   **Usuário:** `guest` / **Senha:** `guest`

### 3. Restaurar pacotes e construir projetos

Com o .NET SDK instalado, restaure as dependências e compile todos os projetos:

```bash
dotnet restore
dotnet build
```

### 4. Executar os serviços

Abra três terminais separados na raiz do projeto e execute cada serviço individualmente:

**Terminal 1 (StockService):**
```bash
dotnet run --project StockService
```

**Terminal 2 (SalesService):**
```bash
dotnet run --project SalesService
```

**Terminal 3 (Gateway):**
```bash
dotnet run --project Gateway
```

### 5. Testar endpoints

Após iniciar todos os serviços, você pode usar um cliente HTTP (Postman, Insomnia) para testar a API.

Para acessar endpoints protegidos, você primeiro precisa obter um token JWT. Faça uma requisição para o endpoint de login:

-   **Endpoint:** `/api/Auth/login` (via Gateway, que redirecionará para o `StockService`)
-   **Método:** `POST`
-   **Corpo (Body):** Forneça as credenciais de um usuário válido.

Com o token JWT gerado, adicione-o ao cabeçalho (header) `Authorization` de suas próximas requisições no formato `Bearer {seu_token}`.
