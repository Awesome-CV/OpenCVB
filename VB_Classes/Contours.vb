Imports cv = OpenCvSharp
Public Class Contours_Basics
    Inherits VBparent
    Public rotatedRect As Rectangle_Rotated
    Public retrievalMode As cv.RetrievalModes
    Public ApproximationMode As cv.ContourApproximationModes
    Public contourlist As New List(Of cv.Point())
    Public centroids As New List(Of cv.Point)
    Public sortedContours As New SortedList(Of Integer, cv.Point())(New compareAllowIdenticalIntegerInverted)
    Public contours0 As cv.Point()()
    Public Sub New()
        initParent()
        radio.Setup(caller + " Retrieval Mode", 5)
        radio.check(0).Text = "CComp"
        radio.check(1).Text = "External"
        radio.check(2).Text = "FloodFill"
        radio.check(3).Text = "List"
        radio.check(4).Text = "Tree"
        radio.check(2).Checked = True

        radio1.Setup(caller + " ContourApproximation Mode", 4)
        radio1.check(0).Text = "ApproxNone"
        radio1.check(1).Text = "ApproxSimple"
        radio1.check(2).Text = "ApproxTC89KCOS"
        radio1.check(3).Text = "ApproxTC89L1"
        radio1.check(1).Checked = True

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Contour minimum area", 0, 50000, 1000)
            sliders.setupTrackBar(1, "Contour epsilon (arc length percent)", 0, 100, 3)
        End If

        label2 = "Contours_Basics with centroid in red"
        task.desc = "Demo options on FindContours."
    End Sub
    Public Sub setOptions()
        Static frm = findfrm("Contours_Basics Retrieval Mode Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                retrievalMode = Choose(i + 1, cv.RetrievalModes.CComp, cv.RetrievalModes.External, cv.RetrievalModes.FloodFill, cv.RetrievalModes.List, cv.RetrievalModes.Tree)
                Exit For
            End If
        Next
        Static frm1 = findfrm("Contours_Basics ContourApproximation Mode Radio Options")
        For i = 0 To frm1.check.length - 1
            If frm1.check(i).Checked Then
                ApproximationMode = Choose(i + 1, cv.ContourApproximationModes.ApproxNone, cv.ContourApproximationModes.ApproxSimple,
                                              cv.ContourApproximationModes.ApproxTC89KCOS, cv.ContourApproximationModes.ApproxTC89L1)
                Exit For
            End If
        Next
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        setOptions()
        Static dontchange As Boolean
        If task.mouseClickFlag And dontchange Then
            dontchange = False
        Else
            If task.mouseClickFlag Then dontchange = True
        End If
        If dontchange = False Then
            If standalone Or task.intermediateReview = caller Then
                If rotatedRect Is Nothing Then rotatedRect = New Rectangle_Rotated
                Dim imageInput As New cv.Mat
                rotatedRect.src = src
                rotatedRect.Run()
                imageInput = rotatedRect.dst1
                If imageInput.Channels = 3 Then
                    dst1 = imageInput.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)
                Else
                    dst1 = imageInput.ConvertScaleAbs(255)
                End If
            Else
                If src.Channels = 3 Then dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else dst1 = src
            End If
        End If

        If retrievalMode = cv.RetrievalModes.FloodFill Then
            Dim img32sc1 As New cv.Mat
            dst1.ConvertTo(img32sc1, cv.MatType.CV_32SC1)
            contours0 = cv.Cv2.FindContoursAsArray(img32sc1, retrievalMode, ApproximationMode)
            img32sc1.ConvertTo(dst1, cv.MatType.CV_8UC1)
        Else
            contours0 = cv.Cv2.FindContoursAsArray(dst1, retrievalMode, ApproximationMode)
        End If

        Static areaSlider = findSlider("Contour minimum area")
        Static epsilonSlider = findSlider("Contour epsilon (arc length percent)")
        Dim minArea = areaSlider.value
        Dim epsilon = epsilonSlider.value
        contourlist.Clear()
        If standalone Then
            dst2.SetTo(0)
            Dim cnt = contours0.ToArray
            If retrievalMode = cv.RetrievalModes.FloodFill Then
                cv.Cv2.DrawContours(dst2, cnt, -1, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
            Else
                cv.Cv2.DrawContours(dst2, cnt, -1, cv.Scalar.Yellow, 3, cv.LineTypes.AntiAlias)
            End If

            For i = 0 To contours0.Length - 1 Step 2
                Dim m = cv.Cv2.Moments(contours0(i), False)
                Dim pt = New cv.Point(m.M10 / m.M00, m.M01 / m.M00)

                Dim area = cv.Cv2.ContourArea(contours0(i))
                If area > minArea Then
                    contourlist.Add(cv.Cv2.ApproxPolyDP(contours0(i), epsilon, True))
                    dst2.Circle(pt, ocvb.dotSize, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                    cv.Cv2.PutText(dst2, Format(area / 1000, "#0") + "k pixels", New cv.Point(pt.X + ocvb.dotSize, pt.Y), cv.HersheyFonts.HersheyComplexSmall, ocvb.fontSize, cv.Scalar.White)
                Else
                    cv.Cv2.PutText(dst2, "too small", New cv.Point(pt.X + ocvb.dotSize, pt.Y), cv.HersheyFonts.HersheyComplexSmall, ocvb.fontSize, cv.Scalar.White)
                End If
            Next
        Else
            centroids.Clear()
            sortedContours.Clear()

            For i = 0 To contours0.Length - 1 Step 2
                Dim m = cv.Cv2.Moments(contours0(i), False)
                Dim pt = New cv.Point(m.M10 / m.M00, m.M01 / m.M00)
                Dim area = cv.Cv2.ContourArea(contours0(i))
                If area > minArea Then
                    sortedContours.Add(area, cv.Cv2.ApproxPolyDP(contours0(i), epsilon, True))
                    contourlist.Add(cv.Cv2.ApproxPolyDP(contours0(i), epsilon, True))
                    centroids.Add(pt)
                End If
            Next
        End If
    End Sub
End Class








Public Class Contours_RGB
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Find and draw the contour of the largest foreground RGB contour."
        label2 = "Background"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim img = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        img.SetTo(0, task.inrange.noDepthMask)

        Dim contours0 = cv.Cv2.FindContoursAsArray(img, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim maxIndex As Integer
        Dim maxNodes As Integer
        For i = 0 To contours0.Length - 1
            Dim contours = cv.Cv2.ApproxPolyDP(contours0(i), 3, True)
            If maxNodes < contours.Length Then
                maxIndex = i
                maxNodes = contours.Length
            End If
        Next

        If contours0(maxIndex).Length = 0 Then Exit Sub

        Dim hull() = cv.Cv2.ConvexHull(contours0(maxIndex), True)
        Dim listOfPoints = New List(Of List(Of cv.Point))
        Dim points = New List(Of cv.Point)
        For i = 0 To hull.Count - 1
            points.Add(New cv.Point(hull(i).X, hull(i).Y))
        Next
        listOfPoints.Add(points)
        dst1.SetTo(0)
        cv.Cv2.DrawContours(dst1, listOfPoints, 0, New cv.Scalar(255, 0, 0), -1)
        cv.Cv2.DrawContours(dst1, contours0, maxIndex, New cv.Scalar(0, 255, 255), -1)
        dst2.SetTo(0)
        src.CopyTo(dst2, task.inrange.noDepthMask)
    End Sub
End Class





' https://github.com/SciSharp/SharpCV/blob/master/src/SharpCV.Examples/Program.cs
Public Class Contours_RemoveLines
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 3)
            sliders.setupTrackBar(0, "Morphology width/height", 1, 100, 20)
            sliders.setupTrackBar(1, "MorphologyEx iterations", 1, 5, 1)
            sliders.setupTrackBar(2, "Contour thickness", 1, 10, 3)
        End If
        label1 = "Original image"
        label2 = "Original with horizontal/vertical lines removed"
        task.desc = "Remove the lines from an invoice image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim tmp = cv.Cv2.ImRead(ocvb.parms.homeDir + "Data/invoice.jpg")
        Dim dstSize = New cv.Size(src.Height / tmp.Height * src.Width, src.Height)
        Dim dstRect = New cv.Rect(0, 0, dstSize.Width, src.Height)
        dst1(dstRect) = tmp.Resize(dstSize)
        Dim gray = tmp.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim thresh = gray.Threshold(0, 255, cv.ThresholdTypes.BinaryInv Or cv.ThresholdTypes.Otsu)

        ' remove horizontal lines
        Dim hkernel = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(sliders.trackbar(0).Value, 1))
        Dim removedH As New cv.Mat
        cv.Cv2.MorphologyEx(thresh, removedH, cv.MorphTypes.Open, hkernel,, sliders.trackbar(1).Value)
        Dim cnts = cv.Cv2.FindContoursAsArray(removedH, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
        For i = 0 To cnts.Count - 1
            cv.Cv2.DrawContours(tmp, cnts, i, cv.Scalar.White, sliders.trackbar(2).Value)
        Next

        Dim vkernel = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(1, sliders.trackbar(0).Value))
        Dim removedV As New cv.Mat
        cv.Cv2.MorphologyEx(thresh, removedV, cv.MorphTypes.Open, vkernel,, sliders.trackbar(1).Value)
        cnts = cv.Cv2.FindContoursAsArray(removedV, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
        For i = 0 To cnts.Count - 1
            cv.Cv2.DrawContours(tmp, cnts, i, cv.Scalar.White, sliders.trackbar(2).Value)
        Next

        dst2(dstRect) = tmp.Resize(dstSize)
        cv.Cv2.ImShow("Altered image at original resolution", tmp)
    End Sub
End Class





Public Class Contours_Depth
    Inherits VBparent
    Public contours As New List(Of cv.Point)
    Public Sub New()
        initParent()
        task.desc = "Find and draw the contour of the depth foreground."
        label1 = "DepthContour input"
        label2 = "DepthContour output"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        dst1 = task.inrange.noDepthMask
        dst2.SetTo(0)
        Dim input As cv.Mat = task.inrange.depthmask
        Dim contours0 = cv.Cv2.FindContoursAsArray(input, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim maxIndex As Integer
        Dim maxNodes As Integer
        For i = 0 To contours0.Length - 1
            Dim c = cv.Cv2.ApproxPolyDP(contours0(i), 3, True)
            If maxNodes < c.Length Then
                maxIndex = i
                maxNodes = c.Length
            End If
        Next
        If contours0.Length Then
            cv.Cv2.DrawContours(dst2, contours0, maxIndex, New cv.Scalar(0, 255, 255), -1)
            contours.Clear()
            For Each ct In contours0(maxIndex)
                contours.Add(ct)
            Next
        End If
    End Sub
End Class






Public Class Contours_Prediction
    Inherits VBparent
    Dim outline As Contours_Depth
    Dim kalman As Kalman_Basics
    Public Sub New()
        initParent()
        kalman = New Kalman_Basics()
        ReDim kalman.kInput(2 - 1)
        outline = New Contours_Depth()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Predict the nth point ahead of the current point", 1, 100, 1)
        End If
        label1 = "Original contour image"
        label2 = "Image after smoothing with Kalman_Basics"
        task.desc = "Predict the next contour point with Kalman to smooth the outline"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        outline.Run()
        dst1 = outline.dst2
        dst2.SetTo(0)
        Dim stepSize = sliders.trackbar(0).Value
        Dim len = outline.contours.Count
        kalman.kInput = {outline.contours(0).X, outline.contours(0).Y}
        kalman.Run()
        Dim origin = New cv.Point(kalman.kOutput(0), kalman.kOutput(1))
        For i = 0 To outline.contours.Count - 1 Step stepSize
            Dim pt1 = New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1))
            kalman.kInput = {outline.contours(i Mod len).X, outline.contours(i Mod len).Y}
            kalman.Run()
            Dim pt2 = New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1))
            dst2.Line(pt1, pt2, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)
        Next
        dst2.Line(New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), origin, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)
        label1 = "There were " + CStr(outline.contours.Count) + " points in this contour"
    End Sub
End Class








Public Class Contours_FindandDraw
    Inherits VBparent
    Dim rotatedRect As Rectangle_Rotated
    Public Sub New()
        initParent()
        rotatedRect = New Rectangle_Rotated()
        rotatedRect.rect.sliders.trackbar(0).Value = 5
        label1 = "FindandDraw input"
        label2 = "FindandDraw output"
        task.desc = "Demo the use of FindContours, ApproxPolyDP, and DrawContours."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        rotatedRect.src = src
        rotatedRect.Run()
        dst1 = rotatedRect.dst1
        Dim img = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
        Dim tmp As New cv.Mat
        img.ConvertTo(tmp, cv.MatType.CV_32SC1)
        Dim contours0 = cv.Cv2.FindContoursAsArray(tmp, cv.RetrievalModes.FloodFill, cv.ContourApproximationModes.ApproxSimple)
        Dim contours As New List(Of cv.Point())
        dst2.SetTo(0)
        For j = 0 To contours0.Length - 1
            Dim nextContour = cv.Cv2.ApproxPolyDP(contours0(j), 3, True)
            If nextContour.Length > 2 Then contours.Add(nextContour)
        Next

        cv.Cv2.DrawContours(dst2, contours.ToArray, -1, New cv.Scalar(0, 255, 255), 2, cv.LineTypes.AntiAlias)
    End Sub
End Class










Public Class Contours_Binarized
    Inherits VBparent
    Dim edges As Edges_BinarizedSobel
    Dim contours As Contours_Basics
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()
        palette = New Palette_Basics
        contours = New Contours_Basics
        edges = New Edges_BinarizedSobel
        Dim kernelSlider = findSlider("Sobel kernel Size")
        kernelSlider.Value = 3

        task.desc = "Find contours in the Edges after binarized"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        edges.src = src
        edges.Run()
        dst1 = edges.dst2

        contours.src = dst1.Clone
        contours.Run()

        Dim cntList = contours.sortedContours
        If cntList.Count = 0 Then Exit Sub ' there were no lines?
        Dim incr = If(cntList.Count > 255, 1, CInt(255 / cntList.Count))
        dst2.SetTo(0)
        For i = 0 To cntList.Count - 1
            'Dim pt = cntList.ElementAt(0).Value(0)
            'If pt.X < 0 Then pt.X = 0
            'If pt.Y < 0 Then pt.Y = 0
            Dim lPoints = New List(Of List(Of cv.Point))
            lPoints.Add(cntList.ElementAt(i).Value.ToList)
            cv.Cv2.DrawContours(CType(dst2, cv.InputOutputArray), lPoints, 0, ocvb.scalarColors(i Mod 255), -1)
            Dim c = ocvb.scalarColors(i Mod 255)
        Next

        'palette.src = gray
        'palette.Run()
        'dst2 = palette.dst1
        'dst2.SetTo(0, dst1)

        ' cv.Cv2.DrawContours(dst2, contours.contours0.ToArray, -1, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        'For i = 0 To contours.centroids.Count - 1
        '    dst2.Circle(contours.centroids(i), ocvb.dotSize, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        'Next
    End Sub
End Class