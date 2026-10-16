/* =========================================================================================
 * PROJECT NAME: OTELS (ONLINE TEST ENVIRONMENT LOCKING SYSTEM)
 * 
 * DEVELOPER DETAILS: NAME: ANUJ TANDON
 *                    BRANCH: BCA
 *                    YEAR: 2nd YEAR
 *                    SECTION: B
 *                    ROLL NUMBER: 25261102021
 *                    STUDENT ID: 2531756
 *                    COLLEGE: Pranveer Singh Institute of Technology, PSIT KANPUR
 *                    
 * SOFTWARE VERSION: 1.0
 * SYSTEM REQUIREMENTS:
 * - Operating System: Windows 10 or Windows 11 (64-bit architecture mandatory)
 * - Browser Runtime: Microsoft Edge WebView2 Runtime (generally pre-installed on modern Windows editions)
 * - Hardware: Dual-Core Processor, Minimum 2 GB RAM
 * - Storage: 100 MB available disk space
 * - Network: Stable, continuous internet connection required
 * ========================================================================================= */

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Drawing;
using System.Net;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace OltSecureBrowser
{
    public partial class Form1 : Form
    {
        /* ---------------------------------------------------------------------------------
         * SECTION 1: CREDENTIALS & REMOTE SYNCHRONIZATION CONFIGURATION VARIABLES
         * --------------------------------------------------------------------------------- */
        private string defaultEntry = "anuj";
        private string defaultExit = "tandon";
        private string fileEntry = "";
        private string fileExit = "";
        private string localVersion = "1.0";
        private string remoteVersion = "";
        private string updateUrl = "";
        private string examUrl = "https://psitche.ac.in/#gsc.tab=0";

        /* ---------------------------------------------------------------------------------
         * SECTION 2: WINDOWS HOOKS & LOW-LEVEL INTERCEPTION CONSTANTS
         * --------------------------------------------------------------------------------- */
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        /* ---------------------------------------------------------------------------------
         * SECTION 3: APPLICATION STATE MANAGEMENT FLAGS
         * --------------------------------------------------------------------------------- */
        public static Form1 CurrentInstance;
        private bool allowExit = false;
        private bool isPrompting = false;
        private bool isExamRunning = false;

        /* ---------------------------------------------------------------------------------
         * SECTION 4: USER INTERFACE (UI) EMBEDDED PANELS AND CONTROLS
         * --------------------------------------------------------------------------------- */
        private Panel entryPanel;
        private TextBox inputBoxEntry;
        private Panel exitPanel;
        private TextBox inputBoxExit;

        /* ---------------------------------------------------------------------------------
         * SECTION 5: WIN32 API IMPORTS (USER32.DLL & KERNEL32.DLL)
         * --------------------------------------------------------------------------------- */
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT { public uint vkCode; public uint scanCode; public uint flags; public uint time; public IntPtr dwExtraInfo; }

        // =====================================================================================
        // SECTION 6: FORM CONSTRUCTOR & INITIALIZATION ROUTINE
        // =====================================================================================
        public Form1()
        {
            InitializeComponent();
            CurrentInstance = this;

            CreateDesktopShortcut();

            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.Bounds = Screen.PrimaryScreen.Bounds;

            // Background is set instantly
            this.BackColor = Color.FromArgb(18, 18, 28);

            // Entry box is shown instantly without waiting for any internet check!
            ShowCustomEntryBox();

            this.Deactivate += Form1_Deactivate;

            Timer guardTimer = new Timer();
            guardTimer.Interval = 50;
            guardTimer.Tick += GuardTimer_Tick;
            guardTimer.Start();

            Timer netCheckTimer = new Timer();
            netCheckTimer.Interval = 5000;
            netCheckTimer.Tick += NetCheckTimer_Tick;
            netCheckTimer.Start();

            this.Resize += Form1_Resize;
            _hookID = SetHook(_proc);
            this.FormClosing += Form1_FormClosing;

            // Network checks happen in the background AFTER the UI has loaded perfectly
            this.Shown += Form1_Shown;
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            bool isOnline = await CheckSystemRequirementsAsync();
            if (isOnline)
            {
                await LoadPasswordsFromWebAsync();
            }
        }

        private void button1_Click(object sender, EventArgs e) { }

        // =====================================================================================
        // EXPLORER TOGGLE UTILITY 
        // =====================================================================================
        private void ToggleExplorer(bool kill)
        {
            try
            {
                if (kill)
                {
                    foreach (Process p in Process.GetProcessesByName("explorer"))
                    {
                        try { p.Kill(); } catch { }
                    }
                }
                else
                {
                    if (Process.GetProcessesByName("explorer").Length == 0)
                    {
                        string explorerPath = Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "explorer.exe");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = explorerPath,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch { }
        }

        // =====================================================================================
        // SECTION 7: INTERNET CONNECTIVITY VALIDATION MODULE (ASYNC)
        // =====================================================================================
        private async Task<bool> CheckSystemRequirementsAsync()
        {
            bool hasInternet = false;
            try
            {
                hasInternet = await Task.Run(() =>
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://clients3.google.com/generate_204");
                        request.Timeout = 3000;
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            return true;
                        }
                    }
                    catch { return false; }
                });
            }
            catch { hasInternet = false; }

            if (!hasInternet)
            {
                ShowDarkAlert("No internet connection detected!\nApp will now close. Please connect to the internet.", "Network Error", true);
                return false;
            }
            return true;
        }

        // =====================================================================================
        // SECTION 8: CUSTOM DARK-THEMED NOTIFICATION & ALERT DIALOG ENGINE
        // =====================================================================================
        private void ShowDarkAlert(string message, string title, bool isFatal = false)
        {
            isPrompting = true;

            Form alertForm = new Form()
            {
                Width = 450,
                Height = 180,
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true,
                BackColor = Color.FromArgb(22, 22, 35),
                ShowInTaskbar = false
            };

            Panel titlePanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(28, 28, 44)
            };

            Label titleLabel = new Label()
            {
                Text = "  " + title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Button closeBtn = new Button()
            {
                Text = "X",
                Width = 40,
                Height = 35,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(28, 28, 44),
                Cursor = Cursors.Hand
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, ev) =>
            {
                alertForm.Close();
                if (isFatal)
                {
                    allowExit = true;
                    Application.Exit();
                }
            };

            titlePanel.Controls.Add(titleLabel);
            titlePanel.Controls.Add(closeBtn);
            alertForm.Controls.Add(titlePanel);

            Label textLabel = new Label()
            {
                Left = 20,
                Top = 55,
                Width = 410,
                Height = 50,
                Text = message,
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Button okBtn = new Button()
            {
                Text = "OK",
                Left = 175,
                Top = 120,
                Width = 100,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            okBtn.FlatAppearance.BorderSize = 0;
            okBtn.Click += (s, ev) =>
            {
                alertForm.Close();
                if (isFatal)
                {
                    allowExit = true;
                    Application.Exit();
                }
            };

            alertForm.Controls.Add(textLabel);
            alertForm.Controls.Add(okBtn);
            alertForm.ShowDialog(this);

            isPrompting = false;
        }

        // =====================================================================================
        // SECTION 9: EMBEDDED ENTRY (LOGIN) INTERFACE GENERATOR
        // =====================================================================================
        private void ShowCustomEntryBox()
        {
            entryPanel = new Panel()
            {
                Width = 470,
                Height = 180,
                BackColor = Color.FromArgb(22, 22, 35),
                BorderStyle = BorderStyle.FixedSingle
            };

            Panel titlePanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(28, 28, 44)
            };

            Label titleLabel = new Label()
            {
                Text = "  Admin Entry",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Button closeBtn = new Button()
            {
                Text = "X",
                Width = 40,
                Height = 35,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(28, 28, 44),
                Cursor = Cursors.Hand
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, ev) =>
            {
                allowExit = true;
                Application.Exit();
            };

            titlePanel.Controls.Add(titleLabel);
            titlePanel.Controls.Add(closeBtn);
            entryPanel.Controls.Add(titlePanel);

            Label textLabel = new Label()
            {
                Left = 25,
                Top = 55,
                Width = 420,
                Text = "Enter Exam Entry Code:",
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 10F)
            };

            inputBoxEntry = new TextBox()
            {
                Left = 25,
                Top = 85,
                Width = 210,
                Height = 30,
                PasswordChar = '*',
                Font = new Font("Segoe UI", 11F),
                BackColor = Color.FromArgb(40, 40, 58),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Button btnEyeEntry = new Button()
            {
                Left = 240,
                Top = 84,
                Width = 40,
                Height = 32,
                Text = "👁",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnEyeEntry.FlatAppearance.BorderSize = 0;
            btnEyeEntry.Click += (s, ev) =>
            {
                if (inputBoxEntry.PasswordChar == '*')
                {
                    inputBoxEntry.PasswordChar = '\0';
                    btnEyeEntry.BackColor = Color.FromArgb(70, 70, 100);
                }
                else
                {
                    inputBoxEntry.PasswordChar = '*';
                    btnEyeEntry.BackColor = Color.FromArgb(50, 50, 70);
                }
            };

            Button btnSubmitEntry = new Button()
            {
                Text = "Enter App",
                Left = 295,
                Top = 84,
                Width = 140,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSubmitEntry.FlatAppearance.BorderSize = 0;
            btnSubmitEntry.Click += BtnSubmitEntry_Click;

            entryPanel.Controls.Add(textLabel);
            entryPanel.Controls.Add(inputBoxEntry);
            entryPanel.Controls.Add(btnEyeEntry);
            entryPanel.Controls.Add(btnSubmitEntry);

            this.Controls.Add(entryPanel);

            entryPanel.Location = new Point((this.ClientSize.Width - entryPanel.Width) / 2, (this.ClientSize.Height - entryPanel.Height) / 2);
            entryPanel.BringToFront();

            this.AcceptButton = btnSubmitEntry;
            this.ActiveControl = inputBoxEntry;
        }

        // =====================================================================================
        // SECTION 10: LOGIN CREDENTIAL VERIFICATION & BROWSER ACTIVATION ENGINE
        // =====================================================================================
        private async void BtnSubmitEntry_Click(object sender, EventArgs e)
        {
            await LoadPasswordsFromWebAsync();
            string code = inputBoxEntry.Text.Trim();

            if (code == defaultEntry || (!string.IsNullOrEmpty(fileEntry) && code == fileEntry))
            {
                entryPanel.Visible = false;

                if (Controls.ContainsKey("panel1"))
                {
                    Controls["panel1"].Visible = false;
                }

                isExamRunning = true;
                ToggleExplorer(true);

                await webView21.EnsureCoreWebView2Async(null);

                // [UPDATED] High Quality Sharp Rendering & Zoom Settings
                webView21.ZoomFactor = 1.0;
                if (webView21.CoreWebView2 != null)
                {
                    webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    webView21.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    webView21.CoreWebView2.Settings.IsZoomControlEnabled = false;
                }

                webView21.Dock = DockStyle.Fill;
                webView21.Visible = true;
                webView21.BringToFront();

                webView21.CoreWebView2.NewWindowRequested += (s, args) =>
                {
                    args.Handled = true;
                    webView21.CoreWebView2.Navigate(args.Uri);
                };

                try
                {
                    if (!examUrl.StartsWith("http://") && !examUrl.StartsWith("https://"))
                    {
                        examUrl = "https://" + examUrl;
                    }
                    webView21.Source = new Uri(examUrl);
                }
                catch
                {
                    webView21.Source = new Uri("https://psitche.ac.in/#gsc.tab=0");
                }
            }
            else
            {
                ShowDarkAlert("Wrong Exam Code! Please enter the correct code.", "Security Alert");
                inputBoxEntry.Clear();
                inputBoxEntry.Focus();
            }
        }

        // =====================================================================================
        // SECTION 11: EMBEDDED ADMIN EXIT INTERFACE GENERATOR
        // =====================================================================================
        public async void PromptExit()
        {
            if (exitPanel != null && exitPanel.Visible) return;

            await LoadPasswordsFromWebAsync();

            exitPanel = new Panel()
            {
                Width = 470,
                Height = 180,
                BackColor = Color.FromArgb(22, 22, 35),
                BorderStyle = BorderStyle.FixedSingle
            };

            Panel titlePanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(28, 28, 44)
            };

            Label titleLabel = new Label()
            {
                Text = "  Admin Exit System",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Button closeBtn = new Button()
            {
                Text = "X",
                Width = 40,
                Height = 35,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(28, 28, 44),
                Cursor = Cursors.Hand
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, ev) =>
            {
                exitPanel.Visible = false;
                exitPanel.Dispose();
                if (webView21 != null && webView21.Visible) webView21.Focus();
            };

            titlePanel.Controls.Add(titleLabel);
            titlePanel.Controls.Add(closeBtn);
            exitPanel.Controls.Add(titlePanel);

            Label textLabel = new Label()
            {
                Left = 25,
                Top = 55,
                Width = 420,
                Text = "Enter Admin Passkey to Exit:",
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 10F)
            };

            inputBoxExit = new TextBox()
            {
                Left = 25,
                Top = 85,
                Width = 210,
                Height = 30,
                PasswordChar = '*',
                Font = new Font("Segoe UI", 11F),
                BackColor = Color.FromArgb(40, 40, 58),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Button btnEyeExit = new Button()
            {
                Left = 240,
                Top = 84,
                Width = 40,
                Height = 32,
                Text = "👁",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnEyeExit.FlatAppearance.BorderSize = 0;
            btnEyeExit.Click += (s, ev) =>
            {
                if (inputBoxExit.PasswordChar == '*')
                {
                    inputBoxExit.PasswordChar = '\0';
                    btnEyeExit.BackColor = Color.FromArgb(70, 70, 100);
                }
                else
                {
                    inputBoxExit.PasswordChar = '*';
                    btnEyeExit.BackColor = Color.FromArgb(50, 50, 70);
                }
            };

            Button confirmation = new Button()
            {
                Text = "Exit App",
                Left = 295,
                Top = 84,
                Width = 140,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            confirmation.FlatAppearance.BorderSize = 0;

            confirmation.Click += (s, ev) =>
            {
                string typedExit = inputBoxExit.Text.Trim();

                if (typedExit == defaultExit || (!string.IsNullOrEmpty(fileExit) && typedExit == fileExit))
                {
                    isExamRunning = false;
                    allowExit = true;
                    Application.Exit();
                }
                else
                {
                    ShowDarkAlert("Wrong Passkey! Access Denied.", "Security Alert");
                    inputBoxExit.Clear();
                    inputBoxExit.Focus();
                }
            };

            exitPanel.Controls.Add(textLabel);
            exitPanel.Controls.Add(inputBoxExit);
            exitPanel.Controls.Add(btnEyeExit);
            exitPanel.Controls.Add(confirmation);

            this.Controls.Add(exitPanel);

            exitPanel.Location = new Point((this.ClientSize.Width - exitPanel.Width) / 2, (this.ClientSize.Height - exitPanel.Height) / 2);
            exitPanel.BringToFront();

            this.AcceptButton = confirmation;
            this.ActiveControl = inputBoxExit;
        }

        // =====================================================================================
        // SECTION 12: BACKGROUND PROCESS MONITOR 
        // =====================================================================================
        private void CheckForbiddenProcesses()
        {
            string[] forbiddenApps = { "discord", "telegram", "anydesk", "teamviewer", "obs64", "obs", "taskmgr" };
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    string procName = process.ProcessName.ToLower();
                    foreach (var badApp in forbiddenApps)
                    {
                        if (procName.Contains(badApp))
                        {
                            process.Kill();
                        }
                    }
                }
                catch { }
            }
        }

        // =====================================================================================
        // SECTION 13: FAST GUARD TIMER (50ms)
        // =====================================================================================
        private void GuardTimer_Tick(object sender, EventArgs e)
        {
            if (isPrompting) return;

            try { Clipboard.Clear(); } catch { }

            if (isExamRunning)
            {
                Process[] explorers = Process.GetProcessesByName("explorer");
                if (explorers.Length > 0)
                {
                    foreach (Process p in explorers) { try { p.Kill(); } catch { } }
                }
            }

            IntPtr currentForeground = GetForegroundWindow();
            if (currentForeground != this.Handle)
            {
                SetForegroundWindow(this.Handle);
                this.TopMost = true;
                this.WindowState = FormWindowState.Maximized;
                this.Activate();
                this.BringToFront();
            }
        }

        // =====================================================================================
        // SECTION 14: SLOW NETWORK CHECK & THREAT SCANNER (5000ms)
        // =====================================================================================
        private async void NetCheckTimer_Tick(object sender, EventArgs e)
        {
            if (isPrompting) return;

            CheckForbiddenProcesses();

            bool hasInternet = false;
            try
            {
                hasInternet = await Task.Run(() =>
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://clients3.google.com/generate_204");
                        request.Timeout = 2000;
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            return true;
                        }
                    }
                    catch { return false; }
                });
            }
            catch { hasInternet = false; }

            if (!hasInternet)
            {
                ((Timer)sender).Stop();
                ToggleExplorer(true);

                this.BackColor = Color.Black;
                if (webView21 != null) webView21.Visible = false;
                if (entryPanel != null) entryPanel.Visible = false;
                if (exitPanel != null) exitPanel.Visible = false;

                Panel fatalPanel = new Panel()
                {
                    Width = 450,
                    Height = 180,
                    BackColor = Color.FromArgb(22, 22, 35),
                    BorderStyle = BorderStyle.FixedSingle
                };

                Label title = new Label()
                {
                    Text = "  NETWORK ERROR",
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(200, 50, 50),
                    Dock = DockStyle.Top,
                    Height = 35,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };

                Label msg = new Label()
                {
                    Text = "Internet connection lost!\n\nExam environment locked.\nClick OK to close the application.",
                    ForeColor = Color.WhiteSmoke,
                    Top = 55,
                    Left = 20,
                    Width = 410,
                    Height = 60,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10F)
                };

                Button okBtn = new Button()
                {
                    Text = "OK",
                    Left = 175,
                    Top = 125,
                    Width = 100,
                    Height = 32,
                    BackColor = Color.FromArgb(200, 50, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                okBtn.FlatAppearance.BorderSize = 0;

                okBtn.Click += (s, ev) =>
                {
                    isExamRunning = false;
                    allowExit = true;
                    Application.Exit();
                };

                fatalPanel.Controls.Add(title);
                fatalPanel.Controls.Add(msg);
                fatalPanel.Controls.Add(okBtn);
                this.Controls.Add(fatalPanel);

                fatalPanel.Location = new Point((this.ClientSize.Width - fatalPanel.Width) / 2, (this.ClientSize.Height - fatalPanel.Height) / 2);
                fatalPanel.BringToFront();
            }
        }

        // =====================================================================================
        // SECTION 15: DYNAMIC WINDOW RESIZE AND CENTERING EVENT HANDLER
        // =====================================================================================
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (entryPanel != null && entryPanel.Visible)
            {
                entryPanel.Location = new Point((this.ClientSize.Width - entryPanel.Width) / 2, (this.ClientSize.Height - entryPanel.Height) / 2);
            }

            if (exitPanel != null && exitPanel.Visible)
            {
                exitPanel.Location = new Point((this.ClientSize.Width - exitPanel.Width) / 2, (this.ClientSize.Height - exitPanel.Height) / 2);
            }

            if (!isPrompting && this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Maximized;
                this.BringToFront();
            }
        }

        // =====================================================================================
        // SECTION 16: WINDOWS MESSAGE PROCESSOR OVERRIDE (BLOCK MINIMIZE COMMANDS)
        // =====================================================================================
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MINIMIZE = 0xF020;
            const int SC_RESTORE = 0xF120;

            if (m.Msg == WM_SYSCOMMAND)
            {
                int command = m.WParam.ToInt32() & 0xfff0;
                if (command == SC_MINIMIZE || command == SC_RESTORE)
                {
                    return;
                }
            }
            base.WndProc(ref m);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        // =====================================================================================
        // SECTION 17: LOW-LEVEL KEYBOARD HOOK CALLBACK (GLOBAL SHORTCUT INTERCEPTOR)
        // =====================================================================================
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_KEYDOWN || msg == WM_KEYUP || msg == WM_SYSKEYDOWN || msg == WM_SYSKEYUP)
                {
                    KBDLLHOOKSTRUCT kbdStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    Keys key = (Keys)kbdStruct.vkCode;
                    bool altPressed = (kbdStruct.flags & 0x20) != 0;

                    if (altPressed && key == Keys.F4) return (IntPtr)1;
                    if (key == Keys.LWin || key == Keys.RWin || key == Keys.PrintScreen || key == Keys.Escape || key == Keys.Snapshot) return (IntPtr)1;
                    if (altPressed && (key == Keys.Tab || key == Keys.Escape)) return (IntPtr)1;

                    if (msg == WM_KEYDOWN && key == Keys.E && Control.ModifierKeys == (Keys.Control | Keys.Shift))
                    {
                        CurrentInstance.BeginInvoke(new Action(() => CurrentInstance.PromptExit()));
                        return (IntPtr)1;
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // =====================================================================================
        // SECTION 18: FORM CLOSING RESTRICTION EVENT HANDLER
        // =====================================================================================
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!allowExit)
            {
                e.Cancel = true;
            }
            else
            {
                ToggleExplorer(false);
                UnhookWindowsHookEx(_hookID);
            }
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            if (isPrompting) return;

            this.WindowState = FormWindowState.Maximized;
            this.Activate();
            this.BringToFront();
        }

        // =====================================================================================
        // SECTION 19: DESKTOP SHORTCUT PROVISIONING UTILITY
        // =====================================================================================
        private void CreateDesktopShortcut()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktopPath, "OLT Secure Browser.url");

                if (!File.Exists(shortcutPath))
                {
                    using (StreamWriter writer = new StreamWriter(shortcutPath))
                    {
                        writer.WriteLine("[InternetShortcut]");
                        writer.WriteLine("URL=file:///" + Application.ExecutablePath.Replace("\\", "/"));
                    }
                }
            }
            catch { }
        }

        // =====================================================================================
        // SECTION 20: REMOTE CONFIGURATION SYNCHRONIZER (ASYNC GITHUB GIST SYNC)
        // =====================================================================================
        private async Task LoadPasswordsFromWebAsync()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string url = "https://gist.githubusercontent.com/anujtandon63-commits/45213c59fd3f2234b30a1164a4baf3cc/raw/passwords.txt?t=" + DateTime.Now.Ticks;

                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Cache-Control", "no-cache");
                    client.Headers.Add("Pragma", "no-cache");

                    string content = await client.DownloadStringTaskAsync(url);
                    string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
                        fileEntry = lines[0].Trim();

                    if (lines.Length > 1 && !string.IsNullOrWhiteSpace(lines[1]))
                        fileExit = lines[1].Trim();

                    if (lines.Length > 2 && !string.IsNullOrWhiteSpace(lines[2]))
                    {
                        remoteVersion = lines[2].Trim();
                        if (remoteVersion != localVersion)
                        {
                            if (lines.Length > 3 && !string.IsNullOrWhiteSpace(lines[3]))
                                updateUrl = lines[3].Trim();

                            ShowDarkAlert("A new update for OLT Secure Browser is available!\nPlease update to continue.", "Update Available", false);

                            if (!string.IsNullOrEmpty(updateUrl))
                            {
                                Process.Start(new ProcessStartInfo(updateUrl) { UseShellExecute = true });
                            }
                        }
                    }

                    if (lines.Length > 4 && !string.IsNullOrWhiteSpace(lines[4]))
                        examUrl = lines[4].Trim();
                }
            }
            catch (Exception ex)
            {
                ShowDarkAlert("Web Load Error: " + ex.Message, "Debug Error");
            }
        }
    }
}