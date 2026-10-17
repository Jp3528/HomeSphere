# HomeSphere | Shop the Room E-commerce

[Español](#espanol) · [English](#english)

<a id="espanol"></a>

## Español

### Descripción

Aplicación de comercio electrónico de hogar que permite explorar ambientes fotográficos y descubrir sus productos mediante puntos interactivos. Combina una web en Next.js con una API .NET organizada en capas de dominio, aplicación e infraestructura.

### Funcionalidades

- Catálogo, variantes, favoritos y ambientes con hotspots y editor visual.
- Carrito, checkout y reservas de inventario con vencimiento.
- Pedidos e integración de pagos simulada con validación de firma de webhooks.
- Administración de inventario, dashboard comercial y registro de auditoría.
- Autenticación, autorización por roles y pruebas de servicios y API.

### Tecnologías

ASP.NET Core 8 · C# · Entity Framework Core · PostgreSQL 17 · Next.js 16 · React · TypeScript · Zustand · Docker

### Ejecutar localmente

**Requisitos:** Docker y Docker Compose para el arranque completo. Para desarrollo y checks nativos: .NET SDK 8 y Node.js con npm.

```bash
git clone https://github.com/Jp3528/HomeSphere.git
cd HomeSphere
```

**Configuración.** Revisa `docker-compose.yml` y [`.env.example`](.env.example). Compose incorpora valores de desarrollo y activa migraciones/seed en la API; las credenciales de ejemplo deben reservarse al entorno local.

```bash
docker compose up --build
```

Web: `http://localhost:3000`. Swagger: `http://localhost:5100/swagger`. Salud: `http://localhost:5100/health/status`. En una base nueva, el seeder crea por defecto `admin@homesphere.dev` / `Admin123!`; los valores se pueden cambiar mediante `Seed:AdminEmail` y `Seed:AdminPassword`. Registra una cuenta desde la web para evaluar el flujo de cliente.

### Comprobaciones

```bash
dotnet test apps/api/HomeSphere.sln
npm --prefix apps/web ci
npm --prefix apps/web run lint
npm --prefix apps/web run build
```

La suite incluye pruebas unitarias y pruebas HTTP con WebApplicationFactory. Su factory usa EF Core InMemory, por lo que no sustituye pruebas de concurrencia contra PostgreSQL. El pipeline versionado está en [GitHub Actions](https://github.com/Jp3528/HomeSphere/actions).

### Estructura

| Ruta                                       | Contenido                                      |
| ------------------------------------------ | ---------------------------------------------- |
| [`apps/api/src/`](apps/api/src/)           | Dominio, casos de uso, persistencia y Web API. |
| [`apps/api/tests/`](apps/api/tests/)       | Pruebas de backend y API.                      |
| [`apps/web/`](apps/web/)                   | Tienda, cuenta de cliente y administración.    |
| [`docs/`](docs/)                           | Decisiones y especificaciones funcionales.     |
| [`docker-compose.yml`](docker-compose.yml) | Servicios locales de la plataforma.            |

### Alcance

Proyecto de portafolio con pasarela de pagos mock. Las reservas, firmas e idempotencia forman parte de la implementación; no se presenta como integración bancaria certificada ni como evidencia de capacidad bajo carga productiva. Los valores de desarrollo de Compose necesitan configuración específica antes de un despliegue público.

### Documentación

- [Arquitectura](docs/adr/0001-architecture.md)
- [Ambientes interactivos](docs/interactive-scenes.md)
- [Carrito y checkout](docs/cart-and-checkout.md)
- [Pedidos y pagos](docs/order-and-payments.md)
- [Seguridad](docs/security.md)
- [Analítica y auditoría](docs/analytics-and-audit.md)
- [Historial de cambios](CHANGELOG.md)

**Licencia:** [MIT](LICENSE).

---

<a id="english"></a>

## English

### Overview

A home goods e-commerce application for exploring photographic room scenes and discovering products through interactive hotspots. It combines a Next.js web app with a .NET API organized into domain, application and infrastructure layers.

### Features

- Catalog, variants, wishlist and room scenes with hotspots and a visual editor.
- Cart, checkout and expiring inventory reservations.
- Orders and simulated payment integration with webhook signature validation.
- Inventory administration, sales dashboard and audit log.
- Authentication, role-based authorization and service and API tests.

### Technology

ASP.NET Core 8 · C# · Entity Framework Core · PostgreSQL 17 · Next.js 16 · React · TypeScript · Zustand · Docker

### Run locally

**Requirements:** Docker and Docker Compose for the full stack. For native development and checks: .NET SDK 8 and Node.js with npm.

```bash
git clone https://github.com/Jp3528/HomeSphere.git
cd HomeSphere
```

**Configuration.** Review `docker-compose.yml` and [`.env.example`](.env.example). Compose supplies development values and enables API migrations/seeding; example credentials are intended for local use.

```bash
docker compose up --build
```

Web: `http://localhost:3000`. Swagger: `http://localhost:5100/swagger`. Health: `http://localhost:5100/health/status`. On a new database, the seeder defaults to `admin@homesphere.dev` / `Admin123!`; these values can be overridden through `Seed:AdminEmail` and `Seed:AdminPassword`. Register an account through the web app to evaluate the customer flow.

### Checks

```bash
dotnet test apps/api/HomeSphere.sln
npm --prefix apps/web ci
npm --prefix apps/web run lint
npm --prefix apps/web run build
```

The suite includes unit tests and HTTP tests through WebApplicationFactory. Its factory uses EF Core InMemory, so it does not replace concurrency tests against PostgreSQL. The versioned pipeline is available in [GitHub Actions](https://github.com/Jp3528/HomeSphere/actions).

### Structure

| Path                                       | Contents                                         |
| ------------------------------------------ | ------------------------------------------------ |
| [`apps/api/src/`](apps/api/src/)           | Domain, use cases, persistence and Web API.      |
| [`apps/api/tests/`](apps/api/tests/)       | Backend and API tests.                           |
| [`apps/web/`](apps/web/)                   | Storefront, customer account and administration. |
| [`docs/`](docs/)                           | Decisions and feature specifications.            |
| [`docker-compose.yml`](docker-compose.yml) | Local platform services.                         |

### Scope

A portfolio project with a mock payment gateway. Reservations, signatures and idempotency are implementation features; they do not establish certified banking integration or production load capacity. Compose development values require environment-specific configuration before public deployment.

### Documentation

- [Architecture](docs/adr/0001-architecture.md)
- [Interactive scenes](docs/interactive-scenes.md)
- [Cart and checkout](docs/cart-and-checkout.md)
- [Orders and payments](docs/order-and-payments.md)
- [Security](docs/security.md)
- [Analytics and auditing](docs/analytics-and-audit.md)
- [Changelog](CHANGELOG.md)

**License:** [MIT](LICENSE).
