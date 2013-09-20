Imports FSUIPC
Imports System
Imports System.IO
Imports System.Text
Imports System.Net
Imports System.Xml
Imports System.Threading
Imports System.Data

Public Class SAVCARS

#Region "FSUIPC Offsets"
    Dim airSpeed As Offset(Of Integer) = New FSUIPC.Offset(Of Integer)("default", &H2BC)
    Dim pause As Offset(Of Short) = New FSUIPC.Offset(Of Short)("default", &H262)
    Dim Engine1Combustion As Offset(Of Short) = New FSUIPC.Offset(Of Short)("default", &H894) 'True if engine firing
    Dim Engine2Combustion As Offset(Of Short) = New FSUIPC.Offset(Of Short)("default", &H92C) 'True if engine firing
    Dim ParkingBrake As Offset(Of Short) = New FSUIPC.Offset(Of Short)("default", &HBC8) '0 if off, 32767 if on
    Dim PushingBack As Offset(Of Integer) = New FSUIPC.Offset(Of Integer)("default", &H31F0) '3=off, 0=pushing back, 1=pushing back, tail to swing to left (port), 2=pushing back, tail to swing to right (starboard)
    Dim OnGround As Offset(Of Short) = New FSUIPC.Offset(Of Short)("default", &H366) '0=airborne, 1=on ground
    Dim ACname As Offset(Of String) = New FSUIPC.Offset(Of String)("default", &H3160, 24)
    Dim ACModel As Offset(Of String) = New FSUIPC.Offset(Of String)("default", &H3500, 24)
    Dim LandingRate As Offset(Of Integer) = New FSUIPC.Offset(Of Integer)("default", &H30C)
    Dim altitude As Offset(Of Integer) = New FSUIPC.Offset(Of Integer)("default", &H3324)
    Dim Overspeed As Offset(Of Byte) = New FSUIPC.Offset(Of Byte)("default", &H36D) ' 0=no, 1=overspeed
    Dim stall As Offset(Of Byte) = New FSUIPC.Offset(Of Byte)("default", &H36C) ' 0=no, 1=stall
    Dim LandingGear As Offset(Of Integer) = New FSUIPC.Offset(Of Integer)("default", &HBE8) ' 0=Up, 16383=Down
    Dim GroundSpeed As Offset(Of Integer) = New FSUIPC.Offset(Of Integer)("default", &H2B4)
    Dim playerLatitude As Offset(Of Long) = New Offset(Of Long)("default", &H560)
    Dim playerLongitude As Offset(Of Long) = New Offset(Of Long)("default", &H568)
    Dim messageText As Offset(Of String) = New Offset(Of String)("textstrip", &H3380, 128, True)
    Dim messageControl As Offset(Of Short) = New Offset(Of Short)("textstrip", &H32FA, True)
#End Region

#Region "FSUIPC related"
    Dim PayloadServices As PayloadServices
    Dim WithEvents userInput As UserInputServices
    Dim lat As FsLatitude
    Dim lon As FsLongitude
    Dim StartPoint
    Dim StartLat As Long
    Dim StartLon As Long
    Dim FuelStart As Integer
    Dim FuelStartKG As Integer
    Dim FuelStartLB As Integer
    Dim FuelStartEL As Double
    Dim FuelUsedKG As Integer = 0
    Dim FuelUsedLB As Integer = 0
    Dim FuelUsedEL As Double = 0.0
#End Region

#Region "Update"
    Public NewVersion As String
    Public VersionURL As String
    Dim WithEvents WC As New WebClient
    Public VersionProgress
    Public NewSaveLocation
#End Region

    Public VATSIMID As String
    Public ScheduleDownload As Boolean = False
    Dim filePath As String = Application.StartupPath & "\SAVSchedules.xml"
    Dim SavedFlights As String = My.Computer.FileSystem.SpecialDirectories.CurrentUserApplicationData & "\Saved Flights"
    Dim FlightHistory As String = My.Computer.FileSystem.SpecialDirectories.CurrentUserApplicationData & "\Saved Flights\Flight History"
    'Public Servers As New List(Of String)
    Dim Key As String = "fe96s24stakEzegEcrusA6ePH" & DateTime.Now.AddDays(13).Day & "utranasegesp" & DateTime.Now.AddMonths(5).Month & "ustas54ava9red" & DateTime.Now.AddYears(7).Year & "r3ram48rUkaFE"
    Const PublicKey As String = "removed"
    Const FilePass As String = "removed"
    Const LoginPass As String = "removed"
    Const sKy As String = "removed"
    Const sIV As String = "removed"


    Public PauseMSG As Boolean = False
    Public ACARSstarted As Boolean = False
    Public ACARSstopped As Boolean = False
    Public LoginStatus
    Public BlocksWatch As New Stopwatch()
    Public AirWatch As New Stopwatch()
    Public landWatch As New Stopwatch()
    Public ConnectWatch As New Stopwatch()
    Public FSconnected As Boolean = False
    Public units As String = "KG"
    Public airbourne As Boolean = False
    Public SoundPlayed As Boolean = False
    Public Cruising As Boolean = False
    Public Stalled As Boolean = False
    Public Overspeeding As Boolean = False
    Public landed As Boolean = False
    Public bounced As Boolean = False
    Public GearUP As Boolean = False
    Public GearDown As Boolean = False
    Public HasBeenAirborne As Boolean = False
    Public NewVersionExit As Boolean = False
    Public AutoStartChecked As Boolean
    Public StartToneChecked As Boolean
    Public ManualChecked As Boolean
    Public AutoStartOption As Integer
    Public ToneStartOption As Integer
    Public Taxi As Boolean = False
    Public StopBounce = False
    Public NoVATSIM As Boolean = False
    Public OnlineWithList As New List(Of String)
    Dim AircraftReg As New List(Of String)
    Dim AircraftICAO As New List(Of String)

    Dim OriginalSize As New Size(512, 469)
    Dim ProgressMax As Integer
    Dim AircraftSet As DataSet = New DataSet




    ' http://www.alphastone.co.uk/severnair/phpvms/action.php/SAVCARS
    ' http://localhost/phpvms/action.php/SAVCARS



    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        My.Computer.FileSystem.CreateDirectory(SavedFlights)
        My.Computer.FileSystem.CreateDirectory(FlightHistory)
    End Sub

    Private Sub Form1_Load(sender As Object, e As System.EventArgs) Handles Me.Load

        LogText.AppendText("[" & DateTime.UtcNow.ToString("HH:mm") & "] - " & "SAVCARS " & "v" & My.Application.Info.Version.ToString & " loaded" & vbNewLine)

        LoadSettings()

        FSUIPC_WRITE_READ.Stop()

        Try
            CreateXML("versioncheck")
        Catch ex As Exception
            'MsgBox(ex.ToString)
        End Try

    End Sub

    Private Sub Form1_FormClosing(ByVal sender As System.Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
        If NewVersionExit = False Then
            If (MessageBox.Show("Are you sure you want to exit?", "SAVCARS", MessageBoxButtons.YesNo) = Windows.Forms.DialogResult.No) Then
                e.Cancel = True
            Else
                'If My.Settings.manual = True Or My.Settings.tone = True Then
                Try
                    userInput.RemoveAll()
                Catch
                End Try
                'End If
                FSUIPCConnection.Close()

                If AutoStartChecked Then
                    My.Settings.start = 0
                    My.Settings.startoption = AutoStartOption
                ElseIf StartToneChecked Then
                    My.Settings.start = 1
                    My.Settings.startoption = ToneStartOption
                ElseIf ManualChecked Then
                    My.Settings.start = 2
                End If
                My.Settings.Save()
            End If
        End If


    End Sub

    Public Sub ACARSstop()

        'Label17.Text = "Acars Stop"
        If HasBeenAirborne = True Then
            If ACARSstarted = True Then
                If Engine1Combustion.Value = False And Engine2Combustion.Value = False Then
                    If ParkingBrake.Value = "32767" Then
                        If OnGround.Value = 1 Then
                            If ACARSstopped = False Then
                                TextBox8.Text = DateTime.UtcNow.ToString("HH:mm")

                                BlocksWatch.Stop()
                                AddToLog("ACARS Stopped")
                                ACARSstarted = False
                                CreateXML("pirep")
                            End If
                        End If
                    End If
                End If
            End If
        End If
    End Sub

    Public Function AddToLog(ByVal text)
        If LoginStatus = 1 Then
            If ACARSstopped = False Then
                LogText.AppendText("[" & DateTime.UtcNow.ToString("HH:mm") & "] - " & text & vbNewLine)
            End If
            Return True
        Else : Return False
        End If
    End Function

    Public Sub LogIn()
        CreateXML("verify")
        If LoginStatus = 1 Then
            Button4.Enabled = True
        Else : Button4.Enabled = False
        End If
    End Sub

    Public Sub BingBongStart()
        messageText.Value = "SAVCARS: ACARS Started!"
        messageControl.Value = 8
        FSUIPCConnection.Process("textstrip")
        My.Computer.Audio.Play(My.Resources.ACARSStarted, AudioPlayMode.Background)
    End Sub

    Public Sub BingBongReminder()
        messageText.Value = "SAVCARS: ACARS Start Reminder!"
        messageControl.Value = 8
        FSUIPCConnection.Process("textstrip")
        My.Computer.Audio.Play(My.Resources.Alarm, AudioPlayMode.Background)
    End Sub

    Public Sub BingBongPSent()
        messageText.Value = "SAVCARS: PIREP Successfuly Sent!"
        messageControl.Value = 8
        FSUIPCConnection.Process("textstrip")
    End Sub

    Public Sub ACARSstart()
        If LoginStatus = 1 Then
            If ACARSstarted = False Then
                If FSconnected = True Then
                    If AutoStartChecked = True Then
                        Select Case AutoStartOption
                            Case Is = 0
                                If Engine1Combustion.Value = "1" Or Engine2Combustion.Value = "1" Then
                                    ACARSstarted = True
                                    If ACARSstarted = True Then
                                        AddToLog("ACARS Auto Started (Engines)")
                                        AddToLog(StrConv(ACname.Value, VbStrConv.ProperCase) & " " & ACModel.Value)
                                        BlocksWatch.Start()
                                        TextBox6.Text = DateTime.UtcNow.ToString("HH:mm")
                                        BingBongStart()
                                        HistoryTimer.Enabled = True
                                    End If
                                End If


                            Case Is = 1
                                If ParkingBrake.Value = 0 Then
                                    ACARSstarted = True
                                    If ACARSstarted = True Then
                                        AddToLog("ACARS Auto Started (Parking Brake)")
                                        AddToLog(StrConv(ACname.Value, VbStrConv.ProperCase) & " " & ACModel.Value)
                                        BlocksWatch.Start()
                                        TextBox6.Text = DateTime.UtcNow.ToString("HH:mm")
                                        BingBongStart()
                                        HistoryTimer.Enabled = True

                                    End If
                                End If


                            Case Is = 2
                                If PushingBack.Value = 0 Then
                                    ACARSstarted = True
                                    If ACARSstarted = True Then
                                        AddToLog("ACARS Auto Started (Pushback)")
                                        AddToLog(StrConv(ACname.Value, VbStrConv.ProperCase) & " " & ACModel.Value)
                                        BlocksWatch.Start()
                                        TextBox6.Text = DateTime.UtcNow.ToString("HH:mm")
                                        BingBongStart()
                                        HistoryTimer.Enabled = True

                                    End If
                                End If
                        End Select
                    End If

                    If StartToneChecked = True Then
                        If ToneStartOption = 0 Then 'Engines started
                            If Engine1Combustion.Value <> False Or Engine2Combustion.Value <> False Then
                                If SoundPlayed = False Then
                                    BingBongReminder()
                                    SoundPlayed = True
                                End If
                            End If
                        End If

                        If ToneStartOption = 1 Then 'Parking break released
                            If ParkingBrake.Value = 0 Then
                                If SoundPlayed = False Then
                                    BingBongReminder()
                                    SoundPlayed = True
                                End If
                            Else : SoundPlayed = False
                            End If
                        End If

                        If ToneStartOption = 2 Then 'Pushback initiated
                            If PushingBack.Value = 0 Then
                                If SoundPlayed = False Then
                                    BingBongReminder()
                                    SoundPlayed = True
                                End If
                            Else : SoundPlayed = False
                            End If
                        End If

                    End If
                End If
            End If
        End If
    End Sub

    Public Sub ABcheck()
        If ACARSstarted = True Then
            If StopBounce = False Then
                If landed = False Then
                    If airbourne = False Then
                        If OnGround.Value = 0 Then
                            AirWatch.Start()
                            TextBox7.Text = DateTime.UtcNow.ToString("HH:mm")
                            Dim airpeedKnots As Double = airSpeed.Value / 128D
                            AddToLog("Airborne")
                            AddToLog("Takeoff speed " & airpeedKnots.ToString("f0") & "knots")
                            airbourne = True
                            HasBeenAirborne = True
                            StartLat = playerLatitude.Value
                            StartLon = playerLongitude.Value
                            lat = New FsLatitude(StartLat)
                            lon = New FsLongitude(StartLon)
                            StartPoint = New FsLatLonPoint(lat, lon)
                        End If
                    ElseIf airbourne = True Then
                        If OnGround.Value = 1 Then
                            AirWatch.Stop()
                            TextBox9.Text = DateTime.UtcNow.ToString("HH:mm")
                            Dim airpeedKnots As Double = airSpeed.Value / 128D
                            AddToLog("Landed")
                            AddToLog("Landing speed: " & airpeedKnots.ToString("f0") & "knots")
                            AddToLog("Landing rate: " & LandingRate.Value & "fpm")
                            airbourne = False
                            landed = True
                            landWatch.Start()
                        End If
                    End If
                ElseIf landed = True Then
                    If OnGround.Value = 0 Then
                        AddToLog("Bounced")
                        landed = False
                        StopBounce = True
                    End If
                End If
            End If
        End If
    End Sub

    Public Sub CruiseCheck()
        If CruiseText.Text.Length >= 4 Then
            If ACARSstarted = True Then
                If Cruising = False Then
                    Dim cruisealt As String = CruiseText.Text - 100
                    If altitude.Value > cruisealt Then
                        AddToLog("Cruising at " & CruiseText.Text & "ft")
                        OnlineWith("CRZ")
                        Cruising = True
                    End If
                ElseIf Cruising = True Then
                    Dim cruisealt As String = CruiseText.Text - 100
                    If altitude.Value < cruisealt Then
                        AddToLog("Descending")
                        Cruising = False
                    End If
                End If
            End If
        End If
    End Sub

    Public Sub stallcheck()
        If ACARSstarted = True Then
            If Stalled = False Then
                If stall.Value = 1 Then

                    AddToLog("Stall")
                    Stalled = True

                End If
            ElseIf Stalled = True Then
                If stall.Value = 0 Then

                    AddToLog("Stall Recovered")
                    Stalled = False

                End If
            End If
        End If
    End Sub

    Public Sub Overspeedcheck()
        If ACARSstarted = True Then
            If Overspeeding = False Then
                If Overspeed.Value = 1 Then

                    AddToLog("Overspeed")
                    Overspeeding = True

                End If
            ElseIf Overspeeding = True Then
                If Overspeed.Value = 0 Then

                    AddToLog("Overspeed Corrected")
                    Overspeeding = False

                End If
            End If
        End If
    End Sub

    Public Sub Fuel()
        If ACARSstarted = True Then
            PayloadServices.RefreshData()

            If FuelStartKG = Nothing And FuelStartLB = Nothing And FuelStartEL = Nothing Then
                FuelStartKG = PayloadServices.FuelWeightKgs
                FuelStartLB = PayloadServices.FuelWeightLbs
                FuelStartEL = PayloadServices.FuelWeightKgs / 5455
            End If

            FuelUsedKG = FuelStartKG - PayloadServices.FuelWeightKgs
            FuelUsedLB = FuelStartLB - PayloadServices.FuelWeightLbs
            FuelUsedEL = FuelStartEL - (PayloadServices.FuelWeightKgs / 5455)

            If units = "KG" Then
                TextBox4.Text = FuelStartKG
                TextBox5.Text = FuelUsedKG
            ElseIf units = "LB" Then
                TextBox4.Text = FuelStartLB
                TextBox5.Text = FuelUsedLB
            ElseIf units = "EL" Then
                TextBox4.Text = FuelUsedEL
            End If

        End If
    End Sub

    Public Sub Gear()
        If ACARSstarted = True Then
            If OnGround.Value = 0 Then
                If GearUP = False Then
                    If LandingGear.Value = 0 Then
                        AddToLog("Gear Up")
                        GearUP = True
                        GearDown = False
                    End If
                ElseIf GearDown = False Then
                    If LandingGear.Value <> 0 Then
                        AddToLog("Gear Down")
                        GearDown = True
                        GearUP = False
                    End If
                End If
            End If
        End If
    End Sub

    Public Sub TaxiCheck()
        'Label11.Text = GroundSpeed.Value * 3600
        If OnGround.Value = 1 Then

            If ACARSstarted = True Then
                Dim GroundSpeedKnots As Double = GroundSpeed.Value / 65536 * 3600 / 1852
                If CInt(GroundSpeedKnots.ToString("f0")) >= 5 Then
                    If Taxi = False Then

                        Taxi = True
                        AddToLog("Taxi")
                        CreateXML("getservers")
                        OnlineWith("GND")

                    End If
                End If
            End If
        End If
    End Sub

    Public Sub DistanceCheck()
        If OnGround.Value = 0 Then
            lon = New FsLongitude(playerLongitude.Value)
            lat = New FsLatitude(playerLatitude.Value)
            Dim currentPosition As FsLatLonPoint = New FsLatLonPoint(lat, lon)

            Dim distance = currentPosition.DistanceFromInNauticalMiles(StartPoint)

            TextBox3.Text = distance.ToString("F0")
        End If
    End Sub

    Private Sub FSUIPC_WRITE_READ_Tick(sender As System.Object, e As System.EventArgs) Handles FSUIPC_WRITE_READ.Tick
        Try

            FSUIPCConnection.Process(New String() {"default"})
            ACARSstart()

            userInput = FSUIPCConnection.UserInputServices 'move these
            PayloadServices = FSUIPCConnection.PayloadServices

            Fuel()

            Paused()

            ABcheck()

            userInput.CheckForInput()

            CruiseCheck()

            stallcheck()

            Overspeedcheck()

            Gear()

            TaxiCheck()

            DistanceCheck()

            ACARSstop()

            If landWatch.Elapsed.Seconds = 15 Then
                OnlineWith("GND")
                landWatch.Stop()
                landWatch.Reset()
            End If
            If ConnectWatch.Elapsed.Seconds = 60 Then
                OnlineWith("GND")
                ConnectWatch.Stop()
                ConnectWatch.Reset()
            End If
            ToolStripStatusLabel2.Text = "Connected to FS"
            ToolStripStatusLabel2.ForeColor = Color.Green
            ToolStripStatusLabel2.Image = My.Resources.planeon
            FSconnected = True




        Catch ex As Exception
            FSUIPC_WRITE_READ.Stop()
            ToolStripStatusLabel2.Text = "FS Connection Error"
            ToolStripStatusLabel2.ForeColor = Color.Red
            ToolStripStatusLabel2.Image = My.Resources.planeoff
            ErrorMessage(ex.ToString)
            FSconnected = False
            Button4.Enabled = True

        End Try


    End Sub

    Public Sub Paused()
        If ACARSstarted = True Then
            If PauseMSG = False Then
                If pause.Value = 1 Then

                    AddToLog("Simulator Paused")
                    PauseMSG = True
                    AirWatch.Stop()
                    BlocksWatch.Stop()


                End If

            ElseIf PauseMSG = True Then
                If pause.Value = 0 Then
                    If ACARSstarted = True Then
                        AddToLog("Simulator UnPaused")
                        PauseMSG = False
                        AirWatch.Start()
                        BlocksWatch.Start()
                    End If
                End If
            End If
        End If

    End Sub

    Private Sub SettingsToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs)
        LoginDetails.Show()
        LoadSettings()


    End Sub

    Private Sub SAVCARS_DragDrop(sender As Object, e As System.Windows.Forms.DragEventArgs) Handles Me.DragDrop
        If LoginStatus = 1 Then
            If e.Data.GetDataPresent(DataFormats.FileDrop) Then
                Dim DroppedFiles() As String

                ' Assign the files to an array.
                DroppedFiles = e.Data.GetData(DataFormats.FileDrop)
                If DroppedFiles.Count > 0 Then
                    ' Loop through the array and add the files to the list.
                    'Try
                    Dim ReadCrypt As String = File.ReadAllText(DroppedFiles(0))
                    'MsgBox(ReadCrypt)
                    Dim DeCryptME As String = decrypt(ReadCrypt, FilePass)
                    If DeCryptME.Length > 0 Then
                        Dim Lines() As String = Split(DeCryptME, "*")
                        Dim callsign As String = Lines(2).Replace("Callsign: ", "")
                        Dim AC As String = Lines(3).Replace("AC: ", "")
                        Dim DEPDEST As String = Lines(4).Replace("Dep/Dest: ", "")
                        Dim DEPDEST1() As String = DEPDEST.Split("-")
                        Dim Cruise As String = Lines(5).Replace("Cruise: ", "")
                        Dim Route As String = Lines(6).Replace("Route: ", "")


                        CallsignText.Text = callsign
                        ComboBox3.SelectedIndex = ComboBox3.FindString(AC)
                        DepText.Text = DEPDEST1(0)
                        DestText.Text = DEPDEST1(1)
                        CruiseText.Text = Cruise
                        RouteText.Text = Route
                        TypeP.Checked = True
                        TypeC.Checked = False
                    End If
                    'Catch ex As Exception
                    'ErrorMessage("Corrupt file. Either the file has been manually edited or it does not have a valid .SAV extension.")
                    ' End Try

                Else : ErrorMessage("Too many files!")
                End If
            End If
        Else : ErrorMessage("You must log-in before you can import a flight plan")
        End If


    End Sub

    Private Sub SAVCARS_DragEnter(sender As Object, e As System.Windows.Forms.DragEventArgs) Handles Me.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.All
        End If
    End Sub

    Public Function GetFullID(ByVal SAV As String)
        If SavedFlights.Length > 0 Then
            SAV = SAV.Replace("SAV", "")
            If SAV.Substring(0, 1) = "0" Then
                While SAV.Substring(0, 1) = "0"
                    SAV = SAV.Substring(1)
                End While
            End If
            Select Case SAV.Length
                Case Is = "1"
                    SAV = "000" & SAV
                Case Is = "2"
                    SAV = "00" & SAV
                Case Is = "3"
                    SAV = "0" & SAV
                Case Else
                    SAV = SAV
            End Select
            Return "SAV" & SAV
        Else
            Return SAV
        End If

    End Function

    Public Sub CreateXML(ByVal DOWHAT)

        Dim request As HttpWebRequest = CType(WebRequest.Create("http://www.alphastone.co.uk/severnair/phpvms/action.php/SAVCARS"), HttpWebRequest)


        request.Method = "POST"

        request.ContentType = "application/xml"

        Dim xmlsettings As XmlWriterSettings = New XmlWriterSettings()

        xmlsettings.Indent = True

        xmlsettings.Encoding = New UTF8Encoding(False)

        Select Case DOWHAT
            Case Is = "verify", "test"

                Using xmlwrt As XmlWriter = XmlWriter.Create(request.GetRequestStream(), xmlsettings)

                    With xmlwrt


                        ' Write the Xml declaration.
                        .WriteStartDocument()

                        ' Write a comment.
                        .WriteComment(DateTime.Now)

                        ' Write the root element.
                        .WriteStartElement("SAVCARS")


                        .WriteStartElement("switch")

                        ' The person nodes.

                        .WriteStartElement("data")
                        .WriteString(DOWHAT) ' change
                        .WriteEndElement()

                        .WriteEndElement() 'switch

                        .WriteStartElement("verify")

                        .WriteStartElement("pilotID")
                        .WriteString(My.Settings.pid)
                        .WriteEndElement()

                        .WriteStartElement("password")
                        .WriteString(My.Settings.password)
                        .WriteEndElement()

                        .WriteStartElement("version")
                        .WriteString(My.Application.Info.Version.ToString)
                        .WriteEndElement()

                        .WriteEndElement() 'verify


                        .WriteEndDocument()


                        .Close()

                    End With


                End Using



            Case Is = "pirep"
                ACARSstopped = False



                Using xmlwrt As XmlWriter = XmlWriter.Create(request.GetRequestStream(), xmlsettings)

                    With xmlwrt


                        ' Write the Xml declaration.
                        .WriteStartDocument()

                        ' Write a comment.
                        .WriteComment(DateTime.Now)

                        ' Write the root element.
                        .WriteStartElement("SAVCARS")


                        .WriteStartElement("switch")

                        ' The person nodes.

                        .WriteStartElement("data")
                        .WriteString(DOWHAT) ' change
                        .WriteEndElement()

                        .WriteEndElement() 'switch

                        .WriteStartElement("verify")

                        .WriteStartElement("pilotID")
                        .WriteString(My.Settings.pid)
                        .WriteEndElement()

                        .WriteStartElement("password")
                        .WriteString(My.Settings.password)
                        .WriteEndElement()

                        .WriteEndElement() 'verify

                        .WriteStartElement("version")

                        .WriteStartElement("versionnumber")
                        .WriteString(My.Application.Info.Version.ToString)
                        .WriteEndElement()

                        .WriteEndElement() 'version


                        .WriteStartElement("pirep")

                        .WriteStartElement("pilotID")
                        .WriteString(My.Settings.pid)
                        .WriteEndElement()

                        ' Dim FlightNum As String = CallsignText.Text.Replace("SAV", "")
                        .WriteStartElement("flightNumber")
                        .WriteString(CallsignText.Text)
                        .WriteEndElement()



                        .WriteStartElement("registration")
                        Try
                            Dim reg As String = ComboBox3.Items(ComboBox3.SelectedIndex)
                            .WriteString(reg.Substring(0, 6))
                            'Console.WriteLine(reg.Substring(0, 6))
                        Catch
                        End Try
                        .WriteEndElement()



                        .WriteStartElement("depICAO")
                        .WriteString(DepText.Text)
                        .WriteEndElement()

                        .WriteStartElement("arrICAO")
                        .WriteString(DestText.Text)
                        .WriteEndElement()

                        .WriteStartElement("flightTime")
                        .WriteString(TextBox2.Text)
                        .WriteEndElement()


                        .WriteStartElement("flightType")
                        If TypeP.Checked = True Then
                            .WriteString("P")
                        ElseIf TypeC.Checked = True Then
                            .WriteString("C")
                        Else
                            .WriteString("P")
                        End If

                        .WriteEndElement()


                        .WriteStartElement("fuelUsed")
                        .WriteString(FuelUsedLB)
                        .WriteEndElement()


                        .WriteStartElement("load")
                        .WriteString(pax.Text)
                        .WriteEndElement()


                        .WriteStartElement("landing")
                        .WriteString(LandingRate.Value)
                        .WriteEndElement()


                        .WriteStartElement("comments")

                        .WriteString(TextBox1.Text)

                        .WriteEndElement()

                        Dim NewLogText As String = LogText.Text
                        'If VATSIMID = 0 Then
                        '    NewLogText = NewLogText & "[Note] - Flight conducted without a valid VATSIM-ID" & vbNewLine
                        'End If


                        'Dim Low As Double = NumberOfCheck - NotOn
                        'If Low > 0 Then
                        '    NewLogText = NewLogText & "[Note] - Online percentage: " & FormatPercent(Low / NumberOfCheck, 0) & vbNewLine
                        'Else : NewLogText = NewLogText & "[Note] - Online percentage: " & "0%" & vbNewLine
                        'End If



                        NewLogText = NewLogText.Replace(vbNewLine, "*")

                        .WriteStartElement("log")
                        .WriteString(NewLogText)
                        .WriteEndElement()

                        .WriteEndElement() 'pirep

                        .WriteEndDocument()


                        .Close()

                    End With


                End Using

            Case Is = "aircraft"
                Using xmlwrt As XmlWriter = XmlWriter.Create(request.GetRequestStream(), xmlsettings)

                    With xmlwrt


                        ' Write the Xml declaration.
                        .WriteStartDocument()

                        ' Write a comment.
                        .WriteComment(DateTime.Now)

                        ' Write the root element.
                        .WriteStartElement("SAVCARS")


                        .WriteStartElement("switch")

                        ' The person nodes.

                        .WriteStartElement("data")
                        .WriteString(DOWHAT) ' change
                        .WriteEndElement()

                        .WriteEndElement() 'switch


                        .WriteEndDocument()


                        .Close()

                    End With


                End Using

            Case Is = "vatsim"

                Using xmlwrt As XmlWriter = XmlWriter.Create(request.GetRequestStream(), xmlsettings)

                    With xmlwrt


                        ' Write the Xml declaration.
                        .WriteStartDocument()

                        ' Write a comment.
                        .WriteComment(DateTime.Now)

                        ' Write the root element.
                        .WriteStartElement("SAVCARS")

                        .WriteStartElement("switch")

                        ' The person nodes.

                        .WriteStartElement("data")
                        .WriteString(DOWHAT) ' change
                        .WriteEndElement()

                        .WriteEndElement() 'switch

                        .WriteStartElement("pirep")


                        .WriteStartElement("ID")
                        .WriteString(VATSIMID)
                        .WriteEndElement()


                        .WriteEndElement() 'pirep

                        .WriteEndDocument()


                        .Close()

                    End With


                End Using

            Case Is = "versioncheck"
                Using xmlwrt As XmlWriter = XmlWriter.Create(request.GetRequestStream(), xmlsettings)

                    With xmlwrt


                        ' Write the Xml declaration.
                        .WriteStartDocument()

                        ' Write a comment.
                        .WriteComment(DateTime.Now)

                        ' Write the root element.
                        .WriteStartElement("SAVCARS")


                        .WriteStartElement("switch")

                        ' The person nodes.

                        .WriteStartElement("data")
                        .WriteString(DOWHAT) ' change
                        .WriteEndElement()

                        .WriteEndElement() 'switch


                        .WriteStartElement("version")

                        .WriteStartElement("versionnumber")
                        .WriteString(My.Application.Info.Version.ToString)
                        .WriteEndElement()

                        .WriteEndElement() 'version


                        .WriteEndDocument()


                        .Close()

                    End With


                End Using

            Case Is = "schedules"
                Using xmlwrt As XmlWriter = XmlWriter.Create(request.GetRequestStream(), xmlsettings)

                    With xmlwrt


                        ' Write the Xml declaration.
                        .WriteStartDocument()

                        ' Write a comment.
                        .WriteComment(DateTime.Now)

                        ' Write the root element.
                        .WriteStartElement("SAVCARS")


                        .WriteStartElement("switch")

                        .WriteStartElement("data")
                        .WriteString(DOWHAT) ' change
                        .WriteEndElement()

                        .WriteEndElement() 'switch

                        .WriteStartElement("verify")

                        .WriteStartElement("pilotID")
                        .WriteString(My.Settings.pid)
                        .WriteEndElement()

                        .WriteStartElement("password")
                        .WriteString(My.Settings.password)
                        .WriteEndElement()

                        .WriteEndElement() 'verify


                        .WriteStartElement("schedules")

                        .WriteStartElement("status")
                        .WriteString("False") ' change
                        .WriteEndElement()

                        .WriteEndElement() 'status


                        .WriteEndDocument()


                        .Close()

                    End With


                End Using

                ScheduleDownload = False


        End Select

        Dim response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)

        Dim postreqreader As New StreamReader(response.GetResponseStream())


        Dim ReceiveContent As String = postreqreader.ReadToEnd

        Console.Write(ReceiveContent & vbNewLine)
        ReadXML(DOWHAT, ReceiveContent)


    End Sub

    Public Sub ReadXML(ByVal DOWHAT, ByVal Resp)
        Dim reader As StringReader = New StringReader(Resp)
        Select Case DOWHAT
            Case Is = "verify"

                Dim m_xmlr As XmlTextReader
                'Create the XML Reader
                m_xmlr = New XmlTextReader(reader)
                'Disable whitespace so that you don't have to read over whitespaces
                m_xmlr.WhitespaceHandling = WhitespaceHandling.None
                m_xmlr.Read()
                m_xmlr.Read()
                m_xmlr.Read()
                m_xmlr.Read()
                LoginStatus = m_xmlr.ReadElementString("loginStatus")
                VATSIMID = m_xmlr.ReadElementString("VATSIMID")
                Try
                    ScheduleDownload = m_xmlr.ReadElementString("Schedule_Download")
                Catch
                End Try
                m_xmlr.Close()

                If LoginStatus = 1 Then
                    ToolStripStatusLabel1.ForeColor = Color.Green
                    ToolStripStatusLabel1.Text = "Logged In"
                    ToolStripStatusLabel1.Image = My.Resources.personon
                    LogText.AppendText("[" & DateTime.UtcNow.ToString("HH:mm") & "] - " & "Log-in successful" & vbNewLine)
                    Button3.Enabled = False
                    If VATSIMID = 0 Then
                        ErrorMessage("There is no VATSIM-ID associated with your account. You may conduct this flight without an ID but before your next flight you should visit the SEVERNAIR Pilot Center and click 'Edit My Profile' to add your VATSIM-ID")
                    Else
                        LogText.AppendText("[" & DateTime.UtcNow.ToString("HH:mm") & "] - " & "VATSIM-ID obtained: " & VATSIMID & vbNewLine)
                    End If
                    If ScheduleDownload = True Then
                        Button10.BackgroundImage = My.Resources.listg
                    ElseIf File.Exists(filePath) = False Then
                        Button10.BackgroundImage = My.Resources.listg
                    Else : Button10.BackgroundImage = My.Resources.list
                    End If

                    CreateXML("aircraft")


                    CallsignText.AutoCompleteCustomSource.Add(CompletePID())



                ElseIf LoginStatus = 0 Then
                    ErrorMessage("There was an error logging in. Check to make sure your credentials are correct")
                    ToolStripStatusLabel1.ForeColor = Color.Red
                    ToolStripStatusLabel1.Text = "Log-in error"
                    ToolStripStatusLabel1.Image = My.Resources.personoff
                    Button3.Enabled = True
                End If


            Case Is = "pirep"
                'Data is populated in createxml so...
                '...read Response only!
                Dim PIREPSTATUS As Byte

                Dim m_xmlr As XmlTextReader
                'Create the XML Reader
                m_xmlr = New XmlTextReader(reader)
                'Disable whitespace so that you don't have to read over whitespaces
                m_xmlr.WhitespaceHandling = WhitespaceHandling.None
                'read the xml declaration and advance to sitedata
                m_xmlr.Read()
                m_xmlr.Read()
                m_xmlr.Read()
                m_xmlr.Read()
                PIREPSTATUS = m_xmlr.ReadElementString("pirepStatus")
                m_xmlr.Close()
                If PIREPSTATUS = 1 Then
                    ACARSstopped = False
                    AddToLog("PIREP successfully sent!")
                    FSUIPC_WRITE_READ.Enabled = False
                    LoginStatus = 0
                    Button3.Enabled = True
                    FSconnected = False
                    Button4.Enabled = True
                    ToolStripStatusLabel2.Text = ""
                    ToolStripStatusLabel2.Image = Nothing
                    ToolStripStatusLabel1.Text = ""
                    ToolStripStatusLabel1.Image = Nothing
                    LogText.BackColor = Color.FromArgb(192, 255, 192)
                    BingBongPSent()
                    HistoryTimer.Enabled = False
                ElseIf PIREPSTATUS = 2 Then
                    If (MessageBox.Show("There was an error sending your PIREP. Would you like to retry?" & vbNewLine & "If sending fails again you can find an encrypted file with all your flight information in " & FlightHistory & ". This file should be sent to an administrator who can credit you the appropriate amount of hours.", "SAVCARS", MessageBoxButtons.YesNo) = Windows.Forms.DialogResult.Yes) Then
                        CreateXML("pirep")
                    End If
                End If
                ACARSstopped = True
            Case Is = "aircraft"
                ComboBox1.Items.Clear()
                ComboBox2.Items.Clear()
                ComboBox3.Items.Clear()


                Dim m_xmlr As XmlTextReader
                m_xmlr = New XmlTextReader(reader)
                Dim AC As String = Nothing
                Dim REG As String = Nothing
                Dim i As Integer = 0
                While m_xmlr.Read
                    Select Case m_xmlr.Name
                        Case "aircraftReg"
                            If m_xmlr.IsStartElement() Then
                                Dim AircraftReg = m_xmlr.ReadElementString("aircraftReg")
                                ComboBox1.Items.Add(AircraftReg)
                            End If
                    End Select
                End While
                m_xmlr.Close()
                Dim reader2 As StringReader = New StringReader(Resp)
                Dim m_xmlr2 As XmlTextReader
                m_xmlr2 = New XmlTextReader(reader2)
                While m_xmlr2.Read
                    Select Case m_xmlr2.Name
                        Case "aircraftICAO"
                            If m_xmlr2.IsStartElement() Then
                                Dim AircraftICAO = m_xmlr2.ReadElementString("aircraftICAO")
                                ComboBox2.Items.Add(AircraftICAO)
                            End If
                    End Select
                End While
                m_xmlr2.Close()
                For s As Integer = 0 To ComboBox1.Items.Count - 1
                    ComboBox3.Items.Add(ComboBox1.Items(s) & "/" & ComboBox2.Items(s))
                Next



            Case Is = "vatsim"

                Dim m_xmlr As XmlTextReader
                'Create the XML Reader
                m_xmlr = New XmlTextReader(reader)
                'Disable whitespace so that you don't have to read over whitespaces
                m_xmlr.WhitespaceHandling = WhitespaceHandling.None
                'read the xml declaration and advance to family tag
                m_xmlr.Read()
                'read the family tag
                m_xmlr.Read()
                'Load the Loop
                'm_xmlr.Read()
                If m_xmlr.ReadElementString("available") = 1 Then

                    While Not m_xmlr.EOF

                        'm_xmlr.Read()
                        If Not m_xmlr.IsStartElement() Then
                            Exit While
                        End If
                        'm_xmlr.Read()

                        Dim Callsign = m_xmlr.ReadElementString("callsign")
                        Dim DepICAO = m_xmlr.ReadElementString("depicao")
                        Dim ArrICAO = m_xmlr.ReadElementString("arricao")
                        Dim Altitude = m_xmlr.ReadElementString("altitude")
                        Dim Route = m_xmlr.ReadElementString("route")
                        Dim ac = m_xmlr.ReadElementString("ac")

                        CallsignText.Text = Callsign
                        DepText.Text = DepICAO
                        DestText.Text = ArrICAO
                        CruiseText.Text = FlightLvlSort(Altitude)
                        RouteText.Text = Route

                        If ac.Substring(1, 1) = "/" Then
                            ac = ac.Substring(2)
                        End If


                        If ac.Substring(4, 1) = "/" Then
                            ac = ac.Substring(0, 4)
                        End If
                        ComboBox3.SelectedIndex = ComboBox2.FindString(ac)
                        TypeP.Checked = True
                        TypeC.Checked = False
                    End While
                Else
                    ErrorMessage("There was an error downloading your flight plan data from VATSIM." & vbNewLine & Chr(149) & " Ensure that you are connected to VATSIM." & vbNewLine & Chr(149) & " Leave at least 2 minutes after sending a flight plan before you attempt to download your flight plan details." & vbNewLine & vbNewLine & "Alternatively the server may be unavailable at this time.")
                End If
                m_xmlr.Close()

            Case Is = "versioncheck"
                Dim m_xmlr As XmlTextReader
                m_xmlr = New XmlTextReader(reader)
                m_xmlr.WhitespaceHandling = WhitespaceHandling.None
                m_xmlr.Read()
                m_xmlr.Read()
                m_xmlr.Read()
                m_xmlr.Read()

                If m_xmlr.ReadElementString("newversionavailable") = 1 Then

                    NewVersion = m_xmlr.ReadElementString("versionavailable")
                    VersionURL = m_xmlr.ReadElementString("url")
                    If MsgBox("Version " & NewVersion & " is now available!" & vbNewLine & "Would you like to download the installer now?", MsgBoxStyle.YesNo, "SAVCARS - New version available!") = MsgBoxResult.Yes Then
                        SaveFileDialog2.FileName = "SAVCARS_" & NewVersion.Replace(".", "_") & "_installer"
                        SaveFileDialog2.ShowDialog()
                    End If

                End If


                m_xmlr.Close()

            Case Is = "test"
                Dim m_xmlr As XmlTextReader
                m_xmlr = New XmlTextReader(reader)
                m_xmlr.WhitespaceHandling = WhitespaceHandling.None
                m_xmlr.Read()
                m_xmlr.Read()
                m_xmlr.Read()
                m_xmlr.Read()
                LoginStatus = m_xmlr.ReadElementString("loginStatus")
                m_xmlr.Close()
                If LoginStatus = 1 Then
                    LoginDetails.Label6.Text = "Verified!"
                    LoginDetails.Label6.ForeColor = Color.Green
                    LoginDetails.PID.BackColor = Color.FromArgb(192, 255, 192)
                    LoginDetails.Pass.BackColor = Color.FromArgb(192, 255, 192)
                ElseIf LoginStatus = 0 Then
                    LoginDetails.Label6.Text = "Error!"
                    LoginDetails.Label6.ForeColor = Color.Red
                    LoginDetails.PID.BackColor = Color.FromArgb(255, 192, 192)
                    LoginDetails.Pass.BackColor = Color.FromArgb(255, 192, 192)
                End If
                LoginStatus = 0
                m_xmlr.Close()

        End Select
    End Sub

    Public Function FlightLvlSort(ByVal FL As String)
        FL = FL.Trim({"F"c, "L"c, "V"c, "R"c, "f"c, "l"c, "v"c, "r"c})
        If FL.Length = 2 Or FL.Length = 3 Then
            FL = FL & "00"
        ElseIf FL.Length = 1 Then
            FL = FL & "000"
        End If
        Return FL
    End Function

    Public Function CompletePID()
        Try
            Dim AutoComp As String = My.Settings.pid

            AutoComp = AutoComp.Replace("SAV", "")
            While AutoComp.Substring(0, 1) = 0
                AutoComp = AutoComp.Substring(1)
            End While

            If AutoComp.Length = 1 Then
                AutoComp = "0" & AutoComp
            End If

            AutoComp = "SAV" & AutoComp & "P"
            Return AutoComp
        Catch
            Return My.Settings.pid
        End Try
    End Function

    Public Sub LoadSettings()

        Label1.Text = GetFullID(My.Settings.pid)

        If My.Settings.start = 0 Then
            AutoStartChecked = True
            AutoStartToolStripMenuItem1.Checked = True
            If My.Settings.startoption = 0 Then
                WhenEnginesStartedToolStripMenuItem2.Checked = True
                AutoStartOption = 0
            ElseIf My.Settings.startoption = 1 Then
                WhenParkingBrakeReleasedToolStripMenuItem2.Checked = True
                AutoStartOption = 1
            ElseIf My.Settings.startoption = 2 Then
                WhenPushbackInitiatedToolStripMenuItem1.Checked = True
                AutoStartOption = 2
            End If
        ElseIf My.Settings.start = 1 Then
            StartToneChecked = True
            ToneToolStripMenuItem1.Checked = True
            If My.Settings.toneoption = 0 Then
                WhenEnginesStartedToolStripMenuItem3.Checked = True
                ToneStartOption = 0
            ElseIf My.Settings.toneoption = 1 Then
                WhenParkingBrakeReleasedToolStripMenuItem3.Checked = True
                ToneStartOption = 1
            ElseIf My.Settings.toneoption = 2 Then
                WhenPushbackInitiatedToolStripMenuItem2.Checked = True
                ToneStartOption = 2
            End If
        ElseIf My.Settings.start = 2 Then
            ManualChecked = True
            ManualToolStripMenuItem.Checked = True
        End If

        Label13.Text = units
        Label14.Text = units
        'LogText.AppendText("[" & DateTime.UtcNow.ToString("H:mm") & "] - " & "SAVCARS loaded." & vbNewLine)
        'DateTime.UtcNow


        If My.Settings.autologin = True Then
            If LoginStatus = 0 Then
                LogIn()
            End If
        End If

        If ManualChecked Or StartToneChecked Then
            Button5.Visible = True
        Else : Button5.Visible = False
        End If

    End Sub

    Private Sub RouteText_TextChanged(sender As System.Object, e As System.EventArgs) Handles RouteText.TextChanged
        If RouteText.Text.Length > 0 Then
            RouteText.BackColor = Color.White
        Else
            RouteText.BackColor = Color.FromArgb(255, 192, 192)


        End If
    End Sub

    Private Sub Button3_Click(sender As System.Object, e As System.EventArgs) Handles Button3.Click
        If LoginStatus = 1 Then
            ErrorMessage("You're already logged in!")
        Else
            LogIn()
        End If


    End Sub

    Public Sub FSCONNECT()
        Try
            FSUIPCConnection.Open()
            'ToolStripStatusLabel2.Text = "Connected to FS"
            'ToolStripStatusLabel2.ForeColor = Color.Green
            'Button4.Enabled = False
            FSUIPC_WRITE_READ.Enabled = True
            FSUIPC_WRITE_READ.Start()
            FSconnected = True
            ConnectWatch.Start()
            Button4.Enabled = False

            LogText.AppendText("[" & DateTime.UtcNow.ToString("HH:mm") & "] - " & "FS Connected" & vbNewLine)
            ' First parameter is the ID we use to identify this menu item
            ' Second parameter is the Text for the menu item (Max 30 chars)
            ' use & before a letter to make it the shortcut key
            ' Third parameter specifies whether or not to pause FS when
            ' the menu item is selected.
            If My.Settings.manual = True Or My.Settings.tone = True Then
                userInput.AddMenuItem("SAVCARS", "SAVCARS Manual Start", False)
            End If

            Exit Try


        Catch ex As Exception
            ErrorMessage("There was an error connecting to FS. Ensure FS is running then attempt to connect again.")
            FSconnected = False
            FSUIPCConnection.Close()
            ToolStripStatusLabel2.Text = "FS Connection Error"
            ToolStripStatusLabel2.ForeColor = Color.Red
            ToolStripStatusLabel2.Image = My.Resources.planeoff
            Button4.Enabled = True
            FSUIPC_WRITE_READ.Stop()
            FSUIPC_WRITE_READ.Enabled = False

        End Try
    End Sub

    Public Sub ErrorMessage(ByVal msg)
        MessageBox.Show(msg, "SAVCARS", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1)
    End Sub

    Private Sub Button4_Click(sender As System.Object, e As System.EventArgs) Handles Button4.Click

        FSCONNECT()

    End Sub

    Private Sub menuItemSelected(ByVal sender As Object, ByVal e As UserInputMenuEventArgs) Handles userInput.MenuSelected
        ManStart()
    End Sub

    Private Sub MiscTimer_Tick(sender As System.Object, e As System.EventArgs) Handles MiscTimer.Tick
        TextBox2.Text = String.Format("{0:00}:{1:00}", BlocksWatch.Elapsed.Hours, BlocksWatch.Elapsed.Minutes)
        TextBox10.Text = String.Format("{0:00}:{1:00}", AirWatch.Elapsed.Hours, AirWatch.Elapsed.Minutes)
        If FSconnected = True Then
            If My.Settings.manual = True Or My.Settings.tone = True Then
                Try
                    userInput.KeepMenuItemsAlive()
                Catch ex As Exception
                End Try
            End If
        End If

    End Sub

    Private Sub NewPIREPToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs)


        CallsignText.Text = Nothing
        DepText.Text = Nothing
        CruiseText.Text = Nothing
        DestText.Text = Nothing
        RouteText.Text = Nothing

        LogText.Clear()
        AddToLog("SAVCARS Loaded")


    End Sub

    Private Sub CallsignText_TextChanged(sender As System.Object, e As System.EventArgs) Handles CallsignText.TextChanged
        If CallsignText.Text.Length > 3 Then
            CallsignText.BackColor = Color.White

        Else
            CallsignText.BackColor = Color.FromArgb(255, 192, 192)
        End If
    End Sub

    Private Sub CruiseText_LostFocus(sender As Object, e As System.EventArgs) Handles CruiseText.LostFocus
        CruiseText.Text = FlightLvlSort(CruiseText.Text)
    End Sub

    Private Sub CruiseText_TextChanged(sender As System.Object, e As System.EventArgs) Handles CruiseText.TextChanged
        If CruiseText.Text.Length > 0 Then
            CruiseText.BackColor = Color.White
        Else
            CruiseText.BackColor = Color.FromArgb(255, 192, 192)

        End If
    End Sub

    Private Sub DepText_TextChanged(sender As System.Object, e As System.EventArgs) Handles DepText.TextChanged
        If DepText.Text.Length = 4 Then
            DepText.BackColor = Color.White
        Else
            DepText.BackColor = Color.FromArgb(255, 192, 192)

        End If
        'GetLATLON()
    End Sub

    Private Sub DestText_TextChanged(sender As System.Object, e As System.EventArgs) Handles DestText.TextChanged
        If DestText.Text.Length = 4 Then
            DestText.BackColor = Color.White
        Else
            DestText.BackColor = Color.FromArgb(255, 192, 192)

        End If
        'GetLATLON()
    End Sub

    Public Sub ManStart()
        If LoginStatus = 1 Then
            If FSconnected = True Then
                If ACARSstarted = False Then
                    AddToLog("ACARS Manually Started")
                    AddToLog(StrConv(ACname.Value, VbStrConv.ProperCase) & " " & ACModel.Value)
                    ACARSstarted = True
                    TextBox6.Text = DateTime.UtcNow.ToString("HH:mm")
                    BlocksWatch.Start()
                    Button5.Enabled = False
                    BingBongStart()
                    HistoryTimer.Enabled = True
                End If
            End If
        Else : ErrorMessage("You are not logged in.")
        End If
    End Sub

    Private Sub SavePIREPToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles SavePIREPToolStripMenuItem.Click
        SavePIREPclicked()
    End Sub

    Public Sub SavePIREPclicked()
        If DetailsCheck() = True Then
            SaveFileDialog1.InitialDirectory = SavedFlights
            SaveFileDialog1.FileName = DepText.Text & "-" & DestText.Text & " " & DateTime.UtcNow.ToString("dd-MM-yyyy")
            'SaveFileDialog1.FileName = CallsignText.Text & " " & DepText.Text & "-" & DestText.Text
            SaveFileDialog1.ShowDialog()


        Else : ErrorMessage("You must input your callsign, departure airfield, destination airfield, cruise level and route before saving your PIREP")
        End If
    End Sub

    Public Function DetailsCheck()
        If CallsignText.Text.Length > 3 And DepText.Text.Length = 4 And DestText.Text.Length = 4 And RouteText.Text.Length > 0 And CruiseText.Text.Length > 0 Then
            Return True
        Else : Return False
        End If
    End Function

    Private Sub SaveHistory()
        Dim ByteMe As Byte() = encrypt(DateTime.UtcNow.ToString("dd/MM/yyyy") _
                                                    & vbNewLine _
                                                    & DateTime.UtcNow.ToString("HH:mm") _
                                                    & vbNewLine _
                                                    & "Callsign: " & CallsignText.Text _
                                                    & vbNewLine _
                                                    & "AC: " & ComboBox3.SelectedItem _
                                                    & vbNewLine _
                                                    & "Dep/Dest: " & DepText.Text & "-" & DestText.Text _
                                                    & vbNewLine _
                                                    & "Cruise: " & CruiseText.Text _
                                                    & vbNewLine _
                                                    & "Route: " & RouteText.Text _
                                                    & vbNewLine _
                                                    & "Off blocks: " & TextBox6.Text _
                                                    & vbNewLine _
                                                    & "Stand-Stand time: " & TextBox2.Text _
                                                    & vbNewLine _
                                                    & "Takeoff time: " & TextBox7.Text _
                                                    & vbNewLine _
                                                    & "Airborne time: " & TextBox10.Text _
                                                    & vbNewLine _
                                                    & "Landing time: " & TextBox9.Text _
                                                    & vbNewLine _
                                                    & "On stand: " & TextBox8.Text _
                                                    & vbNewLine _
                                                    & "Fuel start: " & TextBox4.Text _
                                                    & vbNewLine _
                                                    & "Fuel used: " & TextBox5.Text _
                                                    & vbNewLine _
                                                    & "===========================" _
                                                    & vbNewLine _
                                                    & LogText.Text _
                                                    & vbNewLine _
                                                    & "===========================", FilePass)
        Dim fin As String = Convert.ToBase64String(ByteMe)
        File.WriteAllText(FlightHistory & "\" & DepText.Text & " " & DestText.Text & DateTime.UtcNow.ToString("dd-MM-yyyy HHmm"), fin)
    End Sub

    Private Sub SaveFileDialog1_FileOk(sender As System.Object, e As System.ComponentModel.CancelEventArgs) Handles SaveFileDialog1.FileOk
        'Dim filetosave As String = SaveFileDialog1.FileName
        'Dim Writer As New System.IO.StreamWriter(filetosave)
        'Writer.Write(DateTime.UtcNow.ToString("dd/MM/yyyy") _
        '& vbNewLine _
        '& DateTime.UtcNow.ToString("HH:mm") _
        '& vbNewLine _
        '& "Callsign: " & CallsignText.Text _
        '& vbNewLine _
        '& "AC: " & ComboBox3.SelectedItem _
        '& vbNewLine _
        '& "Dep/Dest: " & DepText.Text & "-" & DestText.Text _
        '& vbNewLine _
        '& "Cruise: " & CruiseText.Text _
        '& vbNewLine _
        '& "Route: " & RouteText.Text _
        '& vbNewLine _
        '& "Off blocks: " & TextBox6.Text _
        '& vbNewLine _
        '& "Stand-Stand time: " & TextBox2.Text _
        '& vbNewLine _
        '& "Takeoff time: " & TextBox7.Text _
        '& vbNewLine _
        '& "Airborne time: " & TextBox10.Text _
        '& vbNewLine _
        '& "Landing time: " & TextBox9.Text _
        '& vbNewLine _
        '& "On stand: " & TextBox8.Text _
        '& vbNewLine _
        '& "Fuel start: " & TextBox4.Text _
        '& vbNewLine _
        '& "Fuel used: " & TextBox5.Text _
        '& vbNewLine _
        '& "===========================" _
        '& vbNewLine _
        '& LogText.Text _
        '& vbNewLine _
        '& "===========================", FilePass)
        'Writer.Close()

        Dim sTextVal As String = DateTime.UtcNow.ToString("dd/MM/yyyy") _
                                                            & vbNewLine _
                                                            & DateTime.UtcNow.ToString("HH:mm") _
                                                            & vbNewLine _
                                                            & "Callsign: " & CallsignText.Text _
                                                            & vbNewLine _
                                                            & "AC: " & ComboBox3.SelectedItem _
                                                            & vbNewLine _
                                                            & "Dep/Dest: " & DepText.Text & "-" & DestText.Text _
                                                            & vbNewLine _
                                                            & "Cruise: " & CruiseText.Text _
                                                            & vbNewLine _
                                                            & "Route: " & RouteText.Text _
                                                            & vbNewLine _
                                                            & "Off blocks: " & TextBox6.Text _
                                                            & vbNewLine _
                                                            & "Stand-Stand time: " & TextBox2.Text _
                                                            & vbNewLine _
                                                            & "Takeoff time: " & TextBox7.Text _
                                                            & vbNewLine _
                                                            & "Airborne time: " & TextBox10.Text _
                                                            & vbNewLine _
                                                            & "Landing time: " & TextBox9.Text _
                                                            & vbNewLine _
                                                            & "On stand: " & TextBox8.Text _
                                                            & vbNewLine _
                                                            & "Fuel start: " & TextBox4.Text _
                                                            & vbNewLine _
                                                            & "Fuel used: " & TextBox5.Text _
                                                            & vbNewLine _
                                                            & "===========================" _
                                                            & vbNewLine _
                                                            & LogText.Text _
                                                            & vbNewLine _
                                                            & "==========================="
        sTextVal = sTextVal.Replace(vbNewLine, "*")
        File.WriteAllText(SaveFileDialog1.FileName, EncryptRJ256(sKy, sIV, sTextVal))

    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
    End Sub

    Private Sub ComboBox3_Click(sender As Object, e As System.EventArgs) Handles ComboBox3.Click
        If LoginStatus = 0 Then
            ErrorMessage("This list will populate when you log in")
        End If
    End Sub

    Private Sub ComboBox3_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles ComboBox3.SelectedIndexChanged
        If ComboBox3.Text.Length > 0 Then
            ComboBox3.BackColor = Color.White
        Else
            ComboBox3.BackColor = Color.FromArgb(255, 192, 192)
        End If
        ' ComboBox1.SelectedIndex = ComboBox3.SelectedIndex
    End Sub

    Private Sub AboutToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles AboutToolStripMenuItem.Click
        About.Show()
    End Sub

    Private Sub Button2_Click(sender As System.Object, e As System.EventArgs)
        SavePIREPclicked()
    End Sub

    Private Sub Button5_Click_1(sender As System.Object, e As System.EventArgs) Handles Button5.Click
        ManStart()
    End Sub

    Private Sub SaveFileDialog2_FileOk(sender As System.Object, e As System.ComponentModel.CancelEventArgs) Handles SaveFileDialog2.FileOk
        WC.DownloadFileAsync(New Uri(VersionURL), SaveFileDialog2.FileName)
    End Sub

    Private Sub WC_DownloadFileCompleted(sender As Object, e As System.ComponentModel.AsyncCompletedEventArgs) Handles WC.DownloadFileCompleted
        If MsgBox("Download complete, would you like to install version " & NewVersion & " now?", MsgBoxStyle.YesNo, "SAVCARS - New version available!") = MsgBoxResult.Yes Then
            NewVersionExit = True
            Me.Close()
            Shell(SaveFileDialog2.FileName, AppWinStyle.NormalFocus)
        End If
    End Sub

    Private Sub NotifyIcon1_MouseDoubleClick(sender As System.Object, e As System.Windows.Forms.MouseEventArgs) Handles NotifyIcon1.MouseDoubleClick
        Me.Show()
        'Me.Activate()
        Me.WindowState = FormWindowState.Normal
        NotifyIcon1.Visible = False
    End Sub

    Private Sub SAVCARS_Resize(sender As Object, e As System.EventArgs) Handles Me.Resize
        If Me.WindowState = FormWindowState.Minimized Then
            Me.WindowState = FormWindowState.Minimized
            NotifyIcon1.Visible = True
            Me.Hide()
        End If
    End Sub

    Private Sub NewPIREPToolStripMenuItem_Click_1(sender As System.Object, e As System.EventArgs) Handles NewPIREPToolStripMenuItem.Click

        newpirep()
    End Sub

    Public Sub newpirep()
        Try
            FSUIPCConnection.Close()
        Catch ex As Exception

        End Try
        landWatch.Reset()
        ConnectWatch.Reset()
        ToolStripStatusLabel2.Text = ""
        ToolStripStatusLabel2.Image = Nothing
        ToolStripStatusLabel1.Text = ""
        ToolStripStatusLabel1.Image = Nothing
        Button4.Enabled = True
        LoginStatus = 0
        Button3.Enabled = True
        FSUIPC_WRITE_READ.Enabled = False
        FuelStart = Nothing
        FuelStartKG = Nothing
        FuelStartLB = Nothing
        FuelStartEL = Nothing
        FuelUsedKG = 0
        FuelUsedLB = 0
        FuelUsedEL = 0

        PauseMSG = False
        ACARSstarted = False
        ACARSstopped = False
        BlocksWatch.Stop()
        BlocksWatch.Reset()
        AirWatch.Stop()
        AirWatch.Reset()
        FSconnected = False
        units = Nothing
        airbourne = False
        SoundPlayed = False
        Cruising = False
        Stalled = False
        Overspeeding = False
        landed = False
        bounced = False
        GearUP = False
        GearDown = False
        HasBeenAirborne = False
        Taxi = False
        StopBounce = False
        LoadSettings()
        CallsignText.Clear()
        DepText.Clear()
        DestText.Clear()
        CruiseText.Clear()
        pax.Clear()
        RouteText.Clear()
        TextBox4.Clear()
        TextBox5.Clear()
        TextBox6.Text = "00:00"
        TextBox7.Text = "00:00"
        TextBox8.Text = "00:00"
        TextBox9.Text = "00:00"
        TextBox2.Text = "00:00"
        TextBox10.Text = "00:00"
        'TextBox3.Text = "0nm"
        'ETD.Text = "00:00"
        'ETA.Text = "00:00"
        TextBox1.Clear()
        LogText.Clear()
        'SetProgress(0)
        LogText.BackColor = Color.White

        LogText.AppendText("[" & DateTime.UtcNow.ToString("HH:mm") & "] - " & "SAVCARS " & "v" & My.Application.Info.Version.ToString & " loaded" & vbNewLine)

    End Sub

    Private Sub FSINN_CHECK_TICK(sender As System.Object, e As System.EventArgs) Handles FSINN_CHECK.Tick

        'If TCP_CHECK() Then
        '    NumberOfCheck += 1
        'Else : NotOn += 1
        'End If



    End Sub

    Private Sub TextBox1_KeyDown(sender As Object, e As System.Windows.Forms.KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            Try





                Dim ReadCrypt As String = TextBox1.Text
                Dim DeCryptME As String = decrypt(ReadCrypt, Key)
                Dim TaskTime() As String = DeCryptME.Split("-")
                Dim KeyDate As DateTime = TaskTime(0)

                If KeyDate.AddMinutes(10) > DateTime.UtcNow Then
                    Select Case TaskTime(1)
                        Case Is = "Admin"
                            MsgBox(KeyDate.AddMinutes(10))
                    End Select
                End If
                TextBox1.Clear()
            Catch ex As Exception

            End Try
        End If
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As System.EventArgs) Handles ComboBox1.SelectedIndexChanged
        ComboBox2.SelectedIndex = ComboBox1.SelectedIndex
        ComboBox3.SelectedIndex = ComboBox1.SelectedIndex
    End Sub

    Private Sub ComboBox2_SelectedIndexChanged(sender As Object, e As System.EventArgs) Handles ComboBox2.SelectedIndexChanged
        ComboBox1.SelectedIndex = ComboBox2.SelectedIndex
        ComboBox3.SelectedIndex = ComboBox2.SelectedIndex
    End Sub

    Private Sub Button2_Click_1(sender As System.Object, e As System.EventArgs) Handles Button2.Click
        newpirep()
    End Sub

    Private Sub Button6_Click(sender As System.Object, e As System.EventArgs) Handles Button6.Click
        SavePIREPclicked()
    End Sub

    Private Sub Button8_Click(sender As System.Object, e As System.EventArgs) Handles Button8.Click
        LoadSettings()
        LoginDetails.Show()
    End Sub

    Private Sub Button9_Click(sender As System.Object, e As System.EventArgs) Handles Button9.Click
        About.Show()
    End Sub

    Private Sub Button7_Click(sender As System.Object, e As System.EventArgs) Handles Button7.Click
        Process.Start("explorer.exe", SavedFlights)
    End Sub

    Public Sub OnlineWith(ByVal WHERE As String)
        Select Case WHERE
            Case Is = "GND"
                If OnGround.Value = 1 Then


                    Dim AI = FSUIPCConnection.AITrafficServices
                    AI.RefreshAITrafficInformation()
                    AI.ApplyFilter(True, True, 0, 360, Nothing, Nothing, 3D)
                    For Each Plane As AIPlaneInfo In AI.AllTraffic
                        If Plane.ATCIdentifier.Contains("SAV") And Plane.AltitudeDifferenceFeet < 100 Then
                            If Plane.ATCIdentifier.Length > 3 Then
                                OnlineWithList.Add(Plane.ATCIdentifier)
                            End If
                            'MsgBox(Plane.ATCIdentifier & " " & Plane.GroundSpeed)
                        End If
                    Next Plane
                    If OnlineWithList.Count > 0 Then


                        Dim LogString As String = "On the ground with "
                        Dim Pilot As String = ""


                        For Each SAV In OnlineWithList
                            Pilot = Pilot & SAV & " "
                        Next
                        AddToLog(LogString & Pilot)
                        OnlineWithList.Clear()
                    End If
                End If
            Case Is = "CRZ"
                Dim AI = FSUIPCConnection.AITrafficServices
                AI.RefreshAITrafficInformation()
                AI.ApplyFilter(True, True, 0, 360, Nothing, Nothing, 40D)
                For Each Plane As AIPlaneInfo In AI.AllTraffic
                    If Plane.ATCIdentifier.Contains("SAV") And Plane.AltitudeDifferenceFeet < 6000 And Plane.AltitudeFeet > 10000 Then
                        If Plane.ATCIdentifier.Length > 3 Then
                            OnlineWithList.Add(Plane.ATCIdentifier)
                        End If
                        'MsgBox(Plane.ATCIdentifier & " " & Plane.GroundSpeed)
                    End If
                Next Plane
                If OnlineWithList.Count > 0 Then


                    Dim LogString As String = "In the cruise with "
                    Dim Pilot As String = ""


                    For Each SAV In OnlineWithList
                        Pilot = Pilot & SAV & " "
                    Next
                    AddToLog(LogString & Pilot)
                    OnlineWithList.Clear()
                End If
        End Select

    End Sub

    Private Sub Button10_Click(sender As System.Object, e As System.EventArgs) Handles Button10.Click
        If LoginStatus = 1 Then
            Schedules.Show()
        Else
            ErrorMessage("You must log-in to view the schedules")
        End If
    End Sub

    Private Sub Button11_Click(sender As System.Object, e As System.EventArgs) Handles Button11.Click
        If GroupBox2.Height = 319 Then
            GroupBox2.Height = 243
        Else
            GroupBox2.Height = 319
        End If

    End Sub

    Private Sub Button1_Click(sender As System.Object, e As System.EventArgs) Handles Button1.Click
        If LoginStatus = 1 Then
            If VATSIMID <> 0 Then
                CreateXML("vatsim")
            Else
                ErrorMessage("VATSIM flight plan could not be downloaded because there is no VATSIM-ID associated with your account.")

            End If
        Else
            ErrorMessage("You must log-in before you can obtain your VATSIM flight plan")
        End If
    End Sub

    Private Sub LandingRateToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs)
        Me.Hide()
        Landing_Rate.Show()
    End Sub

    Private Sub HistoryTimer_Tick(sender As System.Object, e As System.EventArgs) Handles HistoryTimer.Tick
        SaveHistory()
    End Sub

    Private Sub KilogramsToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles KilogramsToolStripMenuItem.Click
        units = "KG"
        Label13.Text = units
        Label14.Text = units
        KilogramsToolStripMenuItem.Checked = True
        ElepahntsToolStripMenuItem.Checked = False
        PoundsToolStripMenuItem.Checked = False
    End Sub

    Private Sub PoundsToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles PoundsToolStripMenuItem.Click
        units = "LB"
        Label13.Text = units
        Label14.Text = units
        KilogramsToolStripMenuItem.Checked = False
        ElepahntsToolStripMenuItem.Checked = False
        PoundsToolStripMenuItem.Checked = True
    End Sub

    Private Sub ElepahntsToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles ElepahntsToolStripMenuItem.Click
        units = "EL"
        Label13.Text = units
        Label14.Text = units
        KilogramsToolStripMenuItem.Checked = False
        ElepahntsToolStripMenuItem.Checked = True
        PoundsToolStripMenuItem.Checked = False
    End Sub

    Private Sub SAVCARS_MouseWheel(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseWheel
        If e.Delta > 0 Then
            Me.Opacity = Me.Opacity + 0.1
        Else
            If Me.Opacity > 0.2 Then
                Me.Opacity = Me.Opacity - 0.1
            End If
        End If
    End Sub

    Private Sub WhenEnginesStartedToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles WhenEnginesStartedToolStripMenuItem2.Click '
        ChangeMenuSettings(True, False, False, False, False, False, True, False, False, False, True, False, 0)
    End Sub

    Private Sub WhenParkingBrakeReleasedToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles WhenParkingBrakeReleasedToolStripMenuItem2.Click '
        ChangeMenuSettings(False, True, False, False, False, False, True, False, False, False, True, False, 1)
    End Sub

    Private Sub WhenPuchbackInitiatedToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles WhenPushbackInitiatedToolStripMenuItem1.Click '
        ChangeMenuSettings(False, False, True, False, False, False, True, False, False, False, True, False, 2)
    End Sub

    Private Sub WhenEnginesStartedToolStripMenuItem1_Click(sender As System.Object, e As System.EventArgs) Handles WhenEnginesStartedToolStripMenuItem3.Click '
        ChangeMenuSettings(False, False, False, True, False, False, False, True, False, False, False, True, 0)
    End Sub

    Private Sub WhenParkingBrakeReleasedToolStripMenuItem1_Click(sender As System.Object, e As System.EventArgs) Handles WhenParkingBrakeReleasedToolStripMenuItem3.Click '
        ChangeMenuSettings(False, False, False, False, True, False, False, True, False, False, False, True, 1)
    End Sub

    Private Sub WhenPushbackInitiatedToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles WhenPushbackInitiatedToolStripMenuItem2.Click '
        ChangeMenuSettings(False, False, False, False, False, True, False, True, False, False, False, True, 2)
    End Sub

    Private Sub ManualStartToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles ManualToolStripMenuItem.Click '
        ChangeMenuSettings(False, False, False, False, False, False, False, False, True, True, False, False)
    End Sub

    Private Sub ToneToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles ToneToolStripMenuItem1.Click '
        ChangeMenuSettings(False, False, False, True, False, False, False, True, False, False, False, True, 0)
    End Sub

    Private Sub AutoStartToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles AutoStartToolStripMenuItem1.Click '
        ChangeMenuSettings(True, False, False, False, False, False, True, False, False, False, True, False, 0)
    End Sub

    Private Sub ChangeMenuSettings(ByVal A As Boolean, ByVal B As Boolean, ByVal C As Boolean, ByVal D As Boolean, ByVal E As Boolean, ByVal F As Boolean, ByVal G As Boolean, ByVal H As Boolean, ByVal I As Boolean, ByVal J As Boolean, ByVal K As Boolean, ByVal L As Boolean, Optional Num As Integer = 0)
        WhenEnginesStartedToolStripMenuItem2.Checked = A
        WhenParkingBrakeReleasedToolStripMenuItem2.Checked = B
        WhenPushbackInitiatedToolStripMenuItem1.Checked = C
        '
        WhenEnginesStartedToolStripMenuItem3.Checked = D
        WhenParkingBrakeReleasedToolStripMenuItem3.Checked = E
        WhenPushbackInitiatedToolStripMenuItem2.Checked = F
        '
        AutoStartToolStripMenuItem1.Checked = G
        ToneToolStripMenuItem1.Checked = H
        ManualToolStripMenuItem.Checked = I
        '
        ManualChecked = J
        AutoStartChecked = K
        StartToneChecked = L
        '
        AutoStartOption = Num
        '
        If ManualChecked Or StartToneChecked Then
            Button5.Visible = True
        Else : Button5.Visible = False
        End If
    End Sub

    Private Sub TypeP_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles TypeP.CheckedChanged
        If TypeP.Checked Then

            Label9.Text = "PAX"
        Else
            Label9.Text = "Cargo"
        End If
    End Sub

    Private Sub LoginCredentialsToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles LoginCredentialsToolStripMenuItem.Click
        LoginDetails.Show()
    End Sub
End Class
