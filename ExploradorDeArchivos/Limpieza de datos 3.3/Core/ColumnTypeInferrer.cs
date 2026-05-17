using System.Globalization;

namespace ExploradorDeArchivos.Limpieza_de_datos_3._3.Core;

/// <summary>
/// Infiere el tipo dominante de cada columna (por mayoría) y detecta
/// las celdas cuyo valor no es compatible con ese tipo.
/// Detecta dos familias de anomalías:
///   1. Texto donde debería haber número o fecha.
///   2. Número donde debería haber texto (ej. un número en columna "estado").
/// </summary>
public sealed class ColumnTypeInferrer
{
    /// <summary>
    /// Umbral mínimo de valores no-vacíos que deben cumplir el tipo para
    /// declarar esa columna como Numérica o Fecha (0–1). Por defecto 0.70.
    /// </summary>
    public double Threshold { get; init; } = 0.70;

    /// <summary>
    /// Para columnas de Texto: si esta proporción de valores son números puros,
    /// se marcan como anomalía "número en columna de texto". Por defecto 0.05
    /// (basta con que el 5 % sean números para señalarlo).
    /// </summary>
    public double NumericInTextThreshold { get; init; } = 0.05;

    public sealed record InferenceResult(
        IReadOnlyDictionary<string, ColumnDataType> ColumnTypes,
        IReadOnlyList<CellError> CellErrors
    );

    public InferenceResult Infer(IReadOnlyList<IDictionary<string, object>> rows)
    {
        if (rows.Count == 0)
            return new InferenceResult(new Dictionary<string, ColumnDataType>(), []);

        var columns = rows[0].Keys.ToList();
        var columnTypes = new Dictionary<string, ColumnDataType>(StringComparer.OrdinalIgnoreCase);

        foreach (var col in columns)
            columnTypes[col] = InferColumnType(rows, col);

        var errors = DetectCellErrors(rows, columnTypes);
        return new InferenceResult(columnTypes, errors);
    }

    // ── Infiere el tipo dominante de una columna ────────────────────────────
    private ColumnDataType InferColumnType(
        IReadOnlyList<IDictionary<string, object>> rows, string column)
    {
        int total = 0, numericCount = 0, dateCount = 0;

        bool columnSuggestsDate = IsFechaNombre(column);
        bool columnSuggestsNumeric = IsNumeroNombre(column);

        foreach (var row in rows)
        {
            var raw = GetRawValue(row, column);
            if (string.IsNullOrWhiteSpace(raw)) continue;
            total++;

            // Columnas semánticamente numéricas (expediente, id, código…)
            // nunca se clasifican como fecha aunque el valor pase IsDate.
            if (columnSuggestsNumeric)
            {
                if (IsNumeric(raw)) numericCount++;
                // Si no es numérico, es texto; no contar como fecha
                continue;
            }

            // Para columnas de fecha: IsDate tiene prioridad sobre IsNumeric.
            if (columnSuggestsDate)
            {
                if (IsDate(raw)) dateCount++;
                else if (IsNumeric(raw)) numericCount++;
                continue;
            }

            // Caso general: fecha primero, luego número (evita que "01/01/1990"
            // cuente como número cuando double.TryParse falla).
            if (IsDate(raw) && !IsNumeric(raw))
                dateCount++;
            else if (IsNumeric(raw))
                numericCount++;
        }

        if (total == 0) return ColumnDataType.Text;

        // Semántica numérica explícita
        if (columnSuggestsNumeric) return ColumnDataType.Numeric;

        // Semántica de fecha explícita con umbral más bajo
        if (columnSuggestsDate && (double)dateCount / total >= 0.50)
            return ColumnDataType.Date;

        if ((double)dateCount / total >= Threshold) return ColumnDataType.Date;
        if ((double)numericCount / total >= Threshold) return ColumnDataType.Numeric;
        return ColumnDataType.Text;
    }

    /// <summary>
    /// Columnas cuyo nombre indica explícitamente que contienen números/IDs,
    /// nunca fechas: expediente, numero, id, codigo, folio, clave, cuenta, etc.
    /// </summary>
    private static bool IsNumeroNombre(string column)
    {
        var lower = column.ToLowerInvariant();
        return lower.Contains("expediente") || lower.Contains("numero") ||
               lower.Contains("número") || lower.Contains("nro") ||
               lower.Contains("folio") || lower.Contains("codigo") ||
               lower.Contains("código") || lower.Contains("clave") ||
               lower.Contains("cuenta") || lower.Contains("id") ||
               lower.Contains("cantidad") || lower.Contains("monto") ||
               lower.Contains("precio") || lower.Contains("importe") ||
               lower.Contains("total") || lower.Contains("saldo") ||
               lower.Contains("telefono") || lower.Contains("teléfono") ||
               lower.Contains("cp") || lower.Contains("zip") ||
               lower.Contains("edad") || lower.Contains("age");
    }

    /// <summary>
    /// Columnas cuyo nombre sugiere que contienen fechas.
    /// </summary>
    private static bool IsFechaNombre(string column)
    {
        var lower = column.ToLowerInvariant();
        return lower.Contains("fecha") || lower.Contains("date") ||
               lower.Contains("nacimien") || lower.Contains("birth") ||
               lower.Contains("dob") || lower.Contains("año") ||
               lower.Contains("anio") || lower.Contains("year") ||
               lower.Contains("vencim") || lower.Contains("expir") ||
               lower.Contains("inicio") || lower.Contains("fin") ||
               lower.Contains("alta") || lower.Contains("baja");
    }

    // ── Detecta anomalías en ambas direcciones ───────────────────────────────
    private List<CellError> DetectCellErrors(
        IReadOnlyList<IDictionary<string, object>> rows,
        IReadOnlyDictionary<string, ColumnDataType> columnTypes)
    {
        var errors = new List<CellError>();

        for (int i = 0; i < rows.Count; i++)
        {
            foreach (var kvp in columnTypes)
            {
                var raw = GetRawValue(rows[i], kvp.Key);
                if (string.IsNullOrWhiteSpace(raw)) continue;

                switch (kvp.Value)
                {
                    // ── Columna Numérica: detectar texto no numérico ─────────
                    case ColumnDataType.Numeric when !IsNumeric(raw):
                        errors.Add(new CellError(i, kvp.Key, raw,
                            ColumnDataType.Numeric, CellErrorKind.UnexpectedText));
                        break;

                    // ── Columna Fecha: detectar texto que no sea fecha ───────
                    case ColumnDataType.Date when !IsDate(raw):
                        errors.Add(new CellError(i, kvp.Key, raw,
                            ColumnDataType.Date, CellErrorKind.UnexpectedDate));
                        break;

                    // ── Columna Texto: detectar números puros ────────────────
                    case ColumnDataType.Text when IsStrictlyNumeric(raw):
                        errors.Add(new CellError(i, kvp.Key, raw,
                            ColumnDataType.Text, CellErrorKind.UnexpectedNumeric));
                        break;
                }
            }
        }

        return errors;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string GetRawValue(IDictionary<string, object> row, string column) =>
        row.TryGetValue(column, out var v) ? v?.ToString() ?? string.Empty : string.Empty;

    /// <summary>Acepta enteros, decimales y notación científica.</summary>
    private static bool IsNumeric(string value) =>
        double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _);

    /// <summary>
    /// Solo marca como "número puro" valores que son exclusivamente dígitos
    /// (con separador decimal opcional). Evita falsos positivos con IDs como
    /// "A-001" o códigos postales con letras.
    /// </summary>
    private static bool IsStrictlyNumeric(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0) return false;

        // Rechazar valores que parezcan IDs o fechas (contienen guión, barra, letras)
        foreach (var ch in trimmed)
        {
            if (!char.IsDigit(ch) && ch != '.' && ch != ',' && ch != '-' && ch != '+')
                return false;
        }

        return double.TryParse(trimmed,
            NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }

    private static readonly string[] ExtraDateFormats =
    [
        // NO incluimos "yyyyMMdd" aquí para evitar que números como 20001231
        // se clasifiquen como fecha en columnas sin semántica de fecha explícita.
        // Ese formato solo se acepta en CleanDate (DataCleaner) donde el contexto
        // ya está confirmado.
        "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd",
        "dd-MM-yyyy", "dd/MM/yyyy", "dd.MM.yyyy",
        "MM-dd-yyyy", "MM/dd/yyyy", "MM.dd.yyyy",
        "dd-MMM-yyyy", "dd/MMM/yyyy", "d-MMM-yyyy",
        "d-M-yyyy", "d/M/yyyy", "d.M.yyyy",
        "yyyy-M-d",  "yyyy/M/d",
        "dd-MM-yy",  "dd/MM/yy",
        "d MMM yyyy", "MMM d, yyyy",
    ];

    private static readonly CultureInfo[] DateCultures =
    [
        CultureInfo.InvariantCulture,
        new CultureInfo("es-ES"),
        new CultureInfo("en-US"),
    ];

    /// <summary>
    /// Determina si un valor es una fecha.
    /// Reglas estrictas para evitar falsos positivos:
    ///   1. Debe tener al menos un separador de fecha (-, /, ., espacio) o letras de mes.
    ///   2. Los números puros (ej. "12345", "20001231") NO son fechas — son numéricos.
    /// </summary>
    private static bool IsDate(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length < 6) return false;

        // Rechazar valores puramente numéricos (sin separador de fecha).
        // "20001231" es un número, no una fecha — solo se acepta como fecha
        // en columnas con semántica explícita de fecha.
        if (IsNumeric(trimmed)) return false;

        // Debe contener al menos un separador típico de fecha o letra (mes abreviado)
        bool hasSeparator = trimmed.Contains('-') || trimmed.Contains('/') ||
                            trimmed.Contains('.') || trimmed.Any(char.IsLetter);
        if (!hasSeparator) return false;

        foreach (var culture in DateCultures)
        {
            if (DateTime.TryParse(trimmed, culture, DateTimeStyles.None, out _))
                return true;
            if (DateTime.TryParseExact(trimmed, ExtraDateFormats, culture,
                    DateTimeStyles.None, out _))
                return true;
        }
        return false;
    }
}
