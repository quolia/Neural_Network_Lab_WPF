namespace Qualia.Controls
{
    partial class PresenterControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.CtlBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.CtlBox)).BeginInit();
            this.SuspendLayout();
            // 
            // CtlBox
            // 
            this.CtlBox.BackColor = System.Drawing.Color.White;
            this.CtlBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CtlBox.Location = new System.Drawing.Point(0, 0);
            this.CtlBox.Name = "CtlBox";
            this.CtlBox.Size = new System.Drawing.Size(150, 150);
            this.CtlBox.TabIndex = 0;
            this.CtlBox.TabStop = false;
            // 
            // PresenterControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.CtlBox);
            this.Name = "PresenterControl";
            ((System.ComponentModel.ISupportInitialize)(this.CtlBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox CtlBox;
    }
}
