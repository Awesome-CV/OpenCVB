Imports cv = OpenCvSharp
' https://docs.opencv.org/3.3.1/d6/d73/Pyramids_8cpp-example.html
Public Class Pyramid_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Zoom in and out", -1, 1, 0)
        task.desc = "Use pyrup and pyrdown to zoom in and out of an image."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim zoom = sliders.trackbar(0).Value
        If zoom <> 0 Then
            If zoom < 0 Then
                Dim tmp = src.PyrDown(New cv.Size(src.Cols / 2, src.Rows / 2))
                Dim roi = New cv.Rect((src.Cols - tmp.Cols) / 2, (src.Rows - tmp.Rows) / 2, tmp.Width, tmp.Height)
                dst1(roi) = tmp
            Else
                Dim tmp = src.PyrUp(New cv.Size(src.Cols * 2, src.Rows * 2))
                Dim roi = New cv.Rect((tmp.Cols - src.Cols) / 2, (tmp.Rows - src.Rows) / 2, src.Width, src.Height)
                dst1 = tmp(roi)
            End If
        Else
            src.CopyTo(dst1)
        End If
    End Sub
End Class






Public Class Pyramid_Filter
    Inherits VBparent
    Dim laplace As Laplacian_PyramidFilter
    Public Sub New()
        initParent()
        laplace = New Laplacian_PyramidFilter()

        task.desc = "Link to Laplacian_PyramidFilter that uses pyrUp and pyrDown extensively"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        laplace.src = src
        laplace.Run()
        dst1 = laplace.dst1
    End Sub
End Class


