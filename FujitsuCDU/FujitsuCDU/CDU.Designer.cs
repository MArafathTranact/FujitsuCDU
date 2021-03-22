
namespace FujitsuCDU
{
    partial class CDU
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CDU));
            this.picHeader = new System.Windows.Forms.PictureBox();
            this.pnlInitialize = new System.Windows.Forms.Panel();
            this.lblInitialize = new System.Windows.Forms.Label();
            this.pnlMessage = new System.Windows.Forms.Panel();
            this.lblProcessMessage = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picHeader)).BeginInit();
            this.pnlInitialize.SuspendLayout();
            this.pnlMessage.SuspendLayout();
            this.SuspendLayout();
            // 
            // picHeader
            // 
            this.picHeader.BackgroundImage = global::FujitsuCDU.Properties.Resources.TranAct_Logo_Scaled;
            this.picHeader.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.picHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.picHeader.Location = new System.Drawing.Point(0, 0);
            this.picHeader.Name = "picHeader";
            this.picHeader.Size = new System.Drawing.Size(2982, 326);
            this.picHeader.TabIndex = 0;
            this.picHeader.TabStop = false;
            // 
            // pnlInitialize
            // 
            this.pnlInitialize.Controls.Add(this.lblInitialize);
            this.pnlInitialize.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlInitialize.Location = new System.Drawing.Point(0, 326);
            this.pnlInitialize.Name = "pnlInitialize";
            this.pnlInitialize.Size = new System.Drawing.Size(2982, 216);
            this.pnlInitialize.TabIndex = 1;
            // 
            // lblInitialize
            // 
            this.lblInitialize.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblInitialize.AutoSize = true;
            this.lblInitialize.Font = new System.Drawing.Font("Microsoft Sans Serif", 45F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInitialize.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.lblInitialize.Location = new System.Drawing.Point(920, 22);
            this.lblInitialize.Name = "lblInitialize";
            this.lblInitialize.Size = new System.Drawing.Size(0, 170);
            this.lblInitialize.TabIndex = 0;
            // 
            // pnlMessage
            // 
            this.pnlMessage.BackColor = System.Drawing.Color.RoyalBlue;
            this.pnlMessage.Controls.Add(this.lblProcessMessage);
            this.pnlMessage.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlMessage.Location = new System.Drawing.Point(0, 542);
            this.pnlMessage.Name = "pnlMessage";
            this.pnlMessage.Size = new System.Drawing.Size(2982, 423);
            this.pnlMessage.TabIndex = 2;
            // 
            // lblProcessMessage
            // 
            this.lblProcessMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblProcessMessage.AutoSize = true;
            this.lblProcessMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 35F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProcessMessage.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.lblProcessMessage.Location = new System.Drawing.Point(1054, 186);
            this.lblProcessMessage.Name = "lblProcessMessage";
            this.lblProcessMessage.Size = new System.Drawing.Size(0, 132);
            this.lblProcessMessage.TabIndex = 1;
            // 
            // CDU
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 31F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.RoyalBlue;
            this.ClientSize = new System.Drawing.Size(2982, 1440);
            this.Controls.Add(this.pnlMessage);
            this.Controls.Add(this.pnlInitialize);
            this.Controls.Add(this.picHeader);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CDU";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CDU_FormClosing);
            this.Load += new System.EventHandler(this.CDU_Load);
            this.SizeChanged += new System.EventHandler(this.CDU_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this.picHeader)).EndInit();
            this.pnlInitialize.ResumeLayout(false);
            this.pnlInitialize.PerformLayout();
            this.pnlMessage.ResumeLayout(false);
            this.pnlMessage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox picHeader;
        private System.Windows.Forms.Panel pnlInitialize;
        private System.Windows.Forms.Label lblInitialize;
        private System.Windows.Forms.Panel pnlMessage;
        private System.Windows.Forms.Label lblProcessMessage;
    }
}

