<img width="1553" height="754" alt="image" src="https://github.com/user-attachments/assets/99e61c6a-8fba-446b-a38e-9c9b98230f30" />

# ms2-plans-and-recipes-catalog

Microservicio de **Catálogo de Alimentos, Recetas y Planes Alimentarios** para un sistema de nutrición y meal-planning.

## Propósito

Gestionar y exponer un catálogo completo de alimentos con su información nutricional, recetas compuestas por dichos alimentos, y planes alimentarios que organizan recetas en días y tiempos de comida.

## Funcionalidades

### Alimentos
- CRUD de alimentos con información nutricional (calorías, proteínas, carbohidratos, grasas), unidad de medida y categoría.
- Búsqueda de alimentos por categoría.
- Publicación de evento de integración al actualizar datos nutricionales.

### Recetas
- CRUD de recetas con nombre e instrucciones.
- Agregar y remover ingredientes (alimento + porción).
- Cálculo de información nutricional total de una receta basado en sus ingredientes.

### Planes Alimentarios
- CRUD de planes con duración (15 o 30 días) y número de comidas por día.
- Generación automática de los días del plan al crearlo.
- Gestión de tiempos de comida (desayuno, almuerzo, cena, etc.) por día.
- Asignación de recetas con raciones a cada tiempo de comida.
- Obtención de la composición completa del plan (plan > días > tiempos de comida > recetas > ingredientes).
- Publicación de evento de integración al crear un plan.

## Stack Tecnológico

| Componente | Tecnología |
|------------|------------|
| Framework | .NET (ASP.NET Core Web API) |
| Arquitectura | DDD + Clean Architecture |
| Patrón CQRS | MediatR |
| ORM | Entity Framework Core |
| Base de datos | PostgreSQL (Docker) |
| Documentación API | Swagger / OpenAPI |
| Patrones | Repository, Unit of Work, Factory, Result |

## Estructura del Proyecto

```
ms2-plans-and-recipes-catalog/
├── Domain/          # Entidades, Value Objects, Eventos, Repositorios (interfaces)
├── Application/     # Casos de uso (Commands/Queries), DTOs, Handlers
├── Infrastructure/  # Persistencia (EF Core DbContexts), Repositorios (implementación)
├── WebApi/          # Controladores REST, Middleware, Startup
├── Shared/          # Tipos compartidos (Result, PagedList, etc.)
└── Test/            # Pruebas unitarias e integración
```

## API Endpoints

### Alimentos (`/api/v1/alimentos`)
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/alimentos` | Crear alimento |
| GET | `/alimentos/{id}` | Obtener alimento por ID |
| GET | `/alimentos` | Listar alimentos (paginado) |
| GET | `/alimentos/categoria/{categoria}` | Buscar por categoría (paginado) |
| PUT | `/alimentos` | Actualizar alimento |

### Recetas (`/api/v1/recetas`)
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/recetas` | Crear receta |
| POST | `/recetas/ingredientes` | Agregar ingredientes |
| DELETE | `/recetas/{recetaId}/ingredientes/{alimentoId}` | Remover ingrediente |
| GET | `/recetas/{id}` | Obtener receta por ID |
| GET | `/recetas` | Listar recetas (paginado) |
| GET | `/recetas/{recetaId}/info-nutricional` | Obtener info nutricional |

### Planes Alimentarios (`/api/v1/planes`)
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/planes` | Crear plan |
| POST | `/planes/tiempos-comida` | Agregar tiempo de comida |
| POST | `/planes/asignar-recetas` | Asignar recetas a tiempo de comida |
| DELETE | `/planes/{planId}/tiempos-comida/{tiempoComidaId}/recetas/{recetaId}` | Remover receta de tiempo de comida |
| GET | `/planes/{planId}` | Obtener composición del plan |
| GET | `/planes` | Listar planes |

## Requisitos

| Herramienta | Versión |
|-------------|---------|
| Docker Desktop | Última estable (recomendado) |
| .NET SDK | 10.x (solo si quieres correrlo directamente) |

## Configuración

Todo comportamiento que dependía del **nombre del entorno** (`ASPNETCORE_ENVIRONMENT`) se decide
ahora por **configuración explícita**, con valores por defecto que son siempre los seguros:

| Sección | Clave | Default | Qué controla |
|---------|-------|---------|--------------|
| `Migrations` | `ApplyOnStartup` | `false` | Aplicar migraciones al arrancar el host |
| `Migrations` | `TimeoutSeconds` | `120` | Presupuesto total de la corrida (ambos contextos) |
| `Seed` | `Enabled` | `false` | Cargar los datos de ejemplo (30 alimentos / 25 recetas / 2 planes) |
| `Swagger` | `Enabled` | `false` | Servir `/swagger` y `/swagger/v1/swagger.json` |
| `HttpsRedirection` | `Enabled` | `false` | Agregar `UseHttpsRedirection()` |

**No hay secretos en archivos versionados.** `src/Catalog.App/appsettings.json` y
`src/Catalog.WorkerService/appsettings.json` ya **no** contienen la cadena de conexión. Cada proyecto
tiene un template versionado con los valores de desarrollo:

```bash
cp src/Catalog.App/appsettings.Development.template.json         src/Catalog.App/appsettings.Development.json
cp src/Catalog.WorkerService/appsettings.Development.template.json  src/Catalog.WorkerService/appsettings.Development.json
```

Las copias se llaman `appsettings.Development.json` y **están en `.gitignore` (línea 107)**: nunca se
versionan. En un despliegue usá variables de entorno, que tienen precedencia sobre los archivos:
`ConnectionStrings__CatalogDbConnectionString`, `Migrations__ApplyOnStartup`,
`Migrations__TimeoutSeconds`, `Seed__Enabled`, `Swagger__Enabled`, `HttpsRedirection__Enabled`.

Un test (`Test/UnitTest/Infrastructure/Configuration/TrackedConfigurationTests.cs`) falla la
compilación si vuelve a aparecer una sección `ConnectionStrings` o una contraseña en cualquiera de los
dos `appsettings.json` versionados.

> `HttpsRedirection:Enabled` está en `false` porque **el TLS termina aguas arriba** (ingress, gateway
> o sidecar de la malla) y lo que llega a este proceso es HTTP plano. Encendelo solo cuando el
> proceso sea realmente el borde TLS (Kestrel con certificado, típicamente desarrollo local).

### Migraciones de base de datos

El esquema se aplica con un **paso explícito**, no al arrancar el servicio:

```bash
dotnet run --project src/Catalog.App/Catalog.App.csproj -- --migrate
# o, sobre el binario publicado:
dotnet Catalog.App.dll --migrate
```

Aplica las migraciones del **lado escritura** (`DomainDbContext`) y después las del **lado lectura**
(`StoredDbContext`). Sale con código `0` si terminó bien y `1` si falló, así que un orquestador (Job
de Kubernetes, servicio one-shot de compose, paso de CI) decide si la aplicación puede arrancar. Es
idempotente: correrlo dos veces no aplica nada la segunda.

Para desarrollo, `Migrations:ApplyOnStartup: true` en `appsettings.Development.json` deja el
`dotnet run` sin ceremonia. **No lo pongas en un despliegue.**

> ### ✅ Los dos lados viven en esquemas distintos
>
> `DomainDbContext` (escritura) usa el esquema **`write`** y `StoredDbContext` (lectura) el esquema
> **`read`**. Los nombres de esquema están declarados en un solo lugar,
> `Catalog.Infrastructure.Persistence.DatabaseSchemas`, y cada `ToTable` los referencia; ninguno
> escribe a `public`. El esquema lo fija `HasDefaultSchema` en cada contexto, así que un mapeo que
> olvide el `ToTable` explícito tampoco vuelve a colisionar.
>
> | Esquema | Tablas |
> |---------|--------|
> | `write` | `Food`, `Recipe`, `RecipeIngredient`, `MealPlan`, `PlanDay`, `MealTime`, `RecipeAssignment` |
> | `read` | `Food`, `Recipe`, `RecipeIngredient`, `MealPlan`, `PlanDay`, `MealTime`, `AssignedRecipe` |
> | `outbox` | `outboxMessage` |
>
> `outboxMessage` **no** es decisión de este proyecto: `Joseco.Outbox.EFCore` declara
> `OutboxMessageConfig<T>` como `internal` y llama `ToTable("outboxMessage", "outbox")` sin punto de
> extensión, así que el esquema `outbox` queda fijo para la librería. Verificado contra PostgreSQL 16:
> con el esquema por defecto `write` en el contexto, la migración generada sigue emitiendo
> `CreateTable(name: "outboxMessage", schema: "outbox")`.
>
> Verificado de punta a punta contra PostgreSQL 16 sobre una base vacía: `--migrate` aplica las dos
> historias sin `42P07`, sale con código `0`, y la segunda corrida es un no-op. La tabla
> `__EFMigrationsHistory` queda en `public` y es compartida: los ids de migración de los dos contextos
> son disjuntos, así que cada historia aplica solo lo que no encuentra registrado.
>
> `Test/UnitTest/Infrastructure/Persistence/PersistenceSchemaSeparationTests.cs` falla si alguna vez
> los dos contextos vuelven a mapear el mismo nombre de tabla en el mismo esquema. Ese test es
> necesario porque EF Core InMemory **no puede** detectar esta clase de defecto: le da a cada
> contexto su propio almacén, así que una suite entera en verde no dice absolutamente nada sobre
> las colisiones de esquema.
>
> **Pendiente, ajeno a este cambio:** la proyección de `FoodCreated` a `read.Food` lanza un
> `NullReferenceException` porque `NutritionalInfo` llega deserializado como `null` desde el
> contenido del outbox (queda `null` solo en los eventos que llevan ese value object; `Portion` y
> `Ration` deserializan bien). Ningún mapeo de EF participa: el fallo ocurre antes de la primera
> sentencia `INSERT`.

## Cómo ejecutar el proyecto

### Opción 1 — Docker Compose (recomendada)

Levanta PostgreSQL, aplica el esquema con un paso one-shot y levanta la API:

```bash
# 1. Configuración local, una sola vez (crea appsettings.Development.json, fuera de git)
cp src/Catalog.App/appsettings.Development.template.json src/Catalog.App/appsettings.Development.json

# 2. Levantar el stack (Postgres -> webapi-migrate -> webapi)
docker compose up -d

# O aplicar el esquema a mano, sin el servicio one-shot:
docker compose run --rm webapi-migrate
```

El servicio `webapi-migrate` corre `dotnet Catalog.App.dll --migrate`, y `webapi` espera a que
termine con éxito (`service_completed_successfully`) antes de arrancar. `webapi` **no** migra ni
sembra por su cuenta.

> El paso de migración es seguro sobre una base vacía. Si tu volumen `catalog-db-data` ya existe
> con un esquema viejo (tablas en `public`, nombres en español de una versión anterior), recrealo
> con `docker compose down -v` antes de volver a intentarlo: las dos historias parten de cero y no
> migran un esquema preexistente.

### Docker Hub — construir y publicar la imagen

El compose consume `robertperezlino/ms2-plans-and-recipes-catalog:latest`. **No hay pipeline que
la publique**: la imagen hay que construirla y empujarla a mano, y `:latest` no garantiza que
corresponda a tu working tree.

El contexto de build es **la carpeta padre**, no este repositorio:

```bash
cd <carpeta que contiene ms2-plans-and-recipes-catalog Y nur-tricenter-shared-kernels>

docker build \
  -f ms2-plans-and-recipes-catalog/Dockerfile \
  -t robertperezlino/ms2-plans-and-recipes-catalog:latest .

docker push robertperezlino/ms2-plans-and-recipes-catalog:latest
```

**Por qué la carpeta padre y no este repositorio:** `Catalog.Application` e
`Catalog.Infrastructure` referencian `nur-tricenter-shared-kernels/*` por ruta relativa, y Docker
no puede copiar archivos de fuera del contexto de build. Construir desde adentro de este repo
falla en la primera copia de un `.csproj` de los kernels.

> `.dockerignore` vive en la carpeta padre, que es el contexto real. Las reglas para
> `nur-tricenter-api-gateway/` o `Docker Proyecto/` que estuviera dentro de este repo no
> excluirían nada: esas carpetas no forman parte de su contexto.

### Imagen para producción

La imagen no declara `ASPNETCORE_ENVIRONMENT`: el compose la fija en `Production`. Para un
despliegue propio hay que exportar las variables de configuración, sobre todo
`ConnectionStrings__CatalogDbConnectionString`, y dejar `Migrations__ApplyOnStartup` en `false`:
el esquema se aplica con el paso explícito `--migrate`, no al arrancar el proceso web.

| Recurso | URL |
|---------|-----|
| Documentación interactiva (Swagger) | http://localhost:8080/swagger |
| Health | http://localhost:8080/health |
| API | http://localhost:8080/api/v1/... |
| PostgreSQL | localhost:5433 |

Para detener el stack:

```bash
docker compose down
```

### Opción 2 — Ejecución directa con el SDK de .NET

Requiere un PostgreSQL disponible (por ejemplo el de `docker compose`) y la configuración local:

```bash
cp src/Catalog.App/appsettings.Development.template.json src/Catalog.App/appsettings.Development.json

dotnet restore
dotnet run --project src/Catalog.App/Catalog.App.csproj
```

Si preferís no tocar ningún archivo, exportá las variables de entorno equivalentes (los valores
están en el template):

```bash
Migrations__ApplyOnStartup=true
Seed__Enabled=true
Swagger__Enabled=true
ConnectionStrings__CatalogDbConnectionString=<la del template>
```

Con `Migrations:ApplyOnStartup: true` y `Seed__Enabled: true` el arranque aplica el esquema y carga
los datos de ejemplo. Swagger queda en `http://localhost:63884/swagger` (o el puerto de
`launchSettings.json`). Sin esos flags el host arranca igual, pero **no** migra, **no** siembra y
**no** sirve Swagger.

## Pruebas

El proyecto de tests vive en `Test/` (xUnit + Moq para unit tests, WebApplicationFactory para integration tests y PactNet para contract testing):

```
Test/
├── UnitTest/          # Unit tests: Domain (entidades, value objects, factories) y Application (handlers CQRS)
├── IntegrationTest/   # Integration tests: flujos HTTP completos por controlador (WebApplicationFactory + EF InMemory)
└── ContractTest/      # Contract testing con Pact: consumer + provider (pact committeado en pacts/)
```

Las reglas y skills usados para generar/verificar los tests están en [`ai/`](ai/): `ai/project/*.md` (reglas de unit, integration y pact) y `ai/skill/*` (skills generadoras y verificadoras).

### Ejecutar la suite completa

```bash
dotnet test
```

### Ejecutar con reporte de code coverage

```bash
dotnet test Test/Test.csproj --collect:"XPlat Code Coverage" --results-directory Test/coverage
```

El reporte `coverage.cobertura.xml` se genera dentro de `Test/coverage/` (carpeta excluida de git).

### Ver el reporte de coverage en HTML

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool   # una sola vez
reportgenerator "-reports:Test\coverage\**\coverage.cobertura.xml" "-targetdir:Test\coverage\report" "-reporttypes:Html"
```

Luego abrir `Test/coverage/report/index.html` en el navegador.

Cobertura objetivo (según `ai/project/unit-testing-rules.md`): Domain ≥ 90%, Application ≥ 80%, Value Objects ≥ 95%.
