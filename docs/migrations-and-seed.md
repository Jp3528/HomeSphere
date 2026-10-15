# Estrategia de Migraciones y Datos Semilla (Seed) — HomeSphere

Este documento describe el flujo estandarizado para la creación, aplicación y mantenimiento de migraciones de base de datos y datos semilla en **HomeSphere**.

---

## 1. Principios Fundamentales
- **Persistencia**: PostgreSQL 16/17 con Entity Framework Core 8 y proveedor `Npgsql.EntityFrameworkCore.PostgreSQL`.
- **Inmutabilidad y Versionado**: Cada cambio al modelo relacional debe quedar encapsulado en una migración con nombre descriptivo en `HomeSphere.Infrastructure/Persistence/Migrations/`.
- **Cero Secretos**: Las cadenas de conexión se inyectan mediante variables de entorno (`ConnectionStrings__DefaultConnection`) o `appsettings.Development.json` (estrictamente excluido de Git en `.gitignore`).
- **Nomenclatura**: Tablas y columnas mapeadas a `snake_case` para convención nativa de PostgreSQL.

---

## 2. Configuración del Entorno de Base de Datos

### Opción A: Contenedor Docker Compose (Recomendada para CI / Entornos Aislados)
```bash
docker compose -f infra/docker-compose.dev.yml up -d
```

### Opción B: PostgreSQL Nativo en Host
Asegurar que el servicio local esté ejecutándose en el puerto 5432 y que la base de datos `homesphere_dev` exista:
```sql
CREATE DATABASE homesphere_dev;
```

---

## 3. Comandos para Desarrolladores

### Añadir una Nueva Migración
```powershell
$env:DOTNET_ROOT = "C:\Users\USER\AppData\Local\dotnet"
$env:Path = "$env:Path;C:\Users\USER\AppData\Local\dotnet;C:\Users\USER\.dotnet\tools"

dotnet ef migrations add <NombreDescriptivo> `
  --project apps/api/src/HomeSphere.Infrastructure/HomeSphere.Infrastructure.csproj `
  --startup-project apps/api/src/HomeSphere.WebApi/HomeSphere.WebApi.csproj `
  --output-dir Persistence/Migrations
```

### Aplicar Migraciones Manualmente
```powershell
dotnet ef database update `
  --project apps/api/src/HomeSphere.Infrastructure/HomeSphere.Infrastructure.csproj `
  --startup-project apps/api/src/HomeSphere.WebApi/HomeSphere.WebApi.csproj
```

### Aplicación Automática en Desarrollo
En modo `Development`, la API ejecuta automáticamente `ApplyMigrationsAsync()` en `Program.cs` al arrancar el servidor, garantizando que cualquier desarrollador o evaluador obtenga el esquema actualizado de forma instantánea.

---

## 4. Estrategia de Datos Semilla (Data Seeding)
- **Seeds de Identidad**: Roles del sistema (`CUSTOMER`, `ADMIN`, `WAREHOUSE`, `MANAGER`) y usuario administrador de desarrollo.
- **Seeds de Catálogo**: Categorías base de muebles, marcas y productos con variantes y atributos.
- **Seeds de Ambientes**: Escenas interactivas (sala, dormitorio, comedor) con coordenadas porcentuales vinculadas a productos de demostración.
- **Regla de idempotencia en Seeds**: Toda rutina de seed valida `if (!await context.Set<T>().AnyAsync())` para impedir duplicación de registros al reiniciar el servicio.
