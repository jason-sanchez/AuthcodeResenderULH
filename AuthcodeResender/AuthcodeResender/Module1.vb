Imports System.Data.SqlClient
Imports System.Configuration
Imports System.IO
Imports System.Threading

Module Module1

    Sub Main()

        Dim sconnect As ConnectionStringSettings
        Dim adapter As New SqlDataAdapter
        Dim Dt As New DataTable
        Dim sql As String = ""
        Dim oneline As String = ""
        'Dim readDir As String = "S:\AuthCode\Frazier\AuthCodeHistory\HL7."
        'Dim outputDir As String = "C:\Users\jsanchez\Documents\GitHub\Systemax\AuthcodeResender\HL7outputtest\HL7."
        'Dim log As New StreamWriter("C:\Users\jsanchez\Documents\GitHub\Systemax\AuthcodeResender\completionLog.txt")
        'Dim dir As String = "C:\Users\jsanchez\Documents\GitHub\Systemax\AuthcodeResender\counter.txt"
        Dim readDir As String = System.Configuration.ConfigurationManager.AppSettings("SourceDirectory")
        Dim outputDir As String = System.Configuration.ConfigurationManager.AppSettings("OutputDirectory")

        Dim dir As String = System.Configuration.ConfigurationManager.AppSettings("ResenderDirectory")
        Dim errorfilepath As String = System.Configuration.ConfigurationManager.AppSettings("ErrorLog")
        Dim log As New StreamWriter(System.Configuration.ConfigurationManager.AppSettings("ResenderLog"))
        'Dim readDir As String = "D:\AuthCode\Frazier\AuthCodeHistory\HL7."
        'Dim outputDir As String = "D:\AuthCode\Frazier\AuthCodeDirectory\HL7."
        'Dim log As New StreamWriter("D:\AuthCode\Frazier\AuthcodeResender\completionLog.txt")
        'Dim dir As String = "D:\AuthCode\Frazier\AuthcodeResender\counter.txt"
        'log = My.Computer.FileSystem.OpenTextFileWriter("d:\dftLog\transmitlog.txt", True)
        Dim strArray()
        Dim MSH As String = ""
        Dim EVN As String = ""
        Dim PID As String = ""
        Dim PV1 As String = ""
        Dim IN1 As String = ""
        Dim LAST As String = Chr(28) & Chr(13)
        Dim fullmes As String = ""
        Dim daysback As Int32 = "-1"
        Dim ident As String = "R"

        Try

            For Each sconnect In ConfigurationManager.ConnectionStrings
                Dim constring As String = sconnect.ToString()
                Using conn As New SqlConnection(constring)
                    Dim objDBCommand As New SqlCommand
                    With objDBCommand
                        .Connection = conn
                        .Connection.Open()

                        sql = " Select v.panum, v.admindate, v.Status, i.IPlanCode, pay.name, i.authnum1, "
                        sql += " i.PSauthNum, max(a.messagedate) as messagedate, a.HL7 "
                        sql += " from [03Insurer] i "

                        If constring Like "*ITW*" Then
                            sql += " inner join [001Episode] v on v.epnum = i.epnum "
                        Else
                            sql += " inner join [01Visit] v on v.panum = i.panum "
                        End If

                        sql += " inner join [113Payor] pay on pay.planCode = i.IPlanCode "
                        sql += " inner join [Authcodeupdatelog] a on a.panum = v.panum and a.iplancode = pay.plancode "
                        sql += " where v.status in ('IP','OEC') "
                        sql += " and PSauthNum is not null and PSauthNum <> '' "
                        sql += " and (authnum1 is null or authnum1 = '') "
                        'sql += " and v.discharged = 0 "
                        sql += " and messagedate > DATEDIFF(DAY, 0, GETDATE()" & daysback & " ) "
                        'sql += " and messagedate > DATEDIFF(DAY, 0, GETDATE()) "
                        sql += " group by v.panum, v.admindate, v.Status, i.IPlanCode, pay.name, i.authnum1, "
                        sql += " i.PSauthNum, a.messagedate, a.HL7 "

                        .CommandText = sql

                        adapter.SelectCommand = objDBCommand
                        adapter.Fill(Dt)


                        For Each DR As DataRow In Dt.Rows

                            Dim Hl7record As String = DR("HL7")
                            Dim hl7file As String = ""
                            'If constring Like "*ITW*" Then
                            'hl7file = readDir & "ITW." & Hl7record
                            'Else
                            hl7file = readDir & Hl7record
                            'End If

                            Dim sreader As New StreamReader(hl7file)

                            Dim line As String = ""
                            Dim objTStreamCounter As Object
                            Dim intCounter As Integer = 0

                            objTStreamCounter = New StreamReader(dir)
                            line = objTStreamCounter.readline
                            intCounter = CInt(line)
                            intCounter = intCounter + 1
                            If intCounter >= 900000 Then intCounter = 0
                            objTStreamCounter.Close()


                            Do While Not sreader.EndOfStream
                                oneline = sreader.ReadLine()

                                strArray = oneline.Split("|")

                                Select Case strArray(0)

                                    Case Chr(11) & "MSH"

                                        Dim MSH0 As String = strArray(0) 'MSH1
                                        Dim MSH1 As String = strArray(1) 'MSH2
                                        Dim MSH2 As String = strArray(2) 'MSH3
                                        Dim MSH3 As String = strArray(3) 'MSH4
                                        Dim MSH4 As String = strArray(4) 'MSH5
                                        Dim MSH5 As String = strArray(5) 'MSH6
                                        Dim MSH6 As String = gettodaysdate() 'MSH7
                                        Dim MSH7 As String = strArray(7) 'MSH8
                                        Dim MSH8 As String = strArray(8) 'MSH9
                                        Dim MSH9 As String = ident & intCounter 'MSH10

                                        Dim MSH10 As String = strArray(10) 'MSH11
                                        Dim MSH11 As String = strArray(11) 'MSH12

                                        MSH = MSH0 & "|" & MSH1 & "|" & MSH2 & "|" & MSH3 & "|" & MSH4 & "|" & MSH5 & "|" & MSH6 & "|"
                                        MSH = MSH & MSH7 & "|" & MSH8 & "|" & MSH9 & "|" & MSH10 & "|" & MSH11 & vbCr

                                    Case "EVN"

                                        Dim EVN0 As String = strArray(0)
                                        Dim EVN1 As String = strArray(1)
                                        Dim EVN2 As String = gettodaysdate()

                                        EVN = EVN0 & "|" & EVN1 & "|" & EVN2 & vbCr

                                    Case "PID"
                                        PID = oneline & vbCr
                                    Case "PV1"
                                        PV1 = oneline & vbCr
                                    Case "IN1"
                                        IN1 = oneline & vbCr

                                End Select

                            Loop

                            fullmes = MSH & EVN & PID & PV1 & IN1 & LAST


                            Dim output = New StreamWriter(outputDir & Hl7record & "Y")
                            output.Write(fullmes)
                            output.Close()

                            Dim currentdate As String = Now

                            log.WriteLine("Message " & Hl7record & "Y resent..." & currentdate & " Originally sent... " & DR("messagedate"))


                            objTStreamCounter = New StreamWriter(dir)
                            objTStreamCounter.writeline(intCounter)
                            objTStreamCounter.close()

                            'Thread.Sleep(120000) 'pause for 2 minutes
                        Next

                    End With
                End Using
            Next

        Catch ex As Exception
            Dim errorfilename As String = String.Format("ResenderError_{0:yyyyMMdd_HH-mm-ss}.txt", Date.Now)
            Dim errorfile = New StreamWriter(errorfilepath & errorfilename, True)
            errorfile.Write(ex)
            errorfile.Close()
            ex.ToString()

        Finally
            log.Close()

        End Try


    End Sub

    Private Function gettodaysdate()
        Dim today As String = Now

        today = FormatDateTime(Now, 4)

        Dim messagetime As String = FormatDateTime(Now, 4)
        Dim messagetimeseg() = Split(messagetime, ":")
        Dim thehour As String = messagetimeseg(0)
        If Len(thehour) < 2 Then
            thehour = 0 & thehour
        End If

        Dim theminute As String = messagetimeseg(1)
        If Len(theminute) < 2 Then
            theminute = 0 & theminute
        End If

        Dim messagedate As String = Date.Today

        Dim messagedateseg() = Split(messagedate, "/")

        Dim thecurrentYear As String = messagedateseg(2)
        Dim thecurrentMonth As String = messagedateseg(0)
        If Len(thecurrentMonth) < 2 Then
            thecurrentMonth = 0 & thecurrentMonth
        End If

        Dim thecurrentDay As String = messagedateseg(1)
        If Len(thecurrentDay) < 2 Then
            thecurrentDay = 0 & thecurrentDay
        End If

        Return thecurrentYear & thecurrentMonth & thecurrentDay & thehour & theminute
    End Function

End Module
