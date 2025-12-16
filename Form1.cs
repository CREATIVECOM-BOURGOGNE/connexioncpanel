using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ConnexionCpanel
{
    public partial class Form1 : Form
    {
        private class Account
        {
            public string Client { get; set; } = "";
            public string Url { get; set; } = "";
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public string ApiToken { get; set; } = "";
        }

        private TextBox txtClient, txtUrl, txtUser, txtPass, txtToken;
        private DataGridView dgv;
        private Button btnAdd, btnUpdate, btnDelete, btnTest;
        private List<Account> accounts = new();
        private string? masterPassword;
        private readonly string dataPath;

        private readonly Color ConnectButtonColor = Color.FromArgb(229, 57, 53); // #E53935
        private readonly Color ConnectTextColor = Color.White;

        public Form1()
        {
            dataPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? ".", "accounts.dat");
            InitializeComponent();

            // ✅ Icône (à activer via Propriétés > Application > Icône)
            // this.Icon = new Icon(Path.Combine(Application.StartupPath, "icone.ico"));

            SetupUI();
            LoadOrPromptPassword();
        }

        private void SetupUI()
        {
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(850, 500);

            var inputPanel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 120, RowCount = 2, ColumnCount = 3, Padding = new Padding(10) };
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            txtClient = new TextBox();
            txtUrl = new TextBox();
            txtUser = new TextBox();
            txtPass = new TextBox { UseSystemPasswordChar = true };
            txtToken = new TextBox();

            // ✅ Interdiction des espaces (saisie + collage)
            txtPass.KeyPress += (_, e) => { if (e.KeyChar == ' ') e.Handled = true; };
            txtToken.KeyPress += (_, e) => { if (e.KeyChar == ' ') e.Handled = true; };
            txtPass.TextChanged += (_, _) => txtPass.Text = txtPass.Text.Replace(" ", "");
            txtToken.TextChanged += (_, _) => txtToken.Text = txtToken.Text.Replace(" ", "");

            // ✅ Raccourci clavier Entrée
            txtClient.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    if (dgv.SelectedRows.Count > 0)
                        UpdateSelected();
                    else
                        SaveNew();
                    e.SuppressKeyPress = true;
                }
            };

            txtUrl.Leave += (_, _) => { if (!txtUrl.Text.Trim().StartsWith("https://")) MessageBox.Show("URL doit commencer par https://"); };
            txtUrl.KeyPress += (_, e) => { if (e.KeyChar == ' ') e.Handled = true; };

            inputPanel.Controls.Add(MakeGroup("Client", txtClient), 0, 0);
            inputPanel.Controls.Add(MakeGroup("URL cPanel", txtUrl), 1, 0);
            inputPanel.Controls.Add(MakeGroup("Identifiant", txtUser), 2, 0);
            inputPanel.Controls.Add(MakeGroup("Mot de passe", txtPass), 0, 1);
            inputPanel.Controls.Add(MakeGroup("Jeton API", txtToken), 1, 1);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 8.5F),
                RowTemplate = { Height = 28 }
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Client", HeaderText = "Client", DataPropertyName = "Client" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "URL", DataPropertyName = "Url" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Username", HeaderText = "Identifiant", DataPropertyName = "Username" });
            var passCol = new DataGridViewTextBoxColumn { Name = "Password", HeaderText = "Mot de passe", DataPropertyName = "Password" };
            dgv.Columns.Add(passCol);
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ApiToken", HeaderText = "Jeton", DataPropertyName = "ApiToken" });

            var btnCol = new DataGridViewButtonColumn
            {
                Name = "Connect",
                HeaderText = "",
                UseColumnTextForButtonValue = false,
                Width = 68
            };
            dgv.Columns.Add(btnCol);

            dgv.CellFormatting += (_, e) =>
            {
                if (e.ColumnIndex == dgv.Columns["Password"].Index && e.Value != null)
                    e.Value = new string('*', e.Value.ToString().Length);
            };

            dgv.CellClick += (s, e) =>
            {
                if (e.ColumnIndex == dgv.Columns["Connect"].Index && e.RowIndex >= 0)
                {
                    var account = accounts[e.RowIndex];
                    ConnectToAccount(account);
                }
            };

            dgv.CellPainting += (s, e) =>
            {
                if (e.ColumnIndex == dgv.Columns["Connect"].Index && e.RowIndex >= 0)
                {
                    e.PaintBackground(e.ClipBounds, true);
                    var rect = new Rectangle(
                        e.CellBounds.X + 6,
                        e.CellBounds.Y + 4,
                        e.CellBounds.Width - 12,
                        e.CellBounds.Height - 8
                    );

                    using (var path = RoundedRectangle(rect, 4))
                    using (var brush = new SolidBrush(ConnectButtonColor))
                    using (var pen = new Pen(Color.FromArgb(210, 30, 20), 1))
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        e.Graphics.FillPath(brush, path);
                        e.Graphics.DrawPath(pen, path);
                    }

                    using (var font = new Font("Segoe UI Semibold", 9.5F))
                    using (var brush = new SolidBrush(ConnectTextColor))
                    {
                        string text = "Conn. →";
                        var size = e.Graphics.MeasureString(text, font);
                        float x = rect.X + (rect.Width - size.Width) / 2;
                        float y = rect.Y + (rect.Height - size.Height) / 2 - 1;
                        e.Graphics.DrawString(text, font, brush, x, y);
                    }

                    e.Handled = true;
                }
            };

            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.Columns["Connect"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgv.Columns["Connect"].Width = 68;

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(10, 5, 10, 5) };

            btnAdd = new Button { Text = "➕ Ajouter", Width = 100 };
            btnUpdate = new Button { Text = "✏️ Modifier", Width = 100, Enabled = false };
            btnDelete = new Button { Text = "🗑️ Supprimer", Width = 100, Enabled = false };
            btnTest = new Button { Text = "🔍 Tester jeton", Width = 120, Enabled = false };

            btnAdd.Click += (_, _) => SaveNew();
            btnUpdate.Click += (_, _) => UpdateSelected();
            btnDelete.Click += (_, _) => DeleteSelected();
            btnTest.Click += (_, _) => TestToken();

            foreach (var b in new[] { btnAdd, btnUpdate, btnDelete, btnTest })
                btnPanel.Controls.Add(b);

            // ✅ SelectionChanged corrigé : remplissage automatique
            dgv.SelectionChanged += (_, _) =>
            {
                bool hasSelection = dgv.SelectedRows.Count > 0;
                btnUpdate.Enabled = btnDelete.Enabled = btnTest.Enabled = hasSelection;

                if (hasSelection)
                {
                    var row = dgv.SelectedRows[0];
                    txtClient.Text = row.Cells["Client"].Value?.ToString() ?? "";
                    txtUrl.Text = row.Cells["Url"].Value?.ToString() ?? "";
                    txtUser.Text = row.Cells["Username"].Value?.ToString() ?? "";
                    txtPass.Text = row.Cells["Password"].Value?.ToString() ?? "";
                    txtToken.Text = row.Cells["ApiToken"].Value?.ToString() ?? "";
                }
                else
                {
                    Clear();
                }
            };

            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.Controls.Add(inputPanel, 0, 0);
            mainLayout.Controls.Add(dgv, 0, 1);
            mainLayout.Controls.Add(btnPanel, 0, 2);

            Controls.Add(mainLayout);
        }

        private GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(bounds); return path; }
            path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
            path.AddArc(bounds.X + bounds.Width - radius, bounds.Y, radius, radius, 270, 90);
            path.AddArc(bounds.X + bounds.Width - radius, bounds.Y + bounds.Height - radius, radius, radius, 0, 90);
            path.AddArc(bounds.X, bounds.Y + bounds.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        private GroupBox MakeGroup(string text, Control ctrl)
        {
            var g = new GroupBox { Text = text, Padding = new Padding(3) };
            ctrl.Dock = DockStyle.Fill;
            g.Controls.Add(ctrl);
            return g;
        }

        private void Clear() => Array.ForEach(new[] { txtClient, txtUrl, txtUser, txtPass, txtToken }, t => t.Text = "");

        private void LoadOrPromptPassword()
        {
            int attempts = 0;
            while (attempts < 3)
            {
                var pwd = Prompt("Mot de passe maître", "Entrez le mot de passe :");
                if (pwd == null) Environment.Exit(0);
                try
                {
                    accounts = LoadAccounts(pwd);
                    masterPassword = pwd;
                    BindGrid();
                    if (masterPassword != null && !File.Exists(dataPath))
                        SaveAccounts(masterPassword);
                    return;
                }
                catch
                {
                    attempts++;
                }
            }
            SecureErase(dataPath);
            MessageBox.Show("Fichier effacé après 3 échecs.");
            Environment.Exit(0);
        }

        private string? Prompt(string title, string msg)
        {
            using var f = new Form { Text = title, Width = 300, Height = 180, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
            var l = new Label { Left = 20, Top = 20, Text = msg, AutoSize = true };
            var t = new TextBox { Left = 20, Top = 50, Width = 200, UseSystemPasswordChar = true };
            var showBtn = new Button { Text = "👁️", Left = 225, Top = 49, Width = 35, FlatStyle = FlatStyle.Popup };
            var ok = new Button { Text = "OK", Left = 100, Top = 120, Width = 80 };
            var cancel = new Button { Text = "Annuler", Left = 190, Top = 120, Width = 80 };

            showBtn.MouseDown += (_, _) => t.UseSystemPasswordChar = false;
            showBtn.MouseUp += (_, _) => t.UseSystemPasswordChar = true;

            ok.Click += (_, _) => { f.DialogResult = DialogResult.OK; f.Close(); };
            cancel.Click += (_, _) => { f.DialogResult = DialogResult.Cancel; f.Close(); };
            t.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) ok.PerformClick(); };

            f.Controls.AddRange(new Control[] { l, t, showBtn, ok, cancel });
            return f.ShowDialog() == DialogResult.OK ? t.Text : null;
        }

        private List<Account> LoadAccounts(string pwd)
        {
            if (!File.Exists(dataPath)) return new List<Account>();
            var d = File.ReadAllBytes(dataPath);
            using var ms = new MemoryStream(d);
            using var r = new BinaryReader(ms);
            var salt = r.ReadBytes(r.ReadInt32());
            var enc = r.ReadBytes(r.ReadInt32());
            var json = Decrypt(salt, enc, pwd);
            return System.Text.Json.JsonSerializer.Deserialize<List<Account>>(json) ?? new List<Account>();
        }

        private void SaveAccounts(string pwd)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(accounts);
            var (salt, enc) = Encrypt(json, pwd);
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(salt.Length);
            w.Write(salt);
            w.Write(enc.Length);
            w.Write(enc);
            var data = ms.ToArray();

            try
            {
                if (File.Exists(dataPath))
                {
                    File.SetAttributes(dataPath, FileAttributes.Normal);
                    File.Delete(dataPath);
                }
                File.WriteAllBytes(dataPath, data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Échec sauvegarde :\n{ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static (byte[], byte[]) Encrypt(string t, string p)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            using var aes = Aes.Create();
            aes.Key = DeriveKey(p, salt);
            aes.GenerateIV();
            using var ms = new MemoryStream();
            ms.Write(aes.IV);
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs)) sw.Write(t);
            return (salt, ms.ToArray());
        }

        private static string Decrypt(byte[] s, byte[] d, string p)
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey(p, s);
            var iv = new byte[16];
            Buffer.BlockCopy(d, 0, iv, 0, 16);
            aes.IV = iv;
            using var ms = new MemoryStream(d, 16, d.Length - 16);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

        private static byte[] DeriveKey(string p, byte[] s) => new Rfc2898DeriveBytes(p, s, 100000, HashAlgorithmName.SHA256).GetBytes(32);

        private void SecureErase(string f)
        {
            if (!File.Exists(f)) return;
            try
            {
                var len = new FileInfo(f).Length;
                using var s = File.OpenWrite(f);
                var b = new byte[1024];
                for (int i = 0; i < 3; i++)
                {
                    RandomNumberGenerator.Fill(b);
                    for (long w = 0; w < len; w += b.Length) s.Write(b, 0, (int)Math.Min(b.Length, len - w));
                    s.Position = 0;
                }
            }
            catch { }
            File.Delete(f);
        }

        // ✅ BindGrid corrigé : tri visuel seulement
        private void BindGrid()
        {
            var sorted = accounts.OrderBy(a => a.Client).ToList();
            dgv.DataSource = new BindingSource(sorted, null);
            dgv.ClearSelection();
        }

        private void SaveNew()
        {
            var url = txtUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("URL obligatoire.");
                return;
            }
            url = url.Replace(" ", "").ToLowerInvariant();
            if (!url.StartsWith("https://"))
            {
                MessageBox.Show("URL doit commencer par https://");
                return;
            }

            EnsurePassword();
            if (string.IsNullOrEmpty(masterPassword)) return;

            accounts.Add(new Account
            {
                Client = txtClient.Text.Trim(),
                Url = url,
                Username = txtUser.Text.Trim(),
                Password = txtPass.Text.Trim(),
                ApiToken = txtToken.Text.Trim()
            });

            // ✅ Tri persistant avant sauvegarde
            accounts = accounts.OrderBy(a => a.Client).ToList();

            SaveAccounts(masterPassword);
            BindGrid();
            Clear();
            txtClient.Focus();

            MessageBox.Show("✅ Compte enregistré.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ✅ UpdateSelected corrigé : modification partielle
        private void UpdateSelected()
        {
            if (dgv.SelectedRows.Count == 0) return;

            var row = dgv.SelectedRows[0];
            var clientName = row.Cells["Client"].Value?.ToString();
            var index = accounts.FindIndex(a => a.Client == clientName);
            if (index == -1)
            {
                MessageBox.Show("Compte introuvable.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ✅ Mise à jour SÉLECTIVE
            var account = accounts[index];
            account.Client = txtClient.Text.Trim();
            account.Url = txtUrl.Text.Trim().Replace(" ", "").ToLowerInvariant();
            account.Username = txtUser.Text.Trim();
            account.Password = txtPass.Text.Trim();
            account.ApiToken = txtToken.Text.Trim();

            // ✅ Validation URL
            if (string.IsNullOrWhiteSpace(account.Url))
            {
                MessageBox.Show("URL obligatoire.");
                return;
            }
            if (!account.Url.StartsWith("https://"))
            {
                MessageBox.Show("URL doit commencer par https://");
                return;
            }

            EnsurePassword();
            if (string.IsNullOrEmpty(masterPassword)) return;

            // ✅ Tri persistant
            accounts = accounts.OrderBy(a => a.Client).ToList();

            SaveAccounts(masterPassword);
            BindGrid();

            // ✅ Re-sélectionner la ligne mise à jour
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (dgv.Rows[i].Cells["Client"].Value?.ToString() == account.Client)
                {
                    dgv.ClearSelection();
                    dgv.Rows[i].Selected = true;
                    break;
                }
            }

            MessageBox.Show("✅ Compte mis à jour.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteSelected()
        {
            if (dgv.SelectedRows.Count == 0) return;
            if (MessageBox.Show("Supprimer ?", "Confirmer", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                EnsurePassword();
                if (string.IsNullOrEmpty(masterPassword)) return;

                var row = dgv.SelectedRows[0];
                var clientName = row.Cells["Client"].Value?.ToString();
                var index = accounts.FindIndex(a => a.Client == clientName);
                if (index != -1)
                {
                    accounts.RemoveAt(index);
                }

                // ✅ Tri persistant
                accounts = accounts.OrderBy(a => a.Client).ToList();

                SaveAccounts(masterPassword);
                BindGrid();
            }
        }

        private void EnsurePassword()
        {
            if (string.IsNullOrEmpty(masterPassword))
            {
                var pwd = Prompt("Mot de passe maître", "Mot de passe requis :");
                if (pwd != null) masterPassword = pwd;
            }
        }

        private void TestToken()
        {
            if (dgv.SelectedRows.Count == 0) return;
            var a = accounts[dgv.SelectedRows[0].Index];
            if (string.IsNullOrWhiteSpace(a.ApiToken))
            {
                MessageBox.Show("Pas de jeton");
                return;
            }
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var req = WebRequest.Create($"{a.Url.TrimEnd('/')}/execute/SSL/list_certs");
                req.Method = "POST";
                req.Headers["Authorization"] = $"cpanel {a.Username}:{a.ApiToken}";
                using var _ = req.GetResponse();
                MessageBox.Show("✅ Jeton valide", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { MessageBox.Show("❌ Échec du test", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ConnectToAccount(Account a)
        {
            try
            {
                string url;
                if (a.Url.Contains("o2switch", StringComparison.OrdinalIgnoreCase))
                {
                    url = $"{a.Url.TrimEnd('/')}/login/?user={Uri.EscapeDataString(a.Username)}&pass={Uri.EscapeDataString(a.Password)}" +
                          (!string.IsNullOrEmpty(a.ApiToken) ? $"&token={Uri.EscapeDataString(a.ApiToken)}" : "");
                }
                else
                {
                    var html = $@"<html><body onload='document.f.submit()'><form name='f' method='post' action='{a.Url.TrimEnd('/')}/login/'><input type='hidden' name='user' value='{WebUtility.HtmlEncode(a.Username)}'/><input type='hidden' name='pass' value='{WebUtility.HtmlEncode(a.Password)}'/>{(string.IsNullOrEmpty(a.ApiToken) ? "" : $"<input type='hidden' name='token' value='{WebUtility.HtmlEncode(a.ApiToken)}'/>")}</form></body></html>";
                    var f = Path.GetTempFileName() + ".html";
                    File.WriteAllText(f, html);
                    url = new Uri(f).AbsoluteUri;
                }
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d’ouvrir : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ Effacement sécurisé à la fermeture
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (masterPassword != null)
            {
                var arr = masterPassword.ToCharArray();
                Array.Clear(arr, 0, arr.Length);
                masterPassword = null;
            }
            txtPass.Text = txtToken.Text = ""; // ✅ Efface les champs sensibles
            base.OnFormClosing(e);
        }
    }
}