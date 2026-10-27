# AutoValentia

[English](README.md) | [Español](README.es.md)

<img src="src/AutoValentia/Assets/autovalentia-logo.png" alt="Logotipo de AutoValentia" width="96" />

AutoValentia es una aplicación de escritorio multiplataforma para revisar y analizar datos conductuales de la tarea Crossing-Mediated Conflict. Lee archivos MATLAB `.mat`, prepara datos de latencia por bloque de ensayos, exporta libros de revisión y crea gráficas de latencia.

## Funciones

- Abre archivos `.mat` que contienen matrices de protocolo crudas de 8 o 9 columnas.
- Normaliza automáticamente matrices compatibles de 9 columnas a 8 columnas.
- Detecta sesiones de cruces sin conflicto, cruces con conflicto y discriminación a partir de los datos.
- Divide las observaciones de latencia válidas en tres bloques cronológicos.
- Exporta libros de Excel con datos crudos diarios, valores por bloque de cada rata, promedios por bloque y error estándar de la media (SEM).
- Crea gráficas de latencia y las exporta como archivos PNG.

## Descargas

Descarga la versión autocontenida más reciente desde la [página de Releases](https://github.com/albertobzn9/AutoValentia/releases/latest).

- **macOS Apple Silicon:** instalador DMG o ZIP portable.
- **Windows x64:** EXE portable o ZIP.

No se necesita MATLAB ni el runtime de .NET para ejecutar una versión publicada.

## Requisitos de entrada

Cada nombre de archivo debe incluir el día de sesión y el número de rata. Por ejemplo:

```text
exp_0526_dis_d3r4.mat
```

Esto identifica el día 3 y la rata 4. El archivo MATLAB debe contener una matriz numérica llamada `Resultados`.

## Reglas de análisis

La especificación completa del procesamiento está disponible en [las reglas de análisis](docs/analysis-rules.md). Define la normalización de matrices, elegibilidad de ensayos, filtros específicos de protocolo, bloques cronológicos, manejo de datos faltantes, promedios y SEM.

Consulta el [índice de documentación](docs/README.md) para ver el mapa de código y las indicaciones para mantener la documentación.

## Usar la aplicación

1. Descarga el paquete para tu plataforma desde [Releases](https://github.com/albertobzn9/AutoValentia/releases/latest).
2. Abre AutoValentia y agrega los archivos `.mat` que quieres revisar.
3. Crea el libro de Excel de revisión o abre la gráfica de latencias.
4. Opcionalmente, exporta la gráfica como imagen PNG.

## Desarrollo

Requisito: SDK de .NET 10. Avalonia proporciona la interfaz de escritorio multiplataforma.

```bash
dotnet run --project src/AutoValentia/AutoValentia.csproj
dotnet test tests/AutoValentia.Tests/AutoValentia.Tests.csproj
```

## Estructura del repositorio

| Ruta | Propósito |
| --- | --- |
| `src/AutoValentia` | Código de la aplicación Avalonia, lógica de análisis, lector de datos, exportación y recursos. |
| `tests/AutoValentia.Tests` | Pruebas unitarias de análisis, exportación y presentación de gráficas. |
| `docs` | Reglas de procesamiento y mapa de código. |
| `packaging/macos` | Metadatos del paquete de aplicación para macOS. |
| `scripts` | Scripts locales para empaquetar releases. |

## Empaquetar un release

En macOS, ejecuta lo siguiente desde la raíz del repositorio usando un número de versión nuevo:

```bash
./scripts/package-release.sh 1.2.0
```

El script genera un DMG y ZIP para macOS Apple Silicon, además de un EXE y ZIP autocontenidos para Windows x64, dentro de `artifacts/release/`.

## Reconocimiento

AutoValentia se originó a partir del proyecto en Python [AutomatizacionCajaValentia](https://github.com/ericramirezz/AutomatizacionCajaValentia) de Eric Ramírez. Este repositorio es una reescritura independiente en .NET/Avalonia que conserva y amplía el flujo de análisis de datos conductuales.

## Referencia metodológica

Illescas-Huerta, E., Ramirez-Lugo, L., Sierra, R. O., Quillfeldt, J. A., & Sotres-Bayon, F. (2021). *Conflict Test Battery for Studying the Act of Facing Threats in Pursuit of Rewards.* Frontiers in Neuroscience, 15, 645769. [https://doi.org/10.3389/fnins.2021.645769](https://doi.org/10.3389/fnins.2021.645769)
