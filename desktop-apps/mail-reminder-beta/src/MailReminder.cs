using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleMailReminder
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--settings-smoke-test")
            {
                SettingsSmokeTest.Run();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class ProviderProfile
    {
        public string Key;
        public string Name;
        public string Host;
        public int Port;
        public string WebmailUrl;

        public override string ToString()
        {
            return Name;
        }
    }

    internal sealed class UiTheme
    {
        public string Key;
        public string Name;
        public Color Back;
        public Color HeaderA;
        public Color HeaderB;
        public Color Surface;
        public Color Surface2;
        public Color Input;
        public Color Text;
        public Color Muted;
        public Color Accent;
        public Color Accent2;
        public Color Danger;
        public bool Animated;

        public UiTheme(string key, string name, Color back, Color headerA, Color headerB, Color surface, Color surface2, Color input, Color text, Color muted, Color accent, Color accent2, Color danger, bool animated)
        {
            Key = key;
            Name = name;
            Back = back;
            HeaderA = headerA;
            HeaderB = headerB;
            Surface = surface;
            Surface2 = surface2;
            Input = input;
            Text = text;
            Muted = muted;
            Accent = accent;
            Accent2 = accent2;
            Danger = danger;
            Animated = animated;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal static class ThemeCatalog
    {
        public static readonly List<UiTheme> All = new List<UiTheme>
        {
            new UiTheme("ikun", "IKUN Black", Color.FromArgb(7, 10, 15), Color.FromArgb(12, 18, 27), Color.FromArgb(19, 28, 42), Color.FromArgb(16, 23, 34), Color.FromArgb(22, 31, 45), Color.FromArgb(11, 16, 24), Color.White, Color.FromArgb(166, 181, 202), Color.FromArgb(74, 173, 255), Color.FromArgb(255, 211, 92), Color.FromArgb(246, 91, 109), false),
            new UiTheme("rgb", "RGB Pulse", Color.FromArgb(4, 5, 10), Color.FromArgb(9, 10, 18), Color.FromArgb(15, 17, 30), Color.FromArgb(13, 16, 27), Color.FromArgb(18, 23, 38), Color.FromArgb(8, 11, 20), Color.White, Color.FromArgb(198, 207, 224), Color.FromArgb(255, 64, 148), Color.FromArgb(57, 226, 255), Color.FromArgb(255, 78, 86), true),
            new UiTheme("ice", "Ice Terminal", Color.FromArgb(4, 18, 26), Color.FromArgb(6, 31, 42), Color.FromArgb(9, 45, 60), Color.FromArgb(10, 34, 45), Color.FromArgb(12, 46, 61), Color.FromArgb(8, 27, 36), Color.FromArgb(235, 252, 255), Color.FromArgb(154, 204, 215), Color.FromArgb(76, 230, 255), Color.FromArgb(126, 255, 204), Color.FromArgb(255, 98, 126), false),
            new UiTheme("ember", "Ember Signal", Color.FromArgb(23, 13, 8), Color.FromArgb(40, 19, 10), Color.FromArgb(60, 28, 12), Color.FromArgb(36, 22, 13), Color.FromArgb(52, 31, 16), Color.FromArgb(24, 15, 9), Color.FromArgb(255, 245, 226), Color.FromArgb(226, 185, 138), Color.FromArgb(255, 170, 56), Color.FromArgb(255, 88, 68), Color.FromArgb(255, 82, 92), false)
        };

        public static UiTheme Get(string key)
        {
            foreach (UiTheme theme in All)
            {
                if (string.Equals(theme.Key, key, StringComparison.OrdinalIgnoreCase)) return theme;
            }
            return All[0];
        }
    }

    internal static class IconAssets
    {
        public static string BaseDirectory
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        public static string IconPath
        {
            get { return Path.Combine(BaseDirectory, "源码和资源", "mail-reminder-icon.ico"); }
        }

        public static string LogoPath
        {
            get { return Path.Combine(BaseDirectory, "源码和资源", "mail-reminder-logo.png"); }
        }

        public static Icon LoadIconSafe()
        {
            try
            {
                if (File.Exists(IconPath)) return new Icon(IconPath);
                string local = Path.Combine(BaseDirectory, "mail-reminder-icon.ico");
                return File.Exists(local) ? new Icon(local) : null;
            }
            catch
            {
                return null;
            }
        }

        public static Image LoadLogoSafe()
        {
            try
            {
                string path = File.Exists(LogoPath) ? LogoPath : Path.Combine(BaseDirectory, "mail-reminder-logo.png");
                if (!File.Exists(path)) return null;
                using (Image source = Image.FromFile(path))
                {
                    return new Bitmap(source);
                }
            }
            catch
            {
                return null;
            }
        }
    }

    internal static class NativeGlass
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        public static void TryEnable(Form form, Color tint)
        {
            try
            {
                int dark = 1;
                DwmSetWindowAttribute(form.Handle, 20, ref dark, sizeof(int));
                DwmSetWindowAttribute(form.Handle, 19, ref dark, sizeof(int));

                AccentPolicy accent = new AccentPolicy();
                accent.AccentState = 4;
                accent.AccentFlags = 2;
                accent.GradientColor = ToAbgr(tint);
                int size = Marshal.SizeOf(accent);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(accent, ptr, false);
                    WindowCompositionAttributeData data = new WindowCompositionAttributeData();
                    data.Attribute = 19;
                    data.SizeOfData = size;
                    data.Data = ptr;
                    SetWindowCompositionAttribute(form.Handle, ref data);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            catch
            {
            }
        }

        private static int ToAbgr(Color color)
        {
            return (color.A << 24) | (color.B << 16) | (color.G << 8) | color.R;
        }
    }

    internal static class GlassPainter
    {
        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void PaintGlass(Graphics graphics, Rectangle rect, UiTheme theme, Color accent, int radius, int fillAlpha)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedRect(rect, radius))
            using (LinearGradientBrush fill = new LinearGradientBrush(rect, Color.FromArgb(fillAlpha, Color.White), Color.FromArgb(Math.Max(70, fillAlpha - 42), theme.Surface), LinearGradientMode.ForwardDiagonal))
            using (Pen border = new Pen(Color.FromArgb(150, Color.White), 1))
            using (Pen glow = new Pen(Color.FromArgb(135, accent), 1))
            {
                graphics.FillPath(fill, path);
                graphics.DrawPath(glow, path);
                Rectangle inner = Rectangle.Inflate(rect, -1, -1);
                using (GraphicsPath innerPath = RoundedRect(inner, Math.Max(2, radius - 1)))
                {
                    graphics.DrawPath(border, innerPath);
                }
            }

            Rectangle highlight = new Rectangle(rect.Left + 14, rect.Top + 10, Math.Max(10, rect.Width - 28), Math.Max(8, rect.Height / 3));
            using (GraphicsPath hiPath = RoundedRect(highlight, Math.Max(4, radius - 3)))
            using (LinearGradientBrush hi = new LinearGradientBrush(highlight, Color.FromArgb(75, Color.White), Color.FromArgb(4, Color.White), LinearGradientMode.Vertical))
            {
                graphics.FillPath(hi, hiPath);
            }
        }
    }

    internal sealed class AppSettings
    {
        public string Provider = "qq";
        public string Email = "";
        public string Password = "";
        public string Host = "imap.qq.com";
        public int Port = 993;
        public string Mailbox = "INBOX";
        public string WebmailUrl = "https://mail.qq.com/";
        public int IntervalSeconds = 60;
        public string SoundPath = "";
        public string Theme = "ikun";

        public static string ConfigDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IKUNANCE", "MailReminderBeta"); }
        }

        public static string ConfigPath
        {
            get { return Path.Combine(ConfigDirectory, "settings.cfg"); }
        }

        private static string LegacyConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MailReminderBeta.config"); }
        }

        public static AppSettings Load()
        {
            string path = File.Exists(ConfigPath) ? ConfigPath : LegacyConfigPath;
            AppSettings settings = new AppSettings();
            if (!File.Exists(path)) return settings;

            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                int index = line.IndexOf('=');
                if (index <= 0) continue;
                string key = line.Substring(0, index);
                string value = DecodeValue(line.Substring(index + 1));
                if (key == "Provider") settings.Provider = value;
                if (key == "Email") settings.Email = value;
                if (key == "Password") settings.Password = Decrypt(value);
                if (key == "Host") settings.Host = value;
                if (key == "Port") settings.Port = ToInt(value, 993);
                if (key == "Mailbox") settings.Mailbox = value;
                if (key == "WebmailUrl") settings.WebmailUrl = value;
                if (key == "IntervalSeconds") settings.IntervalSeconds = ToInt(value, 60);
                if (key == "SoundPath") settings.SoundPath = value;
                if (key == "Theme") settings.Theme = value;
            }

            if (!File.Exists(ConfigPath)) settings.Save();
            return settings;
        }

        public void Save()
        {
            Directory.CreateDirectory(ConfigDirectory);
            List<string> lines = new List<string>();
            lines.Add("Provider=" + EncodeValue(Provider));
            lines.Add("Email=" + EncodeValue(Email));
            lines.Add("Password=" + EncodeValue(Encrypt(Password)));
            lines.Add("Host=" + EncodeValue(Host));
            lines.Add("Port=" + EncodeValue(Port.ToString()));
            lines.Add("Mailbox=" + EncodeValue(Mailbox));
            lines.Add("WebmailUrl=" + EncodeValue(WebmailUrl));
            lines.Add("IntervalSeconds=" + EncodeValue(IntervalSeconds.ToString()));
            lines.Add("SoundPath=" + EncodeValue(SoundPath));
            lines.Add("Theme=" + EncodeValue(Theme));
            File.WriteAllLines(ConfigPath, lines.ToArray(), Encoding.UTF8);
        }

        private static int ToInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static string EncodeValue(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? ""));
        }

        private static string DecodeValue(string value)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
            catch { return ""; }
        }

        private static string Encrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            byte[] plain = Encoding.UTF8.GetBytes(value);
            byte[] protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }

        private static string Decrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            try
            {
                byte[] protectedBytes = Convert.FromBase64String(value);
                byte[] plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                return "";
            }
        }
    }

    internal sealed class MessageInfo
    {
        public long Uid;
        public string From = "";
        public string Subject = "";
        public string Date = "";
    }

    internal sealed class MailCheckResult
    {
        public long LatestUid;
        public List<MessageInfo> Messages = new List<MessageInfo>();
    }

    internal sealed class ImapMailClient : IDisposable
    {
        private readonly TcpClient tcp;
        private readonly Stream stream;
        private int tagNumber = 1;

        private ImapMailClient(string host, int port)
        {
            tcp = new TcpClient();
            tcp.ReceiveTimeout = 20000;
            tcp.SendTimeout = 20000;
            tcp.Connect(host, port);
            SslStream ssl = new SslStream(tcp.GetStream(), false);
            try { ssl.AuthenticateAsClient(host, null, SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false); }
            catch { ssl.AuthenticateAsClient(host); }
            stream = ssl;
            ReadLine();
        }

        public static MailCheckResult Fetch(AppSettings settings, int limit)
        {
            using (ImapMailClient client = new ImapMailClient(settings.Host, settings.Port))
            {
                client.EnsureOk(client.Command("LOGIN " + Quote(settings.Email) + " " + Quote(settings.Password)), "登录失败");
                client.EnsureOk(client.Command("EXAMINE " + Quote(settings.Mailbox)), "打开邮箱失败");
                string search = client.Command("UID SEARCH ALL");
                List<long> uids = ParseSearchUids(search);
                MailCheckResult result = new MailCheckResult();
                if (uids.Count == 0)
                {
                    client.Command("LOGOUT");
                    return result;
                }

                result.LatestUid = uids.Max();
                foreach (long uid in uids.OrderByDescending(x => x).Take(limit))
                {
                    result.Messages.Add(client.FetchHeader(uid));
                }
                client.Command("LOGOUT");
                return result;
            }
        }

        private MessageInfo FetchHeader(long uid)
        {
            string response = Command("UID FETCH " + uid + " (BODY.PEEK[HEADER.FIELDS (FROM SUBJECT DATE)])");
            Dictionary<string, string> headers = ParseHeaders(response);
            MessageInfo info = new MessageInfo();
            info.Uid = uid;
            info.From = DecodeHeader(GetHeader(headers, "From"));
            info.Subject = DecodeHeader(GetHeader(headers, "Subject"));
            info.Date = GetHeader(headers, "Date");
            if (string.IsNullOrWhiteSpace(info.Subject)) info.Subject = "(无主题)";
            if (string.IsNullOrWhiteSpace(info.From)) info.From = "(未知发件人)";
            return info;
        }

        private static string GetHeader(Dictionary<string, string> headers, string name)
        {
            string value;
            return headers.TryGetValue(name, out value) ? value.Trim() : "";
        }

        private static Dictionary<string, string> ParseHeaders(string raw)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string current = "";
            foreach (string rawLine in raw.Replace("\r\n", "\n").Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.StartsWith("* ") || line.StartsWith("A") || line == ")" || line.Length == 0) continue;
                if ((line.StartsWith(" ") || line.StartsWith("\t")) && current.Length > 0)
                {
                    headers[current] = headers[current] + " " + line.Trim();
                    continue;
                }
                int colon = line.IndexOf(':');
                if (colon > 0)
                {
                    current = line.Substring(0, colon);
                    headers[current] = line.Substring(colon + 1).Trim();
                }
            }
            return headers;
        }

        private static List<long> ParseSearchUids(string response)
        {
            List<long> uids = new List<long>();
            Match match = Regex.Match(response, @"\* SEARCH(?<uids>[^\r\n]*)", RegexOptions.IgnoreCase);
            if (!match.Success) return uids;
            foreach (string part in match.Groups["uids"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                long uid;
                if (long.TryParse(part.Trim(), out uid)) uids.Add(uid);
            }
            return uids;
        }

        private string Command(string command)
        {
            string tag = "A" + (tagNumber++).ToString("000");
            byte[] data = Encoding.ASCII.GetBytes(tag + " " + command + "\r\n");
            stream.Write(data, 0, data.Length);
            stream.Flush();
            return ReadResponse(tag);
        }

        private string ReadResponse(string tag)
        {
            StringBuilder builder = new StringBuilder();
            while (true)
            {
                string line = ReadLine();
                builder.Append(line);
                int literal = TryGetLiteralSize(line);
                if (literal > 0)
                {
                    byte[] bytes = ReadBytes(literal);
                    builder.Append(Encoding.UTF8.GetString(bytes));
                }
                if (line.StartsWith(tag + " ", StringComparison.OrdinalIgnoreCase)) break;
            }
            return builder.ToString();
        }

        private string ReadLine()
        {
            List<byte> bytes = new List<byte>();
            while (true)
            {
                int b = stream.ReadByte();
                if (b < 0) throw new IOException("邮箱服务器断开连接");
                bytes.Add((byte)b);
                if (b == 10) break;
            }
            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        private byte[] ReadBytes(int count)
        {
            byte[] bytes = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(bytes, offset, count - offset);
                if (read <= 0) throw new IOException("读取邮件头失败");
                offset += read;
            }
            return bytes;
        }

        private static int TryGetLiteralSize(string line)
        {
            Match match = Regex.Match(line, @"\{(?<n>\d+)\}\r?\n$");
            if (!match.Success) return 0;
            int size;
            return int.TryParse(match.Groups["n"].Value, out size) ? size : 0;
        }

        private void EnsureOk(string response, string message)
        {
            if (response.IndexOf(" OK", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException(message + "：" + Regex.Replace(response, @"\s+", " ").Trim());
            }
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static string DecodeHeader(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return Regex.Replace(value, @"=\?([^?]+)\?([bBqQ])\?([^?]*)\?=", delegate(Match match)
            {
                try
                {
                    string charset = match.Groups[1].Value;
                    string mode = match.Groups[2].Value.ToUpperInvariant();
                    string text = match.Groups[3].Value;
                    byte[] bytes = mode == "B" ? Convert.FromBase64String(text) : DecodeQBytes(text);
                    return Encoding.GetEncoding(charset).GetString(bytes);
                }
                catch { return match.Value; }
            });
        }

        private static byte[] DecodeQBytes(string value)
        {
            MemoryStream output = new MemoryStream();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '_')
                {
                    output.WriteByte((byte)' ');
                    continue;
                }
                if (c == '=' && i + 2 < value.Length)
                {
                    int parsed;
                    if (int.TryParse(value.Substring(i + 1, 2), System.Globalization.NumberStyles.HexNumber, null, out parsed))
                    {
                        output.WriteByte((byte)parsed);
                        i += 2;
                        continue;
                    }
                }
                output.WriteByte((byte)c);
            }
            return output.ToArray();
        }

        public void Dispose()
        {
            if (stream != null) stream.Dispose();
            if (tcp != null) tcp.Close();
        }
    }

    internal sealed class SectionPanel : Panel
    {
        public UiTheme Theme;
        public Color Accent;

        public SectionPanel()
        {
            DoubleBuffered = true;
            Padding = new Padding(18);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color accent = Accent.IsEmpty ? (Theme ?? ThemeCatalog.All[0]).Accent : Accent;
            using (Brush fill = new SolidBrush(BackColor))
            using (Pen border = new Pen(Color.FromArgb(112, accent)))
            {
                e.Graphics.FillRectangle(fill, ClientRectangle);
                e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            }
        }
    }

    internal sealed class StatusLight : Control
    {
        public UiTheme Theme;
        public Color Accent;
        public string StatusText = "Idle";

        public StatusLight()
        {
            DoubleBuffered = true;
            Height = 28;
            Width = 180;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UiTheme theme = Theme ?? ThemeCatalog.All[0];
            Color accent = Accent == Color.Empty ? theme.Accent : Accent;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Brush brush = new SolidBrush(theme.Surface2))
            using (Pen pen = new Pen(Color.FromArgb(120, accent)))
            {
                e.Graphics.FillRectangle(brush, 0, 0, Width - 1, Height - 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
            using (Brush dot = new SolidBrush(accent))
            {
                e.Graphics.FillEllipse(dot, 12, 9, 10, 10);
            }
            using (Brush text = new SolidBrush(theme.Text)) e.Graphics.DrawString(StatusText, Font, text, new PointF(30, 6));
        }
    }

    internal sealed class AlertForm : Form
    {
        private readonly AppSettings settings;
        private readonly MessageInfo message;
        private readonly Action onAcknowledged;
        private readonly Timer beepTimer = new Timer();
        private SoundPlayer player;
        private bool acknowledged;

        public AlertForm(AppSettings settings, MessageInfo message, Action onAcknowledged)
        {
            this.settings = settings;
            this.message = message;
            this.onAcknowledged = onAcknowledged;
            BuildUi();
            StartSound();
        }

        private void BuildUi()
        {
            UiTheme theme = ThemeCatalog.Get(settings.Theme);
            Icon icon = IconAssets.LoadIconSafe();
            if (icon != null) Icon = icon;
            Text = "新邮件提醒";
            TopMost = true;
            ShowInTaskbar = true;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            Width = 486;
            Height = 328;
            BackColor = theme.Back;
            ForeColor = theme.Text;
            Font = new Font("Microsoft YaHei UI", 9);
            Paint += delegate(object sender, PaintEventArgs e) { PaintAlertShell(e, theme); };
            Shown += delegate { NativeGlass.TryEnable(this, Color.FromArgb(188, theme.Back)); };

            Panel top = new Panel();
            top.Left = 0;
            top.Top = 0;
            top.Width = Width;
            top.Height = 92;
            top.BackColor = theme.HeaderB;
            top.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(top);

            Label title = new Label();
            title.Text = "NEW MAIL LOCKED";
            title.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.ForeColor = theme.Accent;
            title.Left = 28;
            title.Top = 20;
            title.Width = 340;
            title.Height = 28;
            top.Controls.Add(title);

            Label rule = new Label();
            rule.Text = "必须打开邮箱网页，弹窗和提示音才会停止";
            rule.ForeColor = theme.Accent2;
            rule.Left = 30;
            rule.Top = 54;
            rule.Width = 410;
            rule.Height = 22;
            top.Controls.Add(rule);

            Controls.Add(CreateInfo("发件人", message.From, 108, theme));
            Controls.Add(CreateInfo("主题", message.Subject, 150, theme));
            Controls.Add(CreateInfo("时间", message.Date, 192, theme));

            Button open = new Button();
            open.Text = "查阅邮箱并停止提醒";
            open.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
            open.Left = 30;
            open.Top = 260;
            open.Width = 426;
            open.Height = 44;
            open.BackColor = theme.Accent;
            open.ForeColor = Color.Black;
            open.FlatStyle = FlatStyle.Flat;
            open.FlatAppearance.BorderColor = Color.FromArgb(190, Color.White);
            open.FlatAppearance.BorderSize = 1;
            open.Click += delegate { ConfirmAndOpen(); };
            Controls.Add(open);

            Load += delegate
            {
                Rectangle area = Screen.PrimaryScreen.WorkingArea;
                Left = area.Right - Width - 14;
                Top = area.Bottom - Height - 14;
                Region = new Region(GlassPainter.RoundedRect(new Rectangle(0, 0, Width, Height), 24));
                Activate();
            };
        }

        private void PaintAlertShell(PaintEventArgs e, UiTheme theme)
        {
            Color accent = theme.Animated ? Color.FromArgb(255, 64, 148) : theme.Accent;
            Rectangle shell = new Rectangle(0, 0, Width - 1, Height - 1);
            using (Brush back = new SolidBrush(theme.Back))
            {
                e.Graphics.FillRectangle(back, shell);
            }
            using (GraphicsPath path = GlassPainter.RoundedRect(shell, 24))
            using (Pen edge = new Pen(Color.FromArgb(170, Color.White), 1))
            using (Pen glow = new Pen(Color.FromArgb(150, accent), 2))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(glow, path);
                e.Graphics.DrawPath(edge, path);
            }
            using (Pen topLine = new Pen(Color.FromArgb(115, Color.White), 1))
            {
                e.Graphics.DrawLine(topLine, 28, 1, Width - 28, 1);
            }
            using (LinearGradientBrush rim = new LinearGradientBrush(new Rectangle(0, 0, Width, 18), Color.FromArgb(95, Color.White), Color.Transparent, LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(rim, 12, 2, Width - 24, 18);
            }
        }

        private Label CreateInfo(string label, string value, int top, UiTheme theme)
        {
            Label info = new Label();
            info.Text = label + "  " + value;
            info.Left = 30;
            info.Top = top;
            info.Width = 426;
            info.Height = 34;
            info.AutoEllipsis = true;
            info.ForeColor = theme.Text;
            info.BackColor = theme.Surface;
            info.Padding = new Padding(8, 5, 8, 0);
            return info;
        }

        private void StartSound()
        {
            string path = settings.SoundPath;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    player = new SoundPlayer(path);
                    player.PlayLooping();
                    return;
                }
                catch { player = null; }
            }

            beepTimer.Interval = 850;
            beepTimer.Tick += delegate { SystemSounds.Exclamation.Play(); };
            beepTimer.Start();
            SystemSounds.Exclamation.Play();
        }

        private void StopSound()
        {
            beepTimer.Stop();
            if (player != null)
            {
                player.Stop();
                player.Dispose();
                player = null;
            }
        }

        private void ConfirmAndOpen()
        {
            try { Process.Start(new ProcessStartInfo(settings.WebmailUrl) { UseShellExecute = true }); }
            catch (Exception ex)
            {
                MessageBox.Show("打开邮箱网页失败，提醒不会停止：" + ex.Message, "邮件提醒器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            acknowledged = true;
            StopSound();
            if (onAcknowledged != null) onAcknowledged();
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!acknowledged)
            {
                e.Cancel = true;
                Activate();
                SystemSounds.Exclamation.Play();
                return;
            }
            base.OnFormClosing(e);
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly List<ProviderProfile> providers = new List<ProviderProfile>();
        private readonly Queue<MessageInfo> pendingAlerts = new Queue<MessageInfo>();
        private readonly Timer pollTimer = new Timer();
        private readonly Timer themeTimer = new Timer();
        private readonly List<Label> labels = new List<Label>();
        private readonly List<Button> buttons = new List<Button>();
        private readonly List<SectionPanel> sections = new List<SectionPanel>();
        private ComboBox themeBox;
        private ComboBox providerBox;
        private TextBox emailBox;
        private TextBox passwordBox;
        private TextBox hostBox;
        private TextBox portBox;
        private TextBox mailboxBox;
        private TextBox webmailBox;
        private NumericUpDown intervalBox;
        private TextBox soundBox;
        private Button startButton;
        private Button checkButton;
        private Button testButton;
        private Label statusLabel;
        private Label configLabel;
        private ListView messageList;
        private StatusLight statusLight;
        private SectionPanel settingsPanel;
        private SectionPanel inboxPanel;
        private Panel headerPanel;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool checking;
        private bool monitoring;
        private bool exitRequested;
        private long lastSeenUid;
        private int rgbHue;
        private AlertForm activeAlert;

        public MainForm()
        {
            BuildProviders();
            BuildUi();
            LoadSettings(AppSettings.Load());
            pollTimer.Tick += delegate { CheckMail(true, false, "自动查收"); };
            themeTimer.Interval = 90;
            themeTimer.Tick += delegate
            {
                UiTheme theme = SelectedTheme();
                if (theme.Animated)
                {
                    rgbHue = (rgbHue + 5) % 360;
                    ApplyTheme();
                }
            };
            themeTimer.Start();
            ApplyTheme();
        }

        private void BuildProviders()
        {
            providers.Add(new ProviderProfile { Key = "qq", Name = "QQ邮箱", Host = "imap.qq.com", Port = 993, WebmailUrl = "https://mail.qq.com/" });
            providers.Add(new ProviderProfile { Key = "gmail", Name = "Gmail", Host = "imap.gmail.com", Port = 993, WebmailUrl = "https://mail.google.com/mail/u/0/#inbox" });
            providers.Add(new ProviderProfile { Key = "outlook", Name = "Outlook", Host = "outlook.office365.com", Port = 993, WebmailUrl = "https://outlook.live.com/mail/0/inbox" });
            providers.Add(new ProviderProfile { Key = "163", Name = "网易163", Host = "imap.163.com", Port = 993, WebmailUrl = "https://mail.163.com/" });
            providers.Add(new ProviderProfile { Key = "custom", Name = "自定义 IMAP", Host = "", Port = 993, WebmailUrl = "" });
        }

        private void BuildUi()
        {
            Text = "IKUNANCE Mail Sentinel";
            Width = 1080;
            Height = 720;
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9);
            Icon icon = IconAssets.LoadIconSafe();
            if (icon != null) Icon = icon;
            InitializeTrayIcon(icon);
            Shown += delegate { NativeGlass.TryEnable(this, Color.FromArgb(178, SelectedTheme().Back)); };

            headerPanel = new Panel();
            headerPanel.Left = 0;
            headerPanel.Top = 0;
            headerPanel.Width = ClientSize.Width;
            headerPanel.Height = 96;
            headerPanel.BackColor = ThemeCatalog.All[0].HeaderB;
            headerPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            headerPanel.Paint += delegate(object sender, PaintEventArgs e) { PaintHeader(e); };
            Controls.Add(headerPanel);

            Image logo = IconAssets.LoadLogoSafe();
            if (logo != null)
            {
                PictureBox logoBox = new PictureBox();
                logoBox.Image = logo;
                logoBox.SizeMode = PictureBoxSizeMode.Zoom;
                logoBox.Left = 26;
                logoBox.Top = 16;
                logoBox.Width = 64;
                logoBox.Height = 64;
                headerPanel.Controls.Add(logoBox);
            }

            Label title = AddLabel(headerPanel, "IKUNANCE MAIL SENTINEL", 104, 20, 420, 30, 18, true);
            title.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            AddLabel(headerPanel, "本地邮箱监听台 / 新邮件强制确认提醒", 106, 54, 420, 22, 9, false);

            statusLight = new StatusLight();
            statusLight.Left = 532;
            statusLight.Top = 30;
            statusLight.StatusText = "Idle";
            headerPanel.Controls.Add(statusLight);

            AddLabel(headerPanel, "主题", 776, 34, 42, 22, 9, false);
            themeBox = new ComboBox();
            themeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (UiTheme theme in ThemeCatalog.All) themeBox.Items.Add(theme);
            themeBox.Left = 820;
            themeBox.Top = 30;
            themeBox.Width = 190;
            themeBox.Height = 28;
            themeBox.SelectedIndexChanged += delegate
            {
                ApplyTheme();
                SaveCurrentSettings(false, false);
            };
            headerPanel.Controls.Add(themeBox);

            settingsPanel = CreateSection(24, 118, 480, 505);
            Controls.Add(settingsPanel);
            AddLabel(settingsPanel, "MAILBOX CONTROL", 20, 18, 240, 24, 12, true);
            AddLabel(settingsPanel, "绑定邮箱、保存授权码、自定义提示音", 20, 42, 330, 20, 9, false);

            int left = 20;
            int top = 78;
            int row = 42;
            providerBox = new ComboBox();
            providerBox.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (ProviderProfile provider in providers) providerBox.Items.Add(provider);
            providerBox.SelectedIndexChanged += delegate { ApplyProviderDefaults(); };
            AddField(settingsPanel, "邮箱类型", providerBox, left, top, 104, 300);

            emailBox = new TextBox();
            AddField(settingsPanel, "邮箱账号", emailBox, left, top + row, 104, 300);

            passwordBox = new TextBox();
            passwordBox.UseSystemPasswordChar = true;
            AddField(settingsPanel, "授权码", passwordBox, left, top + row * 2, 104, 300);

            hostBox = new TextBox();
            AddField(settingsPanel, "IMAP 主机", hostBox, left, top + row * 3, 104, 300);

            portBox = new TextBox();
            AddField(settingsPanel, "端口", portBox, left, top + row * 4, 104, 112);

            mailboxBox = new TextBox();
            AddField(settingsPanel, "邮箱目录", mailboxBox, left + 212, top + row * 4, 82, 126);

            webmailBox = new TextBox();
            AddField(settingsPanel, "查阅链接", webmailBox, left, top + row * 5, 104, 300);

            intervalBox = new NumericUpDown();
            intervalBox.Minimum = 15;
            intervalBox.Maximum = 3600;
            intervalBox.Value = 60;
            AddField(settingsPanel, "监听间隔", intervalBox, left, top + row * 6, 104, 112);

            soundBox = new TextBox();
            AddField(settingsPanel, "提示音 WAV", soundBox, left, top + row * 7, 104, 218);

            Button browseSound = MakeButton("选择", left + 332, top + row * 7 - 2, 72, 31);
            browseSound.Click += delegate { BrowseSound(); };
            settingsPanel.Controls.Add(browseSound);

            int buttonTop = top + row * 8 + 10;
            Button saveButton = MakeButton("保存配置", 20, buttonTop, 104, 38);
            saveButton.Click += delegate { SaveCurrentSettings(true, true); };
            settingsPanel.Controls.Add(saveButton);

            testButton = MakeButton("测试查收", 132, buttonTop, 104, 38);
            testButton.Click += delegate { CheckMail(false, false, "测试查收"); };
            settingsPanel.Controls.Add(testButton);

            checkButton = MakeButton("手动查收", 244, buttonTop, 104, 38);
            checkButton.Click += delegate { CheckMail(false, false, "手动查收"); };
            settingsPanel.Controls.Add(checkButton);

            startButton = MakeButton("开始监听", 356, buttonTop, 104, 38);
            startButton.Click += delegate { ToggleMonitor(); };
            settingsPanel.Controls.Add(startButton);

            inboxPanel = CreateSection(526, 118, 518, 505);
            inboxPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            Controls.Add(inboxPanel);
            AddLabel(inboxPanel, "LIVE INBOX", 20, 18, 240, 24, 12, true);
            AddLabel(inboxPanel, "列表展示最近邮件；只有监听开始之后的新邮件会触发提醒", 20, 42, 460, 20, 9, false);

            Button testAlert = MakeButton("测试强制弹窗", 360, 18, 128, 34);
            testAlert.Click += delegate { ShowAlert(new MessageInfo { Uid = DateTime.Now.Ticks, From = "测试提醒", Subject = "必须点击查阅邮箱才会停止", Date = DateTime.Now.ToString() }); };
            inboxPanel.Controls.Add(testAlert);

            messageList = new ListView();
            messageList.Left = 20;
            messageList.Top = 78;
            messageList.Width = 470;
            messageList.Height = 350;
            messageList.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            messageList.View = View.Details;
            messageList.FullRowSelect = true;
            messageList.GridLines = false;
            messageList.BorderStyle = BorderStyle.None;
            messageList.Columns.Add("UID", 82);
            messageList.Columns.Add("发件人", 150);
            messageList.Columns.Add("主题", 170);
            messageList.Columns.Add("时间", 160);
            messageList.DoubleClick += delegate { OpenWebmail(); };
            inboxPanel.Controls.Add(messageList);

            statusLabel = AddLabel(this, "未监听。请保存配置后开始监听。", 28, 640, 720, 28, 9, false);
            statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            configLabel = AddLabel(this, "配置保存位置：" + AppSettings.ConfigPath, 28, 664, 960, 22, 8, false);
            configLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            SetDoubleBuffered(this);
            Resize += delegate { headerPanel.Invalidate(); };
            Resize += OnMainFormResize;
            FormClosing += OnMainFormClosing;
        }

        private Label AddLabel(Control parent, string text, int left, int top, int width, int height, int size, bool strong)
        {
            Label label = new Label();
            label.Text = text;
            label.Left = left;
            label.Top = top;
            label.Width = width;
            label.Height = height;
            label.Font = new Font(strong ? "Segoe UI" : "Microsoft YaHei UI", size, strong ? FontStyle.Bold : FontStyle.Regular);
            label.AutoEllipsis = true;
            label.BackColor = parent is Panel ? parent.BackColor : BackColor;
            parent.Controls.Add(label);
            labels.Add(label);
            return label;
        }

        private SectionPanel CreateSection(int left, int top, int width, int height)
        {
            SectionPanel panel = new SectionPanel();
            panel.Left = left;
            panel.Top = top;
            panel.Width = width;
            panel.Height = height;
            panel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            sections.Add(panel);
            return panel;
        }

        private void AddField(Control parent, string labelText, Control control, int left, int top, int labelWidth, int controlWidth)
        {
            AddLabel(parent, labelText, left, top + 5, labelWidth, 24, 9, false);
            control.Left = left + labelWidth;
            control.Top = top;
            control.Width = controlWidth;
            control.Height = 29;
            parent.Controls.Add(control);
        }

        private Button MakeButton(string text, int left, int top, int width, int height)
        {
            Button button = new Button();
            button.Text = text;
            button.Left = left;
            button.Top = top;
            button.Width = width;
            button.Height = height;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
            buttons.Add(button);
            return button;
        }

        private void PaintHeader(PaintEventArgs e)
        {
            UiTheme theme = SelectedTheme();
            Color accent = DynamicAccent(theme, 0);
            using (LinearGradientBrush brush = new LinearGradientBrush(headerPanel.ClientRectangle, theme.HeaderA, theme.HeaderB, LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(brush, headerPanel.ClientRectangle);
            }
            using (Pen line = new Pen(Color.FromArgb(150, accent), 2))
            {
                e.Graphics.DrawLine(line, 0, headerPanel.Height - 2, headerPanel.Width, headerPanel.Height - 2);
            }
            using (Pen thin = new Pen(Color.FromArgb(70, DynamicAccent(theme, 120)), 1))
            {
                for (int x = 0; x < headerPanel.Width; x += 44)
                {
                    e.Graphics.DrawLine(thin, x, 0, x + 30, headerPanel.Height);
                }
            }
        }

        private UiTheme SelectedTheme()
        {
            UiTheme selected = themeBox == null ? null : themeBox.SelectedItem as UiTheme;
            return selected ?? ThemeCatalog.All[0];
        }

        private Color DynamicAccent(UiTheme theme, int offset)
        {
            return theme.Animated ? ColorFromHsv((rgbHue + offset) % 360, 0.92, 1.0) : (offset == 0 ? theme.Accent : theme.Accent2);
        }

        private void ApplyTheme()
        {
            UiTheme theme = SelectedTheme();
            Color accent = DynamicAccent(theme, 0);
            Color accent2 = DynamicAccent(theme, 120);
            BackColor = theme.Back;
            ForeColor = theme.Text;
            if (headerPanel != null)
            {
                headerPanel.BackColor = theme.HeaderB;
                headerPanel.Invalidate();
            }

            foreach (SectionPanel section in sections)
            {
                section.Theme = theme;
                section.Accent = accent;
                section.BackColor = theme.Surface;
                section.Invalidate();
            }

            foreach (Label label in labels)
            {
                label.ForeColor = label.Font.Bold ? accent : theme.Muted;
                label.BackColor = label.Parent == null ? theme.Back : label.Parent.BackColor;
            }

            foreach (Button button in buttons)
            {
                button.BackColor = theme.Surface2;
                button.ForeColor = theme.Text;
                button.FlatAppearance.BorderColor = Color.FromArgb(160, accent);
            }

            if (startButton != null)
            {
                startButton.BackColor = monitoring ? theme.Danger : accent;
                startButton.ForeColor = theme.Animated ? Color.Black : theme.Text;
            }

            ApplyInputTheme(this, theme);
            if (messageList != null)
            {
                messageList.BackColor = theme.Input;
                messageList.ForeColor = theme.Text;
            }
            if (statusLabel != null) statusLabel.ForeColor = accent2;
            if (configLabel != null) configLabel.ForeColor = Color.FromArgb(130, theme.Muted);
            if (statusLight != null)
            {
                statusLight.Theme = theme;
                statusLight.Accent = monitoring ? accent : theme.Muted;
                statusLight.Invalidate();
            }
            if (IsHandleCreated)
            {
                NativeGlass.TryEnable(this, Color.FromArgb(178, theme.Back));
            }
        }

        private void ApplyInputTheme(Control parent, UiTheme theme)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is TextBox || control is ComboBox || control is NumericUpDown)
                {
                    control.BackColor = theme.Input;
                    control.ForeColor = theme.Text;
                }
                ApplyInputTheme(control, theme);
            }
        }

        private static Color ColorFromHsv(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);
            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));
            if (hi == 0) return Color.FromArgb(255, v, t, p);
            if (hi == 1) return Color.FromArgb(255, q, v, p);
            if (hi == 2) return Color.FromArgb(255, p, v, t);
            if (hi == 3) return Color.FromArgb(255, p, q, v);
            if (hi == 4) return Color.FromArgb(255, t, p, v);
            return Color.FromArgb(255, v, p, q);
        }

        private void LoadSettings(AppSettings settings)
        {
            themeBox.SelectedItem = ThemeCatalog.Get(settings.Theme);
            providerBox.SelectedItem = providers.FirstOrDefault(p => p.Key == settings.Provider) ?? providers[0];
            emailBox.Text = settings.Email;
            passwordBox.Text = settings.Password;
            hostBox.Text = settings.Host;
            portBox.Text = settings.Port.ToString();
            mailboxBox.Text = settings.Mailbox;
            webmailBox.Text = settings.WebmailUrl;
            intervalBox.Value = Math.Min(intervalBox.Maximum, Math.Max(intervalBox.Minimum, settings.IntervalSeconds));
            soundBox.Text = settings.SoundPath;
        }

        private void ApplyProviderDefaults()
        {
            ProviderProfile provider = providerBox.SelectedItem as ProviderProfile;
            if (provider == null || provider.Key == "custom") return;
            hostBox.Text = provider.Host;
            portBox.Text = provider.Port.ToString();
            webmailBox.Text = provider.WebmailUrl;
            if (string.IsNullOrWhiteSpace(mailboxBox.Text)) mailboxBox.Text = "INBOX";
        }

        private void BrowseSound()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "选择循环提示音";
                dialog.Filter = "WAV 音频|*.wav";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    soundBox.Text = dialog.FileName;
                    SaveCurrentSettings(false, false);
                }
            }
        }

        private AppSettings ReadSettings()
        {
            AppSettings settings = new AppSettings();
            ProviderProfile provider = providerBox.SelectedItem as ProviderProfile;
            settings.Provider = provider == null ? "custom" : provider.Key;
            settings.Email = emailBox.Text.Trim();
            settings.Password = passwordBox.Text;
            settings.Host = hostBox.Text.Trim();
            settings.Port = ParsePort(portBox.Text);
            settings.Mailbox = string.IsNullOrWhiteSpace(mailboxBox.Text) ? "INBOX" : mailboxBox.Text.Trim();
            settings.WebmailUrl = webmailBox.Text.Trim();
            settings.IntervalSeconds = (int)intervalBox.Value;
            settings.SoundPath = soundBox.Text.Trim();
            settings.Theme = SelectedTheme().Key;
            return settings;
        }

        private int ParsePort(string text)
        {
            int port;
            return int.TryParse(text, out port) ? port : 993;
        }

        private bool ValidateSettings(AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password) || string.IsNullOrWhiteSpace(settings.Host))
            {
                MessageBox.Show("请填写邮箱账号、授权码和 IMAP 主机。QQ 等邮箱通常需要授权码，不是网页登录密码。", "邮件提醒器", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (string.IsNullOrWhiteSpace(settings.WebmailUrl))
            {
                MessageBox.Show("请填写查阅链接，例如 https://mail.qq.com/ 。", "邮件提醒器", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        private void SaveCurrentSettings(bool showMessage, bool requireValid)
        {
            AppSettings settings = ReadSettings();
            if (requireValid && !ValidateSettings(settings)) return;
            settings.Save();
            SetStatus("配置已保存。下次打开会自动恢复。");
            if (showMessage) MessageBox.Show("已保存到：" + AppSettings.ConfigPath, "邮件提醒器", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ToggleMonitor()
        {
            if (monitoring)
            {
                pollTimer.Stop();
                monitoring = false;
                startButton.Text = "开始监听";
                statusLight.StatusText = "Idle";
                ApplyTheme();
                SetStatus("已停止监听。");
                return;
            }

            AppSettings settings = ReadSettings();
            if (!ValidateSettings(settings)) return;
            settings.Save();
            pollTimer.Interval = settings.IntervalSeconds * 1000;
            pendingAlerts.Clear();
            lastSeenUid = 0;
            CheckMail(false, true, "启动监听");
        }

        private void CheckMail(bool alertNew, bool baseline, string action)
        {
            if (checking)
            {
                SetStatus("正在查收，稍等。");
                return;
            }

            AppSettings settings = ReadSettings();
            if (!ValidateSettings(settings)) return;
            settings.Save();
            checking = true;
            SetButtonsEnabled(false);
            statusLight.StatusText = "Checking";
            ApplyTheme();
            SetStatus(action + "中...");

            Task.Factory.StartNew(delegate { return ImapMailClient.Fetch(settings, 20); })
                .ContinueWith(delegate(Task<MailCheckResult> task)
                {
                    BeginInvoke(new Action(delegate
                    {
                        checking = false;
                        SetButtonsEnabled(true);
                        if (task.Exception != null)
                        {
                            statusLight.StatusText = monitoring ? "Watching" : "Idle";
                            ApplyTheme();
                            SetStatus("查收失败：" + task.Exception.GetBaseException().Message);
                            return;
                        }
                        HandleCheckResult(task.Result, alertNew, baseline, action);
                    }));
                });
        }

        private void HandleCheckResult(MailCheckResult result, bool alertNew, bool baseline, string action)
        {
            PopulateMessages(result.Messages);
            if (baseline)
            {
                lastSeenUid = result.LatestUid;
                monitoring = true;
                pollTimer.Interval = ((int)intervalBox.Value) * 1000;
                pollTimer.Start();
                startButton.Text = "停止监听";
                statusLight.StatusText = "Watching";
                ApplyTheme();
                SetStatus("监听中。当前最新 UID：" + lastSeenUid + "。只提醒之后新收到的邮件。");
                return;
            }

            if (lastSeenUid == 0)
            {
                lastSeenUid = result.LatestUid;
                statusLight.StatusText = monitoring ? "Watching" : "Idle";
                ApplyTheme();
                SetStatus(action + "完成。最新 UID：" + lastSeenUid + "。");
                return;
            }

            List<MessageInfo> fresh = result.Messages.Where(m => m.Uid > lastSeenUid).OrderBy(m => m.Uid).ToList();
            if (result.LatestUid > lastSeenUid) lastSeenUid = result.LatestUid;
            if (alertNew && fresh.Count > 0)
            {
                foreach (MessageInfo message in fresh) pendingAlerts.Enqueue(message);
                ShowNextPendingAlert();
                statusLight.StatusText = "Alert";
                ApplyTheme();
                SetStatus("发现 " + fresh.Count + " 封新邮件，提醒已触发。");
                return;
            }

            statusLight.StatusText = monitoring ? "Watching" : "Idle";
            ApplyTheme();
            SetStatus(action + "完成，没有新邮件。最新 UID：" + lastSeenUid + "。");
        }

        private void PopulateMessages(List<MessageInfo> messages)
        {
            messageList.Items.Clear();
            foreach (MessageInfo message in messages)
            {
                ListViewItem item = new ListViewItem(message.Uid.ToString());
                item.SubItems.Add(message.From);
                item.SubItems.Add(message.Subject);
                item.SubItems.Add(message.Date);
                messageList.Items.Add(item);
            }
        }

        private void ShowAlert(MessageInfo message)
        {
            pendingAlerts.Enqueue(message);
            ShowNextPendingAlert();
        }

        private void ShowNextPendingAlert()
        {
            if (activeAlert != null || pendingAlerts.Count == 0) return;
            MessageInfo next = pendingAlerts.Dequeue();
            activeAlert = new AlertForm(ReadSettings(), next, delegate
            {
                activeAlert = null;
                statusLight.StatusText = monitoring ? "Watching" : "Idle";
                ApplyTheme();
                ShowNextPendingAlert();
            });
            activeAlert.Show();
        }

        private void OpenWebmail()
        {
            AppSettings settings = ReadSettings();
            try { Process.Start(new ProcessStartInfo(settings.WebmailUrl) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("打开邮箱网页失败：" + ex.Message, "邮件提醒器", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void InitializeTrayIcon(Icon icon)
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("显示窗口", null, delegate { RestoreFromTray(); });
            trayMenu.Items.Add("隐藏到托盘", null, delegate { HideToTray(); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("退出", null, delegate { ExitFromTray(); });

            trayIcon = new NotifyIcon();
            trayIcon.Text = "IKUNANCE Mail Sentinel";
            trayIcon.Icon = icon ?? SystemIcons.Application;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { RestoreFromTray(); };
        }

        private void OnMainFormResize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                HideToTray();
            }
        }

        private void OnMainFormClosing(object sender, FormClosingEventArgs e)
        {
            SaveCurrentSettings(false, false);
            if (!exitRequested && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
            }
        }

        private void HideToTray()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Hide();
            ShowInTaskbar = false;
            if (trayIcon != null)
            {
                trayIcon.Visible = true;
            }
        }

        private void RestoreFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitFromTray()
        {
            exitRequested = true;
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
            }
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }
            if (trayMenu != null)
            {
                trayMenu.Dispose();
                trayMenu = null;
            }
            base.OnFormClosed(e);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            testButton.Enabled = enabled;
            checkButton.Enabled = enabled;
            startButton.Enabled = enabled;
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = DateTime.Now.ToString("HH:mm:ss") + "  " + text;
        }

        private static void SetDoubleBuffered(Control control)
        {
            try
            {
                PropertyInfo property = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                property.SetValue(control, true, null);
            }
            catch { }
        }
    }

    internal static class SettingsSmokeTest
    {
        public static void Run()
        {
            AppSettings settings = new AppSettings();
            settings.Provider = "qq";
            settings.Email = "smoke@example.com";
            settings.Password = "secret";
            settings.Host = "imap.qq.com";
            settings.Port = 993;
            settings.Mailbox = "INBOX";
            settings.WebmailUrl = "https://mail.qq.com/";
            settings.IntervalSeconds = 45;
            settings.Theme = "rgb";
            settings.Save();
            AppSettings loaded = AppSettings.Load();
            if (loaded.Email != settings.Email || loaded.Password != settings.Password || loaded.Theme != settings.Theme || loaded.IntervalSeconds != settings.IntervalSeconds)
            {
                throw new InvalidOperationException("settings smoke test failed");
            }
        }
    }
}
