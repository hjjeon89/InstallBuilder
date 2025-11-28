namespace InstallerBuilder;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        txtProjectPath = new TextBox();
        txtOutputPath = new TextBox();
        lblProjectPath = new Label();
        lblOutputPath = new Label();
        btnBrowseProject = new Button();
        btnBrowseOutput = new Button();
        btnBuild = new Button();
        txtLog = new TextBox();
        lblLog = new Label();
        lblDllFiles = new Label();
        lstDllFiles = new ListBox();
        btnAddDll = new Button();
        btnRemoveDll = new Button();
        txtDllDestPath = new TextBox();
        lblDllDestPath = new Label();
        btnBrowseDllDest = new Button();
        lblAdditionalFiles = new Label();
        lstAdditionalFiles = new ListBox();
        btnAddAdditionalFile = new Button();
        btnRemoveAdditionalFile = new Button();
        txtAdditionalFilesDestPath = new TextBox();
        lblAdditionalFilesDestPath = new Label();
        btnBrowseAdditionalFilesDest = new Button();
        lblVersion = new Label();
        txtVersion = new TextBox();
        SuspendLayout();
        //
        // txtProjectPath
        //
        txtProjectPath.Location = new Point(99, 20);
        txtProjectPath.Name = "txtProjectPath";
        txtProjectPath.Size = new Size(600, 23);
        txtProjectPath.TabIndex = 0;
        //
        // txtOutputPath
        //
        txtOutputPath.Location = new Point(99, 485);
        txtOutputPath.Name = "txtOutputPath";
        txtOutputPath.Size = new Size(600, 23);
        txtOutputPath.TabIndex = 1;
        txtOutputPath.Text = "D:\\Installer";
        //
        // lblProjectPath
        //
        lblProjectPath.AutoSize = true;
        lblProjectPath.Location = new Point(12, 23);
        lblProjectPath.Name = "lblProjectPath";
        lblProjectPath.Size = new Size(81, 15);
        lblProjectPath.TabIndex = 2;
        lblProjectPath.Text = "프로젝트 경로:";
        //
        // lblOutputPath
        //
        lblOutputPath.AutoSize = true;
        lblOutputPath.Location = new Point(12, 488);
        lblOutputPath.Name = "lblOutputPath";
        lblOutputPath.Size = new Size(69, 15);
        lblOutputPath.TabIndex = 3;
        lblOutputPath.Text = "출력 경로:";
        //
        // btnBrowseProject
        //
        btnBrowseProject.Location = new Point(705, 20);
        btnBrowseProject.Name = "btnBrowseProject";
        btnBrowseProject.Size = new Size(83, 23);
        btnBrowseProject.TabIndex = 4;
        btnBrowseProject.Text = "찾아보기...";
        btnBrowseProject.UseVisualStyleBackColor = true;
        btnBrowseProject.Click += btnBrowseProject_Click;
        //
        // btnBrowseOutput
        //
        btnBrowseOutput.Location = new Point(705, 485);
        btnBrowseOutput.Name = "btnBrowseOutput";
        btnBrowseOutput.Size = new Size(83, 23);
        btnBrowseOutput.TabIndex = 5;
        btnBrowseOutput.Text = "찾아보기...";
        btnBrowseOutput.UseVisualStyleBackColor = true;
        btnBrowseOutput.Click += btnBrowseOutput_Click;
        //
        // btnBuild
        //
        btnBuild.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
        btnBuild.Location = new Point(12, 514);
        btnBuild.Name = "btnBuild";
        btnBuild.Size = new Size(776, 40);
        btnBuild.TabIndex = 6;
        btnBuild.Text = "설치파일 생성";
        btnBuild.UseVisualStyleBackColor = true;
        btnBuild.Click += btnBuild_Click;
        //
        // txtLog
        //
        txtLog.Location = new Point(12, 580);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(776, 200);
        txtLog.TabIndex = 7;
        //
        // lblLog
        //
        lblLog.AutoSize = true;
        lblLog.Location = new Point(12, 562);
        lblLog.Name = "lblLog";
        lblLog.Size = new Size(31, 15);
        lblLog.TabIndex = 8;
        lblLog.Text = "로그:";
        //
        // lblDllFiles
        //
        lblDllFiles.AutoSize = true;
        lblDllFiles.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
        lblDllFiles.Location = new Point(12, 60);
        lblDllFiles.Name = "lblDllFiles";
        lblDllFiles.Size = new Size(220, 15);
        lblDllFiles.TabIndex = 9;
        lblDllFiles.Text = "DLL 및 관련 파일 (Drag && Drop)";
        //
        // lstDllFiles
        //
        lstDllFiles.AllowDrop = true;
        lstDllFiles.FormattingEnabled = true;
        lstDllFiles.ItemHeight = 15;
        lstDllFiles.Location = new Point(12, 78);
        lstDllFiles.Name = "lstDllFiles";
        lstDllFiles.Size = new Size(776, 94);
        lstDllFiles.TabIndex = 10;
        lstDllFiles.DragDrop += lstDllFiles_DragDrop;
        lstDllFiles.DragEnter += lstDllFiles_DragEnter;
        //
        // btnAddDll
        //
        btnAddDll.Location = new Point(12, 178);
        btnAddDll.Name = "btnAddDll";
        btnAddDll.Size = new Size(120, 30);
        btnAddDll.TabIndex = 11;
        btnAddDll.Text = "파일 추가";
        btnAddDll.UseVisualStyleBackColor = true;
        btnAddDll.Click += btnAddDll_Click;
        //
        // btnRemoveDll
        //
        btnRemoveDll.Location = new Point(138, 178);
        btnRemoveDll.Name = "btnRemoveDll";
        btnRemoveDll.Size = new Size(120, 30);
        btnRemoveDll.TabIndex = 12;
        btnRemoveDll.Text = "파일 제거";
        btnRemoveDll.UseVisualStyleBackColor = true;
        btnRemoveDll.Click += btnRemoveDll_Click;
        //
        // txtDllDestPath
        //
        txtDllDestPath.Location = new Point(99, 214);
        txtDllDestPath.Name = "txtDllDestPath";
        txtDllDestPath.Size = new Size(600, 23);
        txtDllDestPath.TabIndex = 13;
        txtDllDestPath.Text = "[INSTALLDIR]";
        //
        // lblDllDestPath
        //
        lblDllDestPath.AutoSize = true;
        lblDllDestPath.Location = new Point(12, 217);
        lblDllDestPath.Name = "lblDllDestPath";
        lblDllDestPath.Size = new Size(69, 15);
        lblDllDestPath.TabIndex = 14;
        lblDllDestPath.Text = "복사 위치:";
        //
        // btnBrowseDllDest
        //
        btnBrowseDllDest.Location = new Point(705, 214);
        btnBrowseDllDest.Name = "btnBrowseDllDest";
        btnBrowseDllDest.Size = new Size(83, 23);
        btnBrowseDllDest.TabIndex = 15;
        btnBrowseDllDest.Text = "찾아보기...";
        btnBrowseDllDest.UseVisualStyleBackColor = true;
        btnBrowseDllDest.Click += btnBrowseDllDest_Click;
        //
        // lblAdditionalFiles
        //
        lblAdditionalFiles.AutoSize = true;
        lblAdditionalFiles.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
        lblAdditionalFiles.Location = new Point(12, 250);
        lblAdditionalFiles.Name = "lblAdditionalFiles";
        lblAdditionalFiles.Size = new Size(234, 15);
        lblAdditionalFiles.TabIndex = 16;
        lblAdditionalFiles.Text = "추가 파일 (config, text, json 등, Drag && Drop)";
        //
        // lstAdditionalFiles
        //
        lstAdditionalFiles.AllowDrop = true;
        lstAdditionalFiles.FormattingEnabled = true;
        lstAdditionalFiles.ItemHeight = 15;
        lstAdditionalFiles.Location = new Point(12, 268);
        lstAdditionalFiles.Name = "lstAdditionalFiles";
        lstAdditionalFiles.Size = new Size(776, 94);
        lstAdditionalFiles.TabIndex = 17;
        lstAdditionalFiles.DragDrop += lstAdditionalFiles_DragDrop;
        lstAdditionalFiles.DragEnter += lstAdditionalFiles_DragEnter;
        //
        // btnAddAdditionalFile
        //
        btnAddAdditionalFile.Location = new Point(12, 368);
        btnAddAdditionalFile.Name = "btnAddAdditionalFile";
        btnAddAdditionalFile.Size = new Size(120, 30);
        btnAddAdditionalFile.TabIndex = 18;
        btnAddAdditionalFile.Text = "파일 추가";
        btnAddAdditionalFile.UseVisualStyleBackColor = true;
        btnAddAdditionalFile.Click += btnAddAdditionalFile_Click;
        //
        // btnRemoveAdditionalFile
        //
        btnRemoveAdditionalFile.Location = new Point(138, 368);
        btnRemoveAdditionalFile.Name = "btnRemoveAdditionalFile";
        btnRemoveAdditionalFile.Size = new Size(120, 30);
        btnRemoveAdditionalFile.TabIndex = 19;
        btnRemoveAdditionalFile.Text = "파일 제거";
        btnRemoveAdditionalFile.UseVisualStyleBackColor = true;
        btnRemoveAdditionalFile.Click += btnRemoveAdditionalFile_Click;
        //
        // txtAdditionalFilesDestPath
        //
        txtAdditionalFilesDestPath.Location = new Point(99, 404);
        txtAdditionalFilesDestPath.Name = "txtAdditionalFilesDestPath";
        txtAdditionalFilesDestPath.Size = new Size(600, 23);
        txtAdditionalFilesDestPath.TabIndex = 20;
        txtAdditionalFilesDestPath.Text = "[INSTALLDIR]";
        //
        // lblAdditionalFilesDestPath
        //
        lblAdditionalFilesDestPath.AutoSize = true;
        lblAdditionalFilesDestPath.Location = new Point(12, 407);
        lblAdditionalFilesDestPath.Name = "lblAdditionalFilesDestPath";
        lblAdditionalFilesDestPath.Size = new Size(69, 15);
        lblAdditionalFilesDestPath.TabIndex = 21;
        lblAdditionalFilesDestPath.Text = "복사 위치:";
        //
        // btnBrowseAdditionalFilesDest
        //
        btnBrowseAdditionalFilesDest.Location = new Point(705, 404);
        btnBrowseAdditionalFilesDest.Name = "btnBrowseAdditionalFilesDest";
        btnBrowseAdditionalFilesDest.Size = new Size(83, 23);
        btnBrowseAdditionalFilesDest.TabIndex = 22;
        btnBrowseAdditionalFilesDest.Text = "찾아보기...";
        btnBrowseAdditionalFilesDest.UseVisualStyleBackColor = true;
        btnBrowseAdditionalFilesDest.Click += btnBrowseAdditionalFilesDest_Click;
        //
        // lblVersion
        //
        lblVersion.AutoSize = true;
        lblVersion.Location = new Point(12, 440);
        lblVersion.Name = "lblVersion";
        lblVersion.Size = new Size(69, 15);
        lblVersion.TabIndex = 23;
        lblVersion.Text = "설치 버전:";
        //
        // txtVersion
        //
        txtVersion.Location = new Point(99, 437);
        txtVersion.Name = "txtVersion";
        txtVersion.Size = new Size(150, 23);
        txtVersion.TabIndex = 24;
        txtVersion.Text = "1.0.0.0";
        //
        // Form1
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 800);
        Controls.Add(txtVersion);
        Controls.Add(lblVersion);
        Controls.Add(btnBrowseAdditionalFilesDest);
        Controls.Add(lblAdditionalFilesDestPath);
        Controls.Add(txtAdditionalFilesDestPath);
        Controls.Add(btnRemoveAdditionalFile);
        Controls.Add(btnAddAdditionalFile);
        Controls.Add(lstAdditionalFiles);
        Controls.Add(lblAdditionalFiles);
        Controls.Add(btnBrowseDllDest);
        Controls.Add(lblDllDestPath);
        Controls.Add(txtDllDestPath);
        Controls.Add(btnRemoveDll);
        Controls.Add(btnAddDll);
        Controls.Add(lstDllFiles);
        Controls.Add(lblDllFiles);
        Controls.Add(lblLog);
        Controls.Add(txtLog);
        Controls.Add(btnBuild);
        Controls.Add(btnBrowseOutput);
        Controls.Add(btnBrowseProject);
        Controls.Add(lblOutputPath);
        Controls.Add(lblProjectPath);
        Controls.Add(txtOutputPath);
        Controls.Add(txtProjectPath);
        Name = "Form1";
        Text = "설치파일 빌더";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TextBox txtProjectPath;
    private TextBox txtOutputPath;
    private Label lblProjectPath;
    private Label lblOutputPath;
    private Button btnBrowseProject;
    private Button btnBrowseOutput;
    private Button btnBuild;
    private TextBox txtLog;
    private Label lblLog;
    private Label lblDllFiles;
    private ListBox lstDllFiles;
    private Button btnAddDll;
    private Button btnRemoveDll;
    private TextBox txtDllDestPath;
    private Label lblDllDestPath;
    private Button btnBrowseDllDest;
    private Label lblAdditionalFiles;
    private ListBox lstAdditionalFiles;
    private Button btnAddAdditionalFile;
    private Button btnRemoveAdditionalFile;
    private TextBox txtAdditionalFilesDestPath;
    private Label lblAdditionalFilesDestPath;
    private Button btnBrowseAdditionalFilesDest;
    private Label lblVersion;
    private TextBox txtVersion;
}
