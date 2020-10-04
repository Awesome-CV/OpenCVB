﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Fuzzy_Basics
    Inherits VBparent
    Dim Fuzzy As IntPtr
    Dim reduction As Reduction_Basics
    Dim options As Contours_Basics
    Public palette As Palette_Basics
    Public gray As cv.Mat
    Public contours As cv.Point()()
    Public sortContours As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        options = New Contours_Basics(ocvb)
        options.rotatedRect.rect.sliders.Visible = False
        Dim floodRadio = findRadio("FloodFill")
        If floodRadio.Enabled Then floodRadio.Enabled = False ' too much special handling - cv_32SC1 image 

        palette = New Palette_Basics(ocvb)
        reduction = New Reduction_Basics(ocvb)
        Fuzzy = Fuzzy_Open()

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Threshold for rectangle size", 50, 50000, 10000)
        If standalone Then sliders.Visible = False

        label1 = "Solid regions"
        label2 = "Fuzzy pixels - not solid"
        ocvb.desc = "That which is not solid is fuzzy"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        options.setOptions()
        reduction.src = src
        reduction.Run(ocvb)
        dst1 = reduction.dst1
        dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim srcData(dst1.Total) As Byte
        Marshal.Copy(dst1.Data, srcData, 0, srcData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = Fuzzy_Run(Fuzzy, handleSrc.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(dst1.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            gray = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_8UC1, dstData)
        End If

        dst2 = gray.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        contours = cv.Cv2.FindContoursAsArray(dst2, options.retrievalMode, options.ApproximationMode)

        sortContours.Clear()
        For i = 0 To contours.Length - 1
            ' get this region's ID
            Dim maskID As Integer = 0
            Dim pt = contours(i)(0)
            For y = pt.Y - 1 To pt.Y + 1
                For x = pt.X - 1 To pt.X + 1
                    If x < src.Width And y < src.Height Then
                        Dim val = gray.Get(Of Byte)(y, x)
                        If val <> 0 Then maskID = val
                    End If
                Next
            Next

            Dim len = contours(i).Length
            For j = 0 To len
                dst2.Line(contours(i)(j Mod len), contours(i)((j + 1) Mod len), cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
            Next
            sortContours.Add(len, New cv.Point(i, maskID))
        Next

        palette.src = gray
        palette.Run(ocvb)
        dst1 = palette.dst1
        dst1.SetTo(0, dst2)
    End Sub
    Public Sub Close()
        Fuzzy_Close(Fuzzy)
    End Sub
End Class








Module Fuzzy_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Fuzzy_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Fuzzy_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Fuzzy_Run(cPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
End Module








Public Class Fuzzy_ContoursDepth
    Inherits VBparent
    Public fuzzyD As Fuzzy_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fuzzyD = New Fuzzy_Basics(ocvb)

        ocvb.desc = "Use contours to outline solids in the depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        fuzzyD.src = ocvb.RGBDepth
        fuzzyD.Run(ocvb)
        dst1 = fuzzyD.dst1
    End Sub
End Class








Public Class Fuzzy_NeighborProof
    Inherits VBparent
    Dim fuzzy As Fuzzy_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fuzzy = New Fuzzy_Basics(ocvb)
        ocvb.desc = "Prove that every contour point has at least one and only one neighbor with the mask ID and that the rest are zero"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static proofFailed As Boolean = False
        If proofFailed Then Exit Sub
        fuzzy.src = src
        fuzzy.Run(ocvb)
        dst1 = fuzzy.gray
        For i = 0 To fuzzy.contours.Length - 1
            Dim len = fuzzy.contours(i).Length
            For j = 0 To len - 1
                Dim pt = fuzzy.contours(i)(j)
                Dim maskID As Integer = 0
                For y = pt.Y - 1 To pt.Y + 1
                    For x = pt.X - 1 To pt.X + 1
                        If x < src.Width And y < src.Height Then
                            Dim val = dst1.Get(Of Byte)(y, x)
                            If val <> 0 Then maskID = val
                            If maskID <> 0 And val <> 0 And maskID <> val Then
                                MsgBox("Proof has failed!  There is more than one mask ID identified by this contour point.")
                                proofFailed = True
                                Exit Sub
                            End If
                        End If
                    Next
                Next
            Next
        Next
        ocvb.trueText("Mask ID's for all contour point in each region identified only one region.", 10, 50, 3)
    End Sub
End Class








Public Class Fuzzy_TrackerDepth
    Inherits VBparent
    Public fuzzy As Fuzzy_Basics
    Public centroids As New List(Of cv.Point)
    Public rects As New List(Of cv.Rect)
    Public layoutColor As New List(Of Integer)
    Public highlightPoint As cv.Point
    Public highlightRect As cv.Rect
    Public highlightRegion = -1
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fuzzy = New Fuzzy_Basics(ocvb)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Display centroid and rectangle for each region"
        check.Box(0).Checked = True

        ocvb.desc = "Create centroids and rect's for solid regions and track them - tracker"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        fuzzy.src = ocvb.RGBDepth
        fuzzy.Run(ocvb)
        dst1 = fuzzy.dst1

        centroids.Clear()
        rects.Clear()
        layoutColor.Clear()
        Dim minX As Double, maxX As Double
        Dim minY As Double, maxY As Double
        Static displayCheck = findCheckBox("Display centroid and rectangle for each region")
        Static minRectSizeSlider = findSlider("Threshold for rectangle size")
        Dim minRectSize = minRectSizeSlider.value
        Dim displayRect = displayCheck.checked
        For Each c In fuzzy.sortContours
            Dim contours = fuzzy.contours(c.Value.Item0)
            Dim points = New cv.Mat(contours.Length, 1, cv.MatType.CV_32SC2, contours.ToArray)
            Dim center = points.Sum()
            points = New cv.Mat(contours.Length, 2, cv.MatType.CV_32S, contours.ToArray)
            points.Col(0).MinMaxIdx(minX, maxX)
            points.Col(1).MinMaxIdx(minY, maxY)

            Dim rect = New cv.Rect(minX, minY, maxX - minX, maxY - minY)
            If rect.Width * rect.Height > minRectSize Then
                Dim centroid = New cv.Point2f(center.Item(0) / contours.Length, center.Item(1) / contours.Length)
                centroids.Add(centroid)
                rects.Add(rect)
                layoutColor.Add(c.Value.Item1)
                If displayRect Then
                    dst1.Circle(centroid, 6, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                    dst1.Circle(centroid, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                    dst1.Rectangle(rect, cv.Scalar.Yellow, 2)
                End If
            End If
        Next

        label1 = CStr(fuzzy.sortContours.Count) + " regions were found in the image."
    End Sub
End Class







Public Class Fuzzy_TrackerDepthClick
    Inherits VBparent
    Public tracker As Fuzzy_TrackerDepth
    Public highlightPoint As cv.Point
    Public highlightRect As cv.Rect
    Public highlightRegion = -1
    Public regionMask As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        tracker = New Fuzzy_TrackerDepth(ocvb)
        ocvb.desc = "Create centroids and rect's for solid regions and track them - tracker"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        tracker.src = src
        tracker.Run(ocvb)
        dst1 = tracker.dst1

        If standalone And highlightRegion < 0 Then ocvb.trueText("Click any color region to get more details and track it", 10, 50, 3)

        Dim gray = tracker.fuzzy.gray
        If ocvb.mouseClickFlag Then
            highlightPoint = ocvb.mouseClickPoint
            highlightRegion = gray.Get(Of Byte)(highlightPoint.Y, highlightPoint.X)
            ocvb.mouseClickFlag = False
        End If
        If highlightRegion >= 0 Then
            dst2 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            regionMask = gray.InRange(highlightRegion, highlightRegion + 1)
            dst2.SetTo(cv.Scalar.Yellow, regionMask)
        End If
        label1 = CStr(tracker.fuzzy.sortContours.Count) + " regions were found in the image."
    End Sub
End Class








Public Class Fuzzy_PointTracker
    Inherits VBparent
    Dim fuzzy As Fuzzy_Basics
    Dim pTrack As Kalman_PointTracker
    Dim flood As FloodFill_8bit
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fuzzy = New Fuzzy_Basics(ocvb)
        flood = New FloodFill_8bit(ocvb)
        pTrack = New Kalman_PointTracker(ocvb)
        fuzzy.sliders.Visible = False

        ocvb.desc = "FloodFill the regions defined as solid"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        fuzzy.src = src
        fuzzy.Run(ocvb)
        dst2 = fuzzy.dst1

        flood.src = fuzzy.dst1
        flood.Run(ocvb)

        pTrack.queryPoints = flood.basics.centroids
        pTrack.queryRects = flood.basics.rects
        pTrack.queryMasks = flood.basics.masks
        pTrack.Run(ocvb)

        label2 = CStr(pTrack.viewObjects.Count) + " regions were found"
        dst1 = pTrack.dst1
    End Sub
End Class
