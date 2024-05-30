namespace FujitsuCDU.Replenishment
{
    partial class frmReplenish
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grpreplenishOption = new System.Windows.Forms.GroupBox();
            this.pnlControl = new System.Windows.Forms.Panel();
            this.pnlCancel = new System.Windows.Forms.Panel();
            this.btnAddCash = new System.Windows.Forms.Button();
            this.btnResetCash = new System.Windows.Forms.Button();
            this.btnCashPosition = new System.Windows.Forms.Button();
            this.btnDone = new System.Windows.Forms.Button();
            this.grpreplenishOption.SuspendLayout();
            this.pnlControl.SuspendLayout();
            this.pnlCancel.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpreplenishOption
            // 
            this.grpreplenishOption.Controls.Add(this.pnlCancel);
            this.grpreplenishOption.Controls.Add(this.pnlControl);
            this.grpreplenishOption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpreplenishOption.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpreplenishOption.Location = new System.Drawing.Point(0, 0);
            this.grpreplenishOption.Name = "grpreplenishOption";
            this.grpreplenishOption.Size = new System.Drawing.Size(299, 334);
            this.grpreplenishOption.TabIndex = 0;
            this.grpreplenishOption.TabStop = false;
            this.grpreplenishOption.Text = "Replenish";
            // 
            // pnlControl
            // 
            this.pnlControl.Controls.Add(this.btnCashPosition);
            this.pnlControl.Controls.Add(this.btnResetCash);
            this.pnlControl.Controls.Add(this.btnAddCash);
            this.pnlControl.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlControl.Location = new System.Drawing.Point(3, 23);
            this.pnlControl.Name = "pnlControl";
            this.pnlControl.Size = new System.Drawing.Size(293, 221);
            this.pnlControl.TabIndex = 0;
            // 
            // pnlCancel
            // 
            this.pnlCancel.Controls.Add(this.btnDone);
            this.pnlCancel.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCancel.Location = new System.Drawing.Point(3, 244);
            this.pnlCancel.Name = "pnlCancel";
            this.pnlCancel.Size = new System.Drawing.Size(293, 88);
            this.pnlCancel.TabIndex = 1;
            // 
            // btnAddCash
            // 
            this.btnAddCash.Location = new System.Drawing.Point(44, 25);
            this.btnAddCash.Name = "btnAddCash";
            this.btnAddCash.Size = new System.Drawing.Size(197, 35);
            this.btnAddCash.TabIndex = 0;
            this.btnAddCash.Text = "Add Cash";
            this.btnAddCash.UseVisualStyleBackColor = true;
            this.btnAddCash.Click += new System.EventHandler(this.btnAddCash_Click);
            // 
            // btnResetCash
            // 
            this.btnResetCash.Location = new System.Drawing.Point(44, 95);
            this.btnResetCash.Name = "btnResetCash";
            this.btnResetCash.Size = new System.Drawing.Size(197, 35);
            this.btnResetCash.TabIndex = 1;
            this.btnResetCash.Text = "Reset Cash";
            this.btnResetCash.UseVisualStyleBackColor = true;
            this.btnResetCash.Click += new System.EventHandler(this.btnResetCash_Click);
            // 
            // btnCashPosition
            // 
            this.btnCashPosition.Location = new System.Drawing.Point(44, 165);
            this.btnCashPosition.Name = "btnCashPosition";
            this.btnCashPosition.Size = new System.Drawing.Size(197, 35);
            this.btnCashPosition.TabIndex = 2;
            this.btnCashPosition.Text = "Cash Position";
            this.btnCashPosition.UseVisualStyleBackColor = true;
            this.btnCashPosition.Click += new System.EventHandler(this.btnCashPosition_Click);
            // 
            // btnDone
            // 
            this.btnDone.Location = new System.Drawing.Point(201, 39);
            this.btnDone.Name = "btnDone";
            this.btnDone.Size = new System.Drawing.Size(75, 35);
            this.btnDone.TabIndex = 0;
            this.btnDone.Text = "Done";
            this.btnDone.UseVisualStyleBackColor = true;
            this.btnDone.Click += new System.EventHandler(this.btnDone_Click);
            // 
            // frmReplenish
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(299, 334);
            this.Controls.Add(this.grpreplenishOption);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmReplenish";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.grpreplenishOption.ResumeLayout(false);
            this.pnlControl.ResumeLayout(false);
            this.pnlCancel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpreplenishOption;
        private System.Windows.Forms.Panel pnlCancel;
        private System.Windows.Forms.Panel pnlControl;
        private System.Windows.Forms.Button btnDone;
        private System.Windows.Forms.Button btnCashPosition;
        private System.Windows.Forms.Button btnResetCash;
        private System.Windows.Forms.Button btnAddCash;
    }
}