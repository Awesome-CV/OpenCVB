﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class PixelViewer
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.FontInfo = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'FontInfo
        '
        Me.FontInfo.AutoSize = True
        Me.FontInfo.Location = New System.Drawing.Point(12, 9)
        Me.FontInfo.Name = "FontInfo"
        Me.FontInfo.Size = New System.Drawing.Size(57, 20)
        Me.FontInfo.TabIndex = 0
        Me.FontInfo.Text = "Label1"
        Me.FontInfo.Visible = False
        '
        'PixelViewer
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1707, 1147)
        Me.Controls.Add(Me.FontInfo)
        Me.Name = "PixelViewer"
        Me.Text = "PixelShow"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents FontInfo As Windows.Forms.Label
End Class