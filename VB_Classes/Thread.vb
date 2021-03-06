Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Collections.Concurrent

Public Class Thread_Grid
    Inherits VBparent
    Public roiList As List(Of cv.Rect)
    Public borderList As List(Of cv.Rect)
    Public gridMask As cv.Mat
    Public tilesPerRow As Integer
    Public tilesPerCol As Integer
    Public mouseClickROI As Integer
    Dim incompleteRegions As Integer
    Dim gridToRoi As New cv.Mat
    Private Sub drawGrid()
        For i = 0 To roiList.Count - 1
            Dim roi = roiList(i)
            Dim p1 = New cv.Point(roi.X + roi.Width, roi.Y)
            Dim p2 = New cv.Point(roi.X + roi.Width, roi.Y + roi.Height)
            If roi.X + roi.Width <= gridMask.Width Then
                gridMask.Line(p1, p2, cv.Scalar.White, 1)
            End If
            If roi.Y + roi.Height <= gridMask.Height Then
                Dim p3 = New cv.Point(roi.X, roi.Y + roi.Height)
                gridMask.Line(p2, p3, cv.Scalar.White, 1)
            End If
            gridToRoi.Rectangle(roi, i, -1)
        Next
    End Sub
    Public Sub New()
        initParent()
        src = task.color
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "ThreadGrid Width", 2, src.Width, 32)
            sliders.setupTrackBar(1, "ThreadGrid Height", 2, src.Height, 32)
            sliders.setupTrackBar(2, "ThreadGrid Border", 0, 20, 0)
        End If
        roiList = New List(Of cv.Rect)
        borderList = New List(Of cv.Rect)
        gridMask = New cv.Mat(src.Size(), cv.MatType.CV_8U)
        gridToRoi = New cv.Mat(src.Size(), cv.MatType.CV_32S)
        task.desc = "Create a grid for use with parallel.ForEach."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static lastWidth As Integer
        Static lastHeight As Integer
        Static lastBorder As Integer

        Static widthSlider = findSlider("ThreadGrid Width")
        Static heightSlider = findSlider("ThreadGrid Height")
        Static borderSlider = findSlider("ThreadGrid Border")

        If task.mouseClickFlag Then mouseClickROI = gridToRoi.Get(Of Integer)(task.mouseClickPoint.Y, task.mouseClickPoint.X)
        Dim borderSize = borderSlider.Value
        Dim gWidth = widthSlider.Value
        Dim gHeight = heightSlider.value
        If lastWidth <> gWidth Or lastHeight <> gHeight Or lastBorder <> borderSize Then
            roiList.Clear()
            borderList.Clear()

            gridMask.SetTo(0)
            incompleteRegions = 0
            For y = 0 To src.Height - 1 Step gHeight
                For x = 0 To src.Width - 1 Step gWidth
                    Dim roi = New cv.Rect(x, y, gWidth, gHeight)
                    If x + roi.Width >= src.Width Then roi.Width = src.Width - x
                    If y + roi.Height >= src.Height Then roi.Height = src.Height - y
                    If roi.Width > 0 And roi.Height > 0 Then
                        If y = 0 Then tilesPerRow += 1
                        If x = 0 Then tilesPerCol += 1
                        roiList.Add(roi)
                        If roi.Width <> gWidth Or roi.Height <> gHeight Then incompleteRegions += 1
                    End If
                Next
                drawGrid()
            Next

            For Each roi In roiList
                Dim broi = New cv.Rect(roi.X - borderSize, roi.Y - borderSize, roi.Width + 2 * borderSize, roi.Height + 2 * borderSize)
                If broi.X < 0 Then
                    broi.Width += broi.X
                    broi.X = 0
                End If
                If broi.Y < 0 Then
                    broi.Height += broi.Y
                    broi.Y = 0
                End If
                If broi.Width + broi.X > src.Width Then
                    broi.Width = src.Width - broi.X
                End If
                If broi.Height + broi.Y > src.Height Then
                    broi.Height = src.Height - broi.Y
                End If
                borderList.Add(broi)
            Next

            If standalone Or task.intermediateReview = caller Then drawGrid()

            lastWidth = gWidth
            lastHeight = gHeight
            lastBorder = borderSize
        End If

        If standalone Or task.intermediateReview = caller Then
            src.CopyTo(dst1)
            dst1.SetTo(cv.Scalar.All(255), gridMask)
            label1 = "Thread_Grid " + CStr(roiList.Count - incompleteRegions) + " (" + CStr(tilesPerRow) + "X" + CStr(tilesPerCol) + ") " +
                          CStr(roiList(0).Width) + "X" + CStr(roiList(0).Height) + " regions"
        End If
    End Sub
End Class






Public Class Thread_GridTest
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 64
        gridHeightSlider.Value = 40
        label1 = ""
        task.desc = "Validation test for thread_grid algorithm"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()
        Dim mean = cv.Cv2.Mean(src)

        Parallel.For(0, grid.roiList.Count,
         Sub(i)
             Dim roi = grid.roiList(i)
             cv.Cv2.Subtract(mean, src(roi), dst1(roi))
             cv.Cv2.PutText(dst1(roi), CStr(i), New cv.Point(10, 20), cv.HersheyFonts.HersheyDuplex, 0.7, cv.Scalar.White, 1)
         End Sub)
        dst1.SetTo(cv.Scalar.White, grid.gridMask)

        dst2.SetTo(0)
        Parallel.For(0, grid.roiList.Count,
         Sub(i)
             Dim roi = grid.roiList(i)
             cv.Cv2.Subtract(mean, src(roi), dst2(roi))
             dst2(roi).Line(New cv.Point(0, 0), New cv.Point(roi.Width, roi.Height), cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
         End Sub)
    End Sub
End Class
