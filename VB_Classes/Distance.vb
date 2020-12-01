Imports cv = OpenCvSharp

Public Class Distance_Basics
    Inherits VBparent
    Dim foreground As kMeans_Depth_FG_BG
    Public Sub New()
        initParent()
        radio.Setup(caller, 3)
        radio.check(0).Text = "C"
        radio.check(1).Text = "L1"
        radio.check(2).Text = "L2"
        radio.check(2).Checked = True

        foreground = New kMeans_Depth_FG_BG()
        label1 = "Distance results"
        label2 = "Input mask to distance transformm"
        task.desc = "Distance algorithm basics."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        foreground.Run()
        dst2 = foreground.dst1
        Dim fg = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim DistanceType = cv.DistanceTypes.L2
        If radio.check(0).Checked Then DistanceType = cv.DistanceTypes.C
        If radio.check(1).Checked Then DistanceType = cv.DistanceTypes.L1
        If radio.check(2).Checked Then DistanceType = cv.DistanceTypes.L2

        cv.Cv2.BitwiseAnd(gray, fg, gray)
        Dim kernelSize = 0 ' this is precise distance (there is no distance of 1)

        Dim dist = gray.DistanceTransform(DistanceType, kernelSize)
        Dim dist32f = dist.Normalize(0, 255, cv.NormTypes.MinMax)
        dist32f.ConvertTo(gray, cv.MatType.CV_8UC1)
        dst1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class


