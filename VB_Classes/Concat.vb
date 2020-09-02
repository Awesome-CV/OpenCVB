Imports cv = OpenCvSharp
Public Class Concat_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        label1 = "Horizontal concatenation"
        label2 = "Vertical concatenation"
        desc = "Concatenate 2 images - horizontally and vertically"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim tmp As New cv.Mat
        cv.Cv2.HConcat(src, ocvb.RGBDepth, tmp)
        dst1 = tmp.Resize(src.Size())
        cv.Cv2.VConcat(src, ocvb.RGBDepth, tmp)
        dst2 = tmp.Resize(src.Size())
    End Sub
End Class




Public Class Concat_4way
    Inherits VBparent
    Public img(3) As cv.Mat
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        For i = 0 To img.Length - 1
            img(i) = New cv.Mat
        Next
        label1 = "Color/RGBDepth/Left/Right views"
        desc = "Concatenate 4 images - horizontally and vertically"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        if standalone Then
            img(0) = src
            img(1) = ocvb.RGBDepth
            img(2) = ocvb.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR).Resize(src.Size())
            img(3) = ocvb.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR).Resize(src.Size())
        End If
        Dim tmp1 As New cv.Mat, tmp2 As New cv.Mat, tmp3 As New cv.Mat
        cv.Cv2.HConcat(img(0), img(1), tmp1)
        cv.Cv2.HConcat(img(2), img(3), tmp2)
        cv.Cv2.VConcat(tmp1, tmp2, tmp3)
        dst1 = tmp3.Resize(src.Size())
    End Sub
End Class
