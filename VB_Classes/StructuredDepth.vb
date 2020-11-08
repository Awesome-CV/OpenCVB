﻿Imports cv = OpenCvSharp
Public Class StructuredDepth_BasicsSide
    Inherits VBparent
    Public side2D As Histogram_SideData
    Dim inrange As Depth_InRange
    Public floorRun As Boolean
    Public inputYCoordinate As Integer
    Dim histThresholdSlider As Windows.Forms.TrackBar
    Dim cushionSlider As Windows.Forms.TrackBar
    Public maskPlane As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        side2D = New Histogram_SideData(ocvb)
        inrange = New Depth_InRange(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 100, 1)
        sliders.setupTrackBar(1, "Offset for the slice", 0, src.Height - 1, src.Height / 2)

        histThresholdSlider = findSlider("Histogram threshold")
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")

        ' Some cameras are less accurate and need a fatter slice or a histogram threshold to identify the ceiling or floor...
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D455 Then
            cushionSlider.Value = 25
            histThresholdSlider.Value = 5
        End If
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D435i Then
            cushionSlider.Value = 50
            histThresholdSlider.Value = 5
        End If
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.MyntD1000 Then cushionSlider.Value = 30
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2 Then
            cushionSlider.Value = 30
            histThresholdSlider.Value = 10 ' this camera is showing a lot of data below the ground plane.
        End If

        label2 = "Yellow bar is ceiling.  Yellow line is camera level."
        ocvb.desc = "Find and isolate planes (floor and ceiling) in a side view histogram."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        side2D.Run(ocvb)
        dst2 = side2D.dst1

        Dim yCoordinate = inputYCoordinate ' if zero, find the ycoordinate.
        If inputYCoordinate = 0 Then
            If floorRun Then
                Dim lastSum = dst2.Row(dst2.Height - 1).Sum()
                For yCoordinate = dst2.Height - 1 To 0 Step -1
                    Dim nextSum = dst2.Row(yCoordinate).Sum()
                    If nextSum.Item(0) - lastSum.Item(0) > 3000 Then Exit For
                Next
            Else
                Dim lastSum = dst2.Row(yCoordinate).Sum()
                For yCoordinate = 1 To dst2.Height - 1
                    Dim nextSum = dst2.Row(yCoordinate).Sum()
                    If nextSum.Item(0) - lastSum.Item(0) > 3000 Then Exit For
                Next
            End If
        End If

        Dim cushion = cushionSlider.Value
        dst2.Line(New cv.Point(0, yCoordinate), New cv.Point(dst2.Width, yCoordinate), cv.Scalar.Yellow, cushion)

        Dim planeY = side2D.meterMin * (side2D.cameraLevel - yCoordinate) / side2D.cameraLevel
        If yCoordinate > side2D.cameraLevel Then planeY = side2D.meterMax * (yCoordinate - side2D.cameraLevel) / (dst2.Height - side2D.cameraLevel)

        Dim pixelsPerMeterV = Math.Abs(side2D.meterMax - side2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * pixelsPerMeterV
        inrange.minVal = planeY - thicknessMeters
        inrange.maxVal = planeY + thicknessMeters
        inrange.src = side2D.split(1)
        inrange.Run(ocvb)
        maskPlane = inrange.depth32f.ConvertScaleAbs(255)

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane.Resize(src.Size))
        label2 = side2D.label2
    End Sub
End Class







Public Class StructuredDepth_BasicsTop
    Inherits VBparent
    Public top2D As Histogram_TopData
    Dim inrange As Depth_InRange
    Dim sideStruct As StructuredDepth_BasicsSide
    Dim cushionSlider As Windows.Forms.TrackBar
    Dim offsetSlider As Windows.Forms.TrackBar
    Public maskPlane As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        top2D = New Histogram_TopData(ocvb)
        inrange = New Depth_InRange(ocvb)
        sideStruct = New StructuredDepth_BasicsSide(ocvb)

        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        offsetSlider = findSlider("Offset for the slice")
        offsetSlider.Maximum = src.Width - 1
        offsetSlider.Value = src.Width / 2

        ocvb.desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim xCoordinate = offsetSlider.Value
        top2D.Run(ocvb)
        dst2 = top2D.dst1

        Dim cushion = cushionSlider.Value
        dst2.Line(New cv.Point(xCoordinate, 0), New cv.Point(xCoordinate, dst2.Height), cv.Scalar.Yellow, cushion)

        Dim pixelsPerMeterV = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * pixelsPerMeterV

        Dim planeX = top2D.meterMin * (top2D.cameraLevel - xCoordinate) / top2D.cameraLevel
        If xCoordinate > top2D.cameraLevel Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLevel) / (dst2.Width - top2D.cameraLevel)

        inrange.minVal = planeX - thicknessMeters
        inrange.maxVal = planeX + thicknessMeters
        inrange.src = top2D.split(0)
        inrange.Run(ocvb)
        maskPlane = inrange.depth32f.ConvertScaleAbs(255)

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane.Resize(src.Size))
        label2 = top2D.label2
    End Sub
End Class








Public Class StructuredDepth_Floor
    Inherits VBparent
    Dim structD As StructuredDepth_BasicsSide
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsSide(ocvb)
        structD.floorRun = True
        Static histThresholdSlider = findSlider("Histogram threshold")
        histThresholdSlider.value = 10 ' some cameras can show data below ground level...
        Dim cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        cushionSlider.Value = 5 ' floor runs can use a thinner slice that ceilings...

        ' this camera is less precise and needs a fatter slice of the floor.  The IMU looks to be the culprit.
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D435i Then cushionSlider.Value = 20
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.MyntD1000 Then cushionSlider.Value = 10
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2 Then cushionSlider.Value = 10

        ocvb.desc = "Find the floor plane"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        structD.Run(ocvb)
        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class








Public Class StructuredDepth_Ceiling
    Inherits VBparent
    Dim structD As StructuredDepth_BasicsSide
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsSide(ocvb)
        ocvb.desc = "A complementary algorithm to StructuredDepth_Floor..."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        structD.Run(ocvb)
        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class






Public Class StructuredDepth_SliceH
    Inherits VBparent
    Public structD As StructuredDepth_BasicsSide
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsSide(ocvb)
        ocvb.desc = "Take a slice through the side2d projection and show it in a top-down view."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static sliceSlider = findSlider("Offset for the slice")
        structD.inputYCoordinate = sliceSlider.value
        structD.Run(ocvb)
        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class






Public Class StructuredDepth_SliceV
    Inherits VBparent
    Public structD As StructuredDepth_BasicsTop
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsTop(ocvb)
        ocvb.desc = "Take a slice through the top2d projection and show it in a side view."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        structD.Run(ocvb)
        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class







Public Class StructuredDepth_LineDetect
    Inherits VBparent
    Dim sliceH As StructuredDepth_SliceH
    Dim sliceV As StructuredDepth_SliceV
    Dim ldetect As LineDetector_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ldetect = New LineDetector_Basics(ocvb)
        ldetect.drawLines = True
        Dim lenSlider = findSlider("Line length threshold in pixels")
        lenSlider.Value = 50

        sliceH = New StructuredDepth_SliceH(ocvb)
        sliceV = New StructuredDepth_SliceV(ocvb)

        radio.Setup(ocvb, caller, 2)
        radio.check(0).Text = "Horizontal Slice"
        radio.check(1).Text = "Vertical Slice"
        radio.check(1).Checked = True

        ocvb.desc = "Use the line detector on the output of the structuredDepth_Slice algorithms"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static sortlines As New List(Of cv.Vec4f)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static saveRadio As Boolean = radio.check(1).Checked
        Static offsetSlider = findSlider("Offset for the slice")
        If saveRadio <> radio.check(1).Checked Then
            saveRadio = radio.check(1).Checked
            sortlines.Clear()
            If radio.check(0).Checked Then
                offsetSlider.Value = src.Height / 2
                offsetSlider.Maximum = src.Height - 1
            Else
                offsetSlider.Maximum = src.Width - 1
                offsetSlider.Value = src.Width / 2
            End If
        End If
        If radio.check(0).Checked Then
            sliceH.Run(ocvb)
            ldetect.src = sliceH.structD.maskPlane.Resize(dst2.Size)
        Else
            sliceV.Run(ocvb)
            ldetect.src = sliceV.structD.maskPlane.Resize(dst2.Size)
        End If

        Dim tmp = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ldetect.src.SetTo(0, tmp) ' remove previously recognized lines...
        ldetect.Run(ocvb)

        For Each line In ldetect.sortlines
            If sortlines.Contains(line.Value) = False Then sortlines.Add(line.Value)
        Next

        Static thicknessSlider = findSlider("Line thickness")
        Dim thickness = thicknessSlider.Value
        dst2.SetTo(0)
        For Each v In sortlines
            Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
            Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
            dst2.Line(pt1, pt2, cv.Scalar.Yellow, thickness, cv.LineTypes.AntiAlias)
        Next

        dst1 = If(radio.check(0).Checked, sliceH.dst1, sliceV.dst1)
        label1 = "Detected line count = " + CStr(sortlines.Count)
    End Sub
End Class