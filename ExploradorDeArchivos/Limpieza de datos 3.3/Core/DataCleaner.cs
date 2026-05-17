using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ExploradorDeArchivos.Limpieza_de_datos_3._3.Core;

/// <summary>
/// Aplica limpieza específica según el tipo dominante de cada columna:
///
/// • Text   → elimina caracteres especiales no válidos (*, @, #, ^, ~, etc.)
///            conservando letras, dígitos, espacios, guiones, puntos, comas,
///            apóstrofos, paréntesis y ampersand.
///
/// • Date   → normaliza múltiples formatos de fecha a yyyy-MM-dd.
///            Si el valor no puede parsearse se deja sin modificar y se registra.
///
/// • Numeric→ elimina símbolos de moneda, espacios y caracteres no numéricos
///            (conserva dígitos, punto decimal, coma y signo).
/// </summary>
public static class DataCleaner
{
    // Formatos de fecha que intentamos reconocer (orden de especificidad descendente)
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd",
        "dd-MM-yyyy", "dd/MM/yyyy", "dd.MM.yyyy",
        "MM-dd-yyyy", "MM/dd/yyyy", "MM.dd.yyyy",
        "dd-MMM-yyyy", "dd/MMM/yyyy",          // 15-Jan-2000
        "d-M-yyyy",  "d/M/yyyy",  "d.M.yyyy",
        "yyyy-M-d",  "yyyy/M/d",
        "dd-MM-yy",  "dd/MM/yy",
        "d MMM yyyy", "MMM d, yyyy",            // 15 Jan 2000 / Jan 15, 2000
    ];

    // Regex general: conserva letras, dígitos, espacio y puntuación básica.
    // Elimina: _ * @ # ^ ~ $ % etc.
    private static readonly Regex InvalidTextChars =
        new(@"[^\p{L}\p{N}\s\-.,;:!?'""()&]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Regex estricto para columnas de nombre/apellido de PERSONA:
    // solo letras, espacio, guion (Jean-Paul) y apóstrofo (O'Brien).
    private static readonly Regex InvalidNameChars =
        new(@"[^\p{L}\s\-']",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Devuelve true SOLO si la columna almacena nombres o apellidos de personas.
    /// Se excluyen deliberadamente columnas genéricas como "nombre_producto",
    /// "nombre_juego", "nombre_empresa", etc., que pueden contener dígitos.
    /// La coincidencia debe ser exacta o muy específica para evitar falsos positivos.
    /// </summary>
    private static bool IsNombrePersonaColumna(string column)
    {
        var lower = column.ToLowerInvariant().Trim();

        // Coincidencias exactas inequívocas
        if (lower is "nombre" or "apellido" or "apellidos" or
                     "name" or "surname" or "lastname" or
                     "firstname" or "primer_nombre" or "segundo_nombre" or
                     "primer_apellido" or "segundo_apellido")
            return true;

        // Columna que contenga "apellido" (siempre es de persona)
        if (lower.Contains("apellido") || lower.Contains("surname") ||
            lower.Contains("lastname") || lower.Contains("firstname"))
            return true;

        // "nombre" solo si NO va acompañado de palabras que indiquen no-persona
        if (lower.Contains("nombre") || lower.Contains("name"))
        {
            // Palabras que indican que NO es un nombre de persona
            bool esNoPersona =
                lower.Contains("producto") || lower.Contains("product") ||
                lower.Contains("juego") || lower.Contains("game") ||
                lower.Contains("video") || lower.Contains("empresa") ||
                lower.Contains("company") || lower.Contains("comercial") ||
                lower.Contains("marca") || lower.Contains("brand") ||
                lower.Contains("categoria") || lower.Contains("category") ||
                lower.Contains("archivo") || lower.Contains("file") ||
                lower.Contains("cancion") || lower.Contains("song") ||
                lower.Contains("pelicula") || lower.Contains("movie") ||
                lower.Contains("libro") || lower.Contains("book") ||
                lower.Contains("titulo") || lower.Contains("title") ||
                lower.Contains("descripcion") || lower.Contains("description");

            return !esNoPersona;
        }

        return false;
    }

    /// <summary>
    /// Limpia una colección de filas en función del tipo inferido por columna.
    /// Devuelve las filas limpias y un log de cambios realizados.
    /// </summary>
    public static (IReadOnlyList<IDictionary<string, object>> CleanedRows,
                   IReadOnlyList<string> ChangeLog)
        Clean(
            IReadOnlyList<IDictionary<string, object>> rows,
            IReadOnlyDictionary<string, ColumnDataType> columnTypes)
    {
        if (rows.Count == 0)
            return (rows, []);

        var log = new List<string>();
        var cleaned = new List<IDictionary<string, object>>(rows.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            var original = rows[i];
            var cleanRow = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in original)
            {
                var raw = kvp.Value?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(raw))
                {
                    cleanRow[kvp.Key] = kvp.Value;
                    continue;
                }

                if (!columnTypes.TryGetValue(kvp.Key, out var colType))
                {
                    cleanRow[kvp.Key] = kvp.Value;
                    continue;
                }

                var result = colType switch
                {
                    ColumnDataType.Text => CleanText(raw, kvp.Key),
                    ColumnDataType.Date => CleanDate(raw),
                    ColumnDataType.Numeric => CleanNumeric(raw),
                    _ => raw
                };

                cleanRow[kvp.Key] = result;

                if (result != raw)
                    log.Add($"Fila {i + 1}, '{kvp.Key}': \"{raw}\" → \"{result}\"");
            }

            cleaned.Add(cleanRow);
        }

        return (cleaned, log);
    }

    // ── Limpieza de texto ────────────────────────────────────────────────────

    /// <summary>
    /// Elimina caracteres inválidos según el tipo semántico de la columna:
    /// • Columnas de nombre/apellido → solo letras, espacios, guion y apóstrofo.
    /// • Resto de columnas de texto  → letras, dígitos y puntuación básica.
    /// </summary>
    private static string CleanText(string value, string columnName)
    {
        var regex = IsNombrePersonaColumna(columnName) ? InvalidNameChars : InvalidTextChars;
        var clean = regex.Replace(value, string.Empty);
        // Colapsar espacios múltiples que puedan quedar tras la eliminación
        clean = Regex.Replace(clean, @" {2,}", " ").Trim();
        return clean;
    }

    // ── Normalización de fechas ──────────────────────────────────────────────

    private static readonly CultureInfo[] DateCultures =
    [
        CultureInfo.InvariantCulture,
        new CultureInfo("es-ES"),
        new CultureInfo("en-US"),
    ];

    /// <summary>
    /// Intenta parsear la fecha con múltiples formatos y culturas.
    /// Si lo logra, devuelve yyyy-MM-dd. Si no, devuelve el valor original.
    /// </summary>
    public static string CleanDate(string value)
    {
        var trimmed = value.Trim();

        // Primero intentar con DateTime.Parse estándar (cubre ISO 8601, etc.)
        foreach (var culture in DateCultures)
        {
            if (DateTime.TryParse(trimmed, culture, DateTimeStyles.None, out var dt))
                return dt.ToString("yyyy-MM-dd");
        }

        // Luego forzar formatos específicos
        foreach (var culture in DateCultures)
        {
            if (DateTime.TryParseExact(trimmed, DateFormats, culture,
                    DateTimeStyles.None, out var dt))
                return dt.ToString("yyyy-MM-dd");
        }

        // No se pudo normalizar: devolver el valor original sin modificar
        return value;
    }

    // ── Limpieza de números ──────────────────────────────────────────────────

    private static readonly Regex NonNumericChars =
        new(@"[^\d.,''\-\+]", RegexOptions.Compiled);

    /// <summary>
    /// Elimina símbolos de moneda, espacios y otros caracteres no numéricos.
    /// </summary>
    private static string CleanNumeric(string value)
    {
        var clean = NonNumericChars.Replace(value.Trim(), string.Empty);

        // Normalizar coma decimal europea (1.234,56 → 1234.56)
        // Solo si hay exactamente una coma y es la última separación
        if (clean.Contains(',') && !clean.Contains('.'))
            clean = clean.Replace(',', '.');
        else
            clean = clean.Replace(",", string.Empty);  // separadores de miles

        return string.IsNullOrEmpty(clean) ? value : clean;
    }
}

