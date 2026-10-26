namespace AutoValentia;

internal enum AppLanguage
{
    English,
    Spanish
}

internal static class AppText
{
    public static event Action? LanguageChanged;

    public static AppLanguage CurrentLanguage { get; private set; } = AppLanguage.English;

    private static bool IsSpanish => CurrentLanguage == AppLanguage.Spanish;

    public static string MainSubtitle => IsSpanish
        ? "Preparación y revisión de ensayos conductuales."
        : "Behavioral trial preparation and review.";

    public static string SelectFilesHeading => IsSpanish
        ? "1. Selecciona los archivos MATLAB"
        : "1. Select MATLAB files";

    public static string SelectFilesDescription => IsSpanish
        ? "La app prepara automáticamente los archivos seleccionados para crear el Excel de revisión."
        : "The app automatically prepares selected files to create a review workbook.";

    public static string SelectMatFilesButton => IsSpanish ? "Seleccionar .mat…" : "Select .mat files…";
    public static string RemoveFromListButton => IsSpanish ? "Quitar de la lista" : "Remove from list";
    public static string CreateWorkbookButton => IsSpanish ? "Crear Excel de revisión…" : "Create review workbook…";
    public static string ViewPlotsButton => IsSpanish ? "Ver gráficas…" : "View plots…";
    public static string NoFilesSelected => IsSpanish ? "Aún no hay archivos seleccionados." : "No files selected yet.";
    public static string SelectMatFilesTitle => IsSpanish ? "Seleccionar archivos MATLAB" : "Select MATLAB files";
    public static string MatFiles => IsSpanish ? "Archivos MATLAB" : "MATLAB files";
    public static string PreparingMatrices => IsSpanish ? "Preparando matrices…" : "Preparing matrices…";
    public static string FilesContainErrors => IsSpanish
        ? "Hay archivos con error. Quítalos de la lista antes de crear el Excel."
        : "Some files contain errors. Remove them before creating the workbook.";
    public static string InvalidFileNames => IsSpanish
        ? "Hay nombres que no incluyen dNúmero rNúmero."
        : "Some file names do not include dNumber rNumber.";
    public static string SaveReviewWorkbookTitle => IsSpanish ? "Guardar Excel de revisión" : "Save review workbook";
    public static string ReviewWorkbookFileName => IsSpanish ? "revision_latencias.xlsx" : "latency_review.xlsx";
    public static string ExcelWorkbook => IsSpanish ? "Archivo de Excel" : "Excel workbook";
    public static string NoLocalWorkbookPath => IsSpanish
        ? "No se pudo obtener una ubicación local para guardar el Excel."
        : "Could not get a local path to save the workbook.";
    public static string CreatingWorkbook => IsSpanish ? "Creando Excel de revisión…" : "Creating review workbook…";
    public static string CouldNotCreateWorkbook(string message) => IsSpanish
        ? $"No se pudo crear el Excel: {message}"
        : $"Could not create the workbook: {message}";
    public static string CouldNotCreatePlots(string message) => IsSpanish
        ? $"No se pudieron crear las gráficas: {message}"
        : $"Could not create the plots: {message}";
    public static string WorkbookCreated(CrossingType crossingType, int dayCount, int ratCount) => IsSpanish
        ? $"Excel creado: {crossingType.DisplayName()}; {dayCount} día(s), {ratCount} rata(s)."
        : $"Workbook created: {crossingType.DisplayName()}; {dayCount} day(s), {ratCount} rat(s).";
    public static string PreparedFilesSummary(int readyCount, int errorCount) => errorCount == 0
        ? IsSpanish
            ? $"{readyCount} archivo(s) preparado(s) y listo(s) para el Excel."
            : $"{readyCount} file(s) prepared and ready for the workbook."
        : IsSpanish
            ? $"{readyCount} archivo(s) preparado(s); {errorCount} con error."
            : $"{readyCount} file(s) prepared; {errorCount} with errors.";

    public static string LatencyPlotsTitle => IsSpanish ? "Gráficas de latencias" : "Latency plots";
    public static string SafeCrossings => IsSpanish ? "Cruces seguros" : "No-conflict crossings";
    public static string DangerCrossings => IsSpanish ? "Cruces peligrosos" : "Conflict crossings";
    public static string Discrimination => IsSpanish ? "Discriminación" : "Discrimination";
    public static string SemDescription => IsSpanish
        ? "Las barras muestran el error estándar de la media (SEM)."
        : "Error bars show the standard error of the mean (SEM).";
    public static string ExportPngButton => IsSpanish ? "Exportar imagen PNG…" : "Export PNG image…";
    public static string SavePlotTitle => IsSpanish ? "Guardar gráfica como imagen" : "Save plot as image";
    public static string PngImage => IsSpanish ? "Imagen PNG" : "PNG image";
    public static string PlotFileNamePrefix => IsSpanish ? "latencias" : "latencies";
    public static string NoLocalImagePath => IsSpanish
        ? "No se pudo obtener una ubicación local para guardar la imagen."
        : "Could not get a local path to save the image.";
    public static string PngImageSaved(string fileName) => IsSpanish
        ? $"Imagen PNG guardada: {fileName}"
        : $"PNG image saved: {fileName}";
    public static string CouldNotSaveImage(string message) => IsSpanish
        ? $"No se pudo guardar la imagen: {message}"
        : $"Could not save the image: {message}";
    public static string NoDataToPlot => IsSpanish ? "No hay datos para graficar." : "No data available to plot.";
    public static string LatenciesTitle(CrossingType crossingType) => IsSpanish
        ? $"Latencias: {crossingType.DisplayName()}"
        : $"Latencies: {crossingType.DisplayName()}";
    public static string PlotSubtitle => IsSpanish
        ? "Cada punto es el promedio de las ratas del día; los tres puntos corresponden a los tercios cronológicos del protocolo."
        : "Each point is the daily mean across rats; the three points correspond to the chronological thirds of the protocol.";
    public static string DayAxisLabel => IsSpanish ? "Días (bloques de ensayo)" : "Days (trial blocks)";
    public static string LatencyAxisLabel => IsSpanish ? "Latencias (s)" : "Latencies (s)";

    public static string Pending => IsSpanish ? "pendiente" : "pending";
    public static string RowsPrepared(int rowCount) => IsSpanish ? $"{rowCount} filas preparadas" : $"{rowCount} rows prepared";
    public static string MissingResultadosMatrix => IsSpanish
        ? "No contiene la matriz Resultados."
        : "Does not contain the Resultados matrix.";
    public static string UnsupportedFileFormat => IsSpanish
        ? "El archivo no tiene un formato compatible."
        : "The file format is not supported.";
    public static string FileNotReadyForAnalysis(string fileName) => IsSpanish
        ? $"{fileName} no está preparada para el análisis."
        : $"{fileName} is not ready for analysis.";
    public static string InvalidSessionFileName(string fileName) => IsSpanish
        ? $"El nombre {fileName} debe incluir el patrón dNúmero rNúmero, por ejemplo d3r4."
        : $"{fileName} must include the dNumber rNumber pattern, for example d3r4.";
    public static string NoMatricesReady => IsSpanish ? "No hay matrices listas para analizar." : "No matrices are ready for analysis.";
    public static string DuplicateSessionFile(string sessionName) => IsSpanish
        ? $"Hay más de un archivo identificado como {sessionName}."
        : $"More than one file is identified as {sessionName}.";
    public static string MatrixNotReadyForAnalysis => IsSpanish
        ? "La matriz no está preparada para el análisis."
        : "The matrix is not ready for analysis.";
    public static string DangerDaysOnly(decimal day) => IsSpanish
        ? $"Cruces peligrosos solo admite días d1 a d5; se recibió d{day:0.##}."
        : $"Conflict crossings only support days d1 through d5; received d{day:0.##}.";
    public static string NoValidTrials => IsSpanish
        ? "No hay ensayos con desplazamiento válido para analizar."
        : "No trials with valid movement are available for analysis.";

    public static string SummaryWorksheetName => IsSpanish ? "Resumen" : "Summary";
    public static string AverageLabel => IsSpanish ? "promedio" : "average";
    public static string SemLabel => "sem";

    public static void SetLanguage(AppLanguage language)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        CurrentLanguage = language;
        LanguageChanged?.Invoke();
    }
}
