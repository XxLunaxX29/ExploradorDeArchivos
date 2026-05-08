using GeniusLyricsAPI.Models;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExploradorDeArchivos
{
    public partial class FormMP4 : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private VideoView videoView;
        private System.Windows.Forms.Timer _timerProgreso;
        private bool _isSeeking = false;
        private int _repeatMode = 0;
        private bool _isInitialized = false;
        private const int TRACKBAR_MAX = 10000;
        private long _lastSeekTime = 0;
        private const long SEEK_DEBOUNCE_MS = 100;
        private bool _isLoadingVideo = false;

        public FormMP4()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitializeVideoView();
            InitializeVLC();
            InitializeTimer();
            ConfigurarTrackbar();
            ConfigurarPictureBoxes();
        }

        private void ConfigurarTrackbar()
        {
            trkProgreso.Minimum = 0;
            trkProgreso.Maximum = TRACKBAR_MAX;
            trkProgreso.Value = 0;
            trkProgreso.TickFrequency = TRACKBAR_MAX / 10;
        }

        private void ConfigurarPictureBoxes()
        {
            trkVolumen.Minimum = 0;
            trkVolumen.Maximum = 100;
            trkVolumen.Value = 100;
        }

        private void InitializeVideoView()
        {
            try
            {
                videoView = new VideoView();
                videoView.Dock = DockStyle.Fill;
                pnlVideo.Controls.Add(videoView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear VideoView: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeVLC()
        {
            try
            {
                Core.Initialize();
                _libVLC = new LibVLC();
                _mediaPlayer = new MediaPlayer(_libVLC);
                videoView.MediaPlayer = _mediaPlayer;

                _mediaPlayer.EndReached += MediaPlayer_EndReached;
                _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
                _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar VLC: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeTimer()
        {
            _timerProgreso = new System.Windows.Forms.Timer();
            _timerProgreso.Interval = 100;
            _timerProgreso.Tick += TimerProgreso_Tick;
            _timerProgreso.Start();
        }

        // ================= MÉTODO PÚBLICO PARA REPRODUCCIÓN AUTOMÁTICA ==================
        public void CargarYReproducir(string rutaArchivo)
        {
            if (!_isInitialized)
            {
                // Si aún no está inicializado, esperar un poco
                Task.Run(async () =>
                {
                    await Task.Delay(500);
                    await CargarVideoAsyncAwaitable(rutaArchivo);
                });
            }
            else
            {
                CargarVideoAsync(rutaArchivo);
            }
        }

        private async Task CargarVideoAsyncAwaitable(string rutaArchivo)
        {
            await Task.Run(() => CargarVideoAsync(rutaArchivo));
        }

        private void PicAbrir_Click(object sender, EventArgs e)
        {
            if (!_isInitialized) return;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v|Archivos MP4|*.mp4|Todos los archivos|*.*";
                ofd.Title = "Selecciona un archivo de video";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    CargarVideoAsync(ofd.FileName);
                }
            }
        }

        private async void CargarVideoAsync(string rutaArchivo)
        {
            if (!_isInitialized) return;

            try
            {
                _isLoadingVideo = true;
                _isSeeking = true;

                // Detener video actual
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Stop();
                    await Task.Delay(100);
                }

                // Limpiar media anterior
                _mediaPlayer.Media?.Dispose();

                // Crear nueva media
                var media = new LibVLCSharp.Shared.Media(_libVLC, rutaArchivo, FromType.FromPath);
                await media.Parse(MediaParseOptions.ParseLocal);

                // Asignar al MediaPlayer
                _mediaPlayer.Play(media);

                // Ajustar volumen y estado de mute
                _mediaPlayer.Mute = false;
                _mediaPlayer.Volume = trkVolumen.Value;

                lblNombreArchivo.Text = System.IO.Path.GetFileName(rutaArchivo);

                // Esperar a que el video se cargue
                await Task.Delay(300);

                trkProgreso.Value = 0;
                lblTiempoActual.Text = "00:00";
                lblTiempoTotal.Text = "00:00";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el video: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isLoadingVideo = false;
                _isSeeking = false;
            }
        }

        private void PicPlayPause_Click(object sender, EventArgs e)
        {
            if (!_isInitialized || _mediaPlayer == null || _isLoadingVideo) return;

            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                string play = "C:/Users/josel/Downloads/play-button_15336208.png";
                PicPlayPause.Image = Image.FromFile(play);
            }
            else
            {
                _mediaPlayer.Play();
                string pause = "C:/Users/josel/Downloads/pause_12648871.png";
                PicPlayPause.Image = Image.FromFile(pause);
            }
        }

        private void PicStop_Click(object sender, EventArgs e)
        {
            if (!_isInitialized || _mediaPlayer == null || _isLoadingVideo) return;

            _mediaPlayer.Stop();
            trkProgreso.Value = 0;
            lblTiempoActual.Text = "00:00";
            lblTiempoTotal.Text = "00:00";
        }

        private void PicAdelante_Click(object sender, EventArgs e)
        {
            if (!_isInitialized || _mediaPlayer == null || _isLoadingVideo) return;
            RealizarSeekAsync(_mediaPlayer.Time + 5000);
        }

        private void PicAtras_Click(object sender, EventArgs e)
        {
            if (!_isInitialized || _mediaPlayer == null || _isLoadingVideo) return;
            long nuevoTiempo = Math.Max(0, _mediaPlayer.Time - 5000);
            RealizarSeekAsync(nuevoTiempo);
        }

        private void PicRepeat_Click(object sender, EventArgs e)
        {
            _repeatMode = (_repeatMode + 1) % 3;
            ActualizarVisualsRepeat();
        }

        private void PicMute_Click(object sender, EventArgs e)
        {
            if (!_isInitialized || _mediaPlayer == null) return;

            _mediaPlayer.Mute = !_mediaPlayer.Mute;

            if (_mediaPlayer.Mute)
            {
                picMute.BackColor = Color.FromArgb(200, 50, 50);
            }
            else
            {
                picMute.BackColor = Color.Black;
            }
        }

        private void TrkVolumen_Scroll(object sender, EventArgs e)
        {
            if (!_isInitialized || _mediaPlayer == null) return;
            _mediaPlayer.Volume = trkVolumen.Value;
        }

        private void TrkProgreso_Scroll(object sender, EventArgs e)
        {
            if (!_isInitialized || _mediaPlayer?.Media == null || _isLoadingVideo) return;

            long duracion = _mediaPlayer.Length;
            long nuevoTiempo = (long)((trkProgreso.Value / (double)TRACKBAR_MAX) * duracion);

            RealizarSeekAsync(nuevoTiempo);
        }

        private async void RealizarSeekAsync(long nuevoTiempo)
        {
            if (_isLoadingVideo)
                return;

            long ahora = DateTime.UtcNow.Ticks / 10000;
            if (ahora - _lastSeekTime < SEEK_DEBOUNCE_MS)
                return;

            _lastSeekTime = ahora;
            _isSeeking = true;

            try
            {
                await Task.Run(() =>
                {
                    if (_mediaPlayer?.Media != null)
                    {
                        _mediaPlayer.Time = Math.Max(0, nuevoTiempo);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en seek: {ex.Message}");
            }
            finally
            {
                _isSeeking = false;
            }
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (!_isSeeking && !_isLoadingVideo)
            {
                ActualizarBarraProgreso();
            }
        }

        private void MediaPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            if (!_isLoadingVideo)
            {
                ActualizarBarraProgreso();
            }
        }

        private void TimerProgreso_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer?.Media != null && !_isSeeking && !_isLoadingVideo)
            {
                ActualizarBarraProgreso();
            }
        }

        private void ActualizarVisualsRepeat()
        {
            switch (_repeatMode)
            {
                case 0:
                    picRepeat.BackColor = Color.Black;
                    break;
                case 1:
                    picRepeat.BackColor = Color.FromArgb(255, 100, 200);
                    break;
                case 2:
                    picRepeat.BackColor = Color.FromArgb(100, 200, 255);
                    break;
            }
        }

        private void ActualizarBarraProgreso()
        {
            try
            {
                if (_mediaPlayer?.Media == null) return;

                long duracion = _mediaPlayer.Length;
                long tiempoActual = _mediaPlayer.Time;

                if (duracion <= 0 || tiempoActual < 0)
                    return;

                int nuevoValor = (int)((tiempoActual / (double)duracion) * TRACKBAR_MAX);
                nuevoValor = Math.Clamp(nuevoValor, 0, TRACKBAR_MAX);

                if (trkProgreso.InvokeRequired)
                {
                    trkProgreso.Invoke(new Action(() =>
                    {
                        if (!_isSeeking && !_isLoadingVideo && trkProgreso.Value != nuevoValor)
                            trkProgreso.Value = nuevoValor;
                    }));
                }
                else
                {
                    if (!_isSeeking && !_isLoadingVideo && trkProgreso.Value != nuevoValor)
                        trkProgreso.Value = nuevoValor;
                }

                string tiempoAct = FormatearTiempo(tiempoActual);
                string tiempoTot = FormatearTiempo(duracion);

                if (lblTiempoActual.InvokeRequired)
                {
                    lblTiempoActual.Invoke(new Action(() => lblTiempoActual.Text = tiempoAct));
                }
                else
                {
                    lblTiempoActual.Text = tiempoAct;
                }

                if (lblTiempoTotal.InvokeRequired)
                {
                    lblTiempoTotal.Invoke(new Action(() => lblTiempoTotal.Text = tiempoTot));
                }
                else
                {
                    lblTiempoTotal.Text = tiempoTot;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando barra: {ex.Message}");
            }
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => GestionarFinVideo()));
            }
            else
            {
                GestionarFinVideo();
            }
        }

        private void GestionarFinVideo()
        {
            if (_mediaPlayer == null || _isLoadingVideo)
                return;

            switch (_repeatMode)
            {
                case 0:
                    _mediaPlayer.Stop();
                    ActualizarUIAlInicio();
                    break;
                case 1:
                    ReiniciarVideoAsync();
                    break;
                case 2:
                    ReiniciarVideoAsync();
                    break;
            }
        }

        private void ReiniciarVideoAsync()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ReiniciarVideoAsync));
                return;
            }

            try
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al reiniciar video: {ex.Message}");
            }
        }

        private void ActualizarUIAlInicio()
        {
            if (trkProgreso.InvokeRequired)
            {
                trkProgreso.Invoke(new Action(() =>
                {
                    trkProgreso.Value = 0;
                    lblTiempoActual.Text = "00:00";
                }));
            }
            else
            {
                trkProgreso.Value = 0;
                lblTiempoActual.Text = "00:00";
            }
        }

        private string FormatearTiempo(long milisegundos)
        {
            if (milisegundos < 0)
                return "00:00";

            long horas = milisegundos / 3600000;
            long minutos = (milisegundos % 3600000) / 60000;
            long segundos = (milisegundos % 60000) / 1000;

            return horas > 0
                ? $"{horas:D2}:{minutos:D2}:{segundos:D2}"
                : $"{minutos:D2}:{segundos:D2}";
        }

        protected override void OnClosed(EventArgs e)
        {
            _timerProgreso?.Stop();
            _timerProgreso?.Dispose();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            videoView?.Dispose();
            base.OnClosed(e);
        }
    }
}
