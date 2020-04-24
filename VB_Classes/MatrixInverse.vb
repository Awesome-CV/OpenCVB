﻿Imports cv = OpenCvSharp
Imports CS_Classes
Module matrixInverse_Module
    Public Function printMatrixResults(src As cv.Mat, dst As cv.Mat) As String
        Dim outstr As String = "Original Matrix " + vbCrLf
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                outstr += Format(src.At(Of Double)(y, x), "#0.0000") + vbTab
            Next
            outstr += vbCrLf
        Next
        outstr += vbCrLf + "Matrix Inverse" + vbCrLf
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                outstr += Format(dst.At(Of Double)(y, x), "#0.0000") + vbTab
            Next
            outstr += vbCrLf
        Next
        Return outstr
    End Function
End Module





' https://visualstudiomagazine.com/articles/2020/04/06/invert-matrix.aspx
Public Class MatrixInverse_Basics_CS : Implements IDisposable
    Public matrix As New MatrixInverse
    Public externalUse As Boolean
    Dim defaultInput(,) As Double = {{3, 7, 2, 5}, {4, 0, 1, 1}, {1, 6, 3, 0}, {2, 8, 4, 3}}
    Dim defaultBVector() As Double = {12, 7, 7, 13}
    Public src As New cv.Mat(4, 4, cv.MatType.CV_64F, defaultInput)
    Public dst As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Manually invert a matrix"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Width <> src.Height Then
            ocvb.putText(New ActiveClass.TrueType("The src matrix must be square!", 10, 60, RESULT1))
            Exit Sub
        End If

        If externalUse = False Then matrix.bVector = defaultBVector
        dst = matrix.Run(src)

        Dim outstr = printMatrixResults(src, dst)
        ocvb.putText(New ActiveClass.TrueType(outstr + vbCrLf + "Intermediate results are optionally available in the console log.", 10, 60, RESULT1))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class






Public Class MatrixInverse_OpenCV : Implements IDisposable
    Public externalUse As Boolean
    Dim defaultInput(,) As Double = {{3, 7, 2, 5}, {4, 0, 1, 1}, {1, 6, 3, 0}, {2, 8, 4, 3}}
    Public src As New cv.Mat(4, 4, cv.MatType.CV_64F, defaultInput)
    Public dst As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use OpenCV to invert a matrix"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Width <> src.Height Then
            ocvb.putText(New ActiveClass.TrueType("The src matrix must be square!", 10, 60, RESULT1))
            Exit Sub
        End If

        dst = src.EmptyClone.SetTo(0)
        cv.Cv2.Invert(src, dst, cv.DecompTypes.LU)
        Dim outstr = printMatrixResults(src, dst)
        ocvb.putText(New ActiveClass.TrueType(outstr, 10, 60, RESULT1))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class