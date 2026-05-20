using System.Windows.Forms;

namespace ExploradorDeArchivos
{
    partial class FormEdit
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlTop = new Panel();
            lblTitulo = new Label();
            lblEstado = new Label();
            pnlControles = new Panel();
            btnCargar = new Button();
            btnGuardar = new Button();
            splitter = new SplitContainer();
            dgvDatos = new DataGridView();
            rtbContenido = new RichTextBox();
            
            pnlTop.SuspendLayout();
            pnlControles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitter).BeginInit();
            splitter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDatos).BeginInit();
            SuspendLayout();

            // pnlTop
            pnlTop.BackColor = System.Drawing.Color.FromArgb(30, 30, 50);
            pnlTop.Controls.Add(lblTitulo);
            pnlTop.Controls.Add(lblEstado);
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 60;
            pnlTop.Padding = new Padding(10);

            lblTitulo.Text = "Editor de Archivos";
            lblTitulo.ForeColor = System.Drawing.Color.White;
            lblTitulo.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            lblTitulo.AutoSize = true;
            lblTitulo.Location = new System.Drawing.Point(10, 10);

            lblEstado.Text = "Listo";
            lblEstado.ForeColor = System.Drawing.Color.LightGreen;
            lblEstado.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblEstado.AutoSize = true;
            lblEstado.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblEstado.Location = new System.Drawing.Point(400, 15);

            // pnlControles
            pnlControles.BackColor = System.Drawing.Color.FromArgb(45, 45, 68);
            pnlControles.Controls.Add(btnCargar);
            pnlControles.Controls.Add(btnGuardar);
            pnlControles.Dock = DockStyle.Top;
            pnlControles.Height = 50;
            pnlControles.Padding = new Padding(10);

            btnCargar.Text = " Cargar Archivo";
            btnCargar.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnCargar.ForeColor = System.Drawing.Color.White;
            btnCargar.FlatStyle = FlatStyle.Flat;
            btnCargar.Size = new System.Drawing.Size(150, 32);
            btnCargar.Location = new System.Drawing.Point(10, 9);
            btnCargar.Click += btnCargar_Click;

            btnGuardar.Text = " Guardar Cambios";
            btnGuardar.BackColor = System.Drawing.Color.FromArgb(16, 137, 62);
            btnGuardar.ForeColor = System.Drawing.Color.White;
            btnGuardar.FlatStyle = FlatStyle.Flat;
            btnGuardar.Size = new System.Drawing.Size(150, 32);
            btnGuardar.Location = new System.Drawing.Point(170, 9);
            btnGuardar.Click += btnGuardar_Click;

            // splitter
            splitter.Dock = DockStyle.Fill;
            splitter.SplitterDistance = 400;
            splitter.Orientation = Orientation.Vertical;
            splitter.Panel1.Controls.Add(dgvDatos);
            splitter.Panel2.Controls.Add(rtbContenido);

            // dgvDatos
            dgvDatos.Dock = DockStyle.Fill;
            dgvDatos.AllowUserToAddRows = true;
            dgvDatos.AllowUserToDeleteRows = true;
            dgvDatos.ReadOnly = false;
            dgvDatos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDatos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // rtbContenido
            rtbContenido.Dock = DockStyle.Fill;
            rtbContenido.Font = new System.Drawing.Font("Consolas", 10F);
            rtbContenido.ReadOnly = false;
            rtbContenido.WordWrap = true;

            // FormEdit
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1000, 700);
            Controls.Add(splitter);
            Controls.Add(pnlControles);
            Controls.Add(pnlTop);
            Name = "FormEdit";
            Text = "Editor de Archivos";
            StartPosition = FormStartPosition.CenterScreen;

            pnlTop.ResumeLayout(false);
            pnlTop.PerformLayout();
            pnlControles.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitter).EndInit();
            splitter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDatos).EndInit();
            ResumeLayout(false);
        }

        private Panel pnlTop;
        private Label lblTitulo;
        private Label lblEstado;
        private Panel pnlControles;
        private Button btnCargar;
        private Button btnGuardar;
        private SplitContainer splitter;
        public DataGridView dgvDatos;
        public RichTextBox rtbContenido;
    }
}