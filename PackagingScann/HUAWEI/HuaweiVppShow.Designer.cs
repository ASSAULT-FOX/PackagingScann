
namespace CheckUSBAOI
{
    partial class HuaweiVppShow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HuaweiVppShow));
            this.uiSymbolButton5 = new Sunny.UI.UISymbolButton();
            this.toolBlockEdit = new Cognex.VisionPro.ToolBlock.CogToolBlockEditV2();
            ((System.ComponentModel.ISupportInitialize)(this.toolBlockEdit)).BeginInit();
            this.SuspendLayout();
            // 
            // uiSymbolButton5
            // 
            this.uiSymbolButton5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiSymbolButton5.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiSymbolButton5.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.uiSymbolButton5.Location = new System.Drawing.Point(741, 38);
            this.uiSymbolButton5.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiSymbolButton5.Name = "uiSymbolButton5";
            this.uiSymbolButton5.Size = new System.Drawing.Size(278, 35);
            this.uiSymbolButton5.TabIndex = 6;
            this.uiSymbolButton5.Text = "保存当前视觉程序";
            this.uiSymbolButton5.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiSymbolButton5.Click += new System.EventHandler(this.uiSymbolButton5_Click);
            // 
            // toolBlockEdit
            // 
            this.toolBlockEdit.AllowDrop = true;
            this.toolBlockEdit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.toolBlockEdit.AutoSize = true;
            this.toolBlockEdit.ContextMenuCustomizer = null;
            this.toolBlockEdit.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolBlockEdit.Location = new System.Drawing.Point(5, 79);
            this.toolBlockEdit.Margin = new System.Windows.Forms.Padding(5);
            this.toolBlockEdit.MinimumSize = new System.Drawing.Size(815, 0);
            this.toolBlockEdit.Name = "toolBlockEdit";
            this.toolBlockEdit.ShowNodeToolTips = true;
            this.toolBlockEdit.Size = new System.Drawing.Size(1014, 631);
            this.toolBlockEdit.SuspendElectricRuns = false;
            this.toolBlockEdit.TabIndex = 7;
            // 
            // VPPDebug
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 717);
            this.Controls.Add(this.toolBlockEdit);
            this.Controls.Add(this.uiSymbolButton5);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(1024, 900);
            this.MinimumSize = new System.Drawing.Size(1024, 717);
            this.Name = "VPPDebug";
            this.Padding = new System.Windows.Forms.Padding(2, 35, 2, 2);
            this.ShowDragStretch = true;
            this.ShowRadius = false;
            this.Text = "VPPDebug";
            this.Load += new System.EventHandler(this.VPPDebug_Load);
            ((System.ComponentModel.ISupportInitialize)(this.toolBlockEdit)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Sunny.UI.UISymbolButton uiSymbolButton5;
        private Cognex.VisionPro.ToolBlock.CogToolBlockEditV2 toolBlockEdit;
    }
}