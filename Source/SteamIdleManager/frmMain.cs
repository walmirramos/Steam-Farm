using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SteamIdleManager
{
    public sealed class frmMain : Form
    {
        private readonly UserPreferences preferences = UserPreferences.Load();
        private readonly SessionManager manager = new SessionManager(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam-idle.exe"));
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 500 };
        private readonly TextBox ids = new TextBox();
        private readonly DataGridView grid = new DataGridView();
        private readonly Label steamStatus = new Label(), count = new Label(), notice = new Label(), empty = new Label();
        private readonly ModernButton start = new ModernButton(), stopAll = new ModernButton(), stopSelected = new ModernButton(), theme = new ModernButton(), logs = new ModernButton();
        private readonly Card entryCard = new Card(), sessionCard = new Card();
        private CancellationTokenSource starting;
        private Task launchTask = Task.CompletedTask;
        private bool stopping, closing, allowClose, noticeError;
        private Palette palette;
        private int ticks;
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);

        public frmMain()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10f);
            ClientSize = new Size(660, 574);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "BDK Steam Hour Farm";
            string icon = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
            if (File.Exists(icon)) Icon = new Icon(icon);

            LabelAt(this, "BDK  /  STEAM TOOLS", 24, 18, 330, 20, 9, true, "accent");
            LabelAt(this, "Steam Hour Farm", 22, 42, 430, 38, 23, true);
            LabelAt(this, "Seus jogos. Seu tempo. Tudo em um só lugar.", 24, 87, 460, 22, 10, false, "muted");
            ButtonAt(this, theme, "Tema claro", 522, 24, 114, 34);
            steamStatus.SetBounds(460, 87, 176, 22);
            steamStatus.TextAlign = ContentAlignment.MiddleRight;
            Controls.Add(steamStatus);

            entryCard.SetBounds(24, 125, 612, 167); Controls.Add(entryCard);
            LabelAt(entryCard, "Adicionar jogos", 18, 12, 300, 25, 12, true);
            LabelAt(entryCard, "AppIDs separados por vírgula, espaço ou linha", 18, 40, 565, 20, 9, false, "muted");
            var inputPanel = new Panel { Name = "inputPanel", Padding = new Padding(10, 8, 10, 5) };
            inputPanel.SetBounds(18, 68, 576, 36); entryCard.Controls.Add(inputPanel);
            ids.BorderStyle = BorderStyle.None; ids.Dock = DockStyle.Fill; ids.Font = new Font("Segoe UI", 10);
            ids.Multiline = true; ids.AcceptsReturn = true; ids.ScrollBars = ScrollBars.None;
            ids.AccessibleName = "AppIDs dos jogos"; ids.Text = preferences.GameIds;
            inputPanel.Controls.Add(ids);
            ButtonAt(entryCard, start, "Iniciar jogos", 18, 116, 170, 36);
            ButtonAt(entryCard, stopAll, "Parar todos", 198, 116, 135, 36);
            LabelAt(entryCard, "IDs repetidos são ignorados", 345, 122, 250, 22, 9, false, "muted");

            sessionCard.SetBounds(24, 306, 612, 205); Controls.Add(sessionCard);
            LabelAt(sessionCard, "Sua sessão", 18, 13, 220, 26, 12, true);
            count.SetBounds(410, 17, 184, 22); count.TextAlign = ContentAlignment.MiddleRight; sessionCard.Controls.Add(count);
            grid.SetBounds(18, 48, 576, 100);
            grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false; grid.ReadOnly = true; grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.RowHeadersVisible = false;
            grid.BorderStyle = BorderStyle.None; grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false; grid.ColumnHeadersHeight = 28;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.RowTemplate.Height = 30; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.Columns.Add("appid", "APP ID"); grid.Columns.Add("status", "STATUS"); grid.Columns.Add("time", "TEMPO LOCAL");
            grid.Columns[0].FillWeight = 25; grid.Columns[1].FillWeight = 43; grid.Columns[2].FillWeight = 32;
            foreach (DataGridViewColumn column in grid.Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.AccessibleName = "Jogos desta sessão";
            sessionCard.Controls.Add(grid);
            empty.Text = "Nenhum jogo nesta sessão\nAdicione os AppIDs acima para começar.";
            empty.TextAlign = ContentAlignment.MiddleCenter; empty.SetBounds(18, 77, 576, 70);
            sessionCard.Controls.Add(empty); empty.BringToFront();
            ButtonAt(sessionCard, stopSelected, "Parar selecionado", 18, 158, 170, 33);
            LabelAt(sessionCard, "O tempo mostrado é o desta sessão.", 210, 165, 385, 20, 9, false, "muted");

            notice.SetBounds(24, 523, 490, 40); notice.Font = new Font("Segoe UI", 9);
            notice.Text = "Pronto. Abra a Steam e informe os AppIDs."; Controls.Add(notice);
            ButtonAt(this, logs, "Ver registro", 524, 525, 112, 31);
            start.Click += async (s, e) => { launchTask = StartGamesAsync(); await launchTask; };
            stopAll.Click += async (s, e) => await StopGamesAsync(null);
            stopSelected.Click += async (s, e) => { var selected = SelectedSession(); if (selected != null) await StopGamesAsync(selected); };
            grid.SelectionChanged += (s, e) => UpdateButtons();
            theme.Click += (s, e) => { preferences.Dark = !preferences.Dark; ApplyTheme(); SavePreferences(); };
            logs.Click += (s, e) => OpenLog();
            timer.Tick += (s, e) => { RenderSessions(); if (++ticks % 4 == 0) UpdateSteamStatus(); };
            ApplyTheme();
            ResumeLayout(false);
            UpdateSteamStatus(); RenderSessions(); timer.Start();
        }
        private static void LabelAt(Control parent, string text, int x, int y, int width, int height, float size, bool bold, string tag = null)
        {
            var label = new Label { Text = text, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), Tag = tag, BackColor = Color.Transparent };
            label.SetBounds(x, y, width, height); parent.Controls.Add(label);
        }
        private static void ButtonAt(Control parent, ModernButton button, string text, int x, int y, int width, int height)
        {
            button.Text = text; button.AccessibleName = text; button.SetBounds(x, y, width, height); parent.Controls.Add(button);
        }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); ApplyTitleBar(); }
        private void ApplyTitleBar()
        {
            if (!IsHandleCreated) return;
            int dark = preferences.Dark ? 1 : 0;
            DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int));
            int round = 2; DwmSetWindowAttribute(Handle, 33, ref round, sizeof(int));
        }
        private void ApplyTheme()
        {
            palette = Palette.Create(preferences.Dark);
            BackColor = palette.Background; ForeColor = palette.Text;
            ApplyControlTheme(this);
            theme.Text = preferences.Dark ? "Tema claro" : "Tema escuro";
            theme.AccessibleName = theme.Text;
            start.Fill = palette.Accent; start.Ink = palette.AccentText; start.Stroke = palette.Accent;
            ids.BackColor = palette.Input; ids.ForeColor = palette.Text; ids.Parent.BackColor = palette.Input;
            foreach (var card in new[] { entryCard, sessionCard }) { card.Fill = palette.Surface; card.Stroke = palette.Border; }
            grid.BackgroundColor = palette.Surface; grid.GridColor = palette.Border;
            grid.DefaultCellStyle = new DataGridViewCellStyle {
                BackColor = palette.Surface, ForeColor = palette.Text, SelectionBackColor = palette.Selection,
                SelectionForeColor = palette.Text, Padding = new Padding(6, 0, 0, 0), Font = new Font("Segoe UI", 10)
            };
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = palette.Surface, ForeColor = palette.Muted, SelectionBackColor = palette.Surface,
                SelectionForeColor = palette.Muted, Font = new Font("Segoe UI", 8, FontStyle.Bold), Padding = new Padding(6, 0, 0, 0)
            };
            empty.BackColor = palette.Surface; empty.ForeColor = palette.Muted;
            count.BackColor = palette.Surface; count.ForeColor = palette.Accent;
            notice.ForeColor = noticeError ? palette.Error : palette.Muted;
            UpdateSteamStatus(); ApplyTitleBar(); Invalidate(true);
        }
        private void ApplyControlTheme(Control control)
        {
            foreach (Control child in control.Controls)
            {
                if (child is Label) child.ForeColor = (string)child.Tag == "muted" ? palette.Muted : (string)child.Tag == "accent" ? palette.Accent : palette.Text;
                var button = child as ModernButton;
                if (button != null) { button.Fill = palette.Surface; button.Ink = palette.Text; button.Stroke = palette.Border; }
                ApplyControlTheme(child);
            }
        }
        private static bool SteamIsRunning()
        {
            var processes = Process.GetProcessesByName("steam");
            try { return processes.Length > 0; }
            finally { foreach (var process in processes) process.Dispose(); }
        }
        private void UpdateSteamStatus()
        {
            bool running = SteamIsRunning();
            steamStatus.Text = running ? "●  Steam aberta" : "○  Steam fechada";
            if (palette != null) steamStatus.ForeColor = running ? palette.Accent : palette.Muted;
        }
        private GameSession SelectedSession() { return grid.CurrentRow == null ? null : grid.CurrentRow.Tag as GameSession; }
        private void RenderSessions()
        {
            manager.Refresh();
            while (grid.Rows.Count < manager.Sessions.Count) grid.Rows.Add();
            for (int index = 0; index < manager.Sessions.Count; index++)
            {
                var session = manager.Sessions[index]; var row = grid.Rows[index]; row.Tag = session;
                object[] values = { session.AppId.ToString(), session.Status, session.Elapsed };
                for (int column = 0; column < 3; column++)
                    if (!Equals(row.Cells[column].Value, values[column])) row.Cells[column].Value = values[column];
                row.Cells[1].Style.ForeColor = session.Status.StartsWith("Falha") ? palette.Error : session.IsActive ? palette.Accent : palette.Muted;
            }
            int active = manager.Sessions.Count(s => s.IsActive);
            count.Text = active == 1 ? "1 jogo ativo" : active + " jogos ativos";
            empty.Visible = manager.Sessions.Count == 0;
            UpdateButtons();
        }
        private void UpdateButtons()
        {
            start.Enabled = starting == null && !stopping && !closing;
            stopAll.Enabled = !stopping && !closing && (starting != null || manager.Sessions.Any(s => s.IsActive));
            var selected = SelectedSession();
            stopSelected.Enabled = starting == null && !stopping && !closing && selected != null && selected.IsActive;
            ids.ReadOnly = starting != null || stopping || closing;
        }
        private void ShowNotice(string text, bool error = false)
        {
            noticeError = error; notice.Text = text; notice.ForeColor = error ? palette.Error : palette.Muted;
        }
        private void SavePreferences()
        {
            preferences.GameIds = ids.Text.Trim();
            if (!preferences.Save()) ShowNotice("Não foi possível salvar os AppIDs. Consulte o registro.", true);
        }
        private async Task StartGamesAsync()
        {
            if (starting != null || stopping || closing) return;
            uint[] gameIds;
            try { gameIds = SessionManager.ParseIds(ids.Text); manager.ValidateDependencies(); }
            catch (Exception ex) { ShowNotice(ex.Message, true); return; }
            if (!SteamIsRunning()) { ShowNotice("Abra a Steam e entre na sua conta antes de iniciar.", true); return; }
            SavePreferences();
            starting = new CancellationTokenSource();
            start.Text = "Iniciando…"; UpdateButtons(); ShowNotice("Iniciando jogos. Você pode cancelar em Parar todos.");
            try
            {
                await manager.StartAsync(gameIds, starting.Token);
                if (!stopping && !closing)
                {
                    bool failed = manager.Sessions.Any(s => gameIds.Contains(s.AppId) && s.Status.StartsWith("Falha"));
                    ShowNotice(failed ? "Alguns jogos falharam. Veja o status e o registro." : "Sessão atualizada. Jogos já ativos foram mantidos.", failed);
                }
            }
            catch (Exception ex) { Logger.LogError("Falha ao iniciar sessão", ex); ShowNotice("Falha ao iniciar a sessão. Consulte o registro.", true); }
            finally { starting.Dispose(); starting = null; start.Text = "Iniciar jogos"; RenderSessions(); }
        }
        private async Task StopGamesAsync(GameSession selected)
        {
            if (stopping || closing) return;
            stopping = true; starting?.Cancel(); UpdateButtons();
            await launchTask;
            bool success = selected == null ? await manager.StopAllAsync() : await manager.StopAsync(selected);
            stopping = false; RenderSessions();
            ShowNotice(success ? (selected == null ? "Todos os jogos desta sessão foram parados." : "Jogo selecionado parado.") : "Não foi possível parar um jogo. Consulte o registro.", !success);
        }
        private void OpenLog()
        {
            try { Logger.LogInfo("Registro aberto pelo usuário"); Process.Start(new ProcessStartInfo("notepad.exe", "\"" + Logger.LogPath + "\"") { UseShellExecute = true }); }
            catch (Exception ex) { ShowNotice("Não foi possível abrir o registro: " + ex.Message, true); }
        }
        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            if (allowClose) { base.OnFormClosing(e); return; }
            e.Cancel = true;
            if (closing || stopping) return;
            if (starting != null || manager.Sessions.Any(s => s.IsActive))
            {
                if (MessageBox.Show(this, "Parar os jogos desta sessão e fechar o aplicativo?", "Encerrar sessão", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            }
            closing = true; starting?.Cancel(); UpdateButtons();
            await launchTask;
            bool stopped = await manager.StopAllAsync();
            if (!stopped) { closing = false; RenderSessions(); ShowNotice("Um jogo não pôde ser parado. Tente novamente antes de fechar.", true); return; }
            SavePreferences(); timer.Stop(); allowClose = true; BeginInvoke(new Action(Close));
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Dispose(); starting?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
