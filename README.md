# AutoValentia

Aplicación de escritorio multiplataforma para preparar y analizar ensayos conductuales de Caja Valentía.

La aplicación está desarrollada en **.NET 10 + Avalonia UI** para macOS y Windows. Permite seleccionar archivos `.mat`, prepararlos automáticamente y exportar un Excel de revisión ordenado por día y rata.

La implementación original en Python permanece en `Scripts/` como referencia funcional durante la migración.

## Estado de la reescritura

Este proyecto se está reescribiendo activamente de Python a **.NET + Avalonia UI** para contar con una aplicación de escritorio multiplataforma.

Ya está listo el flujo de revisión: carga automática de archivos `.mat`, preparación de matrices, división de latencias en tercios y generación del Excel de revisión por día y rata.

La siguiente etapa pendiente es la generación de gráficas a partir de los datos y días seleccionados. La definición de esas gráficas se incorporará después de validar el flujo de selección experimental.

**Caja Valentía** es una herramienta de automatización desarrollada para optimizar el flujo de trabajo en laboratorios de neurociencias (específicamente para el IFC de la UNAM). El software procesa archivos de datos crudos (`.mat`) provenientes de ensayos conductuales, automatizando la limpieza de datos, el cálculo de latencias y la generación de reportes estadísticos visuales y tabulares.

Esta herramienta fue diseñada específicamente para trabajar con **Pruebas de Conflicto**, permitiendo a los investigadores centrarse en la interpretación de resultados en lugar del procesamiento manual de datos.

## Características Principales

* **Procesamiento Automatizado:** Lectura y preparación directa de archivos `.mat` de MATLAB.
* **Análisis por Bloques (Tercios):** División automática de sesiones experimentales en tres bloques para el análisis de tendencias intra-sesión (aprendizaje, fatiga, conflicto).
* **Visualización Científica:** Pendiente en la reescritura de Avalonia. Se agregará después de definir las gráficas y los datos seleccionados para el análisis.
* **Exportación Consolidada:** Creación de reportes en Excel (`.xlsx`) con hojas de resumen, promedios por rata y desglose detallado de cada ensayo.
* **Interfaz de Usuario:** Aplicación de escritorio construida con Avalonia UI para macOS y Windows.

## Metodología

La lógica de procesamiento de este software y el diseño del protocolo de análisis se basan estrictamente en la metodología descrita en el siguiente artículo científico:

> **Illescas-Huerta, E., Ramirez-Lugo, L., Sierra, R. O., Quillfeldt, J. A., & Sotres-Bayon, F. (2021).** *Conflict Test Battery for Studying the Act of Facing Threats in Pursuit of Rewards.* Frontiers in Neuroscience, 15, 645769. 
> [DOI: 10.3389/fnins.2021.645769](https://doi.org/10.3389/fnins.2021.645769)

**Si utilizas este software en tu investigación, por favor asegúrate de citar el artículo original mencionado arriba.**

## Vídeo de instalación y explicación: https://youtu.be/M_hgrKpL-go

## Archivo Paralelo En Drive

Existe un snapshot de referencia en:

```text
/Users/ab/Library/CloudStorage/GoogleDrive-jasjabs19@gmail.com/My Drive/workspace/03_lab/01_Proyecto_Grande_App/01_analisis-conductual/01_sources/05_scripts/AutomatizacionCajaValentia
```

Ese directorio no es una copia activa ni un worktree confiable. Este repositorio
es la referencia de código; cualquier hallazgo del snapshot debe revisarse e
incorporarse aquí como un cambio puntual.

## 🛠️ Requisitos e Instalación

Para ejecutar el código desde el repositorio se necesita el SDK de **.NET 10**. Desde la carpeta del proyecto:

```bash
dotnet run
```

Las versiones empaquetadas para usuarios finales se descargarán desde la sección Releases del repositorio.
