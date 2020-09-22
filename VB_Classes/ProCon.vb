﻿Imports cv = OpenCvSharp
Imports System.Threading
' https://www.codeproject.com/Articles/5280034/Generation-of-Infinite-Sequences-in-Csharp-and-Unm
Public Class ProCon_Basics
    Inherits VBparent
    Dim buffer(10 - 1) As Integer
    Dim mutex = New Mutex(True, "BufferMutex")
    Dim p As Thread
    Dim c As Thread
    Dim head = -1, tail = -1
    Dim frameCount = 1
    Dim flow As Font_FlowText
    Dim terminateNotice As Boolean
    Dim pduration As Integer
    Dim cduration As Integer
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Buffer Size", 1, 100, buffer.Length)
        sliders.setupTrackBar(1, "Producer Workload Duration (ms)", 1, 1000, 100)
        sliders.setupTrackBar(2, "Consumer Workload Duration (ms)", 1, 1000, 10)
        pduration = sliders.trackbar(1).Value
        cduration = sliders.trackbar(2).Value

        flow = New Font_FlowText(ocvb)

        buffer = Enumerable.Repeat(-1, buffer.Length).ToArray

        p = New Thread(AddressOf Producer)
        p.Name = "Producer"
        p.Start()
        c = New Thread(AddressOf Consumer)
        c.Name = "Consumer"
        c.Start()

        desc = "DijKstra's Producer/Consumer 'Cooperating Sequential Process'.  Consumer must see every item produced."
    End Sub
    Private Function success(index As Integer) As Integer
        Return (index + 1) Mod buffer.Length
    End Function
    Private Sub Consumer()
        While 1
            SyncLock mutex
                head = success(head)
                Dim item = buffer(head)
                If item <> -1 Then
                    flow.msgs.Add("Consumer: = " + CStr(item))
                    buffer(head) = -1
                End If
            End SyncLock
            If terminateNotice Then Exit While Else Thread.Sleep(cduration)
        End While
    End Sub
    Private Sub Producer()
        While 1
            SyncLock mutex
                tail = success(tail)
                If buffer(tail) = -1 Then
                    buffer(tail) = frameCount
                    'flow.msgs.Add("Producer=: " + CStr(tail) + " = " + CStr(frameCount))
                    frameCount += 1
                End If
            End SyncLock
            If terminateNotice Then Exit While Else Thread.Sleep(pduration)
        End While
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If sliders.trackbar(0).Value <> buffer?.Length Then
            SyncLock mutex
                ReDim buffer(sliders.trackbar(0).Value - 1)
                buffer = Enumerable.Repeat(-1, buffer.Length).ToArray
                frameCount = 0
                head = -1
                tail = -1
            End SyncLock
        End If

        pduration = sliders.trackbar(1).Value
        cduration = sliders.trackbar(2).Value
        SyncLock mutex
            flow.Run(ocvb)
        End SyncLock
    End Sub
    Public Sub Close()
        terminateNotice = True
    End Sub
End Class