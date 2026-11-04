// QuickNote: 전역 단축키로 새 메모(.md / .txt) 파일을 만드는 트레이 도구
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        bool created;
        using (var mutex = new Mutex(true, "QuickNoteTray_SingleInstance", out created))
        {
            if (!created)
            {
                MessageBox.Show("QuickNote가 이미 실행 중입니다. 트레이 아이콘을 확인하세요.", "QuickNote");
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool startHidden = Array.Exists(args, a => a.Equals("/tray", StringComparison.OrdinalIgnoreCase));
            Application.Run(new MainForm(startHidden));
        }
    }
}

class Settings
{
    public string Folder;
    public string MdHotkey = "Ctrl+Alt+N";
    public string TxtHotkey = "Ctrl+Alt+T";
    public bool UseExplorerFolder = true;
    public bool OpenAfterCreate = true;
    public string NamePrefix = "새 메모";
    public int NameFormat = 0; // 0: 앞부분_날짜_시간, 1: 날짜_앞부분, 2: 날짜_시간_앞부분

    public static readonly string[] NameFormatLabels = {
        "앞부분_yyyyMMdd_HHmmss (기본)",
        "yyyyMMdd_앞부분",
        "yyyyMMdd_HHmmss_앞부분",
    };

    // 확장자 없는 파일 이름
    public static string BuildName(int format, string prefix, DateTime t)
    {
        switch (format)
        {
            case 1: return t.ToString("yyyyMMdd") + "_" + prefix;
            case 2: return t.ToString("yyyyMMdd_HHmmss") + "_" + prefix;
            default: return prefix + "_" + t.ToString("yyyyMMdd_HHmmss");
        }
    }

    static string IniPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quicknote.ini"); } }

    public static Settings Load()
    {
        var s = new Settings();
        s.Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QuickNotes");
        if (!File.Exists(IniPath)) return s;
        foreach (var line in File.ReadAllLines(IniPath, Encoding.UTF8))
        {
            int i = line.IndexOf('=');
            if (i <= 0) continue;
            string k = line.Substring(0, i).Trim(), v = line.Substring(i + 1).Trim();
            switch (k)
            {
                case "Folder": if (v != "") s.Folder = v; break;
                case "MdHotkey": s.MdHotkey = v; break;
                case "TxtHotkey": s.TxtHotkey = v; break;
                case "UseExplorerFolder": s.UseExplorerFolder = v == "1"; break;
                case "OpenAfterCreate": s.OpenAfterCreate = v == "1"; break;
                case "NamePrefix": if (v != "") s.NamePrefix = v; break;
                case "NameFormat":
                    int f;
                    if (int.TryParse(v, out f) && f >= 0 && f < NameFormatLabels.Length) s.NameFormat = f;
                    break;
            }
        }
        return s;
    }

    public void Save()
    {
        File.WriteAllLines(IniPath, new[] {
            "Folder=" + Folder,
            "MdHotkey=" + MdHotkey,
            "TxtHotkey=" + TxtHotkey,
            "UseExplorerFolder=" + (UseExplorerFolder ? "1" : "0"),
            "OpenAfterCreate=" + (OpenAfterCreate ? "1" : "0"),
            "NamePrefix=" + NamePrefix,
            "NameFormat=" + NameFormat,
        }, Encoding.UTF8);
    }
}

static class Native
{
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder sb, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder sb, int n);
    public const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_SHIFT = 4, MOD_WIN = 8, MOD_NOREPEAT = 0x4000;
    public const int WM_HOTKEY = 0x0312;

    public static string ClassOf(IntPtr h) { var sb = new StringBuilder(256); GetClassName(h, sb, 256); return sb.ToString(); }
    public static string TitleOf(IntPtr h) { var sb = new StringBuilder(512); GetWindowText(h, sb, 512); return sb.ToString(); }
}

static class Hotkey
{
    // "Ctrl+Alt+N" → (mods, vk). 실패 시 false
    public static bool TryParse(string text, out uint mods, out uint vk)
    {
        mods = 0; vk = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        foreach (var raw in text.Split('+'))
        {
            string p = raw.Trim().ToLowerInvariant();
            if (p == "ctrl" || p == "control") mods |= Native.MOD_CONTROL;
            else if (p == "alt") mods |= Native.MOD_ALT;
            else if (p == "shift") mods |= Native.MOD_SHIFT;
            else if (p == "win") mods |= Native.MOD_WIN;
            else
            {
                Keys k;
                if (!Enum.TryParse(raw.Trim(), true, out k)) return false;
                vk = (uint)k;
            }
        }
        return vk != 0 && mods != 0;
    }
}

[ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IServiceProviderCom
{
    [PreserveSig] int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppv);
}

[ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IShellBrowserWindow
{
    // IShellBrowser는 IOleWindow를 상속하므로 vtable 첫 메서드가 GetWindow. 그것만 쓴다
    [PreserveSig] int GetWindow(out IntPtr hwnd);
}

class ExplorerTarget
{
    public string Path;          // 파일을 만들 폴더. null이면 기본 폴더 사용
    public object Window;        // 새 파일을 선택해 보여 줄 탐색기 창(없으면 null)
    public bool WasExplorer;     // 앞 창이 탐색기였는지 (가상 위치 알림용)
    public string LocationName;
}

static class ExplorerFolder
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);

    static readonly Guid SID_STopLevelBrowser = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");
    static readonly Guid IID_IShellBrowser = new Guid("000214E2-0000-0000-C000-000000000046");

    // 셸 창 객체가 가리키는 탭의 HWND (Windows 11 탭 구분용). 실패 시 Zero
    static IntPtr TabHandleOf(object window)
    {
        var sp = window as IServiceProviderCom;
        if (sp == null) return IntPtr.Zero;
        Guid sid = SID_STopLevelBrowser, iid = IID_IShellBrowser;
        IntPtr p;
        if (sp.QueryService(ref sid, ref iid, out p) != 0 || p == IntPtr.Zero) return IntPtr.Zero;
        try
        {
            var sb = (IShellBrowserWindow)Marshal.GetObjectForIUnknown(p);
            IntPtr h;
            return sb.GetWindow(out h) == 0 ? h : IntPtr.Zero;
        }
        catch { return IntPtr.Zero; }
        finally { Marshal.Release(p); }
    }

    // 단축키를 누를 때 앞에 있던 창이 탐색기(또는 바탕화면)면 그 폴더
    public static ExplorerTarget Get(IntPtr fg)
    {
        var result = new ExplorerTarget();
        string cls = Native.ClassOf(fg);
        if (cls == "Progman" || cls == "WorkerW")
        {
            result.Path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            return result;
        }
        if (cls != "CabinetWClass" && cls != "ExploreWClass") return result;
        result.WasExplorer = true;

        // 활성 탭 = 최상위 창의 첫 ShellTabWindowClass 자식 (탭 없는 구형 탐색기면 Zero)
        IntPtr activeTab = FindWindowEx(fg, IntPtr.Zero, "ShellTabWindowClass", null);
        string title = Native.TitleOf(fg);

        var sameWindow = new List<object>();
        object pick = null;
        try
        {
            dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
            foreach (dynamic w in shell.Windows())
            {
                try
                {
                    if ((long)w.HWND != fg.ToInt64()) continue;
                    object o = w;
                    sameWindow.Add(o);
                    if (activeTab != IntPtr.Zero && TabHandleOf(o) == activeTab) { pick = o; break; }
                }
                catch { }
            }
        }
        catch { return result; }

        // 탭 핸들로 못 찾으면 창 제목(활성 탭 이름)과 맞는 것, 그래도 없으면 첫 번째
        if (pick == null)
            foreach (dynamic w in sameWindow)
                try { if (title.StartsWith((string)w.LocationName, StringComparison.OrdinalIgnoreCase)) { pick = w; break; } } catch { }
        if (pick == null && sameWindow.Count > 0) pick = sameWindow[0];
        if (pick == null) return result;

        try
        {
            dynamic pw = pick;
            result.LocationName = pw.LocationName;
            string path = pw.Document.Folder.Self.Path;
            if (Directory.Exists(path)) { result.Path = path; result.Window = pick; }
        }
        catch { }
        return result;
    }

    // 탐색기 창에서 새 파일을 선택하고 보이게 한다 (목록 갱신을 잠깐 기다린 뒤)
    public static void SelectLater(object window, string filePath)
    {
        if (window == null) return;
        var timer = new System.Windows.Forms.Timer { Interval = 400 };
        int tries = 0;
        timer.Tick += (s, e) =>
        {
            tries++;
            try
            {
                dynamic w = window;
                dynamic item = w.Document.Folder.ParseName(System.IO.Path.GetFileName(filePath));
                if (item != null)
                {
                    w.Document.SelectItem(item, 1 | 4 | 8 | 16); // 선택 | 나머지 해제 | 보이게 | 포커스
                    timer.Stop(); timer.Dispose();
                    return;
                }
            }
            catch { }
            if (tries >= 5) { timer.Stop(); timer.Dispose(); }
        };
        timer.Start();
    }
}

class MainForm : Form
{
    const int ID_MD = 1, ID_TXT = 2;
    Settings settings = Settings.Load();
    NotifyIcon tray;
    TextBox txtFolder, txtMdKey, txtTxtKey, txtPrefix;
    ComboBox cmbFormat;
    Label lblPreview;
    CheckBox chkExplorer, chkOpen, chkStartup;
    Label lblStatus;
    bool exiting, startHidden;

    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "QuickNoteTray";

    public MainForm(bool startHidden)
    {
        this.startHidden = startHidden;
        Text = "QuickNote - 새 메모 단축키";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 352);
        Font = new Font("맑은 고딕", 9f);

        Icon appIcon = SystemIcons.Application;
        try { appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? appIcon; } catch { } // exe에 넣은 sheep.ico
        Icon = appIcon;

        int y = 14;
        Controls.Add(new Label { Text = "기본 저장 폴더", Left = 14, Top = y + 3, Width = 100 });
        txtFolder = new TextBox { Left = 120, Top = y, Width = 260, Text = settings.Folder };
        var btnBrowse = new Button { Text = "찾기", Left = 386, Top = y - 1, Width = 60 };
        btnBrowse.Click += (s, e) =>
        {
            using (var d = new FolderBrowserDialog { SelectedPath = txtFolder.Text })
                if (d.ShowDialog(this) == DialogResult.OK) txtFolder.Text = d.SelectedPath;
        };
        Controls.Add(txtFolder); Controls.Add(btnBrowse);

        y += 32;
        Controls.Add(new Label { Text = "새 .md 단축키", Left = 14, Top = y + 3, Width = 100 });
        txtMdKey = new TextBox { Left = 120, Top = y, Width = 120, Text = settings.MdHotkey };
        Controls.Add(txtMdKey);
        y += 30;
        Controls.Add(new Label { Text = "새 .txt 단축키", Left = 14, Top = y + 3, Width = 100 });
        txtTxtKey = new TextBox { Left = 120, Top = y, Width = 120, Text = settings.TxtHotkey };
        Controls.Add(txtTxtKey);
        Controls.Add(new Label { Text = "예: Ctrl+Alt+N, Ctrl+Shift+M", Left = 250, Top = y - 27, Width = 200, ForeColor = Color.Gray });

        y += 30;
        Controls.Add(new Label { Text = "파일 이름 앞부분", Left = 14, Top = y + 3, Width = 100 });
        txtPrefix = new TextBox { Left = 120, Top = y, Width = 120, Text = settings.NamePrefix };
        Controls.Add(txtPrefix);

        y += 30;
        Controls.Add(new Label { Text = "파일 이름 형식", Left = 14, Top = y + 3, Width = 100 });
        cmbFormat = new ComboBox { Left = 120, Top = y, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbFormat.Items.AddRange(Settings.NameFormatLabels);
        cmbFormat.SelectedIndex = settings.NameFormat;
        lblPreview = new Label { Left = 120, Top = y + 26, Width = 330, ForeColor = Color.Gray };
        Controls.Add(cmbFormat); Controls.Add(lblPreview);
        EventHandler updatePreview = (s, e) =>
        {
            string p = txtPrefix.Text.Trim() == "" ? "새 메모" : txtPrefix.Text.Trim();
            lblPreview.Text = "예: " + Settings.BuildName(cmbFormat.SelectedIndex, p, DateTime.Now) + ".md";
        };
        cmbFormat.SelectedIndexChanged += updatePreview;
        txtPrefix.TextChanged += updatePreview;
        updatePreview(null, null);
        y += 22;

        y += 32;
        chkExplorer = new CheckBox { Text = "탐색기가 앞에 있으면 그 폴더에 만들기", Left = 14, Top = y, Width = 420, Checked = settings.UseExplorerFolder };
        y += 24;
        chkOpen = new CheckBox { Text = "만든 뒤 바로 열기", Left = 14, Top = y, Width = 420, Checked = settings.OpenAfterCreate };
        y += 24;
        chkStartup = new CheckBox { Text = "Windows 시작 시 트레이로 실행", Left = 14, Top = y, Width = 420, Checked = IsStartupEnabled() };
        Controls.Add(chkExplorer); Controls.Add(chkOpen); Controls.Add(chkStartup);

        y += 34;
        var btnApply = new Button { Text = "저장·적용", Left = 14, Top = y, Width = 100 };
        btnApply.Click += (s, e) => ApplySettings();
        var btnMd = new Button { Text = "새 .md", Left = 124, Top = y, Width = 80 };
        btnMd.Click += (s, e) => CreateNote(".md", IntPtr.Zero);
        var btnTxt = new Button { Text = "새 .txt", Left = 210, Top = y, Width = 80 };
        btnTxt.Click += (s, e) => CreateNote(".txt", IntPtr.Zero);
        var btnTray = new Button { Text = "트레이로", Left = 366, Top = y, Width = 80 };
        btnTray.Click += (s, e) => HideToTray();
        Controls.Add(btnApply); Controls.Add(btnMd); Controls.Add(btnTxt); Controls.Add(btnTray);

        y += 34;
        lblStatus = new Label { Left = 14, Top = y, Width = 432, Height = 36, ForeColor = Color.DimGray };
        Controls.Add(lblStatus);

        var menu = new ContextMenuStrip();
        menu.Items.Add("새 .md 메모", null, (s, e) => CreateNote(".md", IntPtr.Zero));
        menu.Items.Add("새 .txt 메모", null, (s, e) => CreateNote(".txt", IntPtr.Zero));
        menu.Items.Add("저장 폴더 열기", null, (s, e) => OpenFolder());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("설정 열기", null, (s, e) => RestoreFromTray());
        menu.Items.Add("종료", null, (s, e) => { exiting = true; Close(); });

        tray = new NotifyIcon { Icon = appIcon, Text = "QuickNote", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (s, e) => RestoreFromTray();
    }

    protected override void SetVisibleCore(bool value)
    {
        // /tray 로 시작하면 창을 띄우지 않고 트레이에만 둔다
        if (startHidden && !IsHandleCreated) { CreateHandle(); value = false; }
        base.SetVisibleCore(value);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterHotkeys();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized) HideToTray();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // X 버튼은 종료가 아니라 트레이로 숨김. 종료는 트레이 메뉴에서
        if (!exiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }
        Native.UnregisterHotKey(Handle, ID_MD);
        Native.UnregisterHotKey(Handle, ID_TXT);
        tray.Visible = false;
        tray.Dispose();
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY)
        {
            IntPtr fg = Native.GetForegroundWindow();
            int id = m.WParam.ToInt32();
            if (id == ID_MD) CreateNote(".md", fg);
            else if (id == ID_TXT) CreateNote(".txt", fg);
        }
        base.WndProc(ref m);
    }

    bool hintShown;
    void HideToTray()
    {
        Hide();
        if (!hintShown)
        {
            tray.ShowBalloonTip(2000, "QuickNote", "트레이에서 실행 중입니다. " + settings.MdHotkey + " = 새 .md", ToolTipIcon.Info);
            hintShown = true;
        }
    }

    void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    void RegisterHotkeys()
    {
        Native.UnregisterHotKey(Handle, ID_MD);
        Native.UnregisterHotKey(Handle, ID_TXT);
        var errors = new List<string>();
        Register(ID_MD, settings.MdHotkey, errors);
        Register(ID_TXT, settings.TxtHotkey, errors);
        SetStatus(errors.Count == 0
            ? "단축키 등록됨: " + settings.MdHotkey + " (.md), " + settings.TxtHotkey + " (.txt)"
            : "단축키 등록 실패: " + string.Join(", ", errors) + " — 다른 프로그램이 사용 중이거나 형식이 틀림");
    }

    void Register(int id, string key, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(key)) return; // 비우면 사용 안 함
        uint mods, vk;
        if (!Hotkey.TryParse(key, out mods, out vk) || !Native.RegisterHotKey(Handle, id, mods | Native.MOD_NOREPEAT, vk))
            errors.Add(key);
    }

    void ApplySettings()
    {
        settings.Folder = txtFolder.Text.Trim();
        settings.MdHotkey = txtMdKey.Text.Trim();
        settings.TxtHotkey = txtTxtKey.Text.Trim();
        settings.NamePrefix = txtPrefix.Text.Trim() == "" ? "새 메모" : txtPrefix.Text.Trim();
        settings.NameFormat = cmbFormat.SelectedIndex < 0 ? 0 : cmbFormat.SelectedIndex;
        settings.UseExplorerFolder = chkExplorer.Checked;
        settings.OpenAfterCreate = chkOpen.Checked;
        try { settings.Save(); } catch (Exception ex) { SetStatus("설정 저장 실패: " + ex.Message); return; }
        SetStartup(chkStartup.Checked);
        RegisterHotkeys();
    }

    void CreateNote(string ext, IntPtr fg)
    {
        try
        {
            var target = (fg != IntPtr.Zero && settings.UseExplorerFolder) ? ExplorerFolder.Get(fg) : new ExplorerTarget();
            string dir = target.Path ?? settings.Folder;
            Directory.CreateDirectory(dir);

            string baseName = Settings.BuildName(settings.NameFormat, settings.NamePrefix, DateTime.Now);
            string path = Path.Combine(dir, baseName + ext);
            for (int n = 2; File.Exists(path); n++) path = Path.Combine(dir, baseName + " (" + n + ")" + ext);

            string body = ext == ".md" ? "# " + baseName + "\r\n\r\n" : "";
            File.WriteAllText(path, body, new UTF8Encoding(false));
            SetStatus("만듦: " + path);
            ExplorerFolder.SelectLater(target.Window, path);
            if (target.WasExplorer && target.Path == null)
                tray.ShowBalloonTip(2500, "QuickNote", "'" + target.LocationName + "'은(는) 실제 폴더가 아니라 기본 폴더에 만들었습니다.", ToolTipIcon.Info);

            if (settings.OpenAfterCreate)
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            else
                tray.ShowBalloonTip(1500, "QuickNote", "만듦: " + Path.GetFileName(path), ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            SetStatus("만들기 실패: " + ex.Message);
            tray.ShowBalloonTip(3000, "QuickNote", "만들기 실패: " + ex.Message, ToolTipIcon.Error);
        }
    }

    void OpenFolder()
    {
        try { Directory.CreateDirectory(settings.Folder); Process.Start("explorer.exe", "\"" + settings.Folder + "\""); }
        catch (Exception ex) { SetStatus("폴더 열기 실패: " + ex.Message); }
    }

    void SetStatus(string s) { if (lblStatus != null) lblStatus.Text = s; }

    static bool IsStartupEnabled()
    {
        using (var k = Registry.CurrentUser.OpenSubKey(RunKey))
            return k != null && k.GetValue(RunName) != null;
    }

    static void SetStartup(bool on)
    {
        using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true))
        {
            if (k == null) return;
            if (on) k.SetValue(RunName, "\"" + Application.ExecutablePath + "\" /tray");
            else if (k.GetValue(RunName) != null) k.DeleteValue(RunName);
        }
    }
}
