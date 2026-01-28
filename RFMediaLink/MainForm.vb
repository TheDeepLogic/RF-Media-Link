Imports System.IO
Imports System.Json

Public Class MainForm
    Inherits Form
    
    Private ConfigDir As String
    Private ConfigFile As String
    Private CatalogFile As String
    Private EmulatorsFile As String
    Private Config As Dictionary(Of String, Object)
    Private Catalog As Dictionary(Of String, Object)
    Private Emulators As Dictionary(Of String, Object)
    
    Private TabControl1 As TabControl
    Private TabTags As TabPage
    Private TabEmulators As TabPage
    Private TabSettings As TabPage
    
    ' Tags tab controls
    Private TagsListBox As ListBox
    Private AddScanBtn As Button
    Private AddManualBtn As Button
    Private DeleteBtn As Button
    Private TagDetailsPanel As Panel
    Private UidLabel As Label
    Private NameLabel As Label
    Private ActionLabel As Label
    Private TargetLabel As Label
    
    ' Settings tab controls
    Private PortTextBox As TextBox
    Private BaudTextBox As TextBox
    Private SaveSettingsBtn As Button
    
    Public Sub New()
        MyBase.New()
        InitializeComponent()
        FindConfigDir()
        LoadAllData()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "RF Media Link Configuration"
        Me.Size = New Size(700, 600)
        Me.StartPosition = FormStartPosition.CenterScreen
        
        ' Main tab control
        TabControl1 = New TabControl()
        TabControl1.Dock = DockStyle.Fill
        Me.Controls.Add(TabControl1)
        
        ' Tags Tab
        TabTags = New TabPage("Tags")
        SetupTagsTab()
        TabControl1.TabPages.Add(TabTags)
        
        ' Emulators Tab
        TabEmulators = New TabPage("Emulators")
        SetupEmulatorsTab()
        TabControl1.TabPages.Add(TabEmulators)
        
        ' Settings Tab
        TabSettings = New TabPage("Settings")
        SetupSettingsTab()
        TabControl1.TabPages.Add(TabSettings)
    End Sub
    
    Private Sub SetupTagsTab()
        Dim pnl As New Panel With {.Dock = DockStyle.Fill, .AutoScroll = True}
        TabTags.Controls.Add(pnl)
        
        Dim y As Integer = 10
        
        ' Title
        Dim lbl As New Label With {.Text = "RFID Tags", .Font = New Font("Arial", 12, FontStyle.Bold), .Location = New Point(10, y), .Size = New Size(300, 25)}
        pnl.Controls.Add(lbl)
        y += 35
        
        ' Buttons
        AddScanBtn = New Button With {.Text = "Add Tag (Scan)", .Location = New Point(10, y), .Size = New Size(120, 30)}
        AddHandler AddScanBtn.Click, AddressOf AddTag_Click
        pnl.Controls.Add(AddScanBtn)
        
        AddManualBtn = New Button With {.Text = "Add Tag (Manual)", .Location = New Point(140, y), .Size = New Size(120, 30)}
        AddHandler AddManualBtn.Click, AddressOf AddTagManual_Click
        pnl.Controls.Add(AddManualBtn)
        
        DeleteBtn = New Button With {.Text = "Delete Selected", .Location = New Point(270, y), .Size = New Size(120, 30)}
        AddHandler DeleteBtn.Click, AddressOf DeleteTag_Click
        pnl.Controls.Add(DeleteBtn)
        y += 40
        
        ' List box
        TagsListBox = New ListBox With {.Location = New Point(10, y), .Size = New Size(650, 300)}
        pnl.Controls.Add(TagsListBox)
        y += 310
        
        ' Details panel
        TagDetailsPanel = New Panel With {.Location = New Point(10, y), .Size = New Size(650, 120), .BorderStyle = BorderStyle.FixedSingle}
        pnl.Controls.Add(TagDetailsPanel)
        
        Dim dy As Integer = 10
        TagDetailsPanel.Controls.Add(New Label With {.Text = "UID:", .Location = New Point(10, dy), .Size = New Size(80, 20)})
        UidLabel = New Label With {.Text = "-", .Location = New Point(100, dy), .Size = New Size(500, 20)}
        TagDetailsPanel.Controls.Add(UidLabel)
        dy += 25
        
        TagDetailsPanel.Controls.Add(New Label With {.Text = "Name:", .Location = New Point(10, dy), .Size = New Size(80, 20)})
        NameLabel = New Label With {.Text = "-", .Location = New Point(100, dy), .Size = New Size(500, 20)}
        TagDetailsPanel.Controls.Add(NameLabel)
        dy += 25
        
        TagDetailsPanel.Controls.Add(New Label With {.Text = "Action:", .Location = New Point(10, dy), .Size = New Size(80, 20)})
        ActionLabel = New Label With {.Text = "-", .Location = New Point(100, dy), .Size = New Size(500, 20)}
        TagDetailsPanel.Controls.Add(ActionLabel)
        dy += 25
        
        TagDetailsPanel.Controls.Add(New Label With {.Text = "Target:", .Location = New Point(10, dy), .Size = New Size(80, 20)})
        TargetLabel = New Label With {.Text = "-", .Location = New Point(100, dy), .Size = New Size(500, 20)}
        TagDetailsPanel.Controls.Add(TargetLabel)
        
        RefreshTagsList()
    End Sub
    
    Private Sub SetupEmulatorsTab()
        Dim pnl As New Panel With {.Dock = DockStyle.Fill, .AutoScroll = True}
        TabEmulators.Controls.Add(pnl)
        
        Dim lbl As New Label With {.Text = "Emulators", .Font = New Font("Arial", 12, FontStyle.Bold), .Location = New Point(10, 10), .Size = New Size(300, 25)}
        pnl.Controls.Add(lbl)
        
        Dim infoPnl As New Panel With {.Location = New Point(10, 50), .Size = New Size(650, 500), .BorderStyle = BorderStyle.FixedSingle, .AutoScroll = True}
        pnl.Controls.Add(infoPnl)
        
        Dim y As Integer = 10
        For Each kvp In Emulators
            Dim emu As Object = kvp.Value
            Dim name As String = If(TypeOf emu Is JsonObject, CStr(CType(emu, JsonObject)("name")), "Unknown")
            
            Dim emulbl As New Label With {.Text = name, .Font = New Font("Arial", 10, FontStyle.Bold), .Location = New Point(10, y), .Size = New Size(600, 20)}
            infoPnl.Controls.Add(emulbl)
            y += 25
            
            Dim exelbl As New Label With {.Text = "Path: " & kvp.Key, .Location = New Point(20, y), .Size = New Size(600, 20)}
            infoPnl.Controls.Add(exelbl)
            y += 25
        Next
    End Sub
    
    Private Sub SetupSettingsTab()
        Dim pnl As New Panel With {.Dock = DockStyle.Fill, .AutoScroll = True}
        TabSettings.Controls.Add(pnl)
        
        Dim lbl As New Label With {.Text = "Settings", .Font = New Font("Arial", 12, FontStyle.Bold), .Location = New Point(10, 10), .Size = New Size(300, 25)}
        pnl.Controls.Add(lbl)
        
        Dim y As Integer = 50
        
        pnl.Controls.Add(New Label With {.Text = "Serial Port:", .Location = New Point(10, y), .Size = New Size(100, 20)})
        PortTextBox = New TextBox With {.Location = New Point(120, y), .Size = New Size(100, 24), .Text = Config.Item("serial_port")}
        pnl.Controls.Add(PortTextBox)
        y += 40
        
        pnl.Controls.Add(New Label With {.Text = "Baud Rate:", .Location = New Point(10, y), .Size = New Size(100, 20)})
        BaudTextBox = New TextBox With {.Location = New Point(120, y), .Size = New Size(100, 24), .Text = CStr(Config.Item("baud_rate"))}
        pnl.Controls.Add(BaudTextBox)
        y += 40
        
        SaveSettingsBtn = New Button With {.Text = "Save Settings", .Location = New Point(10, y), .Size = New Size(120, 30)}
        AddHandler SaveSettingsBtn.Click, AddressOf SaveSettings_Click
        pnl.Controls.Add(SaveSettingsBtn)
    End Sub
    
    Private Sub FindConfigDir()
        ' Check AppData first
        Dim appDataPath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RFMediaLink")
        If Directory.Exists(appDataPath) AndAlso File.Exists(Path.Combine(appDataPath, "config.json")) Then
            ConfigDir = appDataPath
        Else
            ' Fall back to Program Files
            Dim progFilesPath As String = "C:\Program Files\RFMediaLink"
            If Directory.Exists(progFilesPath) AndAlso File.Exists(Path.Combine(progFilesPath, "config.json")) Then
                ConfigDir = progFilesPath
            Else
                ConfigDir = appDataPath
            End If
        End If
        
        ConfigFile = Path.Combine(ConfigDir, "config.json")
        CatalogFile = Path.Combine(ConfigDir, "catalog.json")
        EmulatorsFile = Path.Combine(ConfigDir, "emulators.json")
    End Sub
    
    Private Sub LoadAllData()
        ' Load JSON files
        Config = LoadJson(ConfigFile)
        If Config Is Nothing Then
            Config = New Dictionary(Of String, Object) From {
                {"serial_port", "COM9"},
                {"baud_rate", 115200}
            }
        End If
        
        Dim catalogData = LoadJson(CatalogFile)
        If TypeOf catalogData Is Dictionary(Of String, Object) Then
            Catalog = CType(catalogData, Dictionary(Of String, Object))
        Else
            Catalog = New Dictionary(Of String, Object)
        End If
        
        Emulators = LoadJson(EmulatorsFile)
        If Emulators Is Nothing Then Emulators = New Dictionary(Of String, Object)
    End Sub
    
    Private Function LoadJson(path As String) As Object
        If Not File.Exists(path) Then Return Nothing
        Try
            Dim json As String = File.ReadAllText(path)
            Return JsonValue.Parse(json)
        Catch
            Return Nothing
        End Try
    End Function
    
    Private Sub SaveJson(path As String, data As Object)
        Try
            If TypeOf data Is Dictionary(Of String, Object) Then
                Dim dict = CType(data, Dictionary(Of String, Object))
                Dim json = New Text.StringBuilder()
                json.AppendLine("{")
                Dim first = True
                For Each kvp In dict
                    If Not first Then json.AppendLine(",")
                    json.Append($"  ""{kvp.Key}"": {SerializeValue(kvp.Value)}")
                    first = False
                Next
                json.AppendLine()
                json.AppendLine("}")
                File.WriteAllText(path, json.ToString())
            End If
        Catch ex As Exception
            MessageBox.Show("Error saving file: " & ex.Message)
        End Try
    End Sub
    
    Private Function SerializeValue(val As Object) As String
        If val Is Nothing Then Return "null"
        If TypeOf val Is String Then Return """" & CStr(val) & """"
        If TypeOf val Is Boolean Then Return If(CBool(val), "true", "false")
        If TypeOf val Is Integer Or TypeOf val Is Long Or TypeOf val Is Double Then Return CStr(val)
        Return """"  & CStr(val) & """"
    End Function
    
    Private Sub RefreshTagsList()
        TagsListBox.Items.Clear()
        For Each kvp In Catalog
            Dim tag = kvp.Value
            If TypeOf tag Is Dictionary(Of String, Object) Then
                Dim dict = CType(tag, Dictionary(Of String, Object))
                Dim name = If(dict.ContainsKey("name"), CStr(dict("name")), "Unknown")
                TagsListBox.Items.Add($"{name} [{kvp.Key}]")
            End If
        Next
    End Sub
    
    Private Sub AddTag_Click(sender As Object, e As EventArgs)
        MessageBox.Show("Scanning not fully implemented. Use 'Add Tag (Manual)' for now.")
    End Sub
    
    Private Sub AddTagManual_Click(sender As Object, e As EventArgs)
        Dim uid = InputBox("Enter Tag UID:", "Add RFID Tag")
        If String.IsNullOrEmpty(uid) Then Return
        
        Dim frm As New AddTagForm(uid)
        If frm.ShowDialog() = DialogResult.OK Then
            Catalog(uid) = frm.GetTagData()
            SaveJson(CatalogFile, Catalog)
            RefreshTagsList()
        End If
    End Sub
    
    Private Sub DeleteTag_Click(sender As Object, e As EventArgs)
        If TagsListBox.SelectedIndex = -1 Then
            MessageBox.Show("Select a tag to delete")
            Return
        End If
        
        Dim selectedText = CStr(TagsListBox.Items(TagsListBox.SelectedIndex))
        Dim uid = selectedText.Substring(selectedText.LastIndexOf("[") + 1, selectedText.Length - selectedText.LastIndexOf("[") - 2)
        
        If MessageBox.Show($"Delete tag {uid}?", "Confirm", MessageBoxButtons.YesNo) = DialogResult.Yes Then
            Catalog.Remove(uid)
            SaveJson(CatalogFile, Catalog)
            RefreshTagsList()
        End If
    End Sub
    
    Private Sub SaveSettings_Click(sender As Object, e As EventArgs)
        Config("serial_port") = PortTextBox.Text
        Config("baud_rate") = CInt(BaudTextBox.Text)
        SaveJson(ConfigFile, Config)
        MessageBox.Show("Settings saved!")
    End Sub
    
    Public Shared Sub Main()
        Application.EnableVisualStyles()
        Application.Run(New MainForm())
    End Sub
End Class

Public Class AddTagForm
    Inherits Form
    
    Private uid As String
    Private NameTextBox As TextBox
    Private ActionComboBox As ComboBox
    Private TargetTextBox As TextBox
    
    Public Sub New(uid As String)
        Me.uid = uid
        InitializeComponent()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "Add RFID Tag"
        Me.Size = New Size(400, 250)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        
        Dim y As Integer = 10
        
        Me.Controls.Add(New Label With {.Text = $"Tag UID: {uid}", .Font = New Font("Arial", 10, FontStyle.Bold), .Location = New Point(10, y), .Size = New Size(350, 25)})
        y += 35
        
        Me.Controls.Add(New Label With {.Text = "Name:", .Location = New Point(10, y), .Size = New Size(80, 20)})
        NameTextBox = New TextBox With {.Location = New Point(100, y), .Size = New Size(250, 24)}
        Me.Controls.Add(NameTextBox)
        y += 30
        
        Me.Controls.Add(New Label With {.Text = "Action Type:", .Location = New Point(10, y), .Size = New Size(80, 20)})
        ActionComboBox = New ComboBox With {.Location = New Point(100, y), .Size = New Size(250, 24), .DropDownStyle = ComboBoxStyle.DropDownList}
        ActionComboBox.Items.AddRange(New String() {"emulator", "file", "url", "command"})
        ActionComboBox.SelectedIndex = 0
        Me.Controls.Add(ActionComboBox)
        y += 30
        
        Me.Controls.Add(New Label With {.Text = "Target:", .Location = New Point(10, y), .Size = New Size(80, 20)})
        TargetTextBox = New TextBox With {.Location = New Point(100, y), .Size = New Size(250, 24)}
        Me.Controls.Add(TargetTextBox)
        y += 35
        
        Dim okBtn = New Button With {.Text = "Save", .DialogResult = DialogResult.OK, .Location = New Point(100, y), .Size = New Size(100, 30)}
        Me.Controls.Add(okBtn)
        
        Dim cancelBtn = New Button With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .Location = New Point(220, y), .Size = New Size(100, 30)}
        Me.Controls.Add(cancelBtn)
        
        Me.AcceptButton = okBtn
        Me.CancelButton = cancelBtn
    End Sub
    
    Public Function GetTagData() As Dictionary(Of String, Object)
        Return New Dictionary(Of String, Object) From {
            {"name", NameTextBox.Text},
            {"action_type", ActionComboBox.SelectedItem},
            {"action_target", TargetTextBox.Text}
        }
    End Function
End Class
