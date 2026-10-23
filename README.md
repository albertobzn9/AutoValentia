# AutoValentia

AutoValentia is a cross-platform desktop app for reviewing and analyzing behavioral-task data from the Crossing-Mediated Conflict Task. It reads MATLAB `.mat` files, prepares latency data by trial block, exports review workbooks, and creates latency plots.

It is built with .NET and Avalonia UI for macOS and Windows.

## What it does

- Opens `.mat` files containing an 8- or 9-column matrix of raw protocol data.
- Normalizes compatible 9-column matrices to 8 columns automatically.
- Detects no-conflict crossings, conflict crossings, and discrimination sessions from the data.
- Splits valid latency observations into three ordered blocks.
- Exports an Excel workbook with a daily raw-data view, per-rat block values, block averages, and standard error of the mean (SEM).
- Creates latency plots and exports them as PNG files.

## Input requirements

Each file name must include its session day and rat number, for example:

```text
exp_0526_dis_d3r4.mat
```

This identifies day 3 and rat 4. The MATLAB file must contain a numeric matrix named `Resultados`.

## Analysis rules

Only valid crossings are included in the latency analysis. The app applies the following protocol rules:

- For discrimination, the final day uses the first 7 no-conflict crossings and the first 3 conflict crossings.
- For conflict crossings, latency outliers are removed above the daily protocol limit: 45 s on days 1–2, 75 s on day 3, 105 s on day 4, and 135 s on day 5.
- Excel and plot outputs use the same filtered data.

## Use the app

1. Download the appropriate package from [Releases](../../releases).
2. Open AutoValentia and add the `.mat` files to review.
3. Create the Excel review workbook or open the latency plot.
4. Optionally export the plot as a PNG image.

Available packages are a DMG and ZIP for macOS Apple Silicon, plus an EXE and ZIP for Windows x64.

## Run from source

Install the .NET 10 SDK, then run:

```bash
dotnet run --project src/AutoValentia.csproj
```

## Method reference

The behavioral-task workflow is based on:

Illescas-Huerta, E., Ramirez-Lugo, L., Sierra, R. O., Quillfeldt, J. A., & Sotres-Bayon, F. (2021). *Conflict Test Battery for Studying the Act of Facing Threats in Pursuit of Rewards.* Frontiers in Neuroscience, 15, 645769. [https://doi.org/10.3389/fnins.2021.645769](https://doi.org/10.3389/fnins.2021.645769)
