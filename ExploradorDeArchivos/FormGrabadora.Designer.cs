namespace ExploradorDeArchivos
{
    partial class FormGrabadora
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            
            // Panel principal
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 24, 24),
                Padding = new Padding(20)
            };

            // Título
            var lblTitle = new Label
            {
                Text = "Grabadora de Audio y Video con Cámara (MP4)",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // Panel de controles de modo
            var pnlMode = new GroupBox
            {
                Text = "Modo de Grabación",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(32, 32, 32),
                Font = new Font("Segoe UI", 10F),
                Size = new Size(350, 140),
                Location = new Point(20, 60)
            };

            btnToggleMode = new Button
            {
                Text = "Cambiar a Grabación de Video",
                Size = new Size(310, 40),
                Location = new Point(20, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnToggleMode.Click += BtnToggleMode_Click;

            lblModeStatus = new Label
            {
                Text = "Modo: SOLO AUDIO",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.Cyan,
                AutoSize = true,
                Location = new Point(20, 15)
            };

            pnlMode.Controls.Add(lblModeStatus);
            pnlMode.Controls.Add(btnToggleMode);

            // Panel de dispositivos
            var pnlDevices = new GroupBox
            {
                Text = "Dispositivos",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(32, 32, 32),
                Font = new Font("Segoe UI", 10F),
                Size = new Size(350, 140),
                Location = new Point(20, 210)
            };

            var lblDevice = new Label
            {
                Text = "Micrófono:",
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 25)
            };

            cmbAudioDevices = new ComboBox
            {
                Size = new Size(310, 30),
                Location = new Point(15, 45),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            lblAudioStatus = new Label
            {
                Text = "? Dispositivo de audio detectado",
                ForeColor = Color.LimeGreen,
                AutoSize = true,
                Location = new Point(15, 85),
                Font = new Font("Segoe UI", 9F)
            };

            pnlDevices.Controls.Add(lblDevice);
            pnlDevices.Controls.Add(cmbAudioDevices);
            pnlDevices.Controls.Add(lblAudioStatus);

            // Panel de estado
            var pnlStatus = new GroupBox
            {
                Text = "Estado de Grabación",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(32, 32, 32),
                Font = new Font("Segoe UI", 10F),
                Size = new Size(350, 140),
                Location = new Point(20, 360)
            };

            lblRecordingStatus = new Label
            {
                Text = "Listo para grabar",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 25)
            };

            lblRecordingTime = new Label
            {
                Text = "00:00:00",
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                ForeColor = Color.Cyan,
                AutoSize = true,
                Location = new Point(20, 55)
            };

            pnlStatus.Controls.Add(lblRecordingStatus);
            pnlStatus.Controls.Add(lblRecordingTime);

            // Panel de botones principales
            var pnlButtons = new Panel
            {
                Size = new Size(350, 70),
                Location = new Point(20, 510),
                BackColor = Color.FromArgb(24, 24, 24)
            };

            btnStartRecording = new Button
            {
                Text = "? Iniciar Grabación",
                Size = new Size(160, 45),
                Location = new Point(20, 15),
                BackColor = Color.LimeGreen,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnStartRecording.Click += BtnStartRecording_Click;

            btnStopRecording = new Button
            {
                Text = "? Detener",
                Size = new Size(160, 45),
                Location = new Point(190, 15),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnStopRecording.Click += BtnStopRecording_Click;

            btnOpenRecordings = new Button
            {
                Text = "?? Abrir Carpeta",
                Size = new Size(160, 45),
                Location = new Point(190, 55),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOpenRecordings.Click += BtnOpenRecordings_Click;

            pnlButtons.Controls.Add(btnStartRecording);
            pnlButtons.Controls.Add(btnStopRecording);
            pnlButtons.Controls.Add(btnOpenRecordings);

            // Botón para cambiar entre cámara y pantalla
            btnToggleCameraMode = new Button
            {
                Text = "?? Cambiar a Grabación de Cámara",
                Size = new Size(310, 40),
                Location = new Point(20, 85),
                BackColor = Color.FromArgb(100, 100, 200),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnToggleCameraMode.Click += BtnToggleCameraMode_Click;
            pnlMode.Controls.Add(btnToggleCameraMode);

            pictureModeIcon = new PictureBox
            {
                Size = new Size(60, 60),
                Location = new Point(390, 80),
                BackColor = Color.FromArgb(0, 0, 150),
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            // Añadir todo al panel principal (SIN ComboBox de formatos)
            pnlMain.Controls.Add(lblTitle);
            pnlMain.Controls.Add(pnlMode);
            pnlMain.Controls.Add(pictureModeIcon);
            pnlMain.Controls.Add(pnlDevices);
            pnlMain.Controls.Add(pnlStatus);
            pnlMain.Controls.Add(pnlButtons);

            // Configurar formulario
            ClientSize = new Size(1000, 600);
            BackColor = Color.FromArgb(24, 24, 24);
            Text = "Grabadora";
            Name = "FormGrabadora";
            StartPosition = FormStartPosition.CenterParent;

            Controls.Add(pnlMain);
        }

        private Button btnToggleMode;
        private Label lblModeStatus;
        private PictureBox pictureModeIcon;
        private ComboBox cmbAudioDevices;
        private Label lblAudioStatus;
        private Label lblRecordingStatus;
        private Label lblRecordingTime;
        private Button btnStartRecording;
        private Button btnStopRecording;
        private Button btnOpenRecordings;
        private Button btnToggleCameraMode;
    }
}