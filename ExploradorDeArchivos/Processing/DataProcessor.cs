using ExploradorDeArchivos.Models;
using System.Globalization;

namespace ExploradorDeArchivos.Processing
{
    /// <summary>
    /// Procesa la lista principal List&lt;DataItem&gt;:
    /// filtrado, agrupacion con Dictionary, deteccion de duplicados
    /// y ordenamiento con algoritmos manuales (SIN LINQ .OrderBy).
    /// </summary>
    public static class DataProcessor
    {
        // ════════════════════════════════════════════════════════════════════
        //  FILTRADO
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Devuelve laptops cuyo precio sea >= minPrice.</summary>
        public static List<DataItem> FilterLaptopsByMinPrice(
            List<DataItem> items, double minPrice)
        {
            var result = new List<DataItem>();
            foreach (var item in items)
                if (item.Source == DataSource.CSV && item.Price >= minPrice)
                    result.Add(item);
            return result;
        }

        /// <summary>Devuelve registros TXT cuya temperatura sea &gt; maxTemp.</summary>
        public static List<DataItem> FilterLogsByTemperature(
            List<DataItem> items, double minTemp)
        {
            var result = new List<DataItem>();
            foreach (var item in items)
                if (item.Source == DataSource.TXT && item.Temperatura > minTemp)
                    result.Add(item);
            return result;
        }

        // ════════════════════════════════════════════════════════════════════
        //  AGRUPACION CON DICTIONARY
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Precio promedio de laptops agrupado por marca (Company).
        /// Usa Dictionary para evitar recorrer la lista mas de una vez.
        /// </summary>
        public static Dictionary<string, double> GetAvgPriceByBrand(
            List<DataItem> items)
        {
            var sumMap = new Dictionary<string, double>();
            var countMap = new Dictionary<string, int>();

            foreach (var item in items)
            {
                if (item.Source != DataSource.CSV) continue;

                if (!sumMap.ContainsKey(item.Company))
                {
                    sumMap[item.Company] = 0;
                    countMap[item.Company] = 0;
                }
                sumMap[item.Company] += item.Price;
                countMap[item.Company] += 1;
            }

            var result = new Dictionary<string, double>();
            foreach (var key in sumMap.Keys)
                result[key] = sumMap[key] / countMap[key];

            return result;
        }

        /// <summary>Total de ventas de videojuegos por genero.</summary>
        public static Dictionary<string, double> GetSalesByGenre(
            List<DataItem> items)
        {
            var result = new Dictionary<string, double>();
            foreach (var item in items)
            {
                if (item.Source != DataSource.JSON) continue;
                if (!result.ContainsKey(item.Genre))
                    result[item.Genre] = 0;
                result[item.Genre] += item.Sales;
            }
            return result;
        }

        /// <summary>Stock total de inventario por tipo de componente.</summary>
        public static Dictionary<string, int> GetStockByType(
            List<DataItem> items)
        {
            var result = new Dictionary<string, int>();
            foreach (var item in items)
            {
                if (item.Source != DataSource.XML) continue;
                if (!result.ContainsKey(item.Tipo))
                    result[item.Tipo] = 0;
                result[item.Tipo] += item.Stock;
            }
            return result;
        }

        /// <summary>
        /// Indice Dictionary&lt;int, DataItem&gt; para busqueda O(1) por ID,
        /// evitando recorrer la lista principal multiples veces.
        /// </summary>
        public static Dictionary<int, DataItem> BuildIdIndex(
            List<DataItem> items)
        {
            var index = new Dictionary<int, DataItem>(items.Count);
            foreach (var item in items)
                index[item.Id] = item;
            return index;
        }

        /// <summary>Agrupa DataItems por fuente en un Dictionary.</summary>
        public static Dictionary<DataSource, List<DataItem>> GroupBySource(
            List<DataItem> items)
        {
            var dict = new Dictionary<DataSource, List<DataItem>>();
            foreach (var item in items)
            {
                if (!dict.ContainsKey(item.Source))
                    dict[item.Source] = new List<DataItem>();
                dict[item.Source].Add(item);
            }
            return dict;
        }

        // ════════════════════════════════════════════════════════════════════
        //  DETECCION DE DUPLICADOS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Detecta duplicados comparando la clave compuesta Source + Label.
        /// </summary>
        public static List<DataItem> DetectDuplicates(List<DataItem> items)
        {
            var seen = new HashSet<string>();
            var dupes = new List<DataItem>();

            foreach (var item in items)
            {
                string key = $"{item.Source}:{item.Label}";
                if (!seen.Add(key))
                    dupes.Add(item);
            }
            return dupes;
        }

        // ════════════════════════════════════════════════════════════════════
        //  ORDENAMIENTO MANUAL  —  SIN LINQ (.OrderBy PROHIBIDO)
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Insertion Sort sobre List&lt;DataItem&gt;.
        /// Ordena por el valor numerico principal segun la fuente.
        /// NO se utiliza LINQ.OrderBy en ningun momento.
        /// </summary>
        public static void InsertionSort(List<DataItem> items, bool ascending = true)
        {
            for (int i = 1; i < items.Count; i++)
            {
                var current = items[i];
                double currentVal = GetSortValue(current);
                int j = i - 1;

                while (j >= 0 && CompareValues(GetSortValue(items[j]), currentVal, ascending) > 0)
                {
                    items[j + 1] = items[j];
                    j--;
                }
                items[j + 1] = current;
            }
        }

        /// <summary>
        /// Bubble Sort (alternativa). Incluye la optimizacion de corte
        /// anticipado cuando no hay intercambios en una pasada.
        /// NO se utiliza LINQ.OrderBy en ningun momento.
        /// </summary>
        public static void BubbleSort(List<DataItem> items, bool ascending = true)
        {
            int n = items.Count;
            for (int i = 0; i < n - 1; i++)
            {
                bool swapped = false;
                for (int j = 0; j < n - i - 1; j++)
                {
                    if (CompareValues(GetSortValue(items[j]),
                                      GetSortValue(items[j + 1]),
                                      ascending) > 0)
                    {
                        (items[j], items[j + 1]) = (items[j + 1], items[j]);
                        swapped = true;
                    }
                }
                if (!swapped) break;
            }
        }

        /// <summary>
        /// Insertion Sort con selector de clave personalizado.
        /// Permite ordenar por cualquier campo numerico sin usar LINQ.
        /// </summary>
        public static void InsertionSortBy(
            List<DataItem> items,
            Func<DataItem, double> keySelector,
            bool ascending = true)
        {
            for (int i = 1; i < items.Count; i++)
            {
                var current = items[i];
                double curVal = keySelector(current);
                int j = i - 1;

                while (j >= 0 && CompareValues(keySelector(items[j]), curVal, ascending) > 0)
                {
                    items[j + 1] = items[j];
                    j--;
                }
                items[j + 1] = current;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  ANALISIS UNIVERSAL PARA GRAFICAS (campos conocidos + ExtraFields)
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Analiza TODOS los items y encuentra automaticamente el mejor par
        /// (campo categorico, campo numerico) para generar una grafica.
        /// Busca primero en campos conocidos de DataItem, luego en ExtraFields.
        /// Retorna datos agrupados: categoria → promedio o suma segun el campo.
        /// </summary>
        public static Dictionary<string, double> AutoDetectChartData(
            List<DataItem> items, out string categoryLabel, out string valueLabel)
        {
            categoryLabel = "";
            valueLabel = "";
            var result = new Dictionary<string, double>();

            // ── 1. Detectar campos conocidos con datos reales ──────────────
            bool hasCompany = false, hasGenre = false, hasTipo = false;
            bool hasRegion = false, hasTypeName = false, hasTitle = false;
            bool hasPrice = false, hasSales = false, hasStock = false;
            bool hasTemp = false, hasFPS = false;

            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Company)) hasCompany = true;
                if (!string.IsNullOrEmpty(item.Genre)) hasGenre = true;
                if (!string.IsNullOrEmpty(item.Tipo)) hasTipo = true;
                if (!string.IsNullOrEmpty(item.Region)) hasRegion = true;
                if (!string.IsNullOrEmpty(item.TypeName)) hasTypeName = true;
                if (!string.IsNullOrEmpty(item.Title)) hasTitle = true;
                if (item.Price > 0) hasPrice = true;
                if (item.Sales > 0) hasSales = true;
                if (item.Stock > 0) hasStock = true;
                if (item.Temperatura > 0) hasTemp = true;
                if (item.FPS > 0) hasFPS = true;
            }

            // ── 2. Probar combinaciones de (string, double) conocidas ──────
            // Formato: (tieneCategoria, tieneValor, nombreCat, nombreVal,
            //           getCategoria, getValor)
            if (hasCompany && hasPrice)
            {
                categoryLabel = "Company";
                valueLabel = "Price";
                return GroupKnownFields(items,
                    i => i.Company, i => i.Price,
                    i => !string.IsNullOrEmpty(i.Company) && i.Price > 0,
                    useAverage: true);
            }
            if (hasGenre && hasSales)
            {
                categoryLabel = "Genre";
                valueLabel = "Sales";
                return GroupKnownFields(items,
                    i => i.Genre, i => i.Sales,
                    i => !string.IsNullOrEmpty(i.Genre) && i.Sales > 0,
                    useAverage: false);
            }
            if (hasTipo && hasStock)
            {
                categoryLabel = "Tipo";
                valueLabel = "Stock";
                return GroupKnownFields(items,
                    i => i.Tipo, i => (double)i.Stock,
                    i => !string.IsNullOrEmpty(i.Tipo) && i.Stock > 0,
                    useAverage: false);
            }
            if (hasRegion && hasPrice)
            {
                categoryLabel = "Region";
                valueLabel = "Price";
                return GroupKnownFields(items,
                    i => i.Region, i => i.Price,
                    i => !string.IsNullOrEmpty(i.Region) && i.Price > 0,
                    useAverage: true);
            }
            if (hasTitle && hasSales)
            {
                categoryLabel = "Title";
                valueLabel = "Sales";
                return GroupKnownFields(items,
                    i => i.Title, i => i.Sales,
                    i => !string.IsNullOrEmpty(i.Title) && i.Sales > 0,
                    useAverage: false);
            }
            if (hasTypeName && hasPrice)
            {
                categoryLabel = "TypeName";
                valueLabel = "Price";
                return GroupKnownFields(items,
                    i => i.TypeName, i => i.Price,
                    i => !string.IsNullOrEmpty(i.TypeName) && i.Price > 0,
                    useAverage: true);
            }
            if (hasCompany && hasTemp)
            {
                categoryLabel = "Company";
                valueLabel = "Temperatura";
                return GroupKnownFields(items,
                    i => i.Company, i => i.Temperatura,
                    i => !string.IsNullOrEmpty(i.Company) && i.Temperatura > 0,
                    useAverage: true);
            }

            // ── 3. Buscar en ExtraFields ───────────────────────────────────
            var keySamples = new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
                foreach (var kv in item.ExtraFields)
                {
                    if (!keySamples.ContainsKey(kv.Key))
                        keySamples[kv.Key] = new List<string>();
                    if (keySamples[kv.Key].Count < 200)
                        keySamples[kv.Key].Add(kv.Value);
                }

            if (keySamples.Count < 2) return result;

            string? numericField = null;
            string? categoryField = null;

            foreach (var kv in keySamples)
            {
                int numCount = 0;
                foreach (var val in kv.Value)
                    if (double.TryParse(val, NumberStyles.Any,
                            CultureInfo.InvariantCulture, out _))
                        numCount++;

                double ratio = (double)numCount / kv.Value.Count;

                if (ratio >= 0.8 && numericField == null)
                    numericField = kv.Key;
                else if (ratio < 0.5 && categoryField == null)
                    categoryField = kv.Key;
            }

            if (numericField == null || categoryField == null) return result;

            categoryLabel = categoryField;
            valueLabel = numericField;

            foreach (var item in items)
            {
                if (!item.ExtraFields.TryGetValue(categoryField, out var cat)) continue;
                if (!item.ExtraFields.TryGetValue(numericField, out var numStr)) continue;
                if (!double.TryParse(numStr, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double num)) continue;

                if (string.IsNullOrWhiteSpace(cat)) continue;
                if (!result.ContainsKey(cat))
                    result[cat] = 0;
                result[cat] += num;
            }

            return result;
        }

        /// <summary>
        /// Extrae todas las series numericas disponibles para grafica de linea.
        /// Busca en campos conocidos y en ExtraFields.
        /// Cada serie es un par (nombre, lista de valores).
        /// </summary>
        public static Dictionary<string, List<double>> AutoDetectLineSeries(
            List<DataItem> items)
        {
            var series = new Dictionary<string, List<double>>(
                StringComparer.OrdinalIgnoreCase);

            // Intentar campos conocidos que tengan datos
            bool hasTemp = false, hasFPS = false, hasPrice = false, hasSales = false;
            foreach (var item in items)
            {
                if (item.Temperatura > 0) hasTemp = true;
                if (item.FPS > 0) hasFPS = true;
                if (item.Price > 0) hasPrice = true;
                if (item.Sales > 0) hasSales = true;
            }

            if (hasTemp)
            {
                series["Temperatura"] = new List<double>();
                foreach (var item in items)
                    if (item.Temperatura > 0)
                        series["Temperatura"].Add(item.Temperatura);
            }
            if (hasFPS)
            {
                series["FPS"] = new List<double>();
                foreach (var item in items)
                    if (item.FPS > 0)
                        series["FPS"].Add(item.FPS);
            }
            if (!hasTemp && !hasFPS && hasPrice)
            {
                series["Price"] = new List<double>();
                foreach (var item in items)
                    if (item.Price > 0)
                        series["Price"].Add(item.Price);
            }
            if (!hasTemp && !hasFPS && hasSales)
            {
                series["Sales"] = new List<double>();
                foreach (var item in items)
                    if (item.Sales > 0)
                        series["Sales"].Add(item.Sales);
            }

            // Si no hay campos conocidos, buscar en ExtraFields
            if (series.Count == 0)
            {
                foreach (var item in items)
                    foreach (var kv in item.ExtraFields)
                        if (double.TryParse(kv.Value, NumberStyles.Any,
                                CultureInfo.InvariantCulture, out double val))
                        {
                            if (!series.ContainsKey(kv.Key))
                                series[kv.Key] = new List<double>();
                            series[kv.Key].Add(val);
                        }
            }

            return series;
        }

        private static Dictionary<string, double> GroupKnownFields(
            List<DataItem> items,
            Func<DataItem, string> getCategory,
            Func<DataItem, double> getValue,
            Func<DataItem, bool> filter,
            bool useAverage = false)
        {
            var result = new Dictionary<string, double>();
            var counts = new Dictionary<string, int>();

            foreach (var item in items)
            {
                if (!filter(item)) continue;
                string cat = getCategory(item);
                if (string.IsNullOrEmpty(cat)) continue;

                if (!result.ContainsKey(cat))
                {
                    result[cat] = 0;
                    counts[cat] = 0;
                }
                result[cat] += getValue(item);
                counts[cat]++;
            }

            if (useAverage)
            {
                foreach (var key in counts.Keys)
                    if (counts[key] > 0)
                        result[key] = result[key] / counts[key];
            }

            return result;
        }

        /// <summary>
        /// Devuelve solo las top N categorias por valor (desc) usando Insertion Sort.
        /// Si hay mas de maxCategories, agrupa el resto en "Otros".
        /// </summary>
        public static Dictionary<string, double> LimitTopN(
            Dictionary<string, double> data, int maxCategories)
        {
            if (data.Count <= maxCategories) return data;

            // Volcar a lista para ordenar manualmente (sin LINQ)
            var list = new List<KeyValuePair<string, double>>();
            foreach (var kv in data)
                list.Add(kv);

            // Insertion Sort descendente por valor
            for (int i = 1; i < list.Count; i++)
            {
                var current = list[i];
                int j = i - 1;
                while (j >= 0 && list[j].Value < current.Value)
                {
                    list[j + 1] = list[j];
                    j--;
                }
                list[j + 1] = current;
            }

            var result = new Dictionary<string, double>();
            double othersSum = 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (i < maxCategories)
                    result[list[i].Key] = list[i].Value;
                else
                    othersSum += list[i].Value;
            }

            if (othersSum > 0)
                result["Otros"] = othersSum;

            return result;
        }

        /// <summary>
        /// Reduce una serie numerica a maxPoints muestreando uniformemente.
        /// </summary>
        public static List<double> SampleSeries(List<double> values, int maxPoints)
        {
            if (values.Count <= maxPoints) return values;

            var sampled = new List<double>();
            double step = (double)(values.Count - 1) / (maxPoints - 1);

            for (int i = 0; i < maxPoints; i++)
            {
                int idx = (int)Math.Round(i * step);
                if (idx >= values.Count) idx = values.Count - 1;
                sampled.Add(values[idx]);
            }

            return sampled;
        }

        // ════════════════════════════════════════════════════════════════════
        //  ACCESO DINAMICO A CAMPOS (conocidos + ExtraFields)
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene el valor de texto de cualquier campo de un DataItem.
        /// Busca en propiedades conocidas y luego en ExtraFields.
        /// </summary>
        public static string GetStringValue(DataItem item, string fieldName)
        {
            switch (fieldName.ToLowerInvariant())
            {
                case "company": case "marca": return item.Company;
                case "typename": return item.TypeName;
                case "cpu": return item.Cpu;
                case "title": case "titulo": return item.Title;
                case "genre": case "genero": return item.Genre;
                case "platform": case "plataforma": return item.Platform;
                case "tipo": case "type": return item.Tipo;
                case "modelo": case "model": return item.Modelo;
                case "username": case "nombre": return item.UserName;
                case "email": case "correo": return item.Email;
                case "region": case "zona": return item.Region;
                case "source": case "fuente": return item.Source.ToString();
            }

            if (item.ExtraFields.TryGetValue(fieldName, out var extra))
                return extra;

            return string.Empty;
        }

        /// <summary>
        /// Obtiene el valor numerico de cualquier campo de un DataItem.
        /// Busca en propiedades conocidas y luego en ExtraFields.
        /// </summary>
        public static double GetNumericValue(DataItem item, string fieldName)
        {
            switch (fieldName.ToLowerInvariant())
            {
                case "id": return item.Id;
                case "price": case "precio": return item.Price;
                case "ram": return item.Ram;
                case "sales": case "ventas": return item.Sales;
                case "stock": case "cantidad": return item.Stock;
                case "minuto": case "minute": return item.Minuto;
                case "usocpu": return item.UsoCPU;
                case "temperatura": case "temperature": return item.Temperatura;
                case "fps": return item.FPS;
            }

            if (item.ExtraFields.TryGetValue(fieldName, out var extra))
                if (double.TryParse(extra, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double val))
                    return val;

            return 0;
        }

        /// <summary>
        /// Analiza los items y detecta que campos de texto y numericos
        /// contienen datos reales. Incluye propiedades conocidas y ExtraFields.
        /// </summary>
        public static (List<string> StringFields, List<string> NumericFields)
            DiscoverFields(List<DataItem> items)
        {
            var stringFields = new List<string>();
            var numericFields = new List<string>();
            var strSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var numSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Company) && strSet.Add("Company")) stringFields.Add("Company");
                if (!string.IsNullOrEmpty(item.TypeName) && strSet.Add("TypeName")) stringFields.Add("TypeName");
                if (!string.IsNullOrEmpty(item.Cpu) && strSet.Add("Cpu")) stringFields.Add("Cpu");
                if (!string.IsNullOrEmpty(item.Title) && strSet.Add("Title")) stringFields.Add("Title");
                if (!string.IsNullOrEmpty(item.Genre) && strSet.Add("Genre")) stringFields.Add("Genre");
                if (!string.IsNullOrEmpty(item.Platform) && strSet.Add("Platform")) stringFields.Add("Platform");
                if (!string.IsNullOrEmpty(item.Tipo) && strSet.Add("Tipo")) stringFields.Add("Tipo");
                if (!string.IsNullOrEmpty(item.Modelo) && strSet.Add("Modelo")) stringFields.Add("Modelo");
                if (!string.IsNullOrEmpty(item.UserName) && strSet.Add("UserName")) stringFields.Add("UserName");
                if (!string.IsNullOrEmpty(item.Email) && strSet.Add("Email")) stringFields.Add("Email");
                if (!string.IsNullOrEmpty(item.Region) && strSet.Add("Region")) stringFields.Add("Region");

                if (item.Price > 0 && numSet.Add("Price")) numericFields.Add("Price");
                if (item.Ram > 0 && numSet.Add("Ram")) numericFields.Add("Ram");
                if (item.Sales > 0 && numSet.Add("Sales")) numericFields.Add("Sales");
                if (item.Stock > 0 && numSet.Add("Stock")) numericFields.Add("Stock");
                if (item.Minuto > 0 && numSet.Add("Minuto")) numericFields.Add("Minuto");
                if (item.UsoCPU > 0 && numSet.Add("UsoCPU")) numericFields.Add("UsoCPU");
                if (item.Temperatura > 0 && numSet.Add("Temperatura")) numericFields.Add("Temperatura");
                if (item.FPS > 0 && numSet.Add("FPS")) numericFields.Add("FPS");

                foreach (var kv in item.ExtraFields)
                {
                    if (string.IsNullOrWhiteSpace(kv.Value)) continue;
                    if (double.TryParse(kv.Value, NumberStyles.Any,
                            CultureInfo.InvariantCulture, out _))
                    {
                        if (numSet.Add(kv.Key)) numericFields.Add(kv.Key);
                    }
                    else
                    {
                        if (strSet.Add(kv.Key)) stringFields.Add(kv.Key);
                    }
                }
            }

            return (stringFields, numericFields);
        }

        /// <summary>
        /// Encuentra los mejores pares (campo_texto, campo_numerico) donde
        /// los items realmente tienen ambos valores. Devuelve hasta 4 pares
        /// para alimentar las 4 graficas base.
        /// </summary>
        public static List<(string Category, string Value)> DiscoverChartPairs(
            List<DataItem> items)
        {
            var (stringFields, numericFields) = DiscoverFields(items);
            var pairs = new List<(string, string)>();

            foreach (var cat in stringFields)
            {
                string? bestNum = null;
                int bestCount = 0;

                foreach (var num in numericFields)
                {
                    int count = 0;
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(GetStringValue(item, cat))
                            && GetNumericValue(item, num) > 0)
                            count++;
                        if (count >= 10) break;
                    }
                    if (count > bestCount)
                    {
                        bestCount = count;
                        bestNum = num;
                    }
                }

                if (bestNum != null && bestCount >= 2)
                    pairs.Add((cat, bestNum));

                if (pairs.Count >= 4) break;
            }

            return pairs;
        }

        // ════════════════════════════════════════════════════════════════════
        //  DETECCION INTELIGENTE DE PARES PARA LAS 4 GRAFICAS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Resultado de un par categorico-numerico con metricas de calidad.
        /// </summary>
        public sealed class ChartPairInfo
        {
            public string CategoryField { get; init; } = "";
            public string ValueField { get; init; } = "";
            public Dictionary<string, double> GroupedData { get; init; } = new();
            /// <summary>Numero de categorias distintas (grupos).</summary>
            public int UniqueCats => GroupedData.Count;
            /// <summary>"Promedio" si el campo se promedia, "Total" si se suma.</summary>
            public string AggregationLabel { get; init; } = "Total";
        }

        /// <summary>
        /// Detecta hasta 4 pares (campo_categorico, campo_numerico) de calidad,
        /// filtrando columnas tipo ID (todos valores unicos), columnas con un
        /// solo valor unico, y columnas numericas con mayoria cero.
        /// Los pares se ordenan para que el grafico correcto use el par idoneo:
        ///   [0] = barras (hasta 15 cat)
        ///   [1] = pastel (preferentemente 3-10 cat)
        ///   [2] = anillo (igual que pastel, diferente campo si es posible)
        ///   [3] = lineas (serie temporal o ranking de valores)
        /// </summary>
        public static List<ChartPairInfo> AutoDetectSmartPairs(
            List<DataItem> items, int maxPairs = 4)
        {
            if (items.Count == 0)
                return new List<ChartPairInfo>();

            var (rawStringFields, rawNumericFields) = DiscoverFields(items);

            // ── Filtrar campos categoricos de baja calidad ─────────────────
            var catFields = new List<(string field, int uniqueCount)>();
            foreach (var f in rawStringFields)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int total = 0;
                foreach (var item in items)
                {
                    string v = GetStringValue(item, f);
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        seen.Add(v);
                        total++;
                    }
                }
                if (total < 2) continue;                   // campo casi vacio
                if (seen.Count == 1) continue;             // todos iguales
                if (seen.Count > total * 0.9) continue;    // casi todo unico (ID)
                catFields.Add((f, seen.Count));
            }

            // ── Filtrar campos numericos de baja calidad ───────────────────
            var numFields = new List<string>();
            foreach (var f in rawNumericFields)
            {
                int zeros = 0, total = 0;
                foreach (var item in items)
                {
                    double v = GetNumericValue(item, f);
                    total++;
                    if (v == 0) zeros++;
                }
                if (total == 0) continue;
                if ((double)zeros / total > 0.85) continue; // mayoria cero
                numFields.Add(f);
            }

            if (catFields.Count == 0 || numFields.Count == 0)
                return new List<ChartPairInfo>();

            // ── Generar todos los pares validos ────────────────────────────
            var candidates = new List<(int uniqueCats, string cat, string num,
                                       Dictionary<string, double> data)>();

            foreach (var (catField, _) in catFields)
            {
                foreach (var numField in numFields)
                {
                    var grouped = GroupByFields(items, catField, numField);
                    if (grouped.Count < 2) continue;
                    candidates.Add((grouped.Count, catField, numField, grouped));
                }
            }

            if (candidates.Count == 0)
                return new List<ChartPairInfo>();

            // ── Ordenar candidatos por numero de categorias (asc) ──────────
            for (int i = 1; i < candidates.Count; i++)
            {
                var cur = candidates[i];
                int j = i - 1;
                while (j >= 0 && candidates[j].uniqueCats > cur.uniqueCats)
                {
                    candidates[j + 1] = candidates[j];
                    j--;
                }
                candidates[j + 1] = cur;
            }

            // ── Seleccionar pares diversos (evitar repetir mismo par) ──────
            var result = new List<ChartPairInfo>();
            var usedPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Pastel/Anillo: preferir pares con 3-10 categorias
            // Barras: pares con hasta 15 categorias
            // Lineas: par con mas categorias (ranking) o distinto campo numerico

            // Ordenar: primero los que caben bien en pastel (<=10), luego el resto
            var pieFirst = new List<(int uniqueCats, string cat, string num,
                                     Dictionary<string, double> data)>();
            var barFirst = new List<(int uniqueCats, string cat, string num,
                                     Dictionary<string, double> data)>();

            foreach (var c in candidates)
            {
                if (c.uniqueCats >= 2 && c.uniqueCats <= 10)
                    pieFirst.Add(c);
                else
                    barFirst.Add(c);
            }

            // [0] Barras — mayor riqueza (mas categorias <= 15)
            var barCandidates = new List<(int, string, string,
                                          Dictionary<string, double>)>(barFirst);
            barCandidates.AddRange(pieFirst);
            // ordenar desc para barras
            for (int i = 1; i < barCandidates.Count; i++)
            {
                var cur = barCandidates[i];
                int j = i - 1;
                while (j >= 0 && barCandidates[j].Item1 < cur.Item1)
                {
                    barCandidates[j + 1] = barCandidates[j];
                    j--;
                }
                barCandidates[j + 1] = cur;
            }

            foreach (var c in barCandidates)
            {
                string key = $"{c.Item2}|{c.Item3}";
                if (!usedPairs.Add(key)) continue;
                result.Add(new ChartPairInfo
                {
                    CategoryField = c.Item2,
                    ValueField = c.Item3,
                    GroupedData = c.Item4,
                    AggregationLabel = IsAveragingField(c.Item3) ? "Promedio" : "Total"
                });
                break;
            }

            // [1] Pastel — pocas categorias
            foreach (var c in pieFirst)
            {
                string key = $"{c.cat}|{c.num}";
                if (!usedPairs.Add(key)) continue;
                result.Add(new ChartPairInfo
                {
                    CategoryField = c.cat,
                    ValueField = c.num,
                    GroupedData = c.data,
                    AggregationLabel = IsAveragingField(c.num) ? "Promedio" : "Total"
                });
                break;
            }
            // Si no habia pie con <=10, tomar cualquiera diferente al de barras
            if (result.Count < 2)
            {
                foreach (var c in candidates)
                {
                    string key = $"{c.cat}|{c.num}";
                    if (!usedPairs.Add(key)) continue;
                    result.Add(new ChartPairInfo
                    {
                        CategoryField = c.cat,
                        ValueField = c.num,
                        GroupedData = c.data,
                        AggregationLabel = IsAveragingField(c.num) ? "Promedio" : "Total"
                    });
                    break;
                }
            }

            // [2] Anillo — diferente al pastel si es posible
            foreach (var c in pieFirst)
            {
                string key = $"{c.cat}|{c.num}";
                if (!usedPairs.Add(key)) continue;
                result.Add(new ChartPairInfo
                {
                    CategoryField = c.cat,
                    ValueField = c.num,
                    GroupedData = c.data,
                    AggregationLabel = IsAveragingField(c.num) ? "Promedio" : "Total"
                });
                break;
            }
            // Si no hay otro, reusar el de pastel
            if (result.Count < 3 && result.Count >= 2)
                result.Add(result[result.Count - 1]);

            // [3] Lineas — diferente campo numerico si es posible
            foreach (var c in candidates)
            {
                string key = $"{c.cat}|{c.num}";
                if (!usedPairs.Add(key)) continue;
                result.Add(new ChartPairInfo
                {
                    CategoryField = c.cat,
                    ValueField = c.num,
                    GroupedData = c.data,
                    AggregationLabel = IsAveragingField(c.num) ? "Promedio" : "Total"
                });
                break;
            }
            // Si solo hay un par total, reusar para lineas
            if (result.Count < 4 && result.Count > 0)
                result.Add(result[0]);

            return result;
        }

        /// <summary>
        /// Determina si un campo numerico debe promediar (true) o sumar (false).
        /// Solo suman los campos acumulables bien conocidos (ventas, stock, conteos).
        /// Cualquier otro campo (precio, edad, temperatura, salario, puntuacion, etc.)
        /// se promedia, ya que sumarlos por categoria produce cifras sin sentido.
        /// </summary>
        public static bool IsAveragingField(string fieldName)
        {
            string f = fieldName.ToLowerInvariant();

            // ── Campos acumulables (suma tiene sentido) ────────────────────
            if (f == "sales" || f == "ventas" || f == "stock"
                || f == "cantidad" || f == "count" || f == "conteo"
                || f == "total" || f == "units" || f == "unidades"
                || f == "orders" || f == "pedidos" || f == "downloads"
                || f == "descargas")
                return false;

            // Nombres compuestos acumulables
            if (f.Contains("ventas") || f.Contains("sales")
                || f.Contains("stock") || f.Contains("units")
                || f.Contains("count") || f.Contains("total_")
                || f.StartsWith("total") && f.Length > 5)
                return false;

            // ── Todo lo demas se promedia ──────────────────────────────────
            // Precio, edad, salario, temperatura, fps, ram, puntuacion,
            // calificacion, peso, altura, nota, score, rate, ratio, etc.
            return true;
        }

        private static Dictionary<string, double> GroupByFields(
            List<DataItem> items, string catField, string numField)
        {
            bool useAvg = IsAveragingField(numField);

            var sumMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var countMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                string cat = GetStringValue(item, catField);
                if (string.IsNullOrWhiteSpace(cat)) continue;
                double val = GetNumericValue(item, numField);
                if (val == 0) continue;

                if (!sumMap.ContainsKey(cat))
                {
                    sumMap[cat] = 0;
                    countMap[cat] = 0;
                }
                sumMap[cat] += val;
                countMap[cat]++;
            }

            var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in sumMap.Keys)
            {
                if (countMap[key] == 0) continue;
                result[key] = useAvg
                    ? sumMap[key] / countMap[key]  // promedio
                    : sumMap[key];                  // suma
            }

            return result;
        }

        // ════════════════════════════════════════════════════════════════════
        //  PROCESAMIENTO DINAMICO (filtrado, agrupacion, ordenamiento)
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Filtra items donde el campo numerico indicado sea >= minValue.
        /// Funciona con cualquier campo conocido o ExtraField.
        /// </summary>
        public static List<DataItem> DynamicFilter(
            List<DataItem> items, string numericField, double minValue)
        {
            var result = new List<DataItem>();
            foreach (var item in items)
                if (GetNumericValue(item, numericField) >= minValue)
                    result.Add(item);
            return result;
        }

        /// <summary>
        /// Agrupa por campo de texto y suma un campo numerico.
        /// Usa Dictionary para eficiencia O(n).
        /// </summary>
        public static Dictionary<string, double> DynamicGroupSum(
            List<DataItem> items, string categoryField, string valueField)
        {
            var result = new Dictionary<string, double>();
            foreach (var item in items)
            {
                string cat = GetStringValue(item, categoryField);
                if (string.IsNullOrWhiteSpace(cat)) continue;
                double val = GetNumericValue(item, valueField);

                if (!result.ContainsKey(cat))
                    result[cat] = 0;
                result[cat] += val;
            }
            return result;
        }

        /// <summary>
        /// Agrupa por campo de texto y promedia un campo numerico.
        /// Usa dos Dictionary (suma y conteo) para eficiencia O(n).
        /// </summary>
        public static Dictionary<string, double> DynamicGroupAvg(
            List<DataItem> items, string categoryField, string valueField)
        {
            var sumMap = new Dictionary<string, double>();
            var countMap = new Dictionary<string, int>();

            foreach (var item in items)
            {
                string cat = GetStringValue(item, categoryField);
                if (string.IsNullOrWhiteSpace(cat)) continue;
                double val = GetNumericValue(item, valueField);

                if (!sumMap.ContainsKey(cat))
                {
                    sumMap[cat] = 0;
                    countMap[cat] = 0;
                }
                sumMap[cat] += val;
                countMap[cat] += 1;
            }

            var result = new Dictionary<string, double>();
            foreach (var key in sumMap.Keys)
                result[key] = sumMap[key] / countMap[key];

            return result;
        }

        /// <summary>
        /// Cuenta registros por categoria (campo de texto).
        /// Usa Dictionary para conteo O(n).
        /// </summary>
        public static Dictionary<string, int> DynamicGroupCount(
            List<DataItem> items, string categoryField)
        {
            var result = new Dictionary<string, int>();
            foreach (var item in items)
            {
                string cat = GetStringValue(item, categoryField);
                if (string.IsNullOrWhiteSpace(cat)) continue;

                if (!result.ContainsKey(cat))
                    result[cat] = 0;
                result[cat]++;
            }
            return result;
        }

        /// <summary>
        /// Insertion Sort dinamico por cualquier campo numerico.
        /// NO usa LINQ.OrderBy.
        /// </summary>
        public static void DynamicSort(
            List<DataItem> items, string fieldName, bool ascending = true)
        {
            for (int i = 1; i < items.Count; i++)
            {
                var current = items[i];
                double currentVal = GetNumericValue(current, fieldName);
                int j = i - 1;

                while (j >= 0 && CompareValues(
                    GetNumericValue(items[j], fieldName), currentVal, ascending) > 0)
                {
                    items[j + 1] = items[j];
                    j--;
                }
                items[j + 1] = current;
            }
        }

        /// <summary>
        /// Bubble Sort dinamico por cualquier campo numerico.
        /// Incluye optimizacion de corte anticipado.
        /// NO usa LINQ.OrderBy.
        /// </summary>
        public static void DynamicBubbleSort(
            List<DataItem> items, string fieldName, bool ascending = true)
        {
            int n = items.Count;
            for (int i = 0; i < n - 1; i++)
            {
                bool swapped = false;
                for (int j = 0; j < n - i - 1; j++)
                {
                    if (CompareValues(
                        GetNumericValue(items[j], fieldName),
                        GetNumericValue(items[j + 1], fieldName),
                        ascending) > 0)
                    {
                        (items[j], items[j + 1]) = (items[j + 1], items[j]);
                        swapped = true;
                    }
                }
                if (!swapped) break;
            }
        }

        /// <summary>
        /// Calcula un umbral basado en percentil usando ordenamiento manual.
        /// percentile = 0.75 devuelve el valor al 75% de los datos ordenados.
        /// </summary>
        public static double ComputeThreshold(
            List<DataItem> items, string numericField, double percentile)
        {
            var values = new List<double>();
            foreach (var item in items)
            {
                double v = GetNumericValue(item, numericField);
                if (v > 0) values.Add(v);
            }

            if (values.Count == 0) return 0;

            // Insertion Sort manual sobre doubles
            for (int i = 1; i < values.Count; i++)
            {
                double current = values[i];
                int j = i - 1;
                while (j >= 0 && values[j] > current)
                {
                    values[j + 1] = values[j];
                    j--;
                }
                values[j + 1] = current;
            }

            int idx = (int)(values.Count * percentile);
            if (idx >= values.Count) idx = values.Count - 1;
            return values[idx];
        }

        // ── Helpers privados ───────────────────────────────────────────────

        private static int CompareValues(double a, double b, bool ascending)
        {
            int cmp = a.CompareTo(b);
            return ascending ? cmp : -cmp;
        }

        private static double GetSortValue(DataItem item) => item.Source switch
        {
            DataSource.CSV => item.Price,
            DataSource.JSON => item.Sales,
            DataSource.XML => item.Stock,
            DataSource.TXT => item.Temperatura,
            DataSource.DB => item.Id,
            _ => 0
        };
    }
}
