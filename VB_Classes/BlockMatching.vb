Imports cv = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/stereo_match.cpp
Public Class BlockMatching_Basics
    Inherits VBparent
    Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        colorizer = New Depth_Colorizer_CPP(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Blockmatch max disparity", 2, 5, 2)
        sliders.setupTrackBar(1, "Blockmatch block size", 5, 255, 15)
        sliders.setupTrackBar(2, "Blockmatch distance factor (approx) X1000", 1, 100, 20)
        ocvb.desc = "Use OpenCV's block matching on left and right views"
        label1 = "Block matching disparity colorized like depth"
        label2 = "Right Image (used with left image)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camName.Kinect4AzureCam Then
            ocvb.trueText("For the Kinect 4 Azure camera, the left and right views are the same.")
        End If

        Dim numDisparity = sliders.trackbar(0).Value * 16 ' must be a multiple of 16
        Dim blockSize = sliders.trackbar(1).Value
        If blockSize Mod 2 = 0 Then blockSize += 1 ' must be odd

        Static blockMatch = cv.StereoBM.Create()
        blockMatch.BlockSize = blockSize
        blockMatch.MinDisparity = 0
        blockMatch.ROI1 = New cv.Rect(0, 0, ocvb.leftView.Width, ocvb.leftView.Height)
        blockMatch.ROI2 = New cv.Rect(0, 0, ocvb.leftView.Width, ocvb.leftView.Height)
        blockMatch.PreFilterCap = 31
        blockMatch.NumDisparities = numDisparity
        blockMatch.TextureThreshold = 10
        blockMatch.UniquenessRatio = 15
        blockMatch.SpeckleWindowSize = 100
        blockMatch.SpeckleRange = 32
        blockMatch.Disp12MaxDiff = 1

        Dim disparity As New cv.Mat
        blockMatch.compute(ocvb.leftView, ocvb.rightView, disparity)
        disparity.ConvertTo(colorizer.src, cv.MatType.CV_32F, 1 / 16)
        colorizer.src = colorizer.src.Threshold(0, 0, cv.ThresholdTypes.Tozero)
        Dim topMargin = 10, sideMargin = 8
        Dim rect = New cv.Rect(numDisparity + sideMargin, topMargin, src.Width - numDisparity - sideMargin * 2, src.Height - topMargin * 2)
        Dim tmp = New cv.Mat(src.Size(), cv.MatType.CV_32F, 0)
        Dim distance = sliders.trackbar(2).Value * 1000
        cv.Cv2.Divide(distance, colorizer.src(rect), colorizer.src(rect)) ' this needs much more refinement.  The trackbar3 value is just an approximation.
        colorizer.src(rect) = colorizer.src(rect).Threshold(10000, 10000, cv.ThresholdTypes.Trunc)
        colorizer.Run(ocvb)
        dst1(rect) = colorizer.dst1(rect)
        dst2 = ocvb.rightView.Resize(src.Size())
    End Sub
End Class

