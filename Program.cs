using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace ConnexionCpanel
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            string masterPassword = null;
            int failedAttempts = 0;
            const int MAX_FAILED = 3;

            while (failedAttempts < MAX_FAILED)
            {
                using (var dialog = new PasswordDialog())
                {
                    var result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        return; // ✅ Annulation → sortie immédiate
                    }

                    var pwd = dialog.Password;
                    if (string.IsNullOrWhiteSpace(pwd))
                    {
                        MessageBox.Show("Le mot de passe ne peut pas être vide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    try
                    {
                        var manager = new AccountManager(pwd);
                        manager.LoadAccounts();
                        masterPassword = pwd;
                        break;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        failedAttempts++;
                        if (failedAttempts >= MAX_FAILED)
                        {
                            AccountManager.SecureEraseFile("accounts.dat");
                            MessageBox.Show($"Trop de tentatives incorrectes ({MAX_FAILED}).\nLe fichier sécurisé a été effacé.", "Accès refusé", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            return;
                        }
                        MessageBox.Show($"Mot de passe incorrect (tentative {failedAttempts}/{MAX_FAILED}).", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        var rep = MessageBox.Show(
                            "Le fichier de données est illisible.\nDémarrer avec une base vide ?",
                            "Fichier corrompu",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning
                        );
                        if (rep == DialogResult.Yes)
                        {
                            masterPassword = pwd;
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(masterPassword))
            {
                return;
            }

            Application.Run(new Form1(masterPassword));
        }
    }

    public class Account
    {
        public string Client { get; set; }
        public string CpanelUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ApiToken { get; set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Client)
            && Uri.IsWellFormedUriString(CpanelUrl, UriKind.Absolute)
            && !string.IsNullOrWhiteSpace(Username);
    }

    public class AccountManager
    {
        private const string FILE_PATH = "accounts.dat";
        private readonly string _masterPassword;

        public AccountManager(string masterPassword)
        {
            _masterPassword = masterPassword ?? throw new ArgumentNullException(nameof(masterPassword));
        }

        public List<Account> LoadAccounts()
        {
            string[] pathsToTry = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FILE_PATH),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConnexionCpanel", FILE_PATH)
            };

            foreach (string filePath in pathsToTry)
            {
                if (File.Exists(filePath))
                {
                    try
                    {
                        var encrypted = File.ReadAllBytes(filePath);
                        var json = Decrypt(encrypted, _masterPassword);
                        var accounts = JsonSerializer.Deserialize<List<Account>>(json) ?? new List<Account>();
                        LogDebug($"✅ accounts.dat chargé depuis : {filePath}");
                        return accounts;
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"⚠️ Échec chargement {filePath} : {ex.Message}");
                    }
                }
            }

            LogDebug("ℹ️ Aucun accounts.dat trouvé → nouvelle base");
            return new List<Account>();
        }

        public void SaveAccounts(List<Account> accounts)
        {
            try
            {
                // ✅ Tentative 1 : dossier de l'exécutable
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(exeDir, FILE_PATH);

                try
                {
                    var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
                    var encrypted = Encrypt(json, _masterPassword);
                    File.WriteAllBytes(filePath, encrypted);
                    LogDebug($"✅ accounts.dat sauvegardé dans : {filePath}");
                    return;
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogDebug($"❌ Accès refusé à {filePath} : {ex.Message}");
                }

                // ✅ Tentative 2 : AppData (garanti accessible)
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConnexionCpanel");
                Directory.CreateDirectory(appData);
                filePath = Path.Combine(appData, FILE_PATH);

                var json2 = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
                var encrypted2 = Encrypt(json2, _masterPassword);
                File.WriteAllBytes(filePath, encrypted2);
                LogDebug($"✅ accounts.dat sauvegardé dans AppData : {filePath}");
            }
            catch (Exception ex)
            {
                string msg = $"❌ ÉCHEC TOTAL sauvegarde : {ex.Message}\n{ex.StackTrace}";
                LogDebug(msg);
                MessageBox.Show(msg, "Erreur critique", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        public static void SecureEraseFile(string fileName)
        {
            string[] paths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConnexionCpanel", fileName)
            };

            foreach (string filePath in paths)
            {
                if (!File.Exists(filePath)) continue;

                try
                {
                    var length = new FileInfo(filePath).Length;
                    var buffer = new byte[length];
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                    {
                        for (int pass = 0; pass < 3; pass++)
                        {
                            RandomNumberGenerator.Fill(buffer);
                            fs.Position = 0;
                            fs.Write(buffer, 0, buffer.Length);
                            fs.Flush();
                        }
                    }
                    File.Delete(filePath);
                    LogDebug($"🗑️ {fileName} effacé : {filePath}");
                }
                catch (Exception ex)
                {
                    LogDebug($"⚠️ Échec effacement {filePath} : {ex.Message}");
                }
            }
        }

        private static byte[] Encrypt(string plainText, string password)
        {
            const int KEY_SIZE = 256;
            const int IV_SIZE = 128;
            const int ITERATIONS = 100_000;

            using var aes = Aes.Create();
            aes.KeySize = KEY_SIZE;
            aes.BlockSize = IV_SIZE;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            var key = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                ITERATIONS,
                HashAlgorithmName.SHA256,
                KEY_SIZE / 8);

            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            ms.Write(salt, 0, salt.Length);
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            return ms.ToArray();
        }

        private static string Decrypt(byte[] cipherData, string password)
        {
            const int KEY_SIZE = 256;
            const int IV_SIZE = 128;
            const int ITERATIONS = 100_000;

            if (cipherData.Length < 32)
                throw new UnauthorizedAccessException("Données corrompues.");

            var salt = cipherData.Take(16).ToArray();
            var iv = cipherData.Skip(16).Take(16).ToArray();
            var cipher = cipherData.Skip(32).ToArray();

            var key = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                ITERATIONS,
                HashAlgorithmName.SHA256,
                KEY_SIZE / 8);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);

            try
            {
                return sr.ReadToEnd();
            }
            catch (CryptographicException)
            {
                throw new UnauthorizedAccessException("Mot de passe incorrect.");
            }
        }

        private static void LogDebug(string message)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
                File.AppendAllLines(logPath, new[] { $"[{DateTime.Now:HH:mm:ss}] {message}" });
            }
            catch { /* silencieux */ }
        }
    }
}