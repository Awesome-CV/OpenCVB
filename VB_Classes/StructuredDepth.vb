﻿Imports cv = OpenCvSharp
Public Class StructuredDepth_Floor
    Inherits VBparent
    Public structD As StructuredDepth_SliceH
    Dim kalman As Kalman_VB_Basics
    Public floorYplane As Single
    Public Sub New()
        initParent()
        kalman = New Kalman_VB_Basics()

        structD = New StructuredDepth_SliceH()
        structD.histThresholdSlider.Value = 10 ' some cameras can show data below ground level...
        structD.cushionSlider.Value = 5 ' floor runs can use a thinner slice that ceilings...

        task.desc = "Find the floor plane"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        structD.Run()

        Dim yCoordinate = dst2.Height
        Dim lastSum = dst2.Row(dst2.Height - 1).Sum()
        For yCoordinate = dst2.Height - 1 To 0 Step -1
            Dim nextSum = dst2.Row(yCoordinate).Sum()
            If nextSum.Item(0) - lastSum.Item(0) > 1000 Then Exit For
            lastSum = nextSum
        Next

        kalman.kInput = yCoordinate
        kalman.Run()

        ' it settles down quicker...
        If ocvb.frameCount > 30 Then yCoordinate = kalman.kAverage

        floorYplane = structD.side2D.meterMax * (yCoordinate - structD.side2D.cameraLoc) / (dst2.Height - structD.side2D.cameraLoc)

        structD.offsetSlider.Value = If(yCoordinate >= 0, yCoordinate, dst2.Height)

        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class








Public Class StructuredDepth_Ceiling
    Inherits VBparent
    Public structD As StructuredDepth_SliceH
    Dim kalman As Kalman_Basics
    Public Sub New()
        initParent()
        kalman = New Kalman_Basics()
        ReDim kalman.kInput(0)

        structD = New StructuredDepth_SliceH()
        structD.cushionSlider.Value = 10
        task.desc = "Find the ceiling plane"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        structD.Run()

        Dim yCoordinate As Integer
        Dim lastSum = dst2.Row(yCoordinate).Sum()
        For yCoordinate = 1 To dst2.Height - 1
            Dim nextSum = dst2.Row(yCoordinate).Sum()
            If nextSum.Item(0) - lastSum.Item(0) > 1000 Then Exit For
            lastSum = nextSum
        Next

        kalman.kInput(0) = yCoordinate
        kalman.Run()
        structD.offsetSlider.Value = If(kalman.kOutput(0) >= 0, kalman.kOutput(0), 0)

        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class






Public Class StructuredDepth_MultiSliceH
    Inherits VBparent
    Public side2D As Histogram_SideData
    Public structD As StructuredDepth_SliceH
    Public maskPlane As cv.Mat
    Dim inrange As Depth_InRange
    Public Sub New()
        initParent()
        side2D = New Histogram_SideData()
        inrange = New Depth_InRange()
        structD = New StructuredDepth_SliceH()

        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        side2D.Run()
        dst2 = side2D.dst2
        Dim Split = task.pointCloud.Split()

        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Dim cushion = cushionSlider.Value

        Dim metersPerPixel = Math.Abs(side2D.meterMax - side2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim stepsize = stepSlider.value

        maskPlane = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = side2D.meterMin * (side2D.cameraLoc - yCoordinate) / side2D.cameraLoc
            If yCoordinate > side2D.cameraLoc Then planeY = side2D.meterMax * (yCoordinate - side2D.cameraLoc) / (dst2.Height - side2D.cameraLoc)
            inrange.minVal = planeY - thicknessMeters
            inrange.maxVal = planeY + thicknessMeters
            inrange.src = Split(1).Clone
            inrange.Run()
            maskPlane.SetTo(255, inrange.depthMask)
        Next

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane)
        label2 = side2D.label2
    End Sub
End Class






Public Class StructuredDepth_MultiSliceV
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public structD As StructuredDepth_SliceH
    Dim inrange As Depth_InRange
    Public Sub New()
        initParent()

        top2D = New Histogram_TopData()
        inrange = New Depth_InRange()
        structD = New StructuredDepth_SliceH()

        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        top2D.Run()
        dst2 = top2D.dst2

        Dim split = task.pointCloud.Split()

        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Dim cushion = cushionSlider.Value

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim stepsize = stepSlider.value

        Dim maskPlane = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For xCoordinate = 0 To src.Width - 1 Step stepsize
            Dim planeX = top2D.meterMin * (top2D.cameraLoc - xCoordinate) / top2D.cameraLoc
            If xCoordinate > top2D.cameraLoc Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLoc) / (dst2.Width - top2D.cameraLoc)
            inrange.minVal = planeX - thicknessMeters
            inrange.maxVal = planeX + thicknessMeters
            inrange.src = split(0).Clone
            inrange.Run()
            maskPlane.SetTo(255, inrange.depthMask)
        Next

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane)
        label2 = top2D.label2
    End Sub
End Class






Public Class StructuredDepth_MultiSlice
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public side2D As Histogram_SideData
    Dim struct As StructuredDepth_SliceV
    Public inrange As Depth_InRange
    Public maskPlane As cv.Mat
    Public split() As cv.Mat
    Public Sub New()
        initParent()

        side2D = New Histogram_SideData()
        top2D = New Histogram_TopData()
        inrange = New Depth_InRange()
        struct = New StructuredDepth_SliceV()

        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        top2D.Run()
        side2D.Run()
        ' dst2 = top2D.dst2
        split = task.pointCloud.Split()

        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Dim cushion = cushionSlider.Value

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim stepsize = stepSlider.value

        split = task.pointCloud.Split()

        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For xCoordinate = 0 To src.Width - 1 Step stepsize
            Dim planeX = top2D.meterMin * (top2D.cameraLoc - xCoordinate) / top2D.cameraLoc
            If xCoordinate > top2D.cameraLoc Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLoc) / (dst2.Width - top2D.cameraLoc)
            inrange.minVal = planeX - thicknessMeters
            inrange.maxVal = planeX + thicknessMeters
            inrange.src = split(0).Clone
            inrange.Run()
            maskPlane = inrange.depthMask
            dst2.SetTo(255, maskPlane)
        Next

        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = side2D.meterMin * (side2D.cameraLoc - yCoordinate) / side2D.cameraLoc
            If yCoordinate > side2D.cameraLoc Then planeY = side2D.meterMax * (yCoordinate - side2D.cameraLoc) / (dst2.Height - side2D.cameraLoc)
            inrange.minVal = planeY - thicknessMeters
            inrange.maxVal = planeY + thicknessMeters
            inrange.src = Split(1).Clone
            inrange.Run()
            Dim tmp = inrange.depthMask
            cv.Cv2.BitwiseOr(tmp, maskPlane, maskPlane)
            dst2.SetTo(255, maskPlane)
        Next

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, dst2)
    End Sub
End Class







Public Class StructuredDepth_MultiSliceLines
    Inherits VBparent
    Dim multi As StructuredDepth_MultiSlice
    Public ldetect As LineDetector_Basics
    Public Sub New()
        initParent()
        ldetect = New LineDetector_Basics()
        Dim lenSlider = findSlider("Line length threshold in pixels")
        lenSlider.Value = lenSlider.Maximum ' don't need the yellow line...
        multi = New StructuredDepth_MultiSlice()
        task.desc = "Detect lines in the multiSlice output"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        multi.Run()
        cv.Cv2.BitwiseNot(multi.dst2, dst2)
        ldetect.src = multi.dst2
        ldetect.Run()
        dst1 = ldetect.dst1
    End Sub
End Class







Public Class StructuredDepth_MultiSlicePolygon
    Inherits VBparent
    Dim multi As StructuredDepth_MultiSlice
    Public Sub New()
        initParent()
        multi = New StructuredDepth_MultiSlice()
        label1 = "Input to FindContours"
        label2 = "ApproxPolyDP 4-corner object from FindContours input"

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Max number of sides in the identified polygons", 3, 100, 4)
        End If
        task.desc = "Detect polygons in the multiSlice output"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        multi.Run()
        cv.Cv2.BitwiseNot(multi.dst2, dst1)

        Dim rawContours = cv.Cv2.FindContoursAsArray(dst1, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim contours(rawContours.Length - 1)() As cv.Point
        For j = 0 To rawContours.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(rawContours(j), 3, True)
        Next

        dst2.SetTo(0)
        Dim sidesSlider = findSlider("Max number of sides in the identified polygons")
        Dim maxSides = sidesSlider.Value
        For i = 0 To contours.Length - 1
            If contours(i).Length = 2 Then Continue For
            If contours(i).Length <= maxSides Then
                cv.Cv2.DrawContours(dst2, contours, i, New cv.Scalar(0, 255, 255), 2, cv.LineTypes.AntiAlias)
            End If
        Next
    End Sub
End Class






Public Class StructuredDepth_SliceXPlot
    Inherits VBparent
    Dim multi As StructuredDepth_MultiSlice
    Dim structD As StructuredDepth_SliceV
    Dim cushionSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        multi = New StructuredDepth_MultiSlice()
        structD = New StructuredDepth_SliceV()
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        cushionSlider.Value = 25
        task.desc = "Find any plane around a peak value in the top-down histogram"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        structD.Run()
        dst2 = structD.dst2
        multi.Run()

        Static offsetSlider = findSlider("Offset for the slice")
        Dim col = CInt(offsetSlider.value)

        Dim cushion = cushionSlider.Value
        Dim rect = New cv.Rect(col, 0, cushion, dst2.Height - 1)
        Dim minVal As Double, maxVal As Double
        Dim minLoc As cv.Point, maxLoc As cv.Point
        multi.top2D.histOutput(rect).MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

        dst2.Circle(New cv.Point(col, maxLoc.Y), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Dim filterZ = (dst2.Height - maxLoc.Y) / dst2.Height * ocvb.maxZ

        Dim maskZplane As New cv.Mat(multi.split(0).Size, cv.MatType.CV_8U, 255)
        If filterZ > 0 Then
            multi.inrange.minVal = filterZ - 0.05 ' a 10 cm buffer surrounding the z value
            multi.inrange.maxVal = filterZ + 0.05
            multi.inrange.src = multi.split(2)
            multi.inrange.Run()
            maskZplane = multi.inrange.depthMask
        End If

        If filterZ > 0 Then cv.Cv2.BitwiseAnd(multi.maskPlane, maskZplane, maskZplane)

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, maskZplane)

        Dim pixelsPerMeter = dst2.Height / ocvb.maxZ
        label2 = "Peak histogram count (" + Format(maxVal, "#0") + ") at " + Format(filterZ, "#0.00") + " meters +-" + Format(10 / pixelsPerMeter, "#0.00") + " m"
    End Sub
End Class







Public Class StructuredDepth_LinearizeFloor
    Inherits VBparent
    Public floor As StructuredDepth_Floor
    Dim kalman As Kalman_VB_Basics
    Public imuPointCloud As cv.Mat
    Public maskPlane As cv.Mat
    Public floorYPlane As Single
    Public Sub New()
        initParent()
        kalman = New Kalman_VB_Basics()
        floor = New StructuredDepth_Floor()

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 3)
            check.Box(0).Text = "Smooth in X-direction"
            check.Box(1).Text = "Smooth in Y-direction"
            check.Box(2).Text = "Smooth in Z-direction"
            check.Box(1).Checked = True
        End If
        task.desc = "Using the mask for the floor create a better representation of the floor plane"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim minVal As Double, maxVal As Double
        Dim minLoc As cv.Point, maxLoc As cv.Point
        Static imuPC As cv.Mat
        floor.Run()
        dst1 = floor.dst1
        dst2 = floor.dst2
        maskPlane = floor.structD.maskPlane
        If maskPlane.CountNonZero() > 0 Then
            Dim nonFloorMask As New cv.Mat
            cv.Cv2.BitwiseNot(maskPlane, nonFloorMask)
            imuPC = task.pointCloud.Clone
            imuPointCloud = imuPC.Clone
            imuPC.SetTo(0, nonFloorMask)

            Dim split = imuPC.Split()
            Static xCheck = findCheckBox("Smooth in X-direction")
            If xCheck.Checked Then
                split(0).MinMaxLoc(minVal, maxVal, minLoc, maxLoc, maskPlane)

                Dim firstCol As Integer, lastCol As Integer
                For firstCol = 0 To maskPlane.Width - 1
                    If maskPlane.Col(firstCol).CountNonZero() > 0 Then Exit For
                Next
                For lastCol = maskPlane.Width - 1 To 0 Step -1
                    If maskPlane.Col(lastCol).CountNonZero() Then Exit For
                Next

                Dim xIncr = (maxVal - minVal) / (lastCol - firstCol)
                For i = firstCol To lastCol
                    Dim maskCol = maskPlane.Col(i)
                    If maskCol.CountNonZero > 0 Then split(0).Col(i).SetTo(minVal + xIncr * i, maskCol)
                Next
            End If

            Static yCheck = findCheckBox("Smooth in Y-direction")
            If yCheck.Checked Then
                split(1).MinMaxLoc(minVal, maxVal, minLoc, maxLoc, maskPlane)
                kalman.kInput = (minVal + maxVal) / 2
                kalman.Run()
                floorYPlane = kalman.kAverage
                split(1).SetTo(floorYPlane, maskPlane)
            End If

            Static zCheck = findCheckBox("Smooth in Z-direction")
            If zCheck.Checked Then
                Dim firstRow As Integer, lastRow As Integer
                For firstRow = 0 To maskPlane.Height - 1
                    If maskPlane.Row(firstRow).CountNonZero() > 20 Then Exit For
                Next
                For lastRow = maskPlane.Height - 1 To 0 Step -1
                    If maskPlane.Row(lastRow).CountNonZero() > 20 Then Exit For
                Next

                If lastRow >= 0 And firstRow < maskPlane.Height Then
                    Dim meanMin = split(2).Row(lastRow).Mean(maskPlane.Row(lastRow))
                    Dim meanMax = split(2).Row(firstRow).Mean(maskPlane.Row(firstRow))
                    Dim zIncr = (meanMax.Item(0) - meanMin.Item(0)) / Math.Abs(lastRow - firstRow)
                    For i = firstRow To lastRow
                        Dim maskRow = maskPlane.Row(i)
                        Dim mean = split(2).Row(i).Mean(maskRow)
                        If maskRow.CountNonZero > 0 Then
                            split(2).Row(i).SetTo(mean.Item(0))
                            'Dim xy = New cv.Point3f(0, i, mean.Item(0))
                            'For xy.X = 0 To split(2).Width - 1
                            '    Dim xyz = getWorldCoordinates(xy)
                            '    imuPC.Set(Of cv.Point3f)(i, xy.X, xyz)
                            'Next
                        End If
                    Next
                    dst1.Line(New cv.Point(0, firstRow), New cv.Point(dst1.Width, firstRow), cv.Scalar.Yellow, 2)
                    dst1.Line(New cv.Point(0, lastRow), New cv.Point(dst1.Width, lastRow), cv.Scalar.Yellow, 2)
                End If
            End If

            cv.Cv2.Merge(split, imuPC)

            imuPC.CopyTo(imuPointCloud, maskPlane)
        End If
    End Sub
End Class







Public Class StructuredDepth_SliceH
    Inherits VBparent
    Public side2D As Histogram_SideData
    Dim inrange As Depth_InRange
    Public histThresholdSlider As Windows.Forms.TrackBar
    Public cushionSlider As Windows.Forms.TrackBar
    Public offsetSlider As Windows.Forms.TrackBar
    Public maskPlane As cv.Mat
    Public yPlaneOffset As Integer
    Public Sub New()
        initParent()
        side2D = New Histogram_SideData()
        inrange = New Depth_InRange()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 100, 1)
            sliders.setupTrackBar(1, "Offset for the slice", 0, src.Width - 1, src.Height / 2 - 20)
            sliders.setupTrackBar(2, "Slice step size in pixels (multi-slice option only)", 1, 100, 20)
        End If

        histThresholdSlider = findSlider("Top/Side View Histogram threshold")
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        offsetSlider = findSlider("Offset for the slice")

        label2 = "Yellow bar is ceiling.  Yellow line is camera level."
        task.desc = "Find and isolate planes (floor and ceiling) in a side view histogram."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        side2D.Run()
        dst2 = side2D.dst2
        Dim Split = task.pointCloud.Split()

        Dim yCoordinate = CInt(offsetSlider.Value)

        Dim planeY = side2D.meterMin * (side2D.cameraLoc - yCoordinate) / side2D.cameraLoc
        If yCoordinate > side2D.cameraLoc Then planeY = side2D.meterMax * (yCoordinate - side2D.cameraLoc) / (dst2.Height - side2D.cameraLoc)

        Dim metersPerPixel = Math.Abs(side2D.meterMax - side2D.meterMin) / dst2.Height
        Dim cushion = cushionSlider.Value
        Dim thicknessMeters = cushion * metersPerPixel
        inrange.minVal = planeY - thicknessMeters
        inrange.maxVal = planeY + thicknessMeters
        inrange.src = Split(1).Clone
        inrange.Run()
        maskPlane = inrange.depthMask

        label1 = "At offset " + CStr(yCoordinate) + " y = " + Format((inrange.maxVal + inrange.minVal) / 2, "#0.00") + " with " +
                 Format(Math.Abs(inrange.maxVal - inrange.minVal) * 100, "0.00") + " cm width"

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane)
        label2 = side2D.label2

        dst2 = dst2.ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2.Circle(New cv.Point(0, side2D.cameraLoc), ocvb.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        yPlaneOffset = If(offsetSlider.Value < dst2.Height - cushion, CInt(offsetSlider.Value), dst2.Height - cushion - 1)
        dst2.Line(New cv.Point(0, yPlaneOffset), New cv.Point(dst2.Width, yPlaneOffset), cv.Scalar.Yellow, cushion)
    End Sub
End Class







Public Class StructuredDepth_SliceV
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public inrange As Depth_InRange
    Dim sideStruct As StructuredDepth_SliceH
    Public cushionSlider As Windows.Forms.TrackBar
    Public offsetSlider As Windows.Forms.TrackBar
    Public maskPlane As cv.Mat
    Public Sub New()
        initParent()
        top2D = New Histogram_TopData()
        inrange = New Depth_InRange()
        sideStruct = New StructuredDepth_SliceH()

        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        offsetSlider = findSlider("Offset for the slice")
        offsetSlider.Value = src.Width / 2 - 20

        task.desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim xCoordinate = offsetSlider.Value
        top2D.Run()
        dst2 = top2D.dst2

        Dim split = task.pointCloud.Split()

        Dim planeX = top2D.meterMin * (top2D.cameraLoc - xCoordinate) / top2D.cameraLoc
        If xCoordinate > top2D.cameraLoc Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLoc) / (dst2.Width - top2D.cameraLoc)

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim cushion = cushionSlider.Value
        Dim thicknessMeters = cushion * metersPerPixel

        inrange.minVal = planeX - thicknessMeters
        inrange.maxVal = planeX + thicknessMeters
        inrange.src = split(0).Clone
        inrange.Run()
        maskPlane = inrange.depthMask

        label1 = "At offset " + CStr(xCoordinate) + " x = " + Format((inrange.maxVal + inrange.minVal) / 2, "#0.00") + " with " +
                 Format(Math.Abs(inrange.maxVal - inrange.minVal) * 100, "0.00") + " cm width"

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane)
        label2 = top2D.label2

        dst2 = dst2.Normalize(0, 255, cv.NormTypes.MinMax)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2.Circle(New cv.Point(top2D.cameraLoc, dst2.Height), ocvb.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Dim offset = CInt(offsetSlider.Value)
        dst2.Line(New cv.Point(offset, 0), New cv.Point(offset, dst2.Height), cv.Scalar.Yellow, cushion)
    End Sub
End Class














Public Class StructuredDepth_SliceVStable
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public inrange As Depth_InRange
    Dim sideStruct As StructuredDepth_SliceH
    Public cushionSlider As Windows.Forms.TrackBar
    Public offsetSlider As Windows.Forms.TrackBar
    Public maskPlane As cv.Mat
    Public Sub New()
        initParent()
        top2D = New Histogram_TopData()
        inrange = New Depth_InRange()
        sideStruct = New StructuredDepth_SliceH()

        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        offsetSlider = findSlider("Offset for the slice")
        offsetSlider.Value = src.Width / 2 - 20

        task.desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim xCoordinate = offsetSlider.Value
        top2D.Run()
        dst2 = top2D.dst2
        Dim split = task.pointCloud.Split()

        Dim planeX = top2D.meterMin * (top2D.cameraLoc - xCoordinate) / top2D.cameraLoc
        If xCoordinate > top2D.cameraLoc Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLoc) / (dst2.Width - top2D.cameraLoc)

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim cushion = cushionSlider.Value
        Dim thicknessMeters = cushion * metersPerPixel

        inrange.minVal = planeX - thicknessMeters
        inrange.maxVal = planeX + thicknessMeters
        inrange.src = split(0).Clone
        inrange.Run()
        maskPlane = inrange.depthMask

        label1 = "At offset " + CStr(xCoordinate) + " x = " + Format((inrange.maxVal + inrange.minVal) / 2, "#0.00") + " with " +
                 Format(Math.Abs(inrange.maxVal - inrange.minVal) * 100, "0.00") + " cm width"

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane)
        label2 = top2D.label2

        dst2 = dst2.Normalize(0, 255, cv.NormTypes.MinMax)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2.Circle(New cv.Point(top2D.cameraLoc, dst2.Height), ocvb.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Dim offset = CInt(offsetSlider.Value)
        dst2.Line(New cv.Point(offset, 0), New cv.Point(offset, dst2.Height), cv.Scalar.Yellow, cushion)
    End Sub
End Class
