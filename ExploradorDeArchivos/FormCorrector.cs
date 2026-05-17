using ExploradorDeArchivos.Limpieza_de_datos_3._3.Core;
using System.Data;
using System.Windows.Forms;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ExploradorDeArchivos
{
    public partial class FormCorrector : Form
    {
        private const string AllItems = "(Todos)";

        private readonly DataPipeline _pipeline = new();
        private readonly ColumnTypeInferrer _inferrer = new();

        // Datos válidos en memoria (fuente de verdad para filtros y correcciones).
        private IReadOnlyList<IDictionary<string, object>> _validRows = [];

        // Resultado de la última inferencia de tipos: necesario para resaltar celdas.
        private ColumnTypeInferrer.InferenceResult? _inference;

        // Índice rápido: (rowIndex, columnName) → CellError para el CellFormatting.
        private HashSet<(int, string)> _cellErrorIndex = [];

        public FormCorrector()
        {
            InitializeComponent();
            EnableDoubleBuffer(dgvDatos);
        }

        // Activa el doble buffer del DataGridView (propiedad protegida, requiere reflexión).
        // Elimina el artefacto de "ghosting" al hacer scroll horizontal con muchas columnas.
        private static void EnableDoubleBuffer(DataGridView grid)
        {
            typeof(DataGridView)
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)!
                .SetValue(grid, true);
        }

        // ── Selección de archivo ─────────────────────────────────────────────────
        private void BtnSeleccionar_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                txtArchivo.Text = openFileDialog.FileName;
        }

        // ── Procesamiento principal (async para no congelar la UI) ───────────────
        private async void BtnProcesar_Click(object sender, EventArgs e)
        {
            var filePath = txtArchivo.Text.Trim();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                MessageBox.Show("Selecciona un archivo primero.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetUiBusy(true);

            try
            {
                var orderBy = txtOrdenarPor.Text.Trim();
                var result = await Task.Run(() => RunPipeline(filePath, orderBy));

                _validRows = result.ValidRows;

                // Inferencia de tipos y detección de celdas erróneas.
                _inference = await Task.Run(() => _inferrer.Infer(_validRows));
                _cellErrorIndex = BuildErrorIndex(_inference.CellErrors);

                BuildFilterControls(_validRows);
                RefreshGrid(_validRows);

                // Resumen de errores de tipo.
                int numInText = _inference.CellErrors.Count(e => e.ErrorKind == CellErrorKind.UnexpectedNumeric);
                int textInType = _inference.CellErrors.Count - numInText;
                lblTiposError.Text = _inference.CellErrors.Count > 0
                    ? $"⚠ {textInType} texto en col. numérica/fecha (🟠)  |  {numInText} número en col. de texto (🔴)"
                    : string.Empty;

                btnGuardarCorrecciones.Enabled = true;
                btnExportar.Enabled = true;
                btnLimpiarDatos.Enabled = true;
                // Log de validación.
                lstErrores.Items.Clear();
                if (result.ErrorLog.Count > 0)
                {
                    lstErrores.Items.AddRange(result.ErrorLog.ToArray<object>());
                    lblErrores.Text = $"Errores de validación  ({result.InvalidRows.Count} fila(s) rechazada(s))";
                }
                else
                {
                    lstErrores.Items.Add("✔ Sin errores de validación.");
                    lblErrores.Text = "Errores de validación";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar el archivo:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUiBusy(false);
            }
        }

        // ── Limpiar datos automáticamente según tipo de columna ─────────────────
        private void BtnLimpiarDatos_Click(object sender, EventArgs e)
        {
            if (_validRows.Count == 0 || _inference is null)
            {
                MessageBox.Show("Procesa un archivo primero.", "Limpiar datos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Mostrar el diálogo de revisión de tipos antes de limpiar
            using var reviewForm = new ColumnTypesForm(_inference.ColumnTypes);
            if (reviewForm.ShowDialog(this) != DialogResult.OK)
                return; // el usuario canceló

            // Usar los tipos confirmados/corregidos por el usuario
            var typesToUse = reviewForm.ConfirmedTypes!;

            var (cleaned, changeLog) = DataCleaner.Clean(_validRows, typesToUse);
            _validRows = cleaned;

            // Re-inferir tras la limpieza
            _inference = _inferrer.Infer(_validRows);
            _cellErrorIndex = BuildErrorIndex(_inference.CellErrors);

            RefreshGrid(_validRows);
            BuildFilterControls(_validRows);

            int numInText = _inference.CellErrors.Count(e => e.ErrorKind == CellErrorKind.UnexpectedNumeric);
            int textInType = _inference.CellErrors.Count - numInText;
            lblTiposError.Text = _inference.CellErrors.Count > 0
                ? $"⚠ {textInType} texto en col. numérica/fecha (🟠)  |  {numInText} número en col. de texto (🔴)"
                : "✔ Sin errores de tipo detectados.";

            lblDatos.Text = $"Datos válidos  ({_validRows.Count} fila(s))";

            // Mostrar el log de cambios
            lstErrores.Items.Clear();
            if (changeLog.Count > 0)
            {
                lstErrores.Items.AddRange(changeLog.ToArray<object>());
                lblErrores.Text = $"Log de limpieza  ({changeLog.Count} celda(s) modificada(s))";
                MessageBox.Show($"Limpieza completada: {changeLog.Count} celda(s) modificada(s).\n" +
                                "Revisa el log en la parte inferior.",
                    "Limpiar datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lstErrores.Items.Add("✔ No se encontraron valores que limpiar.");
                lblErrores.Text = "Log de limpieza";
                MessageBox.Show("No se encontraron valores que requieran limpieza.",
                    "Limpiar datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ── Guardar correcciones: sincroniza el DataGridView con _validRows ───────
        private void BtnGuardarCorrecciones_Click(object sender, EventArgs e)
        {
            if (dgvDatos.DataSource is not DataTable dt) return;

            // Reconstruir _validRows desde el DataTable editado por el usuario.
            var columns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var updated = new List<IDictionary<string, object>>(dt.Rows.Count);

            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var col in columns)
                    dict[col] = row[col]?.ToString() ?? string.Empty;
                updated.Add(dict);
            }

            _validRows = updated;

            // Re-inferir tipos sobre los datos corregidos.
            _inference = _inferrer.Infer(_validRows);
            _cellErrorIndex = BuildErrorIndex(_inference.CellErrors);

            // Refrescar la vista (fuerza repintado con CellFormatting).
            RefreshGrid(_validRows);

            int numInText2 = _inference.CellErrors.Count(e => e.ErrorKind == CellErrorKind.UnexpectedNumeric);
            int textInType2 = _inference.CellErrors.Count - numInText2;
            lblTiposError.Text = _inference.CellErrors.Count > 0
                ? $"⚠ {textInType2} texto en col. numérica/fecha (🟠)  |  {numInText2} número en col. de texto (🔴)"
                : "✔ Sin errores de tipo detectados.";

            lblDatos.Text = $"Datos válidos  ({_validRows.Count} fila(s))";

            MessageBox.Show("Correcciones guardadas correctamente.", "Guardar correcciones",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Exportar datos del grid ──────────────────────────────────────────────
        private void BtnExportar_Click(object sender, EventArgs e)
        {
            if (_validRows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Exportar",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Si el usuario editó el grid, sincronizar _validRows con los datos actuales
            if (dgvDatos.DataSource is DataTable dt)
            {
                var columns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                var synced = new List<IDictionary<string, object>>(dt.Rows.Count);
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var col in columns)
                        dict[col] = row[col]?.ToString() ?? string.Empty;
                    synced.Add(dict);
                }
                _validRows = synced;
            }

            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                DataExporter.Export(_validRows, saveFileDialog.FileName);
                MessageBox.Show($"Archivo exportado correctamente:\n{saveFileDialog.FileName}",
                    "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── CellFormatting: pinta en naranja las celdas con tipo incorrecto ───────
        private void DgvDatos_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvDatos.Columns[e.ColumnIndex] is not DataGridViewColumn col) return;

            if (_cellErrorIndex.Contains((e.RowIndex, col.Name)))
            {
                var errorInfo = _inference?.CellErrors
                    .FirstOrDefault(c => c.RowIndex == e.RowIndex && c.Column == col.Name);

                // Naranja: texto donde debería haber número o fecha.
                // Rojo claro: número donde debería haber texto.
                (e.CellStyle.BackColor, e.CellStyle.SelectionBackColor) =
                    errorInfo?.ErrorKind == CellErrorKind.UnexpectedNumeric
                        ? (Color.FromArgb(255, 160, 160), Color.IndianRed)
                        : (Color.FromArgb(255, 200, 100), Color.Orange);

                e.FormattingApplied = true;

                if (errorInfo is not null)
                    dgvDatos.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText =
                        errorInfo.Description;
            }
            else
            {
                e.FormattingApplied = false;
            }
        }

        // ── Construye el índice rápido de errores ─────────────────────────────────
        private static HashSet<(int, string)> BuildErrorIndex(IReadOnlyList<CellError> errors)
        {
            var index = new HashSet<(int, string)>(errors.Count);
            foreach (var err in errors)
                index.Add((err.RowIndex, err.Column));
            return index;
        }

        // ── Actualiza el DataGridView y reaplica los filtros activos ─────────────
        private void RefreshGrid(IReadOnlyList<IDictionary<string, object>> rows)
        {
            dgvDatos.SuspendLayout();

            // Desvincular completamente antes de reasignar evita columnas duplicadas
            // y el ghosting al hacer scroll.
            dgvDatos.DataSource = null;
            dgvDatos.Columns.Clear();
            dgvDatos.DataSource = ToDatatable(rows);

            // Ajustar ancho de columnas una sola vez tras cargar, sin dejar el
            // autosize activo (que provoca redibujos costosos en cada scroll).
            dgvDatos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvDatos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            dgvDatos.ResumeLayout();
            lblDatos.Text = $"Datos válidos  ({rows.Count} fila(s))";
        }

        // ── Construye un Label + ComboBox por cada columna del dataset ───────────
        private void BuildFilterControls(IReadOnlyList<IDictionary<string, object>> rows)
        {
            pnlFiltros.SuspendLayout();
            pnlFiltros.Controls.Clear();

            if (rows.Count == 0)
            {
                pnlFiltros.ResumeLayout();
                btnAplicarFiltros.Enabled = false;
                btnLimpiarFiltros.Enabled = false;
                return;
            }

            var columns = rows[0].Keys.ToList();

            foreach (var col in columns)
            {
                var uniqueValues = rows
                    .Select(r => r.TryGetValue(col, out var v) ? v?.ToString() ?? string.Empty : string.Empty)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v)
                    .ToList();

                var cell = new Panel { Width = 160, Height = 52, Margin = new Padding(4) };

                var lbl = new Label
                {
                    Text = col,
                    AutoSize = false,
                    Width = 156,
                    Height = 18,
                    Location = new Point(0, 0),
                    Font = new Font(Font.FontFamily, 8f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                };

                // Si el tipo de la columna es Numérico o Fecha, indicarlo en el label.
                if (_inference is not null &&
                    _inference.ColumnTypes.TryGetValue(col, out var colType) &&
                    colType != ColumnDataType.Text)
                {
                    lbl.Text = $"{col} [{colType}]";
                    lbl.ForeColor = Color.DarkSlateBlue;
                }

                var cmb = new System.Windows.Forms.ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Width = 156,
                    Location = new Point(0, 20),
                    Tag = col,
                };

                cmb.Items.Add(AllItems);
                cmb.Items.AddRange(uniqueValues.ToArray<object>());
                cmb.SelectedIndex = 0;

                cell.Controls.Add(lbl);
                cell.Controls.Add(cmb);
                pnlFiltros.Controls.Add(cell);
            }

            pnlFiltros.ResumeLayout();
            btnAplicarFiltros.Enabled = true;
            btnLimpiarFiltros.Enabled = true;
        }

        // ── Aplica todos los filtros seleccionados (AND) ─────────────────────────
        private void BtnAplicarFiltros_Click(object sender, EventArgs e)
        {
            var filtered = ApplyFilters();
            dgvDatos.SuspendLayout();
            dgvDatos.DataSource = null;
            dgvDatos.Columns.Clear();
            dgvDatos.DataSource = ToDatatable(filtered);
            dgvDatos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvDatos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvDatos.ResumeLayout();
            lblDatos.Text = $"Datos válidos  ({filtered.Count} fila(s) mostradas / {_validRows.Count} totales)";
        }

        // ── Restablece todos los ComboBox a "(Todos)" ────────────────────────────
        private void BtnLimpiarFiltros_Click(object sender, EventArgs e)
        {
            foreach (var cmb in GetFilterComboBoxes())
                cmb.SelectedIndex = 0;

            RefreshGrid(_validRows);
        }

        // ── Filtra _validRows aplicando AND entre todos los ComboBox activos ─────
        private IReadOnlyList<IDictionary<string, object>> ApplyFilters()
        {
            var activeFilters = GetFilterComboBoxes()
                .Where(c => c.SelectedItem is string s && s != AllItems)
                .Select(c => (Column: c.Tag!.ToString()!, Value: c.SelectedItem!.ToString()!))
                .ToList();

            if (activeFilters.Count == 0)
                return _validRows;

            return _validRows
                .Where(row => activeFilters.All(f =>
                    row.TryGetValue(f.Column, out var cell) &&
                    string.Equals(cell?.ToString(), f.Value, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private IEnumerable<System.Windows.Forms.ComboBox> GetFilterComboBoxes() =>
            pnlFiltros.Controls
                .OfType<Panel>()
                .SelectMany(p => p.Controls.OfType<System.Windows.Forms.ComboBox>());

        // ── Lógica de negocio ejecutada en hilo de fondo ─────────────────────────
        private PipelineResult RunPipeline(string filePath, string? orderBy)
        {
            IFileReader reader = CreateReader(filePath);
            var rows = reader.ReadFile(filePath);
            return _pipeline.Execute(rows, orderBy);
        }

        private static IFileReader CreateReader(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();

            // Para extensiones inequívocas confiamos directamente en ellas.
            if (ext == ".json") return new JsonFileReader();
            if (ext == ".xml") return new XmlFileReader();
            if (ext == ".csv") return new CsvFileReader(',');

            // Para extensiones ambiguas (.txt, .tsv, sin extensión, etc.)
            // usamos sniffing del contenido para elegir el lector correcto.
            var sniffed = DetectFormatByContent(path);
            if (sniffed != null) return sniffed;

            // Fallback final por extensión
            return ext switch
            {
                ".tsv" => new CsvFileReader('\t'),
                ".txt" => new CsvFileReader('\t'),
                _ => new CsvFileReader(',')
            };
        }

        /// <summary>
        /// Lee los primeros bytes del archivo para determinar el formato real.
        /// Solo se invoca para extensiones ambiguas (.txt, etc.).
        /// Devuelve null si no se puede determinar con certeza (CSV/TXT).
        /// </summary>
        private static IFileReader? DetectFormatByContent(string path)
        {
            try
            {
                Span<char> buffer = stackalloc char[4096];
                using var sr = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
                int read = sr.Read(buffer);

                for (int i = 0; i < read; i++)
                {
                    char c = buffer[i];
                    if (char.IsWhiteSpace(c)) continue;

                    if (c == '<') return new XmlFileReader();
                    if (c == '{' || c == '[') return new JsonFileReader();

                    // Cualquier otro carácter → CSV/TXT, dejar que el fallback decida
                    return null;
                }
            }
            catch
            {
                // Si falla la lectura anticipada caemos al comportamiento por extensión
            }
            return null;
        }

        private static char DetectDelimiter(string path) =>
            Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".tsv" => '\t',
                ".txt" => '\t',
                _ => ','
            };

        // ── Convierte la lista de diccionarios en DataTable para el DataGridView ─
        private static DataTable ToDatatable(IReadOnlyList<IDictionary<string, object>> rows)
        {
            var table = new DataTable();
            if (rows.Count == 0)
                return table;

            foreach (var key in rows[0].Keys)
                table.Columns.Add(key, typeof(string));

            foreach (var row in rows)
            {
                var dataRow = table.NewRow();
                foreach (var kvp in row)
                {
                    if (table.Columns.Contains(kvp.Key))
                        dataRow[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                }
                table.Rows.Add(dataRow);
            }

            return table;
        }

        // ── Habilita / deshabilita la UI durante el procesamiento ────────────────
        private void SetUiBusy(bool busy)
        {
            btnProcesar.Enabled = !busy;
            btnSeleccionar.Enabled = !busy;
            progressBar.Visible = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }
    }
}