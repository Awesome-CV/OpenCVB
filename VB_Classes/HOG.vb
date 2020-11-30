Imports cv = OpenCvSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/HOGSample.vb
Public Class HOG_Basics
    Inherits VBparent
    Dim staticImage As cv.Mat
    Dim staticImageProcessed As Boolean
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Threshold", 0, 100, 0)
        sliders.setupTrackBar(1, "Stride", 1, 100, 1)
        sliders.setupTrackBar(2, "Scale", 0, 2000, 300)
        ocvb.desc = "Find people with Histogram of Gradients (HOG) 2D feature"
        staticImage = cv.Cv2.ImRead(ocvb.parms.homeDir + "Data/Asahiyama.jpg", cv.ImreadModes.Color)
        dst2 = staticImage.Resize(dst2.Size)
    End Sub
    Private Sub drawFoundRectangles(dst1 As cv.Mat, found() As cv.Rect)
        For Each rect As cv.Rect In found
            ' the HOG detector returns slightly larger rectangles than the real objects.
            ' so we slightly shrink the rectangles to get a nicer output.
            Dim r As cv.Rect = New cv.Rect With
            {
                .X = rect.X + CInt(Math.Truncate(Math.Round(rect.Width * 0.1))),
                .Y = rect.Y + CInt(Math.Truncate(Math.Round(rect.Height * 0.1))),
                .Width = CInt(Math.Truncate(Math.Round(rect.Width * 0.8))),
                .Height = CInt(Math.Truncate(Math.Round(rect.Height * 0.8)))
            }
            dst1.Rectangle(r.TopLeft, r.BottomRight, cv.Scalar.Red, 3, cv.LineTypes.Link8, 0)
        Next rect
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim hog As New cv.HOGDescriptor()
        hog.SetSVMDetector(cv.HOGDescriptor.GetDefaultPeopleDetector())

        Dim b As Boolean = hog.CheckDetectorSize()
        b.ToString()

        ' run the detector with default parameters. to get a higher hit-rate
        ' (and more false alarms, respectively), decrease the hitThreshold and
        ' groupThreshold (set groupThreshold to 0 to turn off the grouping completely).
        Dim threshold = sliders.trackbar(0).Value
        Dim stride = sliders.trackbar(1).Value
        Dim scale = sliders.trackbar(2).Value / 1000
        Dim found() As cv.Rect = hog.DetectMultiScale(src, threshold, New cv.Size(stride, stride), New cv.Size(24, 16), scale, 2)
        label1 = String.Format("{0} region(s) found", found.Length)
        src.CopyTo(dst1)
        drawFoundRectangles(dst1, found)

        If staticImageProcessed = False Then
            found = hog.DetectMultiScale(dst2, threshold, New cv.Size(stride, stride), New cv.Size(24, 16), scale, 2)
            drawFoundRectangles(dst2, found)
            If found.Length > 0 Then
                staticImageProcessed = True
                label2 = String.Format("{0} region(s) found", found.Length)
                sliders.trackbar(1).Value = 30 ' this will speed up the frame rate.  This algorithm is way too slow!  It won't find much at this rate...
            Else
                label2 = "Try adjusting slider bars."
            End If
        End If
    End Sub
End Class



