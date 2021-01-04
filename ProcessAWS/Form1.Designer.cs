namespace ProcessAWS
{
    partial class ProcessTP1
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
            this.rtb_Log = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // rtb_Log
            // 
            this.rtb_Log.HideSelection = false;
            this.rtb_Log.Location = new System.Drawing.Point(13, 13);
            this.rtb_Log.Name = "rtb_Log";
            this.rtb_Log.Size = new System.Drawing.Size(497, 358);
            this.rtb_Log.TabIndex = 0;
            this.rtb_Log.Text = "";
            // 
            // ProcessTP1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(522, 383);
            this.Controls.Add(this.rtb_Log);
            this.Name = "ProcessTP1";
            this.Text = "ProcessTP1";
            this.Load += new System.EventHandler(this.ProcessTP1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtb_Log;
    }
}

