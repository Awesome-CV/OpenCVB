﻿Imports cv = OpenCvSharp
Public Class Object_Basics
    Inherits VB_Class
    Dim trim As Depth_InRange
    Dim ccomp As CComp_EdgeMask
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        trim = New Depth_InRange(ocvb, "Object_Basics")
        trim.externalUse = True

        ccomp = New CComp_EdgeMask(ocvb, "Object_Basics")
        ccomp.externalUse = True

        ocvb.desc = "Identify objects in the foreground."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        If externalUse = False Then
            ocvb.result1 = trim.Mask
            ocvb.result2 = trim.zeroMask
        End If

        ocvb.color.CopyTo(ccomp.srcGray, trim.Mask)
        ccomp.srcGray = ccomp.srcGray.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        trim.Dispose()
        ccomp.Dispose()
    End Sub
End Class