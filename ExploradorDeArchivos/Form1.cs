using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ExploradorDeArchivos
{
    public partial class Form1 : Form
    {
        private string _currentPath;
        private Stack<string> _history = new Stack<string>();

        int idxVideo, idxMusic, idxText, idxFolder, idxOther, idxImage;

        private Dictionary<string, string> _shortcutPaths = new Dictionary<string, string>();

        // Instancias de los formularios
        private FormMP3 _formMP3;
        private FormMP4 _formMP4;
        private FormDataBase _formDataBase;
        private FormCorrector _formCorrector;
        private FormEdit _formEdit;
        private FormGrabadora _formGrabadora;
        private FormEditarFotos _formEditarFotos;

        public Form1()
        {
            InitializeComponent();

            // Configuración del DataGridView
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.ReadOnly = true;

            imageList1.ImageSize = new Size(24, 24);

            LoadCustomIcons();
            ConfigurarGrid1();
            ConfigurarGrid2();

            string userDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            LoadDirectory(userDocs);

            // Configurar accesos rápidos
            InitializeQuickAccess();

            // Eventos
            dataGridView1.CellDoubleClick += dataGridView1_CellDoubleClick;
            dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
            btnOpen.Click += btnOpen_Click;
            btnBack.Click += btnBack_Click;
            btnDrives.Click += btnDrives_Click;
            txtPath.KeyDown += txtPath_KeyDown;
            listBoxShortcuts.DoubleClick += ListBoxShortcuts_DoubleClick;

            // Agregar manejador de cierre
            this.FormClosing += (s, e) =>
            {
                if (_formMP3 != null && !_formMP3.IsDisposed)
                {
                    _formMP3.CerrarCompletamente();
                    _formMP3.Close();
                }
                if (_formMP4 != null && !_formMP4.IsDisposed)
                {
                    _formMP4.Close();
                }
            };
        }

        // ================= ACCESO RÁPIDO ==================
        private void InitializeQuickAccess()
        {
            listBoxShortcuts.Items.Clear();
            _shortcutPaths.Clear();

            // Documentos
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (Directory.Exists(docs))
            {
                _shortcutPaths.Add("Documentos", docs);
                listBoxShortcuts.Items.Add("Documentos");
            }

            // Descargas
            string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloads))
            {
                _shortcutPaths.Add("Descargas", downloads);
                listBoxShortcuts.Items.Add("Descargas");
            }

            // Imágenes
            string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (Directory.Exists(pictures))
            {
                _shortcutPaths.Add("Imágenes", pictures);
                listBoxShortcuts.Items.Add("Imágenes");
            }

            // Música
            string music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (Directory.Exists(music))
            {
                _shortcutPaths.Add("Música", music);
                listBoxShortcuts.Items.Add("Música");
            }

            // Vídeos
            string videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            if (Directory.Exists(videos))
            {
                _shortcutPaths.Add("Vídeos", videos);
                listBoxShortcuts.Items.Add("Vídeos");
            }

            // Escritorio
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (Directory.Exists(desktop))
            {
                _shortcutPaths.Add("Escritorio", desktop);
                listBoxShortcuts.Items.Add("Escritorio");
            }

            // Este equipo
            _shortcutPaths.Add("Este equipo", "DRIVES");
            listBoxShortcuts.Items.Add("Este equipo");
        }

        private void ListBoxShortcuts_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxShortcuts.SelectedIndex < 0) return;

            string selectedItem = listBoxShortcuts.SelectedItem.ToString().Trim();

            // Buscar la clave sin emoji en el diccionario
            string clave = null;
            foreach (var key in _shortcutPaths.Keys)
            {
                if (selectedItem.EndsWith(key))
                {
                    clave = key;
                    break;
                }
            }

            if (clave != null && _shortcutPaths.TryGetValue(clave, out string path))
            {
                if (path == "DRIVES")
                    btnDrives_Click(null, null);
                else
                    LoadDirectory(path);
            }
        }

        // ================= ICONOS ==================
        private void LoadCustomIcons()
        {
            idxVideo = AddImage(@"C:\Users\josel\Downloads\video-player_10421039.png");
            idxMusic = AddImage(@"C:\Users\josel\Downloads\music_2402461.png");
            idxText = AddImage(@"C:\Users\josel\Downloads\file_2521903.png");
            idxFolder = AddImage(@"C:\Users\josel\Downloads\shared-folder_5542294.png");
            idxOther = AddImage(@"C:\Users\josel\Downloads\incognito_7921015.png");
            idxImage = AddImage(@"C:\Users\josel\Downloads\image_icon.png");
        }

        private int AddImage(string path)
        {
            if (!File.Exists(path)) return -1;
            Image img = Image.FromFile(path);
            imageList1.Images.Add(img);
            return imageList1.Images.Count - 1;
        }

        // ================= CONFIG GRID ==================
        private void ConfigurarGrid1()
        {
            dataGridView1.Columns.Clear();

            DataGridViewImageColumn colIcon = new DataGridViewImageColumn();
            colIcon.Name = "Icono";
            colIcon.Width = 30;
            dataGridView1.Columns.Add(colIcon);

            dataGridView1.Columns.Add("Nombre", "Nombre");
            dataGridView1.Columns.Add("Tipo", "Tipo");
            dataGridView1.Columns.Add("Contenido", "Contenido");
            dataGridView1.Columns.Add("Tamano", "Tamaño");
            dataGridView1.Columns.Add("Fecha", "Fecha");

            DataGridViewTextBoxColumn colPath = new DataGridViewTextBoxColumn();
            colPath.Name = "Ruta";
            colPath.Visible = false;
            dataGridView1.Columns.Add(colPath);
        }

        private void ConfigurarGrid2()
        {
            dataGridView2.Columns.Clear();
            dataGridView2.ReadOnly = true;
            dataGridView2.AllowUserToAddRows = false;
            dataGridView2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            DataGridViewImageColumn colIcon = new DataGridViewImageColumn();
            colIcon.Name = "Icono";
            colIcon.Width = 30;
            dataGridView2.Columns.Add(colIcon);

            dataGridView2.Columns.Add("Nombre", "Nombre");
            dataGridView2.Columns.Add("Contenido", "Contenido");

            foreach (DataGridViewColumn col in dataGridView2.Columns)
                col.ReadOnly = true;
        }

        // ================= LOAD DIRECTORY ==================
        private void LoadDirectory(string path)
        {
            try
            {
                dataGridView1.Rows.Clear();
                txtPath.Text = path;
                _currentPath = path;

                DirectoryInfo dirInfo = new DirectoryInfo(path);

                foreach (var d in dirInfo.GetDirectories())
                {
                    int subFolders = 0;
                    int subFiles = 0;
                    try
                    {
                        subFolders = Directory.GetDirectories(d.FullName).Length;
                        subFiles = Directory.GetFiles(d.FullName).Length;
                    }
                    catch { }

                    dataGridView1.Rows.Add(
                        GetIcon(true, d.FullName),
                        d.Name,
                        "Carpeta",
                        $"{subFolders} carpetas, {subFiles} archivos",
                        "",
                        d.LastWriteTime,
                        d.FullName
                    );
                }
                foreach (var f in dirInfo.GetFiles())
                {
                    dataGridView1.Rows.Add(
                        GetIcon(false, f.FullName),
                        f.Name,
                        f.Extension,
                        "-",
                        FormatSize(f.Length),
                        f.LastWriteTime,
                        f.FullName
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar carpeta: " + ex.Message);
            }
        }

        // ================= ICON BY TYPE ==================
        private Image GetIcon(bool isDirectory, string path)
        {
            if (isDirectory && idxFolder >= 0)
                return imageList1.Images[idxFolder];

            string ext = Path.GetExtension(path).ToLower().TrimStart('.');

            if (IsVideo(ext) && idxVideo >= 0) return imageList1.Images[idxVideo];
            if (IsMusic(ext) && idxMusic >= 0) return imageList1.Images[idxMusic];
            if (IsText(ext) && idxText >= 0) return imageList1.Images[idxText];
            if (IsImage(ext) && idxImage >= 0) return imageList1.Images[idxImage];
            if (idxOther >= 0) return imageList1.Images[idxOther];

            return null;
        }

        private bool IsVideo(string ext) => ext is "mp4" or "avi" or "mkv" or "mov";
        private bool IsMusic(string ext) => ext is "mp3" or "wav" or "aac";
        private bool IsText(string ext) => ext is "txt" or "csv" or "json" or "xml" or "log";
        private bool IsImage(string ext) => ext is "jpg" or "jpeg" or "png" or "gif";

        // ================= REPRODUCCIÓN AUTOMÁTICA ==================
        private void AbrirReproductor(string rutaArchivo)
        {
            string ext = Path.GetExtension(rutaArchivo).ToLower().TrimStart('.');

            if (IsVideo(ext))
            {
                // Abrir FormMP4
                if (_formMP4 == null || _formMP4.IsDisposed)
                {
                    _formMP4 = new FormMP4();
                    _formMP4.FormClosed += (s, e) => _formMP4 = null;
                    _formMP4.Show();
                }

                // Cargar y reproducir el video
                _formMP4.CargarYReproducir(rutaArchivo);
                _formMP4.BringToFront();
            }
            else if (IsMusic(ext))
            {
                // Abrir FormMP3
                if (_formMP3 == null || _formMP3.IsDisposed)
                {
                    _formMP3 = new FormMP3();
                    _formMP3.FormClosed += (s, e) => _formMP3 = null;
                    _formMP3.Show();
                }

                // Agregar y reproducir la canción
                _formMP3.AgregarYReproducir(rutaArchivo);
                _formMP3.BringToFront();
            }
            else if (IsImage(ext))
            {
                // ? NUEVO: Abrir FormEditarFotos
                if (_formEditarFotos == null || _formEditarFotos.IsDisposed)
                {
                    _formEditarFotos = new FormEditarFotos();
                    _formEditarFotos.FormClosed += (s, e) => _formEditarFotos = null;
                    _formEditarFotos.Show();
                }

                // Cargar la imagen automáticamente
                _formEditarFotos.AbrirImagen(rutaArchivo);
                _formEditarFotos.BringToFront();
            }
        }

        // ================= EVENTS ==================
        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string path = dataGridView1.Rows[e.RowIndex]
                .Cells["Ruta"].Value?.ToString();

            if (string.IsNullOrEmpty(path)) return;

            if (Directory.Exists(path))
            {
                if (!string.IsNullOrEmpty(_currentPath) && _currentPath != "DRIVES")
                    _history.Push(_currentPath);

                LoadDirectory(path);
            }
            else if (File.Exists(path))
            {
                string ext = Path.GetExtension(path).ToLower().TrimStart('.');

                // ? MODIFICADO: Agregar IsImage aquí también
                if (IsMusic(ext) || IsVideo(ext) || IsImage(ext))
                {
                    AbrirReproductor(path);
                }
                else
                {
                    // Si es otro tipo, abrir con programa predeterminado
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;

            string path = dataGridView1.CurrentRow.Cells["Ruta"].Value?.ToString();

            if (string.IsNullOrEmpty(path)) return;

            if (Directory.Exists(path))
                MostrarContenidoGrid2(path);
            else
                dataGridView2.Rows.Clear();

            // NUEVO: Agregar archivo de audio a la lista automáticamente al seleccionar
            if (File.Exists(path))
            {
                string ext = Path.GetExtension(path).ToLower().TrimStart('.');

                if (IsMusic(ext) && _formMP3 != null && !_formMP3.IsDisposed)
                {
                    // Si FormMP3 está abierto, agregar la canción a la playlist
                    _formMP3.AgregarALista(path);
                }
            }
        }

        private void MostrarContenidoGrid2(string ruta)
        {
            dataGridView2.Rows.Clear();

            try
            {
                // Recorremos carpetas
                foreach (var d in Directory.GetDirectories(ruta))
                {
                    DirectoryInfo di = new DirectoryInfo(d);

                    int subFolders = 0;
                    int subFiles = 0;

                    try
                    {
                        subFolders = Directory.GetDirectories(di.FullName).Length;
                        subFiles = Directory.GetFiles(di.FullName).Length;
                    }
                    catch { }

                    //Fila de la carpeta
                    dataGridView2.Rows.Add(
                        GetIcon(true, di.FullName),
                        di.Name,
                        $"{subFolders} carpetas, {subFiles} archivos"
                    );

                    //  Archivos dentro de esa carpeta (debajo)
                    foreach (var file in Directory.GetFiles(di.FullName))
                    {
                        FileInfo fi = new FileInfo(file);

                        dataGridView2.Rows.Add(
                            GetIcon(false, fi.FullName),
                            "   -> " + fi.Name,
                            "Archivo"
                        );
                    }
                }

                // Archivos sueltos de la carpeta seleccionada
                foreach (var f in Directory.GetFiles(ruta))
                {
                    FileInfo fi = new FileInfo(f);

                    dataGridView2.Rows.Add(
                        GetIcon(false, fi.FullName),
                        fi.Name,
                        "Archivo"
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo mostrar contenido: " + ex.Message);
            }
        }

        // ================= BOTONES ==================
        private void btnOpen_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                LoadDirectory(dlg.SelectedPath);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (_history.Count > 0)
            {
                string prev = _history.Pop();
                LoadDirectory(prev);
            }
        }

        private void btnDrives_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentPath))
                _history.Push(_currentPath);

            dataGridView1.Rows.Clear();
            txtPath.Text = "Equipo";
            _currentPath = "DRIVES";

            foreach (var drive in DriveInfo.GetDrives())
            {
                dataGridView1.Rows.Add(
                    imageList1.Images[idxFolder],
                    drive.Name,
                    "Unidad",
                    "",
                    "",
                    drive.Name
                );
            }
        }

        private void txtPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string ruta = txtPath.Text.Trim();

                if (Directory.Exists(ruta))
                {
                    if (!string.IsNullOrEmpty(_currentPath))
                        _history.Push(_currentPath);

                    LoadDirectory(ruta);
                }
                else
                {
                    MessageBox.Show("La ruta no existe", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ================= SIZE FORMAT ==================
        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return kb.ToString("F1") + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024) return mb.ToString("F1") + " MB";
            double gb = mb / 1024.0;
            return gb.ToString("F2") + " GB";
        }

        private void btnllamarFormDataBase_Click(object sender, EventArgs e)
        {
            //  Si ya está abierto, solo traerlo al frente
            if (_formDataBase != null && !_formDataBase.IsDisposed)
            {
                _formDataBase.BringToFront();
                _formDataBase.WindowState = FormWindowState.Normal;
                return;
            }

            //  Si no existe o fue cerrado, crear una nueva instancia
            _formDataBase = new FormDataBase();
            _formDataBase.FormClosed += (s, e) => _formDataBase = null;
            _formDataBase.Show();
        }

        private void btnllamarFormCorrector_Click(object sender, EventArgs e)
        {
            //  Si ya está abierto, solo traerlo al frente
            if (_formCorrector != null && !_formCorrector.IsDisposed)
            {
                _formCorrector.BringToFront();
                _formCorrector.WindowState = FormWindowState.Normal;
                return;
            }

            //  Si no existe o fue cerrado, crear una nueva instancia
            _formCorrector = new FormCorrector();
            _formCorrector.FormClosed += (s, e) => _formCorrector = null;
            _formCorrector.Show();
        }

        private void btnLlamarEditor_Click(object sender, EventArgs e)
        {
            //  Si ya está abierto, solo traerlo al frente
            if (_formEdit != null && !_formEdit.IsDisposed)
            {
                _formEdit.BringToFront();
                _formEdit.WindowState = FormWindowState.Normal;
                return;
            }

            //  Si no existe o fue cerrado, crear una nueva instancia
            _formEdit = new FormEdit();
            _formEdit.FormClosed += (s, e) => _formEdit = null;
            _formEdit.Show();
        }

        private void btnFormGrabador_Click(object sender, EventArgs e)
        {
            //  Si ya está abierto, solo traerlo al frente
            if (_formGrabadora != null && !_formGrabadora.IsDisposed)
            {
                _formGrabadora.BringToFront();
                _formGrabadora.WindowState = FormWindowState.Normal;
                return;
            }

            //  Si no existe o fue cerrado, crear una nueva instancia
            _formGrabadora = new FormGrabadora();
            _formGrabadora.FormClosed += (s, e) => _formGrabadora = null;
            _formGrabadora.Show();
        }
    }
}