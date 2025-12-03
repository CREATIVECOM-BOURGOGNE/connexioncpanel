namespace ConnexionCpanel
{
    partial class Form1
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
            dgvAccounts = new DataGridView();
            panelForm = new Panel();
            btnConnect = new Button();
            btnTestToken = new Button();
            btnDelete = new Button();
            btnEdit = new Button();
            btnUpdate = new Button();
            btnSave = new Button();
            btnAdd = new Button();
            tableLayoutPanel1 = new TableLayoutPanel();
            label1 = new Label();
            txtClient = new TextBox();
            label2 = new Label();
            txtUrl = new TextBox();
            label3 = new Label();
            txtUsername = new TextBox();
            label4 = new Label();
            txtPassword = new TextBox();
            label5 = new Label();
            txtApiToken = new TextBox();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)dgvAccounts).BeginInit();
            panelForm.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvAccounts
            // 
            dgvAccounts.AllowUserToAddRows = false;
            dgvAccounts.AllowUserToDeleteRows = false;
            dgvAccounts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAccounts.Dock = DockStyle.Fill;
            dgvAccounts.Location = new Point(0, 0);
            dgvAccounts.Name = "dgvAccounts";
            dgvAccounts.ReadOnly = true;
            dgvAccounts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvAccounts.Size = new Size(854, 301);
            dgvAccounts.TabIndex = 0;
            // 
            // panelForm
            // 
            panelForm.Controls.Add(btnConnect);
            panelForm.Controls.Add(btnTestToken);
            panelForm.Controls.Add(btnDelete);
            panelForm.Controls.Add(btnEdit);
            panelForm.Controls.Add(btnUpdate);
            panelForm.Controls.Add(btnSave);
            panelForm.Controls.Add(btnAdd);
            panelForm.Controls.Add(tableLayoutPanel1);
            panelForm.Dock = DockStyle.Bottom;
            panelForm.Location = new Point(0, 301);
            panelForm.Name = "panelForm";
            panelForm.Size = new Size(854, 180);
            panelForm.TabIndex = 1;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(639, 145);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(100, 25);
            btnConnect.TabIndex = 12;
            btnConnect.Text = "Connecter";
            btnConnect.UseVisualStyleBackColor = true;
            // 
            // btnTestToken
            // 
            btnTestToken.Location = new Point(533, 145);
            btnTestToken.Name = "btnTestToken";
            btnTestToken.Size = new Size(100, 25);
            btnTestToken.TabIndex = 11;
            btnTestToken.Text = "Tester jeton";
            btnTestToken.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(427, 145);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(100, 25);
            btnDelete.TabIndex = 10;
            btnDelete.Text = "Supprimer";
            btnDelete.UseVisualStyleBackColor = true;
            // 
            // btnEdit
            // 
            btnEdit.Location = new Point(321, 145);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(100, 25);
            btnEdit.TabIndex = 9;
            btnEdit.Text = "Modifier";
            btnEdit.UseVisualStyleBackColor = true;
            // 
            // btnUpdate
            // 
            btnUpdate.Location = new Point(215, 145);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(100, 25);
            btnUpdate.TabIndex = 8;
            btnUpdate.Text = "Mettre à jour";
            btnUpdate.UseVisualStyleBackColor = true;
            btnUpdate.Visible = false;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(109, 145);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(100, 25);
            btnSave.TabIndex = 7;
            btnSave.Text = "Enregistrer";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Visible = false;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(3, 145);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(100, 25);
            btnAdd.TabIndex = 6;
            btnAdd.Text = "Ajouter";
            btnAdd.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(label1, 0, 0);
            tableLayoutPanel1.Controls.Add(txtClient, 1, 0);
            tableLayoutPanel1.Controls.Add(label2, 0, 1);
            tableLayoutPanel1.Controls.Add(txtUrl, 1, 1);
            tableLayoutPanel1.Controls.Add(label3, 0, 2);
            tableLayoutPanel1.Controls.Add(txtUsername, 1, 2);
            tableLayoutPanel1.Controls.Add(label4, 0, 3);
            tableLayoutPanel1.Controls.Add(txtPassword, 1, 3);
            tableLayoutPanel1.Controls.Add(label5, 0, 4);
            tableLayoutPanel1.Controls.Add(txtApiToken, 1, 4);
            tableLayoutPanel1.Location = new Point(12, 12);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 5;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanel1.Size = new Size(640, 125);
            tableLayoutPanel1.TabIndex = 5;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Location = new Point(3, 5);
            label1.Name = "label1";
            label1.Size = new Size(44, 15);
            label1.TabIndex = 0;
            label1.Text = "Client :";
            // 
            // txtClient
            // 
            txtClient.Anchor = AnchorStyles.Left;
            txtClient.Location = new Point(103, 3);
            txtClient.Name = "txtClient";
            txtClient.Size = new Size(200, 23);
            txtClient.TabIndex = 5;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Left;
            label2.AutoSize = true;
            label2.Location = new Point(3, 30);
            label2.Name = "label2";
            label2.Size = new Size(72, 15);
            label2.TabIndex = 1;
            label2.Text = "URL cPanel :";
            // 
            // txtUrl
            // 
            txtUrl.Anchor = AnchorStyles.Left;
            txtUrl.Location = new Point(103, 28);
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(400, 23);
            txtUrl.TabIndex = 6;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Left;
            label3.AutoSize = true;
            label3.Location = new Point(3, 55);
            label3.Name = "label3";
            label3.Size = new Size(67, 15);
            label3.TabIndex = 2;
            label3.Text = "Identifiant :";
            // 
            // txtUsername
            // 
            txtUsername.Anchor = AnchorStyles.Left;
            txtUsername.Location = new Point(103, 53);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(200, 23);
            txtUsername.TabIndex = 7;
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Left;
            label4.AutoSize = true;
            label4.Location = new Point(3, 80);
            label4.Name = "label4";
            label4.Size = new Size(83, 15);
            label4.TabIndex = 3;
            label4.Text = "Mot de passe :";
            // 
            // txtPassword
            // 
            txtPassword.Anchor = AnchorStyles.Left;
            txtPassword.Location = new Point(103, 78);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(200, 23);
            txtPassword.TabIndex = 8;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Left;
            label5.AutoSize = true;
            label5.Location = new Point(3, 105);
            label5.Name = "label5";
            label5.Size = new Size(62, 15);
            label5.TabIndex = 4;
            label5.Text = "Jeton API :";
            // 
            // txtApiToken
            // 
            txtApiToken.Anchor = AnchorStyles.Left;
            txtApiToken.Location = new Point(103, 103);
            txtApiToken.Name = "txtApiToken";
            txtApiToken.Size = new Size(400, 23);
            txtApiToken.TabIndex = 9;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip1.Location = new Point(0, 481);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(854, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(28, 17);
            toolStripStatusLabel1.Text = "Prêt";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(854, 503);
            Controls.Add(dgvAccounts);
            Controls.Add(panelForm);
            Controls.Add(statusStrip1);
            Name = "Form1";
            Text = "Connexion cPanel — Portable & Sécurisé";
            ((System.ComponentModel.ISupportInitialize)dgvAccounts).EndInit();
            panelForm.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.DataGridView dgvAccounts;
        private System.Windows.Forms.Panel panelForm;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtClient;
        private System.Windows.Forms.TextBox txtUrl;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.TextBox txtApiToken;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnTestToken;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
    }
}