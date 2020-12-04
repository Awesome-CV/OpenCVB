﻿Imports System.Windows.Forms
Public Class OptionsCheckbox
    Public Box() As CheckBox
    Public Sub Setup(caller As String, count As Integer)
        Me.MdiParent = aOptions
        ReDim Box(count - 1)
        Me.Text = caller + " CheckBox Options"
        aOptions.addTitle(Me)
        For i = 0 To Box.Count - 1
            Box(i) = New CheckBox
            Box(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(Box(i))
        Next
        Me.Show()
    End Sub
End Class
