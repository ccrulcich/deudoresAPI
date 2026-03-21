# DeudoresAPI

API REST en .NET para procesar el archivo de la Central de Deudores del BCRA, persistir los datos en PostgreSQL y exponer endpoints de consulta.

---

## Tecnologías

- **.NET 10** — ASP.NET Core Web API
- **PostgreSQL 17** — base de datos relacional
- **Entity Framework Core 10** — ORM con Npgsql
- **Serilog** — logging estructurado
- **MailKit** — notificaciones por email (opcional)
- **Docker / Docker Compose** — contenedores y orquestación
- **xUnit** — tests unitarios

---

## Requisitos previos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado y corriendo
- (Solo para desarrollo local) [.NET 10 SDK](https://dotnet.microsoft.com/download)

---

## Cómo ejecutar con Docker

```bash
# 1. Clonar el repositorio
git clone <url-del-repo>
cd deudoresAPI

# 2. (Opcional) Configurar variables de entorno
cp .env.example .env
# Editar .env si querés webhook o email

# 3. Levantar la aplicación
docker compose up --build
```

La API queda disponible en **http://localhost:8080**  
Swagger UI en **http://localhost:8080/swagger**

Docker Compose levanta automáticamente:
- `postgres` — PostgreSQL 17 con healthcheck
- `api` — la aplicación .NET (espera a que Postgres esté listo, aplica migraciones al iniciar)

---

## Cómo ejecutar en local (sin Docker)

```bash
# Requiere PostgreSQL corriendo en localhost:5432

cd DeudoresApi
dotnet run
```

La connection string por defecto en `appsettings.json`:
```
Host=localhost;Port=5432;Database=deudores_db;Username=postgres;Password=postgres
```

---

## Endpoints

### Importar archivo

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/Import/upload` | Sube un archivo `.txt` del BCRA vía multipart |
| `POST` | `/Import/process-local` | Procesa un archivo desde ruta local del servidor |

**Ejemplo upload:**
```bash
curl -X POST http://localhost:8080/Import/upload \
  -F "file=@/ruta/al/archivo.txt"
```

**Respuesta:**
```json
{
  "message": "Archivo procesado y persistido",
  "deudores": 1500,
  "entidades": 42
}
```

### Consultas

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/Deudores/{cuit}` | Retorna situación máxima y suma total de préstamos de un deudor |
| `GET` | `/Deudores/top/{n}` | Top N deudores por mayor suma total de préstamos |
| `GET` | `/Deudores?situacion={1-6}` | Filtra deudores por situación máxima |
| `GET` | `/Entidades/{codigo}` | Retorna suma total de préstamos de una entidad |

**Ejemplo:**
```bash
curl http://localhost:8080/Deudores/20123456781
```
```json
{
  "nroIdentificacion": "20123456781",
  "situacionMaxima": 3,
  "sumaTotalPrestamos": 150000.00
}
```

---

## Configuración

Todas las variables de entorno siguen la convención de ASP.NET Core (`__` como separador de sección):

| Variable de entorno | Descripción | Default |
|---------------------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Connection string de PostgreSQL | `Host=localhost;...` |
| `Notifications__WebhookUrl` | URL del webhook al finalizar procesamiento | vacío (desactivado) |
| `Notifications__Email__SmtpHost` | Host SMTP para notificaciones por email | vacío (desactivado) |
| `Notifications__Email__SmtpPort` | Puerto SMTP | `587` |
| `Notifications__Email__Username` | Usuario SMTP | vacío |
| `Notifications__Email__Password` | Contraseña SMTP | vacío |
| `Notifications__Email__From` | Dirección remitente | vacío |
| `Notifications__Email__To` | Dirección destinatario | vacío |
| `FileUpload__MaxFileSizeMb` | Tamaño máximo del archivo en MB | `100` |
| `FileUpload__AllowedExtensions` | Extensiones permitidas | `.txt` |

Copiá `.env.example` a `.env` y completá los valores que necesitás.

---

## Notificaciones al finalizar

Al completar el procesamiento de un archivo, la API puede notificar por múltiples canales de forma simultánea:

- **Log estructurado** — siempre activo, registra cantidad de deudores, entidades y fecha
- **Webhook** — si `Notifications__WebhookUrl` está configurado, hace un `POST` con el resumen en JSON
- **Email** — si `Notifications__Email__SmtpHost` está configurado, envía un email con el resumen

---

## Procesamiento asíncrono con SQS (Bonus)

Por defecto la API procesa el archivo de forma **sincrónica** (el cliente espera hasta que termina).  
Configurando SQS, el procesamiento pasa a ser **asíncrono**:

```
POST /Import/upload  →  guarda el archivo  →  encola el path  →  202 Accepted (inmediato)
                                                       ↓
                                    SqsImportWorker (background) lee la cola
                                    y llama a ImportService.ProcessAsync()
```

En local se usa **LocalStack** (incluido en docker-compose), que simula AWS sin necesidad de cuenta real.

### Cómo funciona

1. `docker compose up --build` levanta Postgres + LocalStack + API
2. LocalStack crea automáticamente la cola `import-queue` al iniciar (script `localstack-init/01-create-queue.sh`)
3. La API detecta `SqsSettings__QueueUrl` y activa el modo async
4. Al subir un archivo, la respuesta es `202 Accepted` inmediatamente
5. El `SqsImportWorker` (BackgroundService) procesa el archivo en segundo plano y emite la notificación al terminar

### Para deshabilitar SQS (modo síncrono)

Comentar o eliminar las variables `SqsSettings__*` del entorno. La API vuelve al modo síncrono automáticamente.

## Tests

```bash
cd deudoresAPI
dotnet test DeudoresApi.Tests/DeudoresApi.Tests.csproj
```

Cubre los casos del `BcraParser`:
- Parsing de líneas válidas
- Líneas demasiado cortas (ignoradas)
- Campos obligatorios vacíos
- Situación no numérica
- Préstamos no parseables (usa 0 y continúa)
- Stream vacío
- Mix de líneas válidas e inválidas
- Acumulación correcta de préstamos por deudor y por entidad
- Selección de situación máxima cuando un deudor aparece en múltiples entidades

---

## Estructura del proyecto

```
DeudoresApi/
├── Controllers/          # Controladores HTTP (Import, Deudores, Entidades)
├── Application/
│   ├── DTOs/             # Objetos de transferencia de datos
│   └── Services/         # Casos de uso (ImportService, QueryService)
├── Domain/
│   ├── Models/           # Entidades de dominio (Deudor, Entidad, BcraRecord)
│   ├── Repositories/     # Interfaces de repositorios
│   ├── Services/         # Interfaces de servicios de dominio
│   └── Events/           # Contratos de eventos
└── Infrastructure/
    ├── Data/             # AppDbContext y migraciones EF Core
    ├── Parsing/          # BcraParser (lector de archivo BCRA en streaming)
    ├── Repositories/     # Implementaciones de repositorios (EF Core)
    └── Events/           # Publicadores de eventos (Log, Webhook, Email)

DeudoresApi.Tests/        # Tests unitarios (xUnit)
Dockerfile                # Build multi-stage para la API
docker-compose.yml        # Orquestación API + PostgreSQL
.env.example              # Plantilla de variables de entorno
```
