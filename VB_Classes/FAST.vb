Imports cv = OpenCvSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FASTSample.vb
Public Class FAST_Basics
    Inherits VBparent
    Public keypoints() As cv.KeyPoint
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Threshold", 0, 200, 15)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Use Non-Max = True"
        check.Box(0).Checked = True

        ocvb.desc = "Find interesting points with the FAST (Features from Accelerated Segment Test) algorithm"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        src.CopyTo(dst1)
        keypoints = cv.Cv2.FAST(src, sliders.trackbar(0).Value, If(check.Box(0).Checked, True, False))

        For Each kp As cv.KeyPoint In keypoints
            dst1.Circle(kp.Pt, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias, 0)
        Next kp
        label1 = "FAST_Basics nonMax = " + If(check.Box(0).Checked, "True", "False")
    End Sub
End Class





Public Class FAST_Centroid
    Inherits VBparent
    Dim fast As FAST_Basics
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.input(1) ' 2 elements - cv.point

        fast = New FAST_Basics(ocvb)
        ocvb.desc = "Find interesting points with the FAST and smooth the centroid with kalman"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        fast.src = src
        fast.Run(ocvb)
        dst1 = fast.dst1
        dst2.SetTo(0)
        For Each kp As cv.KeyPoint In fast.keypoints
            dst2.Circle(kp.Pt, 10, cv.Scalar.White, -1, cv.LineTypes.AntiAlias, 0)
        Next kp
        Dim gray = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim m = cv.Cv2.Moments(gray, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.input(0) = m.M10 / m.M00
            kalman.input(1) = m.M01 / m.M00
            kalman.Run(ocvb)
            dst2.Circle(New cv.Point(kalman.output(0), kalman.output(1)), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        End If
    End Sub
End Class


