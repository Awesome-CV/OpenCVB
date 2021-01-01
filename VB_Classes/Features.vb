Imports cv = OpenCvSharp

' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Features_GoodFeatures
    Inherits VBparent
    Public goodFeatures As New List(Of cv.Point2f)
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Number of Points", 10, 1000, 200)
            sliders.setupTrackBar(1, "Quality Level", 1, 100, 1)
            sliders.setupTrackBar(2, "Distance", 1, 100, 30)
        End If
        task.desc = "Find good features to track in an RGB image."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim numPoints = sliders.trackbar(0).Value
        Dim quality = sliders.trackbar(1).Value / 100
        Dim minDistance = sliders.trackbar(2).Value
        Dim features = cv.Cv2.GoodFeaturesToTrack(src, numPoints, quality, minDistance, Nothing, 7, True, 3)

        src.CopyTo(dst1)
        goodFeatures.Clear()
        For i = 0 To features.Length - 1
            goodFeatures.Add(features.ElementAt(i))
            cv.Cv2.Circle(dst1, features(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class





Public Class Features_PointTracker
    Inherits VBparent
    Dim features As Features_GoodFeatures
    Dim pTrack As KNN_PointTracker
    Dim rRadius = 10
    Public Sub New()
        initParent()
        features = New Features_GoodFeatures()
        pTrack = New KNN_PointTracker()
        Dim drawRectCheck = findCheckBox("Draw rectangle and centroid for each mask")
        drawRectCheck.Checked = False
        Dim pixelSlider = findSlider("Minimum size of object in pixels")
        pixelSlider.Value = 1

        label1 = "Good features without Kalman"
        label2 = "Good features with Kalman"
        task.desc = "Find good features and track them"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        features.src = src
        features.Run()
        dst1 = features.dst1

        pTrack.queryPoints.Clear()
        pTrack.queryRects.Clear()
        pTrack.queryMasks.Clear()

        For i = 0 To features.goodFeatures.Count - 1
            Dim pt = features.goodFeatures(i)
            pTrack.queryPoints.Add(pt)
            Dim r = New cv.Rect(pt.X - rRadius, pt.Y - rRadius, rRadius * 2, rRadius * 2)
            pTrack.queryRects.Add(r)
            pTrack.queryMasks.Add(New cv.Mat)
        Next

        pTrack.src = src
        pTrack.Run()

        dst2.SetTo(0)
        For Each obj In pTrack.drawRC.viewObjects
            Dim r = obj.Value.rectInHist
            If r.Width > 0 And r.Height > 0 Then
                If r.X + r.Width < dst2.Width And r.Y + r.Height < dst2.Height Then src(obj.Value.rectInHist).CopyTo(dst2(obj.Value.rectInHist))
            End If
        Next
    End Sub
End Class

