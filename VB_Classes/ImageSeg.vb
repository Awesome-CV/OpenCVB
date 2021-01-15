﻿Imports cv = OpenCvSharp
Public Class ImageSeg_Basics
    Inherits VBparent
    Dim addw As AddWeighted_Basics

    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public floodPoints As New List(Of cv.Point)

    Public flood As FloodFill_FullImage
    Public Sub New()
        initParent()
        addw = New AddWeighted_Basics
        flood = New FloodFill_FullImage
        task.desc = "Get the image segments and their associated features - centroids, masks, size, and enclosing rectangles"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        flood.src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        flood.Run()
        dst1 = flood.dst2

        maskSizes = New SortedList(Of Integer, Integer)(flood.maskSizes)
        rects = New List(Of cv.Rect)(flood.rects)
        masks = New List(Of cv.Mat)(flood.masks)
        centroids = New List(Of cv.Point2f)(flood.centroids)
        floodPoints = New List(Of cv.Point)(flood.floodPoints)

        addw.src = dst1
        addw.src2 = src
        addw.Run()
        dst2 = addw.dst1

        For Each pt In floodPoints
            dst1.Circle(pt, ocvb.dotSize - 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Next

        label2 = addw.label1.Replace("depth", "ImageSeg")
    End Sub
End Class







Public Class ImageSeg_InRange
    Inherits VBparent
    Dim iSeg As ImageSeg_Basics
    Public Sub New()
        initParent()
        iSeg = New ImageSeg_Basics
        task.desc = "Trim segments that are not in the range requested"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        iSeg.src = src
        iSeg.Run()
        dst1 = iSeg.dst2

        For i = 0 To iSeg.maskSizes.Count - 1
            Dim mask = iSeg.masks(i)
            Dim r = iSeg.rects(i)
            Dim meanDepth = task.depth32f(r).Mean(mask)
            If meanDepth.Val0 >= task.inrange.maxval Then dst1(r).SetTo(0, mask)
            If meanDepth.Val0 <= task.inrange.minval Then dst1(r).SetTo(0, mask)
        Next
    End Sub
End Class








Public Class ImageSeg_MissingSegments
    Inherits VBparent
    Public flood As FloodFill_FullImage
    Public Sub New()
        initParent()

        flood = New FloodFill_FullImage

        task.desc = "Floodfill segments which were marked as missing and clear small unused segments"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static lenContourSlider = findSlider("Minimum length for missing contours")
        Dim maxLen = lenContourSlider.value
        Static stepSlider = findSlider("FloodFill Step Size")
        Static fillSlider = findSlider("FloodFill point distance from edge")
        Dim fill = fillSlider.value
        Dim stepSize = stepSlider.Value

        Static saveStepSize As Integer
        Static saveFillDistance As Integer
        Dim resetColors As Boolean
        If saveStepSize <> stepSize Or saveFillDistance <> fill Then
            resetColors = True
            saveStepSize = stepSize
            saveFillDistance = fill
        End If

        flood.src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        flood.Run()
        dst1 = flood.dst2

        dst2 = flood.missingSegments
        Dim tmp As New cv.Mat
        flood.missingSegments.ConvertTo(tmp, cv.MatType.CV_32SC1)
        Dim contours0 = cv.Cv2.FindContoursAsArray(tmp, cv.RetrievalModes.FloodFill, cv.ContourApproximationModes.ApproxSimple)
        Dim contours As New List(Of cv.Point())
        For i = 0 To contours0.Length - 1
            Dim nextContour = cv.Cv2.ApproxPolyDP(contours0(i), 3, True)

            If nextContour.Length >= maxLen Then contours.Add(nextContour)
        Next
        cv.Cv2.DrawContours(dst2, contours.ToArray, -1, 128, -1, cv.LineTypes.AntiAlias)
        label2 = CStr(contours.Count) + " contours were found "
    End Sub
End Class








Public Class ImageSeg_Unstable
    Inherits VBparent
    Dim iSeg As ImageSeg_Basics
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()
        iSeg = New ImageSeg_Basics
        palette = New Palette_Basics


        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "A segment is considered present after this many appearances", 1, 40, 20)
        End If

        task.desc = "Find the unstable segments and remove them"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static segSlider = findSlider("A segment is considered present after this many appearances")
        Dim refreshCount = segSlider.value

        iSeg.src = src
        iSeg.Run()
        dst1 = iSeg.dst1

        Dim tmp = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static previousFrame = tmp

        If ocvb.frameCount Mod refreshCount = 0 Then previousFrame = tmp
        cv.Cv2.Min(tmp, previousFrame, dst2)
        previousFrame = dst2

        palette.src = dst2
        palette.Run()
        dst2 = palette.dst1
        dst2.SetTo(0, iSeg.flood.mats.mat(1))
    End Sub
End Class







Public Class ImageSeg_CentroidTracker
    Inherits VBparent
    Public iSeg As ImageSeg_Basics
    Public pTrack As KNN_PointTracker
    Public Sub New()
        initParent()
        iSeg = New ImageSeg_Basics
        pTrack = New KNN_PointTracker
        Dim drawCheckbox = findCheckBox("Caller will handle any drawing required")
        drawCheckbox.Checked = True

        label1 = "Output of ImageSeg_Basics"
        task.desc = "Track the centroids that are found consistently from frame to frame."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        iSeg.src = src
        iSeg.Run()
        dst1 = iSeg.dst1

        If iSeg.flood.dst1.Channels = 3 Then pTrack.src = iSeg.flood.dst1 Else pTrack.src = iSeg.flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(iSeg.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(iSeg.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(iSeg.masks)
        pTrack.floodPoints = New List(Of cv.Point)(iSeg.floodPoints)
        pTrack.Run()
        dst2 = dst1.Clone
        For Each vo In pTrack.drawRC.viewObjects
            dst2.Circle(vo.Value.centroid, ocvb.dotSize + 1, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
            dst2.Circle(vo.Value.centroid, ocvb.dotSize - 2, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
            dst2.Line(vo.Value.floodPoint, vo.Value.centroid, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next

        For Each pt In pTrack.floodPoints
            dst2.Circle(pt, ocvb.dotSize - 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Next
        label2 = "Centroid " + CStr(pTrack.drawRC.viewObjects.Count) + " blue, floodPoint " + CStr(pTrack.floodPoints.Count) + " yellow"
    End Sub
End Class