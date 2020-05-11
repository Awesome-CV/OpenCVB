Imports cv = OpenCvSharp
Public Class Bitwise_Not
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.label1 = "Color BitwiseNot"
        ocvb.label2 = "Gray BitwiseNot"
        ocvb.desc = "Gray and color bitwise_not"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.BitwiseNot(ocvb.color, dst1)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.BitwiseNot(gray, dst2)
    End Sub
End Class
