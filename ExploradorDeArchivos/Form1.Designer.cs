using System.Windows.Forms;

namespace ExploradorDeArchivos
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources being disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method by code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            imageList1 = new ImageList(components);
            btnOpen = new Button();
            btnBack = new Button();
            txtPath = new TextBox();
            lblInfo = new Label();
            btnDrives = new Button();
            dataGridView1 = new DataGridView();
            dataGridView2 = new DataGridView();
            panelQuickAccess = new Panel();
            lblQuickAccess = new Label();
            listBoxShortcuts = new ListBox();
            splitter1 = new Splitter();
            btnllamarFormDataBase = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).BeginInit();
            panelQuickAccess.SuspendLayout();
            SuspendLayout();
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            imageList1.ImageSize = new Size(16, 16);
            imageList1.TransparentColor = Color.Transparent;
            // 
            // btnOpen
            // 
            btnOpen.Location = new Point(12, 3);
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new Size(100, 28);
            btnOpen.TabIndex = 1;
            btnOpen.Text = "Abrir carpeta";
            btnOpen.UseVisualStyleBackColor = true;
            // 
            // btnBack
            // 
            btnBack.Location = new Point(118, 3);
            btnBack.Name = "btnBack";
            btnBack.Size = new Size(75, 28);
            btnBack.TabIndex = 2;
            btnBack.Text = "Atrás";
            btnBack.UseVisualStyleBackColor = true;
            // 
            // txtPath
            // 
            txtPath.Location = new Point(199, 4);
            txtPath.Name = "txtPath";
            txtPath.Size = new Size(520, 27);
            txtPath.TabIndex = 3;
            // 
            // lblInfo
            // 
            lblInfo.Location = new Point(725, 3);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(149, 28);
            lblInfo.TabIndex = 4;
            lblInfo.Text = "Ninguna carpeta";
            lblInfo.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnDrives
            // 
            btnDrives.Location = new Point(12, 37);
            btnDrives.Name = "btnDrives";
            btnDrives.Size = new Size(100, 28);
            btnDrives.TabIndex = 6;
            btnDrives.Text = "Discos";
            btnDrives.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(216, 71);
            dataGridView1.MultiSelect = false;
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(524, 554);
            dataGridView1.TabIndex = 11;
            // 
            // dataGridView2
            // 
            dataGridView2.AllowUserToAddRows = false;
            dataGridView2.AllowUserToDeleteRows = false;
            dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView2.Location = new Point(748, 71);
            dataGridView2.MultiSelect = false;
            dataGridView2.Name = "dataGridView2";
            dataGridView2.ReadOnly = true;
            dataGridView2.RowHeadersWidth = 51;
            dataGridView2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView2.Size = new Size(413, 554);
            dataGridView2.TabIndex = 12;
            // 
            // panelQuickAccess
            // 
            panelQuickAccess.BackColor = SystemColors.Control;
            panelQuickAccess.BorderStyle = BorderStyle.FixedSingle;
            panelQuickAccess.Controls.Add(lblQuickAccess);
            panelQuickAccess.Controls.Add(listBoxShortcuts);
            panelQuickAccess.Location = new Point(12, 71);
            panelQuickAccess.Name = "panelQuickAccess";
            panelQuickAccess.Size = new Size(200, 554);
            panelQuickAccess.TabIndex = 13;
            // 
            // lblQuickAccess
            // 
            lblQuickAccess.AutoSize = true;
            lblQuickAccess.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblQuickAccess.Location = new Point(8, 8);
            lblQuickAccess.Name = "lblQuickAccess";
            lblQuickAccess.Size = new Size(111, 20);
            lblQuickAccess.TabIndex = 1;
            lblQuickAccess.Text = "Acceso Rápido";
            // 
            // listBoxShortcuts
            // 
            listBoxShortcuts.BorderStyle = BorderStyle.None;
            listBoxShortcuts.FormattingEnabled = true;
            listBoxShortcuts.Location = new Point(8, 31);
            listBoxShortcuts.Name = "listBoxShortcuts";
            listBoxShortcuts.Size = new Size(184, 520);
            listBoxShortcuts.TabIndex = 0;
            // 
            // splitter1
            // 
            splitter1.Location = new Point(0, 0);
            splitter1.Name = "splitter1";
            splitter1.Size = new Size(4, 637);
            splitter1.TabIndex = 14;
            splitter1.TabStop = false;
            // 
            // btnllamarFormDataBase
            // 
            btnllamarFormDataBase.Location = new Point(880, 4);
            btnllamarFormDataBase.Name = "btnllamarFormDataBase";
            btnllamarFormDataBase.Size = new Size(192, 28);
            btnllamarFormDataBase.TabIndex = 15;
            btnllamarFormDataBase.Text = "Llamar Form Conexiones";
            btnllamarFormDataBase.UseVisualStyleBackColor = true;
            btnllamarFormDataBase.Click += btnllamarFormDataBase_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1173, 637);
            Controls.Add(btnllamarFormDataBase);
            Controls.Add(splitter1);
            Controls.Add(dataGridView2);
            Controls.Add(dataGridView1);
            Controls.Add(panelQuickAccess);
            Controls.Add(btnDrives);
            Controls.Add(lblInfo);
            Controls.Add(txtPath);
            Controls.Add(btnBack);
            Controls.Add(btnOpen);
            Name = "Form1";
            Text = "Explorador de archivos";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).EndInit();
            panelQuickAccess.ResumeLayout(false);
            panelQuickAccess.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button btnOpen;
        private Button btnBack;
        private TextBox txtPath;
        private Label lblInfo;
        private ImageList imageList1;
        private Button btnDrives;
        private DataGridView dataGridView1;
        private DataGridView dataGridView2;
        private Panel panelQuickAccess;
        private Label lblQuickAccess;
        private ListBox listBoxShortcuts;
        private Splitter splitter1;
        private Button btnllamarFormDataBase;
    }
}