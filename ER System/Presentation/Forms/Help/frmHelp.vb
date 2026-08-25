Imports System.Diagnostics
Imports System.IO

Public Class frmHelp
    Private Const TutorialRelativePath As String = "Internal Develop Application\ERSystem 3.5\ERF System Tutorial and Documentation\outputs\ERF Video Tutorial"

    Private ReadOnly tutorialList As New ListBox()
    Private ReadOnly playButton As New Button()
    Private ReadOnly locationLabel As New Label()
    Private tutorialDirectory As String

    Private Sub frmHelp_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ConfigureTutorialHelp()
        LoadTutorials()
    End Sub

    Private Sub ConfigureTutorialHelp()
        Text = "Help - Video Tutorials"
        MinimumSize = New Size(720, 480)
        Size = New Size(900, 560)

        Panel1.Visible = False
        PictureBox1.Visible = False

        Dim page As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .ColumnCount = 1,
            .RowCount = 5,
            .Padding = New Padding(28)
        }
        page.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        page.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        page.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        page.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        page.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim titleLabel As New Label() With {
            .AutoSize = True,
            .Font = New Font("Segoe UI Semibold", 20.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(31, 55, 76),
            .Margin = New Padding(0, 0, 0, 6),
            .Text = "Video Tutorials"
        }
        Dim instructionLabel As New Label() With {
            .AutoSize = True,
            .Font = New Font("Segoe UI", 10.0F),
            .ForeColor = Color.FromArgb(80, 92, 103),
            .Margin = New Padding(0, 0, 0, 18),
            .Text = "Select a tutorial below, then click Play Video."
        }

        tutorialList.Dock = DockStyle.Fill
        tutorialList.Font = New Font("Segoe UI", 11.0F)
        tutorialList.IntegralHeight = False
        tutorialList.BorderStyle = BorderStyle.FixedSingle
        tutorialList.Margin = New Padding(0, 0, 0, 16)

        playButton.AutoSize = True
        playButton.Anchor = AnchorStyles.Right
        playButton.BackColor = Color.FromArgb(30, 99, 160)
        playButton.FlatStyle = FlatStyle.Flat
        playButton.FlatAppearance.BorderSize = 0
        playButton.Font = New Font("Segoe UI Semibold", 10.0F, FontStyle.Bold)
        playButton.ForeColor = Color.White
        playButton.Padding = New Padding(18, 8, 18, 8)
        playButton.Text = "Play Video"
        AddHandler playButton.Click, AddressOf PlaySelectedTutorial

        locationLabel.AutoSize = True
        locationLabel.Font = New Font("Segoe UI", 8.5F)
        locationLabel.ForeColor = Color.FromArgb(100, 110, 120)
        locationLabel.Margin = New Padding(0, 12, 0, 0)

        page.Controls.Add(titleLabel, 0, 0)
        page.Controls.Add(instructionLabel, 0, 1)
        page.Controls.Add(tutorialList, 0, 2)
        page.Controls.Add(playButton, 0, 3)
        page.Controls.Add(locationLabel, 0, 4)
        Controls.Add(page)
        page.BringToFront()

        AcceptButton = playButton
        AddHandler tutorialList.DoubleClick, AddressOf PlaySelectedTutorial
    End Sub

    Private Sub LoadTutorials()
        tutorialDirectory = FindTutorialDirectory()
        tutorialList.Items.Clear()

        If String.IsNullOrEmpty(tutorialDirectory) Then
            locationLabel.Text = "Tutorial folder was not found. Connect or sync the company OneDrive and reopen Help."
            playButton.Enabled = False
            Return
        End If

        Try
            For Each videoPath As String In Directory.GetFiles(tutorialDirectory, "*.mp4").OrderBy(Function(videoFilePath As String) System.IO.Path.GetFileName(videoFilePath))
                tutorialList.Items.Add(New TutorialVideo(videoPath))
            Next

            playButton.Enabled = tutorialList.Items.Count > 0
            If tutorialList.Items.Count > 0 Then
                tutorialList.SelectedIndex = 0
            End If

            locationLabel.Text = If(tutorialList.Items.Count = 0,
                                    "No MP4 tutorial videos were found in the tutorial folder.",
                                    tutorialList.Items.Count.ToString() & " video tutorials available")
        Catch ex As IOException
            playButton.Enabled = False
            locationLabel.Text = "The tutorial folder could not be read."
        Catch ex As UnauthorizedAccessException
            playButton.Enabled = False
            locationLabel.Text = "You do not have permission to read the tutorial folder."
        End Try
    End Sub

    Private Function FindTutorialDirectory() As String
        Dim localTutorialPath As String = Path.Combine(Application.StartupPath, "Help", "Video Tutorials")
        If Directory.Exists(localTutorialPath) Then
            Return localTutorialPath
        End If

        Dim oneDriveRoot As String = Environment.GetEnvironmentVariable("OneDriveCommercial")
        If String.IsNullOrWhiteSpace(oneDriveRoot) Then
            oneDriveRoot = Environment.GetEnvironmentVariable("OneDrive")
        End If

        If Not String.IsNullOrWhiteSpace(oneDriveRoot) Then
            Dim companyTutorialPath As String = Path.Combine(oneDriveRoot, TutorialRelativePath)
            If Directory.Exists(companyTutorialPath) Then
                Return companyTutorialPath
            End If
        End If

        Return Nothing
    End Function

    Private Sub PlaySelectedTutorial(sender As Object, e As EventArgs)
        Dim selectedVideo As TutorialVideo = TryCast(tutorialList.SelectedItem, TutorialVideo)
        If selectedVideo Is Nothing Then
            MessageBox.Show(Me, "Please select a video tutorial first.", "Help", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Try
            Process.Start(selectedVideo.FilePath)
        Catch ex As Exception
            MessageBox.Show(Me,
                            "The video could not be opened. Please verify that a video player is installed and the tutorial file is available.",
                            "Help",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Try
    End Sub

    Private NotInheritable Class TutorialVideo
        Private ReadOnly videoFilePath As String

        Public Sub New(filePath As String)
            videoFilePath = filePath
        End Sub

        Public ReadOnly Property FilePath As String
            Get
                Return videoFilePath
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return Path.GetFileNameWithoutExtension(FilePath)
        End Function
    End Class
End Class
