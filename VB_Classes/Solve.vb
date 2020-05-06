﻿Imports cv = OpenCvSharp
' https://github.com/shimat/opencvsharp/wiki/Solve-Equation
Public Class Solve_ByMat
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "Solve a set of equations with OpenCV's Solve API."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ' x + y = 10
        ' 2x + 3y = 26
        ' (x=4, y=6)
        Dim av(,) As Double = {{1, 1}, {2, 3}}
        Dim yv() As Double = {10, 26}
        Dim a = New cv.Mat(2, 2, cv.MatType.CV_64FC1, av)
        Dim y = New cv.Mat(2, 1, cv.MatType.CV_64FC1, yv)
        Dim x As New cv.Mat
        cv.Cv2.Solve(a, y, x, cv.DecompTypes.LU)

        ocvb.putText(New ActiveClass.TrueType("Solution ByMat: X1 = " + CStr(x.Get(Of Double)(0, 0)) + vbTab + "X2 = " + CStr(x.Get(Of Double)(0, 1)), 10, 125))
    End Sub
    Public Sub MyDispose()
    End Sub
End Class




' https://github.com/shimat/opencvsharp/wiki/Solve-Equation
Public Class Solve_ByArray
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "Solve a set of equations with OpenCV's Solve API with a normal array as input  "
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ' x + y = 10
        ' 2x + 3y = 26
        ' (x=4, y=6)
        Dim av(,) As Double = {{1, 1}, {2, 3}}
        Dim yv() As Double = {10, 26}
        Dim x As New cv.Mat
        cv.Cv2.Solve(cv.InputArray.Create(av), cv.InputArray.Create(yv), x, cv.DecompTypes.LU)

        ocvb.putText(New ActiveClass.TrueType("Solution ByArray: X1 = " + CStr(x.Get(of Double)(0, 0)) + vbTab + "X2 = " + CStr(x.Get(of Double)(0, 1)), 10, 125))
    End Sub
    Public Sub MyDispose()
    End Sub
End Class