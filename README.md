# Sistema de Análisis de Opiniones de Clientes — Proceso ETL (Extracción)

Worker Service en **.NET 8** que implementa la fase de **extracción (E de ETL)** desde tres fuentes heterogéneas y guarda los datos validados en las tablas de *staging* de SQL Server.

| Fuente | Tecnología de extracción | Datos |
|---|---|---|
| Archivos CSV | CsvHelper | Encuestas internas y catálogos (clientes, productos, fuentes) |
| Base de datos relacional (`TiendaWebOrigen`) | ADO.NET | Reseñas del sitio web |
| API REST (`TiendaSocialApi`) | IHttpClientFactory | Comentarios de redes sociales |

## Estructura de la solución

```
├── SistemaAnalisisOpiniones/   Worker Service ETL (Domain / Application / Infrastructure / Configuration)
├── TiendaSocialApi/            Minimal API que expone los comentarios sociales (puerto 5180)
└── scripts/                    Creación y siembra de la BD origen TiendaWebOrigen
```

## Requisitos

- .NET SDK 8.0
- SQL Server local con autenticación integrada de Windows
- Base de datos de staging `SistemaAnalisisOpiniones` existente

## Ejecución

```bash
# 1. Crear y sembrar la base de datos origen (una sola vez)
sqlcmd -S localhost -E -i scripts/01_crear_tiendaweborigen.sql
sqlcmd -S localhost -E -f 65001 -i scripts/02_sembrar_resenas.sql

# 2. Levantar la API de comentarios sociales
dotnet run --project TiendaSocialApi

# 3. En otra terminal, ejecutar el Worker ETL
dotnet run --project SistemaAnalisisOpiniones
```

Al finalizar, el Worker imprime el resumen de extracción (registros y duración por fuente) y de carga (insertados y rechazados por tabla), y genera `etl_rejected_log.csv` con el detalle de los registros rechazados.

## Configuración

Las fuentes se configuran en la sección `Fuentes` de `SistemaAnalisisOpiniones/appsettings.json`. Cada una puede deshabilitarse con `"Enabled": false` sin tocar código. Las cadenas de conexión pueden sobreescribirse con User Secrets:

```bash
dotnet user-secrets set "Fuentes:BaseDatos:ConnectionString" "..." --project SistemaAnalisisOpiniones
```

## Documentación

El documento técnico completo (arquitectura, diagrama de flujo, justificación de decisiones y evidencias) se entrega por separado en formato Word junto con esta actividad.
