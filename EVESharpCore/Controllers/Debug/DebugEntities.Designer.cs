namespace EVESharpCore.Controllers.Debug
{
    partial class DebugEntities
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
            this.components = new System.ComponentModel.Container();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyIdToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.monitorEntityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.uNLOADCOLLISIONINFOToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sHOWCOLLISIONDATAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sHOWDESTINYBALLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sHOWMODELSPHEREToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sHOWBOUNDINGSPHEREToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dRAWMINIBALLSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dRAWMINICAPSULESToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dRAWMINIBOXESToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rEMOVEALLDRAWNOBJECTSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pRINTDMGEFFECTSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridView2
            // 
            this.dataGridView2.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.ContextMenuStrip = this.contextMenuStrip1;
            this.dataGridView2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView2.Location = new System.Drawing.Point(3, 3);
            this.dataGridView2.MultiSelect = false;
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView2.Size = new System.Drawing.Size(1890, 475);
            this.dataGridView2.TabIndex = 152;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyIdToClipboardToolStripMenuItem,
            this.monitorEntityToolStripMenuItem,
            this.toolStripMenuItem1,
            this.uNLOADCOLLISIONINFOToolStripMenuItem,
            this.sHOWCOLLISIONDATAToolStripMenuItem,
            this.sHOWDESTINYBALLToolStripMenuItem,
            this.sHOWMODELSPHEREToolStripMenuItem,
            this.sHOWBOUNDINGSPHEREToolStripMenuItem,
            this.dRAWMINIBALLSToolStripMenuItem,
            this.dRAWMINICAPSULESToolStripMenuItem,
            this.dRAWMINIBOXESToolStripMenuItem,
            this.rEMOVEALLDRAWNOBJECTSToolStripMenuItem,
            this.pRINTDMGEFFECTSToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(244, 290);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // copyIdToClipboardToolStripMenuItem
            // 
            this.copyIdToClipboardToolStripMenuItem.Name = "copyIdToClipboardToolStripMenuItem";
            this.copyIdToClipboardToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.copyIdToClipboardToolStripMenuItem.Text = "Copy id to clipboard";
            this.copyIdToClipboardToolStripMenuItem.Click += new System.EventHandler(this.copyIdToClipboardToolStripMenuItem_Click);
            // 
            // monitorEntityToolStripMenuItem
            // 
            this.monitorEntityToolStripMenuItem.Name = "monitorEntityToolStripMenuItem";
            this.monitorEntityToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.monitorEntityToolStripMenuItem.Text = "Monitor entity";
            this.monitorEntityToolStripMenuItem.Click += new System.EventHandler(this.monitorEntityToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(243, 22);
            this.toolStripMenuItem1.Text = "Open model in blue viewer";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // uNLOADCOLLISIONINFOToolStripMenuItem
            // 
            this.uNLOADCOLLISIONINFOToolStripMenuItem.Name = "uNLOADCOLLISIONINFOToolStripMenuItem";
            this.uNLOADCOLLISIONINFOToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.uNLOADCOLLISIONINFOToolStripMenuItem.Text = "UNLOAD_COLLISION_INFO";
            this.uNLOADCOLLISIONINFOToolStripMenuItem.Click += new System.EventHandler(this.uNLOADCOLLISIONINFOToolStripMenuItem_Click);
            // 
            // sHOWCOLLISIONDATAToolStripMenuItem
            // 
            this.sHOWCOLLISIONDATAToolStripMenuItem.Name = "sHOWCOLLISIONDATAToolStripMenuItem";
            this.sHOWCOLLISIONDATAToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.sHOWCOLLISIONDATAToolStripMenuItem.Text = "SHOW_COLLISION_DATA";
            this.sHOWCOLLISIONDATAToolStripMenuItem.Click += new System.EventHandler(this.sHOWCOLLISIONDATAToolStripMenuItem_Click);
            // 
            // sHOWDESTINYBALLToolStripMenuItem
            // 
            this.sHOWDESTINYBALLToolStripMenuItem.Name = "sHOWDESTINYBALLToolStripMenuItem";
            this.sHOWDESTINYBALLToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.sHOWDESTINYBALLToolStripMenuItem.Text = "SHOW_DESTINY_BALL";
            this.sHOWDESTINYBALLToolStripMenuItem.Click += new System.EventHandler(this.sHOWDESTINYBALLToolStripMenuItem_Click);
            // 
            // sHOWMODELSPHEREToolStripMenuItem
            // 
            this.sHOWMODELSPHEREToolStripMenuItem.Name = "sHOWMODELSPHEREToolStripMenuItem";
            this.sHOWMODELSPHEREToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.sHOWMODELSPHEREToolStripMenuItem.Text = "SHOW_MODEL_SPHERE";
            this.sHOWMODELSPHEREToolStripMenuItem.Click += new System.EventHandler(this.sHOWMODELSPHEREToolStripMenuItem_Click);
            // 
            // sHOWBOUNDINGSPHEREToolStripMenuItem
            // 
            this.sHOWBOUNDINGSPHEREToolStripMenuItem.Name = "sHOWBOUNDINGSPHEREToolStripMenuItem";
            this.sHOWBOUNDINGSPHEREToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.sHOWBOUNDINGSPHEREToolStripMenuItem.Text = "SHOW_BOUNDING_SPHERE";
            this.sHOWBOUNDINGSPHEREToolStripMenuItem.Click += new System.EventHandler(this.sHOWBOUNDINGSPHEREToolStripMenuItem_Click);
            // 
            // dRAWMINIBALLSToolStripMenuItem
            // 
            this.dRAWMINIBALLSToolStripMenuItem.Name = "dRAWMINIBALLSToolStripMenuItem";
            this.dRAWMINIBALLSToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.dRAWMINIBALLSToolStripMenuItem.Text = "DRAW_MINIBALLS";
            this.dRAWMINIBALLSToolStripMenuItem.Click += new System.EventHandler(this.dRAWMINIBALLSToolStripMenuItem_Click);
            // 
            // dRAWMINICAPSULESToolStripMenuItem
            // 
            this.dRAWMINICAPSULESToolStripMenuItem.Name = "dRAWMINICAPSULESToolStripMenuItem";
            this.dRAWMINICAPSULESToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.dRAWMINICAPSULESToolStripMenuItem.Text = "DRAW_MINICAPSULES";
            // 
            // dRAWMINIBOXESToolStripMenuItem
            // 
            this.dRAWMINIBOXESToolStripMenuItem.Name = "dRAWMINIBOXESToolStripMenuItem";
            this.dRAWMINIBOXESToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.dRAWMINIBOXESToolStripMenuItem.Text = "DRAW_MINIBOXES";
            this.dRAWMINIBOXESToolStripMenuItem.Click += new System.EventHandler(this.dRAWMINIBOXESToolStripMenuItem_Click);
            // 
            // rEMOVEALLDRAWNOBJECTSToolStripMenuItem
            // 
            this.rEMOVEALLDRAWNOBJECTSToolStripMenuItem.Name = "rEMOVEALLDRAWNOBJECTSToolStripMenuItem";
            this.rEMOVEALLDRAWNOBJECTSToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.rEMOVEALLDRAWNOBJECTSToolStripMenuItem.Text = "REMOVE_ALL_DRAWN_OBJECTS";
            this.rEMOVEALLDRAWNOBJECTSToolStripMenuItem.Click += new System.EventHandler(this.rEMOVEALLDRAWNOBJECTSToolStripMenuItem_Click);
            // 
            // pRINTDMGEFFECTSToolStripMenuItem
            // 
            this.pRINTDMGEFFECTSToolStripMenuItem.Name = "pRINTDMGEFFECTSToolStripMenuItem";
            this.pRINTDMGEFFECTSToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.pRINTDMGEFFECTSToolStripMenuItem.Text = "PRINT_DMG_EFFECTS";
            this.pRINTDMGEFFECTSToolStripMenuItem.Click += new System.EventHandler(this.pRINTDMGEFFECTSToolStripMenuItem_Click);
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.White;
            this.button2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.Location = new System.Drawing.Point(221, 3);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(212, 46);
            this.button2.TabIndex = 151;
            this.button2.Text = "Show entities";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.White;
            this.button1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(3, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(212, 46);
            this.button1.TabIndex = 153;
            this.button1.Text = "Clear action queue";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.dataGridView2, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 89.30817F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10.69182F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1896, 539);
            this.tableLayoutPanel1.TabIndex = 154;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.button1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.button2, 1, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 484);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(436, 52);
            this.tableLayoutPanel2.TabIndex = 155;
            // 
            // DebugEntities
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1896, 539);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "DebugEntities";
            this.Text = "DebugEntities";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem copyIdToClipboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem monitorEntityToolStripMenuItem;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem uNLOADCOLLISIONINFOToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sHOWCOLLISIONDATAToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sHOWDESTINYBALLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sHOWMODELSPHEREToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sHOWBOUNDINGSPHEREToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dRAWMINIBALLSToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dRAWMINICAPSULESToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dRAWMINIBOXESToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rEMOVEALLDRAWNOBJECTSToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pRINTDMGEFFECTSToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
    }
}