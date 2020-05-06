﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices



Module histogram_Functions
    Public Sub histogram2DPlot(histogram As cv.Mat, dst As cv.Mat, aBins As Int32, bBins As Int32)
        Dim maxVal As Double
        histogram.MinMaxLoc(0, maxVal)
        Dim hScale = CInt(Math.Ceiling(dst.Rows / aBins))
        Dim sScale = CInt(Math.Ceiling(dst.Cols / bBins))
        For h = 0 To aBins - 1
            For s = 0 To bBins - 1
                Dim binVal = histogram.Get(of Single)(h, s)
                Dim intensity = Math.Round(binVal * 255 / maxVal)
                Dim pt1 = New cv.Point(s * sScale, h * hScale)
                Dim pt2 = New cv.Point((s + 1) * sScale - 1, (h + 1) * hScale - 1)
                If pt1.X >= dst.Cols Then pt1.X = dst.Cols - 1
                If pt1.Y >= dst.Rows Then pt1.Y = dst.Rows - 1
                If pt2.X >= dst.Cols Then pt2.X = dst.Cols - 1
                If pt2.Y >= dst.Rows Then pt2.Y = dst.Rows - 1
                If pt1.X <> pt2.X And pt1.Y <> pt2.Y Then
                    Dim value = cv.Scalar.All(255 - intensity)
                    value = New cv.Scalar(pt1.X * 255 / dst.Cols, pt1.Y * 255 / dst.Rows, 255 - intensity)
                    dst.Rectangle(pt1, pt2, value, -1, cv.LineTypes.AntiAlias)
                End If
            Next
        Next
    End Sub

    Public Sub Show_HSV_Hist(img As cv.Mat, hist As cv.Mat)
        Dim binCount = hist.Height
        Dim binWidth = img.Width / hist.Height
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)
        img.SetTo(0)
        If maxVal = 0 Then Exit Sub
        For i = 0 To binCount - 2
            Dim h = img.Height * (hist.Get(of Single)(i, 0)) / maxVal
            If h = 0 Then h = 5 ' show the color range in the plot
            cv.Cv2.Rectangle(img, New cv.Rect(i * binWidth + 1, img.Height - h, binWidth - 2, h), New cv.Scalar(CInt(180.0 * i / binCount), 255, 255), -1)
        Next
    End Sub

    Public Sub histogramBars(hist As cv.Mat, dst As cv.Mat, savedMaxVal As Single)
        Dim barWidth = Int(dst.Width / hist.Rows)
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)

        maxVal = Math.Round(maxVal / 1000, 0) * 1000 + 1000

        If maxVal < 0 Then maxVal = savedMaxVal
        If Math.Abs((maxVal - savedMaxVal)) / maxVal < 0.2 Then maxVal = savedMaxVal Else savedMaxVal = Math.Max(maxVal, savedMaxVal)

        dst.SetTo(cv.Scalar.Red)
        If maxVal > 0 And hist.Rows > 0 Then
            Dim incr = CInt(255 / hist.Rows)
            For i = 0 To hist.Rows - 1
                Dim offset = hist.Get(Of Single)(i)
                If Single.IsNaN(offset) Then offset = 0
                Dim h = CInt(offset * dst.Height / maxVal)
                Dim color As cv.Scalar = cv.Scalar.Black
                If hist.Rows <= 255 Then color = cv.Scalar.All((i Mod 255) * incr)
                cv.Cv2.Rectangle(dst, New cv.Rect(i * barWidth, dst.Height - h, barWidth, h), color, -1)
            Next
        End If
    End Sub
End Module




' https://github.com/opencv/opencv/blob/master/samples/python/hist.py
Public Class Histogram_Basics
    Inherits VB_Class
        Public histRaw(2) As cv.Mat
    Public histNormalized(2) As cv.Mat
    Public src As New cv.Mat
    Public bins As Int32 = 50
    Public minRange As Int32 = 0
    Public maxRange As Int32 = 255
    Public externalUse As Boolean
    Public plotRequested As Boolean
    Public plotColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Histogram Bins", 2, 256, 256)
        sliders.setupTrackBar2(ocvb, callerName, "Histogram line thickness", 1, 20, 3)
        sliders.setupTrackBar3(ocvb, callerName,"Histogram Font Size x10", 1, 20, 10)
        
        ocvb.desc = "Plot histograms for up to 3 channels."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then src = ocvb.color
        bins = sliders.TrackBar1.Value

        Dim thickness = sliders.TrackBar2.Value
        Dim dimensions() = New Integer() {bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

        ocvb.result1.SetTo(0)
        Dim lineWidth = ocvb.result1.Cols / bins

        Dim maxVal As Double
        For i = 0 To src.Channels - 1
            Dim hist As New cv.Mat
            cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {i}, New cv.Mat(), hist, 1, dimensions, ranges)
            histRaw(i) = hist.Clone()
            histRaw(i).MinMaxLoc(0, maxVal)
            histNormalized(i) = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)
            If externalUse = False Or plotRequested Then
                Dim points = New List(Of cv.Point)
                Dim listOfPoints = New List(Of List(Of cv.Point))
                For j = 0 To bins - 1
                    points.Add(New cv.Point(CInt(j * lineWidth), ocvb.result1.Rows - ocvb.result1.Rows * histRaw(i).Get(Of Single)(j, 0) / maxVal))
                Next
                listOfPoints.Add(points)
                ocvb.result1.Polylines(listOfPoints, False, plotColors(i), thickness, cv.LineTypes.AntiAlias)
            End If
        Next

        If externalUse = False Or plotRequested Then
            maxVal = Math.Round(maxVal / 1000, 0) * 1000 + 1000 ' smooth things out a little for the scale below
            AddPlotScale(ocvb.result1, 0, maxVal, sliders.TrackBar3.Value / 10)
            ocvb.label1 = "Histogram for Color image above - " + CStr(bins) + " bins"
        End If
    End Sub
    Public Sub MyDispose()
            End Sub
End Class




Public Class Histogram_NormalizeGray
    Inherits VB_Class
            Public histogram As Histogram_KalmanSmoothed
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Min Gray", 0, 255, 0)
        sliders.setupTrackBar2(ocvb, callerName, "Max Gray", 0, 255, 255)
        
        histogram = New Histogram_KalmanSmoothed(ocvb, "Histogram_NormalizeGray")
        histogram.externalUse = True

        check.Setup(ocvb, callerName,  1)
        check.Box(0).Text = "Normalize Before Histogram"
        check.Box(0).Checked = True
                ocvb.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        histogram.gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If check.Box(0).Checked Then
            cv.Cv2.Normalize(histogram.gray, histogram.gray, sliders.TrackBar1.Value, sliders.TrackBar2.Value, cv.NormTypes.MinMax) ' only minMax is working...
        End If
        histogram.Run(ocvb)
    End Sub
    Public Sub MyDispose()
                histogram.Dispose()
        check.Dispose()
    End Sub
End Class





Public Class Histogram_EqualizeColor
    Inherits VB_Class
    Dim kalman As Histogram_KalmanSmoothed
    Dim mats As Mat_2to1
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        kalman = New Histogram_KalmanSmoothed(ocvb, "Histogram_EqualizeColor")
        kalman.externalUse = True
        kalman.sliders.TrackBar1.Value = 40

        mats = New Mat_2to1(ocvb, "Histogram_EqualizeColor")
        mats.externalUse = True

        ocvb.desc = "Create an equalized histogram of the color image.  Histogram differences are very subtle but image is noticeably enhanced."
        ocvb.label1 = "Image Enhanced with Equalized Histogram"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rgb(2) As cv.Mat
        Dim rgbEq(2) As cv.Mat
        cv.Cv2.Split(ocvb.color, rgb)

        For i = 0 To rgb.Count - 1
            rgbEq(i) = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
            cv.Cv2.EqualizeHist(rgb(i), rgbEq(i))
        Next

        If externalUse = False Then
            kalman.gray = rgb(0).Clone() ' just show the green plane
            kalman.dst = mats.mat(0)
            kalman.plotHist.backColor = cv.Scalar.Green
            kalman.Run(ocvb)

            kalman.gray = rgbEq(0).Clone()
            kalman.dst = mats.mat(1)
            kalman.Run(ocvb)

            mats.Run(ocvb)
            ocvb.label2 = "Before (top) and After Green Histograms"

            cv.Cv2.Merge(rgbEq, ocvb.result1)
        End If
    End Sub
    Public Sub MyDispose()
        kalman.Dispose()
        mats.Dispose()
    End Sub
End Class




Public Class Histogram_EqualizeGray
    Inherits VB_Class
    Public histogram As Histogram_KalmanSmoothed
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        histogram = New Histogram_KalmanSmoothed(ocvb, "Histogram_EqualizeGray")
        ocvb.desc = "Create an equalized histogram of the grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then histogram.gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.EqualizeHist(histogram.gray, histogram.gray)
        histogram.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        histogram.Dispose()
    End Sub
End Class





' https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html
Public Class Histogram_2D_HueSaturation
    Inherits VB_Class
    Public histogram As New cv.Mat
    Public dst As New cv.Mat
    Public src As New cv.Mat
    Public hsv As cv.Mat

        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Hue bins", 1, 180, 30) ' quantize hue to 30 levels
        sliders.setupTrackBar2(ocvb, callerName, "Saturation bins", 1, 256, 32) ' quantize sat to 32 levels
                ocvb.desc = "Create a histogram for hue and saturation."
        dst = ocvb.result1
        src = ocvb.color
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hsv = src.CvtColor(cv.ColorConversionCodes.RGB2HSV)
        Dim sbins = sliders.TrackBar1.Value
        Dim hbins = sliders.TrackBar2.Value
        Dim histSize() = {hbins, sbins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, sliders.TrackBar1.Maximum - 1), New cv.Rangef(0, sliders.TrackBar2.Maximum - 1)} ' hue ranges from 0-179

        cv.Cv2.CalcHist(New cv.Mat() {hsv}, New Integer() {0, 1}, New cv.Mat(), histogram, 2, histSize, ranges)

        histogram2DPlot(histogram, dst, hbins, sbins)
    End Sub
    Public Sub MyDispose()
            End Sub
End Class




Public Class Histogram_2D_XZ_YZ
    Inherits VB_Class
    Dim xyDepth As Mat_ImageXYZ_MT
    Dim mats As Mat_4to1
    Dim trim As Depth_InRange
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        xyDepth = New Mat_ImageXYZ_MT(ocvb, "Histogram_2D_XZ_YZ")

        mats = New Mat_4to1(ocvb, "Histogram_2D_XZ_YZ")
        mats.externalUse = True

        trim = New Depth_InRange(ocvb, "Histogram_2D_XZ_YZ")
        trim.sliders.TrackBar2.Value = 1500 ' up to x meters away 

        sliders.setupTrackBar1(ocvb, callerName, "Histogram X bins", 1, ocvb.color.Width / 2, 30)
        sliders.setupTrackBar2(ocvb, callerName, "Histogram Z bins", 1, 200, 100)
        
        ocvb.desc = "Create a 2D histogram for depth in XZ and YZ."
        ocvb.label2 = "Left is XZ (Top View) and Right is YZ (Side View)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        xyDepth.Run(ocvb) ' get xyDepth coordinates - note: in image coordinates not physical coordinates.
        Dim xbins = sliders.TrackBar1.Value
        Dim zbins = sliders.TrackBar2.Value
        Dim histSize() = {xbins, zbins}
        trim.Run(ocvb)
        Dim minRange = trim.sliders.TrackBar1.Value
        Dim maxRange = trim.sliders.TrackBar2.Value

        Dim histogram As New cv.Mat

        Dim rangesX() = New cv.Rangef() {New cv.Rangef(0, ocvb.color.Width - 1), New cv.Rangef(minRange, maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {xyDepth.xyDepth}, New Integer() {2, 0}, New cv.Mat(), histogram, 2, histSize, rangesX)
        histogram2DPlot(histogram, ocvb.result2, xbins, zbins)
        mats.mat(2) = ocvb.result2.Clone()

        Dim rangesY() = New cv.Rangef() {New cv.Rangef(0, ocvb.color.Height - 1), New cv.Rangef(minRange, maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {xyDepth.xyDepth}, New Integer() {1, 2}, New cv.Mat(), histogram, 2, histSize, rangesY)
        histogram2DPlot(histogram, ocvb.result2, xbins, zbins)
        mats.mat(3) = ocvb.result2.Clone()
        mats.Run(ocvb)
    End Sub
    Public Sub MyDispose()
                trim.Dispose()
        xyDepth.Dispose()
        mats.Dispose()
    End Sub
End Class





Public Class Histogram_BackProjectionGrayScale
    Inherits VB_Class
        Dim hist As Histogram_KalmanSmoothed
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        hist = New Histogram_KalmanSmoothed(ocvb, "Histogram_BackProjectionGrayScale")
        hist.externalUse = True
        hist.dst = ocvb.result2
        hist.kalman.check.Box(0).Checked = False

        sliders.setupTrackBar1(ocvb, callerName, "Histogram Bins", 1, 255, 50)
        sliders.setupTrackBar2(ocvb, callerName, "Number of neighbors to include", 0, 10, 1)
        
        ocvb.desc = "Create a histogram and back project into the image the grayscale color with the highest occurance."
        ocvb.label2 = "Grayscale Histogram"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.sliders.TrackBar1.Value = sliders.TrackBar1.Value ' reflect the number of bins into the histogram code.

        hist.gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        hist.Run(ocvb)

        Dim minVal As Single, maxVal As Single
        Dim minIdx(2) As Int32, maxIdx(2) As Int32
        hist.histogram.MinMaxIdx(minVal, maxVal, minIdx, maxIdx)
        Dim pixelMin = CInt(255 * maxIdx(0) / hist.sliders.TrackBar1.Value)
        Dim pixelMax = CInt(255 * (maxIdx(0) + 1) / hist.sliders.TrackBar1.Value)
        Dim incr = pixelMax - pixelMin
        Dim neighbors = sliders.TrackBar2.Value
        If neighbors Mod 2 = 0 Then
            pixelMin -= incr * neighbors / 2
            pixelMax += incr * neighbors / 2
        Else
            pixelMin -= incr * ((neighbors / 2) + 1)
            pixelMax += incr * ((neighbors - 1) / 2)
        End If
        pixelMin -= incr
        pixelMax += incr
        Dim mask = hist.gray.InRange(pixelMin, pixelMax)
        ocvb.result1.SetTo(0)
        ocvb.color.CopyTo(ocvb.result1, mask)
        ocvb.label1 = "BackProjection of most frequent pixel + " + CStr(neighbors) + " neighbor" + If(neighbors <> 1, "s", "")
    End Sub
    Public Sub MyDispose()
        hist.Dispose()
            End Sub
End Class



' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class Histogram_BackProjection
    Inherits VB_Class
        Dim hist As Histogram_2D_HueSaturation
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Backprojection Mask Threshold", 0, 255, 10)
        
        hist = New Histogram_2D_HueSaturation(ocvb, "Histogram_BackProjection")
        hist.dst = ocvb.result2

        ocvb.desc = "Backproject from a hue and saturation histogram."
        ocvb.label1 = "Backprojection of detected hue and saturation."
        ocvb.label2 = "2D Histogram for Hue (X) vs. Saturation (Y)"

        ocvb.drawRect = New cv.Rect(100, 100, 200, 100)  ' an arbitrary rectangle to use for the backprojection.
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.src = ocvb.color(ocvb.drawRect)
        hist.Run(ocvb)
        Dim histogram = hist.histogram.Normalize(0, 255, cv.NormTypes.MinMax)
        Dim bins() = {0, 1}
        Dim hsv = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim mat() As cv.Mat = {hsv}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(0, 256)}
        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject(mat, bins, histogram, mask, ranges)

        mask = mask.Threshold(sliders.TrackBar1.Value, 255, cv.ThresholdTypes.Binary)
        ocvb.result1.SetTo(0)
        ocvb.color.CopyTo(ocvb.result1, mask)
    End Sub
    Public Sub MyDispose()
        hist.Dispose()
            End Sub
End Class




Public Class Histogram_ColorsAndGray
    Inherits VB_Class
            Dim histogram As Histogram_KalmanSmoothed
    Dim mats As Mat_4to1
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        mats = New Mat_4to1(ocvb, "Histogram_ColorsAndGray")
        mats.externalUse = True

        sliders.setupTrackBar1(ocvb, callerName, "Min Gray", 0, 255, 0)
        sliders.setupTrackBar2(ocvb, callerName, "Max Gray", 0, 255, 255)
        
        histogram = New Histogram_KalmanSmoothed(ocvb, "Histogram_ColorsAndGray")
        histogram.externalUse = True
        histogram.kalman.check.Box(0).Checked = False
        histogram.kalman.check.Box(0).Enabled = False ' if we use Kalman, all the plots are identical as the values converge on the gray level setting...
        histogram.sliders.TrackBar1.Value = 40

        check.Setup(ocvb, callerName,  1)
        check.Box(0).Text = "Normalize Before Histogram"
        check.Box(0).Checked = True
                ocvb.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim split = ocvb.color.Split()
        ReDim Preserve split(3)
        split(3) = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        histogram.gray = New cv.Mat
        For i = 0 To split.Length - 1
            If check.Box(0).Checked Then
                cv.Cv2.Normalize(split(i), histogram.gray, sliders.TrackBar1.Value, sliders.TrackBar2.Value, cv.NormTypes.MinMax) ' only minMax is working...
            Else
                histogram.gray = split(i).Clone()
            End If
            histogram.plotHist.backColor = Choose(i + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.PowderBlue)
            histogram.Run(ocvb)
            mats.mat(i) = histogram.dst.Clone()
        Next

        mats.Run(ocvb)
    End Sub
    Public Sub MyDispose()
                histogram.Dispose()
        check.Dispose()
        mats.Dispose()
    End Sub
End Class




Public Class Histogram_KalmanSmoothed
    Inherits VB_Class
    Public gray As cv.Mat
    Public mask As New cv.Mat
    Public dst As New cv.Mat
    Public externalUse As Boolean

        Public histogram As New cv.Mat
    Public kalman As Kalman_Basics
    Public plotHist As Plot_Histogram
    Dim splitColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        dst = ocvb.result2
        plotHist = New Plot_Histogram(ocvb, "Histogram_KalmanSmoothed")
        plotHist.externalUse = True

        kalman = New Kalman_Basics(ocvb, "Histogram_KalmanSmoothed")
        kalman.externalUse = True

        sliders.setupTrackBar1(ocvb, callerName, "Histogram Bins", 1, 255, 50)
        
        ocvb.label1 = "Gray scale input to histogram"
        ocvb.label2 = "Histogram - x=bins/y=count"
        ocvb.desc = "Create a histogram of the grayscale image and smooth the bar chart with a kalman filter."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static splitIndex As Int32 = -1
        If externalUse = False Then
            Dim split() = cv.Cv2.Split(ocvb.color)
            If ocvb.frameCount Mod 500 = 0 Then
                splitIndex += 1
                If splitIndex > 2 Then splitIndex = 0
            End If
            gray = split(splitIndex).Clone
        End If
        plotHist.bins = sliders.TrackBar1.Value
        plotHist.minRange = 0
        Dim histSize() = {plotHist.bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}

        Dim dimensions() = New Integer() {plotHist.bins}
        cv.Cv2.CalcHist(New cv.Mat() {gray}, New Integer() {0}, mask, histogram, 1, dimensions, ranges)

        ocvb.result1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.label2 = "Plot Histogram bins = " + CStr(plotHist.bins)

        ReDim kalman.src(plotHist.bins - 1)
        For i = 0 To plotHist.bins - 1
            kalman.src(i) = histogram.Get(Of Single)(i, 0)
        Next
        kalman.Run(ocvb)
        For i = 0 To plotHist.bins - 1
            histogram.Set(Of Single)(i, 0, kalman.dst(i))
        Next

        plotHist.hist = histogram
        plotHist.dst = dst
        If externalUse = False Then plotHist.backColor = splitColors(splitIndex)
        plotHist.Run(ocvb)
    End Sub
    Public Sub MyDispose()
                kalman.Dispose()
        plotHist.Dispose()
    End Sub
End Class




Public Class Histogram_Depth
    Inherits VB_Class
    Public trim As Depth_InRange
        Public plotHist As Plot_Histogram
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        plotHist = New Plot_Histogram(ocvb, "Histogram_Depth")
        plotHist.externalUse = True

        trim = New Depth_InRange(ocvb, "Histogram_Depth")
        sliders.setupTrackBar1(ocvb, callerName, "Histogram Depth Bins", 2, ocvb.color.Width, 50) ' max is the number of columns * 2 
        
        ocvb.desc = "Show depth data as a histogram."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        plotHist.minRange = trim.sliders.TrackBar1.Value
        plotHist.maxRange = trim.sliders.TrackBar2.Value
        plotHist.bins = sliders.TrackBar1.Value
        Dim histSize() = {plotHist.bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}

        cv.Cv2.CalcHist(New cv.Mat() {getDepth32f(ocvb)}, New Integer() {0}, New cv.Mat, plotHist.hist, 1, histSize, ranges)

        If externalUse = False Then
            plotHist.dst = ocvb.result2
            plotHist.Run(ocvb)
        End If
    End Sub
    Public Sub MyDispose()
                trim.Dispose()
        plotHist.Dispose()
    End Sub
End Class




Public Class Histogram_DepthValleys
    Inherits VB_Class
    Dim kalman As Kalman_Basics
    Dim hist As Histogram_Depth
    Public rangeBoundaries As New List(Of cv.Point)
    Public sortedSizes As New List(Of Int32)
    Private Class CompareCounts : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just int32?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return -1
            Return 1
        End Function
    End Class
    Private Sub histogramBarsValleys(img As cv.Mat, hist As cv.Mat, plotColors() As cv.Scalar)
        Dim binCount = hist.Height
        Dim binWidth = CInt(img.Width / hist.Height)
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)
        img.SetTo(0)
        If maxVal = 0 Then Exit Sub
        For i = 0 To binCount - 1
            Dim nextHistCount = hist.Get(Of Single)(i, 0)
            Dim h = CInt(img.Height * nextHistCount / maxVal)
            If h = 0 Then h = 1 ' show the color range in the plot
            Dim barRect As cv.Rect
            barRect = New cv.Rect(i * binWidth, img.Height - h, binWidth, h)
            cv.Cv2.Rectangle(img, barRect, plotColors(i), -1)
        Next
    End Sub
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        hist = New Histogram_Depth(ocvb, "Histogram_DepthValleys")
        hist.trim.sliders.TrackBar2.Value = 5000 ' depth to 5 meters.
        hist.sliders.TrackBar1.Value = 40 ' number of bins.

        kalman = New Kalman_Basics(ocvb, "Histogram_DepthValleys")
        kalman.externalUse = True

        ocvb.desc = "Identify valleys in the Depth histogram."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.Run(ocvb)
        ReDim kalman.src(hist.plotHist.hist.Rows - 1)
        For i = 0 To hist.plotHist.hist.Rows - 1
            kalman.src(i) = hist.plotHist.hist.Get(Of Single)(i, 0)
        Next
        kalman.Run(ocvb)
        For i = 0 To hist.plotHist.hist.Rows - 1
            hist.plotHist.hist.Set(Of Single)(i, 0, kalman.dst(i))
        Next

        Dim depthIncr = CInt(hist.trim.sliders.TrackBar2.Value / hist.sliders.TrackBar1.Value) ' each bar represents this number of millimeters
        Dim pointCount = hist.plotHist.hist.Get(Of Single)(0, 0) + hist.plotHist.hist.Get(Of Single)(1, 0)
        Dim startDepth = 1
        Dim startEndDepth As cv.Point
        Dim depthBoundaries As New SortedList(Of Single, cv.Point)(New CompareCounts)
        For i = 0 To kalman.dst.Length - 1
            Dim prev2 = If(i > 2, kalman.dst(i - 2), 0)
            Dim prev = If(i > 1, kalman.dst(i - 1), 0)
            Dim curr = kalman.dst(i)
            Dim post = If(i < kalman.dst.Length - 1, kalman.dst(i + 1), 0)
            Dim post2 = If(i < kalman.dst.Length - 2, kalman.dst(i + 2), 0)
            pointCount += kalman.dst(i)
            If curr < (prev + prev2) / 2 And curr < (post + post2) / 2 And i * depthIncr > startDepth + depthIncr Then
                startEndDepth = New cv.Point(startDepth, i * depthIncr)
                depthBoundaries.Add(pointCount, startEndDepth)
                pointCount = 0
                startDepth = i * depthIncr + 0.1
            End If
        Next

        startEndDepth = New cv.Point(startDepth, hist.trim.sliders.TrackBar2.Value)
        depthBoundaries.Add(pointCount, startEndDepth) ' capped at the max depth we are observing

        rangeBoundaries.Clear()
        sortedSizes.Clear()
        For i = depthBoundaries.Count - 1 To 0 Step -1
            rangeBoundaries.Add(depthBoundaries.ElementAt(i).Value)
            sortedSizes.Add(depthBoundaries.ElementAt(i).Key)
        Next

        Dim plotColors(hist.plotHist.hist.Rows - 1) As cv.Scalar
        For i = 0 To hist.plotHist.hist.Rows - 1
            Dim depth = i * depthIncr + 1
            For j = 0 To rangeBoundaries.Count - 1
                Dim startEnd = rangeBoundaries.ElementAt(j)
                If depth >= startEnd.X And depth < startEnd.Y Then
                    plotColors(i) = ocvb.colorScalar(j Mod 255)
                    Exit For
                End If
            Next
        Next
        histogramBarsValleys(ocvb.result1, hist.plotHist.hist, plotColors)
        ocvb.label1 = "Histogram clustered by valleys and smoothed"
        ocvb.label2 = "Original (unsmoothed) histogram"
    End Sub
    Public Sub MyDispose()
        hist.Dispose()
        kalman.Dispose()
    End Sub
End Class





Public Class Histogram_DepthClusters
    Inherits VB_Class
    Public valleys As Histogram_DepthValleys
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        valleys = New Histogram_DepthValleys(ocvb, "Histogram_DepthClusters")
        ocvb.desc = "Color each of the Depth Clusters found with Histogram_DepthValleys - stabilized with Kalman."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        valleys.Run(ocvb)

        ocvb.result2.SetTo(0)
        Dim mask As New cv.Mat
        Dim tmp As New cv.Mat
        For i = 0 To valleys.rangeBoundaries.Count - 1
            Dim startEndDepth = valleys.rangeBoundaries.ElementAt(i)
            cv.Cv2.InRange(getDepth32f(ocvb), startEndDepth.X, startEndDepth.Y, tmp)
            cv.Cv2.ConvertScaleAbs(tmp, mask)
            If externalUse = False Then ocvb.result2.SetTo(ocvb.colorScalar(i), mask)
        Next
        ocvb.label1 = "Histogram of " + CStr(valleys.rangeBoundaries.Count) + " Depth Clusters"
        ocvb.label2 = "Backprojection of " + CStr(valleys.rangeBoundaries.Count) + " histogram clusters"
    End Sub
    Public Sub MyDispose()
        valleys.Dispose()
    End Sub
End Class