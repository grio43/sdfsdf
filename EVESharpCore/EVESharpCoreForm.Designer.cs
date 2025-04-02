namespace EVESharpCore
{
	partial class EVESharpCoreForm
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

		private void InitializeComponent()
		{
            this.button2 = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            this.buttonOpenLogDirectory = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.PauseCheckBox = new System.Windows.Forms.CheckBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.buildTimeLabel = new System.Windows.Forms.Label();
            this.listViewLogs = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.dataGridControllers = new System.Windows.Forms.DataGridView();
            this.buttonRemoveController = new System.Windows.Forms.Button();
            this.buttonAddController = new System.Windows.Forms.Button();
            this.comboBoxControllers = new System.Windows.Forms.ComboBox();
            this.tabPage3.SuspendLayout();
            this.tabControlMain.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridControllers)).BeginInit();
            this.SuspendLayout();
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.DarkGray;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button2.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.ForeColor = System.Drawing.SystemColors.Window;
            this.button2.Location = new System.Drawing.Point(600, 0);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(83, 15);
            this.button2.TabIndex = 129;
            this.button2.Text = "+";
            this.button2.UseMnemonic = false;
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.Button2Click);
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.White;
            this.label11.Dock = System.Windows.Forms.DockStyle.Top;
            this.label11.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.Black;
            this.label11.Location = new System.Drawing.Point(0, 0);
            this.label11.MaximumSize = new System.Drawing.Size(600, 15);
            this.label11.MinimumSize = new System.Drawing.Size(600, 15);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(600, 15);
            this.label11.TabIndex = 130;
            this.label11.Text = "label11";
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.White;
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button3.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button3.Location = new System.Drawing.Point(602, 304);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 16);
            this.button3.TabIndex = 134;
            this.button3.Text = "Exit";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // buttonOpenLogDirectory
            // 
            this.buttonOpenLogDirectory.BackColor = System.Drawing.Color.White;
            this.buttonOpenLogDirectory.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOpenLogDirectory.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonOpenLogDirectory.Location = new System.Drawing.Point(524, 304);
            this.buttonOpenLogDirectory.Name = "buttonOpenLogDirectory";
            this.buttonOpenLogDirectory.Size = new System.Drawing.Size(75, 16);
            this.buttonOpenLogDirectory.TabIndex = 133;
            this.buttonOpenLogDirectory.Text = "Logs";
            this.buttonOpenLogDirectory.UseVisualStyleBackColor = false;
            this.buttonOpenLogDirectory.Click += new System.EventHandler(this.buttonOpenLogDirectory_Click);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.White;
            this.label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.label1.Location = new System.Drawing.Point(0, 326);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(683, 1);
            this.label1.TabIndex = 144;
            // 
            // PauseCheckBox
            // 
            this.PauseCheckBox.BackColor = System.Drawing.Color.Blue;
            this.PauseCheckBox.Cursor = System.Windows.Forms.Cursors.Default;
            this.PauseCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PauseCheckBox.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PauseCheckBox.ForeColor = System.Drawing.Color.White;
            this.PauseCheckBox.Location = new System.Drawing.Point(463, 304);
            this.PauseCheckBox.Name = "PauseCheckBox";
            this.PauseCheckBox.Size = new System.Drawing.Size(61, 16);
            this.PauseCheckBox.TabIndex = 132;
            this.PauseCheckBox.Text = "Pause";
            this.PauseCheckBox.UseVisualStyleBackColor = false;
            this.PauseCheckBox.CheckedChanged += new System.EventHandler(this.PauseCheckBox_CheckedChanged);
            // 
            // tabPage3
            // 
            this.tabPage3.BackColor = System.Drawing.Color.Blue;
            this.tabPage3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.tabPage3.Controls.Add(this.buildTimeLabel);
            this.tabPage3.Controls.Add(this.listViewLogs);
            this.tabPage3.Location = new System.Drawing.Point(4, 25);
            this.tabPage3.Margin = new System.Windows.Forms.Padding(0);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(694, 291);
            this.tabPage3.TabIndex = 1;
            this.tabPage3.Text = " Main";
            // 
            // buildTimeLabel
            // 
            this.buildTimeLabel.AutoSize = true;
            this.buildTimeLabel.BackColor = System.Drawing.Color.Blue;
            this.buildTimeLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buildTimeLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.buildTimeLabel.ForeColor = System.Drawing.Color.White;
            this.buildTimeLabel.Location = new System.Drawing.Point(403, 266);
            this.buildTimeLabel.Name = "buildTimeLabel";
            this.buildTimeLabel.Size = new System.Drawing.Size(62, 13);
            this.buildTimeLabel.TabIndex = 142;
            this.buildTimeLabel.Text = "buildTime";
            this.buildTimeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // listViewLogs
            // 
            this.listViewLogs.AutoArrange = false;
            this.listViewLogs.BackColor = System.Drawing.Color.Blue;
            this.listViewLogs.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listViewLogs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listViewLogs.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listViewLogs.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listViewLogs.HideSelection = false;
            this.listViewLogs.Location = new System.Drawing.Point(9, 3);
            this.listViewLogs.Name = "listViewLogs";
            this.listViewLogs.Size = new System.Drawing.Size(671, 254);
            this.listViewLogs.TabIndex = 141;
            this.listViewLogs.UseCompatibleStateImageBehavior = false;
            this.listViewLogs.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Logbox";
            this.columnHeader1.Width = 1526;
            // 
            // tabControlMain
            // 
            this.tabControlMain.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tabControlMain.Controls.Add(this.tabPage3);
            this.tabControlMain.Controls.Add(this.tabPage1);
            this.tabControlMain.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControlMain.Location = new System.Drawing.Point(-7, 15);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.Padding = new System.Drawing.Point(0, 0);
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(702, 320);
            this.tabControlMain.TabIndex = 127;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.Color.Blue;
            this.tabPage1.Controls.Add(this.dataGridControllers);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(694, 291);
            this.tabPage1.TabIndex = 3;
            this.tabPage1.Text = "Controllers";
            // 
            // dataGridControllers
            // 
            this.dataGridControllers.AllowUserToAddRows = false;
            this.dataGridControllers.AllowUserToDeleteRows = false;
            this.dataGridControllers.AllowUserToOrderColumns = true;
            this.dataGridControllers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridControllers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridControllers.Dock = System.Windows.Forms.DockStyle.Top;
            this.dataGridControllers.EnableHeadersVisualStyles = false;
            this.dataGridControllers.Location = new System.Drawing.Point(3, 3);
            this.dataGridControllers.MultiSelect = false;
            this.dataGridControllers.Name = "dataGridControllers";
            this.dataGridControllers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridControllers.Size = new System.Drawing.Size(688, 252);
            this.dataGridControllers.TabIndex = 147;
            this.dataGridControllers.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dataGridControllers_CellFormatting);
            this.dataGridControllers.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dataGridControllers_DataError);
            // 
            // buttonRemoveController
            // 
            this.buttonRemoveController.BackColor = System.Drawing.Color.White;
            this.buttonRemoveController.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonRemoveController.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonRemoveController.Location = new System.Drawing.Point(324, 304);
            this.buttonRemoveController.Name = "buttonRemoveController";
            this.buttonRemoveController.Size = new System.Drawing.Size(75, 16);
            this.buttonRemoveController.TabIndex = 146;
            this.buttonRemoveController.Text = "Remove";
            this.buttonRemoveController.UseVisualStyleBackColor = false;
            this.buttonRemoveController.Click += new System.EventHandler(this.buttonRemoveController_Click);
            // 
            // buttonAddController
            // 
            this.buttonAddController.BackColor = System.Drawing.Color.White;
            this.buttonAddController.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonAddController.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAddController.Location = new System.Drawing.Point(245, 304);
            this.buttonAddController.Name = "buttonAddController";
            this.buttonAddController.Size = new System.Drawing.Size(75, 16);
            this.buttonAddController.TabIndex = 145;
            this.buttonAddController.Text = "Add";
            this.buttonAddController.UseVisualStyleBackColor = false;
            this.buttonAddController.Click += new System.EventHandler(this.buttonAddController_Click);
            // 
            // comboBoxControllers
            // 
            this.comboBoxControllers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxControllers.FormattingEnabled = true;
            this.comboBoxControllers.Location = new System.Drawing.Point(8, 301);
            this.comboBoxControllers.Name = "comboBoxControllers";
            this.comboBoxControllers.Size = new System.Drawing.Size(231, 21);
            this.comboBoxControllers.TabIndex = 0;
            // 
            // EVESharpCoreForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.BackColor = System.Drawing.Color.Blue;
            this.ClientSize = new System.Drawing.Size(683, 327);
            this.ControlBox = false;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.PauseCheckBox);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.buttonOpenLogDirectory);
            this.Controls.Add(this.buttonRemoveController);
            this.Controls.Add(this.buttonAddController);
            this.Controls.Add(this.comboBoxControllers);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.tabControlMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Location = new System.Drawing.Point(-15000, -15000);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EVESharpCoreForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "EVESharp";
            this.TransparencyKey = System.Drawing.Color.Blue;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EVESharpFormClosing);
            this.Load += new System.EventHandler(this.EVESharpCoreFormLoad);
            this.Shown += new System.EventHandler(this.EVESharpCoreFormShown);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabControlMain.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridControllers)).EndInit();
            this.ResumeLayout(false);

		}
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button buttonOpenLogDirectory;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox PauseCheckBox;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.ListView listViewLogs;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        public System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button buttonRemoveController;
        private System.Windows.Forms.Button buttonAddController;
        private System.Windows.Forms.ComboBox comboBoxControllers;
        private System.Windows.Forms.DataGridView dataGridControllers;
        private System.Windows.Forms.Label buildTimeLabel;
    }
}