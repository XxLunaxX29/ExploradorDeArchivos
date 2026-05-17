namespace ExploradorDeArchivos.Limpieza_de_datos_3._3.Core;
/// <summary>
/// Diálogo que muestra el tipo inferido para cada columna y permite
/// al usuario corregirlo antes de aplicar la limpieza automática.
/// </summary>
public sealed class ColumnTypesForm : Form
{
    // Tipos disponibles para el combo
    private static readonly string[] TypeLabels = ["Texto", "Numérico", "Fecha"];

    private readonly DataGridView _grid;
    private readonly Button _btnConfirmar;
    private readonly Button _btnCancelar;
    private readonly Label _lblInstruccion;

    /// <summary>
    /// Tipos confirmados por el usuario (columna → ColumnDataType).
    /// Solo se rellena si el usuario presionó Confirmar.
    /// </summary>
    public IReadOnlyDictionary<string, ColumnDataType>? ConfirmedTypes { get; private set; }

    public ColumnTypesForm(IReadOnlyDictionary<string, ColumnDataType> inferredTypes)
    {
        Text = "Revisar tipos de columna antes de limpiar";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(520, 480);
        Font = new Font("Segoe UI", 9f);

        _lblInstruccion = new Label
        {
            Text = "Revisa el tipo detectado para cada columna y corrígelo si es necesario.\n" +
                        "La limpieza automática usará estos tipos.",
            Location = new Point(12, 12),
            Size = new Size(478, 40),
            ForeColor = Color.DarkSlateGray
        };

        _grid = new DataGridView
        {
            Location = new Point(12, 58),
            Size = new Size(478, 340),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle
        };

        // Columna: nombre de columna (solo lectura)
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Columna",
            Name = "colNombre",
            ReadOnly = true,
            FillWeight = 55
        });

        // Columna: tipo detectado (ComboBox editable)
        var comboCol = new DataGridViewComboBoxColumn
        {
            HeaderText = "Tipo detectado",
            Name = "colTipo",
            DataSource = TypeLabels,
            FlatStyle = FlatStyle.Flat,
            FillWeight = 45
        };
        _grid.Columns.Add(comboCol);

        // Llenar filas
        foreach (var kvp in inferredTypes)
            _grid.Rows.Add(kvp.Key, TypeToLabel(kvp.Value));

        // Colorear filas según tipo para facilitar la revisión visual
        _grid.CellFormatting += Grid_CellFormatting;

        _btnConfirmar = new Button
        {
            Text = "Confirmar y limpiar",
            Size = new Size(150, 28),
            Location = new Point(222, 410),
            TabIndex = 0
        };
        _btnConfirmar.Click += BtnConfirmar_Click;

        _btnCancelar = new Button
        {
            Text = "Cancelar",
            Size = new Size(90, 28),
            Location = new Point(400, 410),
            TabIndex = 1,
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = _btnConfirmar;
        CancelButton = _btnCancelar;

        Controls.AddRange([_lblInstruccion, _grid, _btnConfirmar, _btnCancelar]);
    }

    private void BtnConfirmar_Click(object sender, EventArgs e)
    {
        // Forzar que el ComboBox activo confirme su selección
        _grid.EndEdit();

        var result = new Dictionary<string, ColumnDataType>(StringComparer.OrdinalIgnoreCase);

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var colName = row.Cells["colNombre"].Value?.ToString() ?? string.Empty;
            var label = row.Cells["colTipo"].Value?.ToString() ?? "Texto";
            result[colName] = LabelToType(label);
        }

        ConfirmedTypes = result;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var grid = (DataGridView)sender!;
        var label = grid.Rows[e.RowIndex].Cells["colTipo"].Value?.ToString();

        var backColor = label switch
        {
            "Numérico" => Color.FromArgb(232, 245, 233),   // verde suave
            "Fecha" => Color.FromArgb(227, 242, 253),   // azul suave
            _ => Color.FromArgb(255, 253, 231),   // amarillo suave (Texto)
        };

        grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = backColor;
    }

    // ── Conversión label ↔ enum ──────────────────────────────────────────────

    private static string TypeToLabel(ColumnDataType t) => t switch
    {
        ColumnDataType.Numeric => "Numérico",
        ColumnDataType.Date => "Fecha",
        _ => "Texto"
    };

    private static ColumnDataType LabelToType(string label) => label switch
    {
        "Numérico" => ColumnDataType.Numeric,
        "Fecha" => ColumnDataType.Date,
        _ => ColumnDataType.Text
    };
}
