Imports cv = OpenCvSharp
Public Class LUT_Basics
    Inherits VBparent
    Public reduction As Reduction_Basics
    Dim gradMap As Palette_BuildGradientColorMap
    Public colorMap As cv.Mat
    Public Sub New()
        initParent()
        reduction = New Reduction_Basics()
        gradMap = New Palette_BuildGradientColorMap

        label2 = "Custom Color Lookup Table"
        task.desc = "Use a palette to provide the lookup table for LUT - Painterly Effect"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone or task.intermediateReview = caller Then
            reduction.src = src
            reduction.Run()
        End If

        gradMap.Run()
        colorMap = gradMap.gradientColorMap.Flip(cv.FlipMode.X)
        dst1 = reduction.dst1.LUT(colorMap)
        dst2 = colorMap.Resize(src.Size())
    End Sub
End Class







' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class LUT_Simple
    Inherits VBparent
    Public reduction As Reduction_Basics
    Public colorMat As cv.Mat
    Public Sub New()
        initParent()
        reduction = New Reduction_Basics()
        colorMat = New cv.Mat(1, 256, cv.MatType.CV_8UC3, ocvb.vecColors)
        label2 = "Custom Color Lookup Table"
        task.desc = "Build and use a custom color palette - Painterly Effect"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        reduction.src = src
        reduction.Run()
        dst1 = reduction.dst1.LUT(colorMat)
        If standalone or task.intermediateReview = caller Then dst2 = colorMat.Resize(src.Size())
    End Sub
End Class












Public Class LUT_Gray
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "LUT zero through xxx", 1, 255, 65)
            sliders.setupTrackBar(1, "LUT xxx through yyy", 1, 255, 110)
            sliders.setupTrackBar(2, "LUT xxx through yyy", 1, 255, 160)
            sliders.setupTrackBar(3, "LUT xxx through 255", 1, 255, 210)
        End If

        task.desc = "Use an OpenCV Lookup Table to define 5 regions in a grayscale image - Painterly Effect."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        sliders.sLabels(0).Text = "LUT zero through " + CStr(sliders.trackbar(0).Value)
        sliders.sLabels(1).Text = "LUT " + CStr(sliders.trackbar(0).Value) + " through " + CStr(sliders.trackbar(1).Value)
        sliders.sLabels(2).Text = "LUT " + CStr(sliders.trackbar(1).Value) + " through " + CStr(sliders.trackbar(2).Value)
        sliders.sLabels(3).Text = "LUT " + CStr(sliders.trackbar(2).Value) + " through 255"
        Dim splits = {sliders.trackbar(0).Value, sliders.trackbar(1).Value, sliders.trackbar(2).Value, sliders.trackbar(3).Value, 255}
        Dim vals = {0, sliders.trackbar(0).Value, sliders.trackbar(1).Value, sliders.trackbar(2).Value, 255}
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim myLut As New cv.Mat(1, 256, cv.MatType.CV_8U)
        Dim splitIndex As integer
        For i = 0 To 255
            myLut.Set(Of Byte)(0, i, vals(splitIndex))
            If i >= splits(splitIndex) Then splitIndex += 1
        Next
        dst1 = gray.LUT(myLut)
    End Sub
End Class







' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class LUT_Color
    Inherits VBparent
    Public paletteMap(256) As cv.Vec3b
    Dim colorMat As cv.Mat
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Reduction for color image", 1, 256, 32)
        End If
        colorMat = New cv.Mat(1, 256, cv.MatType.CV_8UC3, ocvb.vecColors) ' Create a new color palette here.
        task.desc = "Build and use a custom color palette - Painterly Effect"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim reduction = sliders.trackbar(0).Value
        If standalone or task.intermediateReview = caller Then
            src /= reduction
            src *= reduction
        End If
        dst1 = src.LUT(colorMat)
        If standalone or task.intermediateReview = caller Then dst2 = colorMat.Resize(src.Size())
    End Sub
End Class




' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
' https://docs.opencv.org/2.4/modules/core/doc/operations_on_arrays.html
Public Class LUT_Rebuild
    Inherits VBparent
    Public paletteMap(256 - 1) As Byte
    Public Sub New()
        initParent()
        For i = 0 To paletteMap.Count - 1
            paletteMap(i) = i
        Next
        task.desc = "Rebuild any grayscale image with a 256 element Look-Up Table"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim lut = New cv.Mat(1, 256, cv.MatType.CV_8U, paletteMap)
        dst1 = src.LUT(lut)
        If standalone or task.intermediateReview = caller Then dst2 = lut.Resize(src.Size())
    End Sub
End Class

