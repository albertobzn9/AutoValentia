# AutoValentia

[English](README.md) | [Español](README.es.md)

<img src="src/AutoValentia/Assets/autovalentia-logo.png" alt="AutoValentia logo" width="96" />

AutoValentia is a cross-platform desktop app for reviewing and analyzing behavioral-task data from the Crossing-Mediated Conflict Task. It reads MATLAB `.mat` files, prepares latency data by trial block, exports review workbooks, and creates latency plots.

## Features

- Opens `.mat` files containing 8- or 9-column raw protocol matrices.
- Normalizes compatible 9-column matrices to 8 columns automatically.
- Detects no-conflict crossings, conflict crossings, and discrimination sessions from the data.
- Splits valid latency observations into three chronological blocks.
- Exports Excel workbooks with daily raw data, per-rat block values, block averages, and standard error of the mean (SEM).
- Creates latency plots and exports them as PNG files.

## Download

Download the latest self-contained build from the [Releases page](https://github.com/albertobzn9/AutoValentia/releases/latest).

- **macOS Apple Silicon:** DMG installer or portable ZIP.
- **Windows x64:** portable EXE or ZIP.

MATLAB and the .NET runtime are not required to run a release build.

## Input requirements

Each file name must include its session day and rat number, for example:

```text
exp_0526_dis_d3r4.mat
```

This identifies day 3 and rat 4. The MATLAB file must contain a numeric matrix named `Resultados`.

## Analysis rules

The complete processing specification is available in [the analysis rules](docs/analysis-rules.md). It defines matrix normalization, trial eligibility, protocol-specific filtering, chronological blocks, missing-data handling, averages, and SEM.

See the [documentation index](docs/README.md) for the code map and documentation maintenance guidance.

## Use the app

1. Download the package for your platform from [Releases](https://github.com/albertobzn9/AutoValentia/releases/latest).
2. Open AutoValentia and add the `.mat` files to review.
3. Create the Excel review workbook or open the latency plot.
4. Optionally export the plot as a PNG image.

## Development

Requirements: the .NET 10 SDK. Avalonia provides the cross-platform desktop interface.

```bash
dotnet run --project src/AutoValentia/AutoValentia.csproj
dotnet test tests/AutoValentia.Tests/AutoValentia.Tests.csproj
```

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/AutoValentia` | Avalonia application source, analysis logic, data reader, export code, and assets. |
| `tests/AutoValentia.Tests` | Unit tests for analysis, exports, and plot presentation. |
| `docs` | Processing rules and code map. |
| `packaging/macos` | macOS application bundle metadata. |
| `scripts` | Local release-packaging scripts. |

## Packaging a release

On macOS, run the following from the repository root with a new version number:

```bash
./scripts/package-release.sh 1.2.0
```

The script produces a macOS Apple Silicon DMG and ZIP, plus a self-contained Windows x64 EXE and ZIP under `artifacts/release/`.

## Method reference

Illescas-Huerta, E., Ramirez-Lugo, L., Sierra, R. O., Quillfeldt, J. A., & Sotres-Bayon, F. (2021). *Conflict Test Battery for Studying the Act of Facing Threats in Pursuit of Rewards.* Frontiers in Neuroscience, 15, 645769. [https://doi.org/10.3389/fnins.2021.645769](https://doi.org/10.3389/fnins.2021.645769)
