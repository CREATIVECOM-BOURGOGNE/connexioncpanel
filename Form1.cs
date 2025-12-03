using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConnexionCpanel
{
    public partial class Form1 : Form
    {
        private List<Account> _accounts = new List<Account>();
        private AccountManager _accountManager;
        private readonly string _masterPassword;

        public Form1(string masterPassword)
        {
            _masterPassword = masterPassword ?? throw new ArgumentNullException(nameof(masterPassword));
            InitializeComponent();
            CheckCompatibility();
            LoadAccounts();
            SetupDataGridView();
            SetupEventHandlers();

            this.Load += (s, e) =>
            {
                dgvAccounts.ClearSelection();
                btnAdd.Visible = true;
                btnSave.Visible = false;
                btnUpdate.Visible = false;
                UpdateButtonStates();
            };
        }

        private void CheckCompatibility()
        {
            var os = Environment.OSVersion;
            if (os.Platform != PlatformID.Win32NT || os.Version < new Version(6, 1))
            {
                MessageBox.Show("Ce logiciel requiert Windows 7 ou supérieur.", "Incompatible", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        private void LoadAccounts()
        {
            _accountManager = new AccountManager(_masterPassword);
            _accounts = _accountManager.LoadAccounts();
            _accounts.Sort((a, b) => string.Compare(a.Client, b.Client, StringComparison.OrdinalIgnoreCase));
            dgvAccounts.DataSource = new BindingSource(_accounts, null);
            dgvAccounts.ClearSelection();
            UpdateButtonStates();
        }

        private void SetupDataGridView()
        {
            dgvAccounts.AutoGenerateColumns = false;
            dgvAccounts.Columns.Clear();
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Client", DataPropertyName = "Client", Width = 120 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "URL", DataPropertyName = "CpanelUrl", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Identifiant", DataPropertyName = "Username", Width = 100 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Token API", DataPropertyName = "ApiToken", Width = 150 });

            foreach (DataGridViewColumn col in dgvAccounts.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            dgvAccounts.SelectionChanged += (s, e) => UpdateButtonStates();
            dgvAccounts.CellDoubleClick += (s, e) => ConnectSelectedAccount();
        }

        private void SetupEventHandlers()
        {
            btnAdd.Click += (s, e) => PrepareAdd();
            btnSave.Click += (s, e) => SaveNewAccount();
            btnUpdate.Click += (s, e) => UpdateAccount();
            btnEdit.Click += (s, e) => PrepareEdit();
            btnDelete.Click += (s, e) => DeleteAccount();
            btnTestToken.Click += async (s, e) => await TestApiTokenAsync();
            btnConnect.Click += (s, e) => ConnectSelectedAccount();

            foreach (var tb in new[] { txtUrl, txtPassword, txtApiToken })
            {
                tb.KeyPress += (s, e) =>
                {
                    if (e.KeyChar == ' ')
                    {
                        e.Handled = true;
                    }
                };

                tb.TextChanged += (s, e) =>
                {
                    string original = tb.Text;
                    string cleaned = original.Replace(" ", "");
                    if (original != cleaned)
                    {
                        int pos = tb.SelectionStart;
                        tb.Text = cleaned;
                        tb.SelectionStart = Math.Min(pos, cleaned.Length);
                    }
                };
            }

            txtUrl.Leave += (s, e) =>
            {
                string url = txtUrl.Text.Trim();
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    txtUrl.Text = "https://" + url.Substring(7).TrimStart('/');
                }
            };
        }

        private void UpdateButtonStates()
        {
            var hasSelection = dgvAccounts.SelectedRows.Count > 0;
            bool isAdding = btnSave.Visible;
            bool isEditing = btnUpdate.Visible;

            btnAdd.Visible = !isAdding && !isEditing;
            btnSave.Visible = isAdding;
            btnUpdate.Visible = isEditing;

            bool interactive = !isAdding && !isEditing;
            btnEdit.Enabled = hasSelection && interactive;
            btnDelete.Enabled = hasSelection && interactive;
            btnTestToken.Enabled = hasSelection && interactive;
            btnConnect.Enabled = hasSelection && interactive;

            if (hasSelection && interactive)
            {
                var acc = dgvAccounts.SelectedRows[0].DataBoundItem as Account;
                if (acc != null)
                {
                    txtClient.Text = acc.Client;
                    txtUrl.Text = acc.CpanelUrl;
                    txtUsername.Text = acc.Username;
                    txtPassword.Text = acc.Password;
                    txtApiToken.Text = acc.ApiToken;
                }
            }
        }

        private void PrepareAdd()
        {
            ClearFields();
            btnAdd.Visible = false;
            btnSave.Visible = true;
            btnUpdate.Visible = false;
            btnEdit.Enabled = false;
            btnDelete.Enabled = false;
            btnTestToken.Enabled = false;
            btnConnect.Enabled = false;
        }

        private void SaveNewAccount()
        {
            string client = txtClient.Text.Trim();
            string url = txtUrl.Text;
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string apiToken = txtApiToken.Text.Trim();

            if (url.Contains(' ') || password.Contains(' ') || apiToken.Contains(' '))
            {
                MessageBox.Show("Les espaces sont interdits dans l'URL, le mot de passe et le jeton API.", "Caractère invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("L'URL cPanel ne peut pas être vide.", "URL manquante", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url.Substring(7).TrimStart('/');
            }
            else if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("L'URL cPanel doit commencer par 'https://' (ex: https://exemple.com:2083).", "URL invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
            {
                url = "https://" + url.Substring(8).TrimStart('/');
            }

            url = url.Replace(":///", "://").TrimEnd('/');

            if (string.IsNullOrWhiteSpace(client) || string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Veuillez remplir au moins 'Client', 'URL' et 'Identifiant'.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var acc = new Account
            {
                Client = client,
                CpanelUrl = url,
                Username = username,
                Password = password,
                ApiToken = apiToken
            };

            _accounts.Add(acc);

            // ✅ APPEL OBLIGATOIRE
            SaveAccounts();

            btnAdd.Visible = true;
            btnSave.Visible = false;
            dgvAccounts.ClearSelection();
            ClearFields();
        }

        private void PrepareEdit()
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            var acc = dgvAccounts.SelectedRows[0].DataBoundItem as Account;
            if (acc == null) return;

            txtClient.Text = acc.Client;
            txtUrl.Text = acc.CpanelUrl;
            txtUsername.Text = acc.Username;
            txtPassword.Text = acc.Password;
            txtApiToken.Text = acc.ApiToken;

            btnAdd.Visible = false;
            btnSave.Visible = false;
            btnUpdate.Visible = true;
            UpdateButtonStates();
        }

        private void UpdateAccount()
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            var acc = dgvAccounts.SelectedRows[0].DataBoundItem as Account;
            if (acc == null) return;

            string client = txtClient.Text.Trim();
            string url = txtUrl.Text;
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string apiToken = txtApiToken.Text.Trim();

            if (url.Contains(' ') || password.Contains(' ') || apiToken.Contains(' '))
            {
                MessageBox.Show("Les espaces sont interdits dans l'URL, le mot de passe et le jeton API.", "Caractère invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("L'URL cPanel ne peut pas être vide.", "URL manquante", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url.Substring(7).TrimStart('/');
            }
            else if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("L'URL cPanel doit commencer par 'https://' (ex: https://exemple.com:2083).", "URL invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
            {
                url = "https://" + url.Substring(8).TrimStart('/');
            }

            url = url.Replace(":///", "://").TrimEnd('/');

            if (string.IsNullOrWhiteSpace(client) || string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Veuillez remplir au moins 'Client', 'URL' et 'Identifiant'.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            acc.Client = client;
            acc.CpanelUrl = url;
            acc.Username = username;
            acc.Password = password;
            acc.ApiToken = apiToken;

            // ✅ APPEL OBLIGATOIRE
            SaveAccounts();

            dgvAccounts.ClearSelection();
            ClearFields();
        }

        private void DeleteAccount()
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            _accounts.RemoveAt(dgvAccounts.SelectedRows[0].Index);

            // ✅ APPEL OBLIGATOIRE
            SaveAccounts();
        }

        // ✅ MÉTHODE MANQUANTE AJOUTÉE ICI
        private void SaveAccounts()
        {
            // Tri A→Z
            _accounts.Sort((a, b) => string.Compare(a.Client, b.Client, StringComparison.OrdinalIgnoreCase));

            // Sauvegarde via AccountManager
            _accountManager.SaveAccounts(_accounts);

            // Mise à jour de l'interface
            string selectedKey = null;
            if (dgvAccounts.SelectedRows.Count > 0)
            {
                var selectedAcc = dgvAccounts.SelectedRows[0].DataBoundItem as Account;
                if (selectedAcc != null)
                    selectedKey = $"{selectedAcc.Client}||{selectedAcc.CpanelUrl}||{selectedAcc.Username}";
            }

            dgvAccounts.DataSource = new BindingSource(_accounts, null);
            UpdateButtonStates();

            // Restauration de la sélection
            if (!string.IsNullOrEmpty(selectedKey))
            {
                for (int i = 0; i < dgvAccounts.Rows.Count; i++)
                {
                    var acc = dgvAccounts.Rows[i].DataBoundItem as Account;
                    if (acc != null && $"{acc.Client}||{acc.CpanelUrl}||{acc.Username}" == selectedKey)
                    {
                        dgvAccounts.ClearSelection();
                        dgvAccounts.Rows[i].Selected = true;
                        break;
                    }
                }
            }
        }

        private void ClearFields()
        {
            txtClient.Clear();
            txtUrl.Clear();
            txtUsername.Clear();
            txtPassword.Clear();
            txtApiToken.Clear();
            dgvAccounts.ClearSelection();
            UpdateButtonStates();
        }

        private async Task TestApiTokenAsync()
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            var acc = dgvAccounts.SelectedRows[0].DataBoundItem as Account;
            if (string.IsNullOrWhiteSpace(acc?.ApiToken))
            {
                MessageBox.Show("Aucun jeton API fourni.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"cpanel {acc.Username}:{acc.ApiToken}");
                    var endpoint = $"{acc.CpanelUrl.TrimEnd('/')}/execute/SSL/get_csr_domains";
                    var response = await client.GetAsync(endpoint);

                    if (response.IsSuccessStatusCode)
                        MessageBox.Show("✅ Jeton API valide.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        MessageBox.Show($"❌ Échec : {response.StatusCode}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de connexion : {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConnectSelectedAccount()
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            var acc = dgvAccounts.SelectedRows[0].DataBoundItem as Account;
            if (acc == null) return;

            if (acc.CpanelUrl.Contains("o2switch", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(acc.Username) || string.IsNullOrWhiteSpace(acc.Password))
                {
                    MessageBox.Show("Pour o2switch, l'identifiant et le mot de passe sont obligatoires.", "Données manquantes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string token = !string.IsNullOrWhiteSpace(acc.ApiToken) ? acc.ApiToken : "";
                string url = $"{acc.CpanelUrl.TrimEnd('/')}/login/?" +
                             $"user={Uri.EscapeDataString(acc.Username)}&" +
                             $"pass={Uri.EscapeDataString(acc.Password)}" +
                             (!string.IsNullOrEmpty(token) ? $"&token={Uri.EscapeDataString(token)}" : "");

                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    return;
                }
                catch
                {
                    MessageBox.Show("Impossible d'ouvrir le navigateur.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            try
            {
                string template = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Connexion cPanel</title>
</head>
<body>
    <form id='loginForm' action='{0}' method='post'>
        <input type='hidden' name='username' value='{1}'>
        <input type='hidden' name='password' value='{2}'>
        <input type='hidden' name='token' value=''>
    </form>
    <script>
        setTimeout(() => {{
            document.getElementById('loginForm').submit();
        }}, 100);
    </script>
</body>
</html>";

                string loginUrl = acc.CpanelUrl.TrimEnd('/') + "/login/";
                string html = string.Format(template,
                    loginUrl,
                    acc.Username.Replace("'", "&#x27;").Replace("\"", "&quot;"),
                    acc.Password.Replace("'", "&#x27;").Replace("\"", "&quot;")
                );

                string tempFile = Path.Combine(Path.GetTempPath(), $"cpanel_login_{Guid.NewGuid()}.html");
                File.WriteAllText(tempFile, html, Encoding.UTF8);

                string fileUrl = $"file:///{tempFile.Replace("\\", "/")}";
                Process.Start(new ProcessStartInfo(fileUrl) { UseShellExecute = true });

                Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    try { File.Delete(tempFile); } catch { }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Échec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            txtPassword.Text = "";
            txtApiToken.Text = "";
            base.OnFormClosed(e);
        }
    }

    internal partial class PasswordDialog : Form
    {
        public string Password => txtPassword.Text;

        public PasswordDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();

            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Text = "Mot de passe maître :";
            this.txtPassword.Location = new System.Drawing.Point(15, 40);
            this.txtPassword.Size = new System.Drawing.Size(360, 23);
            this.txtPassword.UseSystemPasswordChar = true;
            this.btnOK.Location = new System.Drawing.Point(217, 80);
            this.btnOK.Size = new System.Drawing.Size(75, 30);
            this.btnOK.Text = "OK";
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCancel.Location = new System.Drawing.Point(298, 80);
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.Text = "Annuler";
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 122);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "Sécurité";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label1;
    }
}