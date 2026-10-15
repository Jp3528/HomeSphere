# HomeSphere 🏠🛋️
> **E-commerce avanzado para el hogar con experiencia inmersiva de ambientes interactivos.**  
> *Proyecto de portafolio para prácticas de Ingeniería de Sistemas.*

---

## 🚀 Visión General
HomeSphere es una plataforma completa de comercio electrónico para muebles, iluminación y decoración del hogar. A diferencia de las tiendas convencionales, combina un catálogo transaccional robusto con **escenas interactivas de habitaciones** (hotspots responsive y accesibles) que permiten al usuario explorar ambientes decorados y adquirir los productos directamente.

---

## 🛠️ Stack Tecnológico

| Capa | Tecnologías |
|---|---|
| **Frontend** | Next.js 15 (App Router, React Server Components), TypeScript, Tailwind CSS, Zustand, Lucide Icons |
| **Backend** | ASP.NET Core 8 Web API, C#, Clean Architecture pragmática, MediatR, FluentValidation |
| **Persistencia** | PostgreSQL 16, Entity Framework Core 8, Npgsql |
| **Seguridad** | JWT con rotación de Refresh Tokens, RBAC server-side, prevención de IDOR |
| **DevOps & Infra** | Docker, Docker Compose, GitHub Actions |

---

## 📁 Estructura del Monorepo

```text
homesphere/
├── apps/
│   ├── api/                          # Backend ASP.NET Core 8
│   │   ├── src/
│   │   │   ├── HomeSphere.Domain/    # Entidades y reglas de negocio puras
│   │   │   ├── HomeSphere.Application/ # Casos de uso, DTOs y validaciones
│   │   │   ├── HomeSphere.Infrastructure/ # EF Core, migraciones y adaptadores
│   │   │   └── HomeSphere.WebApi/    # Controllers, endpoints REST, middleware
│   │   └── tests/
│   │       ├── HomeSphere.UnitTests/
│   │       └── HomeSphere.IntegrationTests/
│   └── web/                          # Frontend Next.js (App Router)
│       └── src/
├── docs/                             # ADRs, modelo de datos y diagramas
├── infra/                            # Docker Compose y configs de despliegue
├── .editorconfig
├── .env.example
└── README.md
```

---

## ⚡ Inicio Rápido (Desarrollo Local)

### Prerrequisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker & Docker Compose](https://www.docker.com/)

### 1. Iniciar la Base de Datos
```bash
docker compose -f infra/docker-compose.dev.yml up -d
```

### 2. Ejecutar el Backend (API)
```bash
cd apps/api
dotnet restore
dotnet run --project src/HomeSphere.WebApi
```
*La API estará disponible en `http://localhost:5100` con Swagger UI en `http://localhost:5100/swagger` y health check en `http://localhost:5100/health`.*

### 3. Ejecutar el Frontend (Web)
```bash
cd apps/web
npm install
npm run dev
```
*El frontend estará disponible en `http://localhost:3000`.*

---

## 📜 Reglas de Negocio No Negociables
1. **El precio y totales se recalculan 100% en el servidor**. Nunca se acepta el precio o total enviado por el cliente.
2. **Stock disponible = onHand - reserved**. Nunca puede ser negativo.
3. **Checkout y reservas son transaccionales**. La reserva ocurre al iniciar checkout, nunca al agregar al carrito.
4. **Idempotencia en pedidos y webhooks de pago** mediante `Idempotency-Key`.
5. **Hotspots porcentuales (0-100%)** con alternativa accesible y soporte por teclado.
