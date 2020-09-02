Imports cv = OpenCvSharp
' https://docs.opencv.org/3.4/js_contour_features_fitLine.html
Public Class Fitline_Basics
    Inherits ocvbClass
    Public draw As Draw_Line
    Public lines As New List(Of cv.Point) ' there are always an even number - 2 points define the line.
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        draw = New Draw_Line(ocvb)
        draw.sliders.trackbar(0).Value = 2

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Accuracy for the radius X100", 0, 100, 10)
        sliders.setupTrackBar(1, "Accuracy for the angle X100", 0, 100, 10)

        desc = "Show how Fitline API works.  When the lines overlap the image has a single contour and the lines are occasionally not found."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            draw.Run(ocvb)
            src = draw.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(254, 255, cv.ThresholdTypes.BinaryInv)
            dst2 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst1 = dst2
        Else
            If draw.sliders.Visible Then draw.sliders.Visible = False
            lines.Clear()
        End If

        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(src, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim radiusAccuracy = sliders.trackbar(0).Value / 100
        Dim angleAccuracy = sliders.trackbar(1).Value / 100
        For i = 0 To contours.Length - 1
            Dim cnt = contours(i)
            Dim line2d = cv.Cv2.FitLine(cnt, cv.DistanceTypes.L2, 0, radiusAccuracy, angleAccuracy)
            Dim slope = line2d.Vy / line2d.Vx
            Dim leftY = Math.Round(-line2d.X1 * slope + line2d.Y1)
            Dim rightY = Math.Round((src.Cols - line2d.X1) * slope + line2d.Y1)
            Dim p1 = New cv.Point(0, leftY)
            Dim p2 = New cv.Point(src.Cols - 1, rightY)
            If standalone Then
                lines.Add(p1)
                lines.Add(p2)
            End If
            dst1.Line(p1, p2, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class



Public Class Fitline_3DBasics_MT
    Inherits ocvbClass
    Dim hlines As Hough_Lines_MT
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        hlines = New Hough_Lines_MT(ocvb)
        desc = "Use visual lines to find 3D lines."
        label2 = "White is featureless RGB, blue depth shadow"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hlines.src = src
        hlines.Run(ocvb)
        dst2 = hlines.dst2
        Dim mask = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        src.CopyTo(dst1)
        Dim depth32f = getDepth32f(ocvb)

        Dim lines As New List(Of cv.Line3D)
        Dim nullLine = New cv.Line3D(0, 0, 0, 0, 0, 0)
        Parallel.ForEach(Of cv.Rect)(hlines.grid.roiList,
        Sub(roi)
            Dim depth = depth32f(roi)
            Dim fMask = mask(roi)
            Dim points As New List(Of cv.Point3f)
            Dim rows = src.Rows, cols = src.Cols
            For y = 0 To roi.Height - 1
                For x = 0 To roi.Width - 1
                    If fMask.Get(Of Byte)(y, x) > 0 Then
                        Dim d = depth.Get(Of Single)(y, x)
                        If d > 0 And d < 10000 Then
                            points.Add(New cv.Point3f(x / rows, y / cols, d / 10000))
                        End If
                    End If
                Next
            Next
            Dim line = nullLine
            If points.Count = 0 Then
                ' save the average color for this roi
                Dim mean = ocvb.RGBDepth(roi).Mean()
                mean(0) = 255 - mean(0)
                dst2.Rectangle(roi, mean, -1, cv.LineTypes.AntiAlias)
            Else
                Dim fitArray = points.ToArray()
                line = cv.Cv2.FitLine(fitArray, cv.DistanceTypes.L2, 0, 0, 0.01)
            End If
            SyncLock lines
                lines.Add(line)
            End SyncLock
        End Sub)
        ' putting this in the parallel for above causes a memory leak - could not find it...
        For i = 0 To hlines.grid.roiList.Count - 1
            houghShowLines3D(dst1(hlines.grid.roiList(i)), lines.ElementAt(i))
        Next
        dst1.SetTo(cv.Scalar.White, hlines.grid.gridMask)
    End Sub
End Class



Public Class Fitline_RawInput
    Inherits ocvbClass
    Public points As New List(Of cv.Point2f)
    Public m As Single
    Public bb As Single
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Random point count", 0, 500, 100)
        sliders.setupTrackBar(1, "Line Point Count", 0, 500, 20)
        sliders.setupTrackBar(2, "Line Noise", 1, 100, 10)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Highlight Line Data"
        check.Box(1).Text = "Recompute with new random data"
        check.Box(0).Checked = True
        check.Box(1).Checked = True

        desc = "Generate a noisy line in a field of random data."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If check.Box(1).Checked Or ocvb.frameCount = 0 Then
            If ocvb.testAllRunning = False Then check.Box(1).Checked = False
            dst1.SetTo(0)
            Dim dotSize = 2
            Dim width = ocvb.color.Width
            Dim height = ocvb.color.Height

            points.Clear()
            For i = 0 To sliders.trackbar(0).Value - 1
                Dim pt = New cv.Point2f(Rnd() * width, Rnd() * height)
                If pt.X < 0 Then pt.X = 0
                If pt.X > width Then pt.X = width
                If pt.Y < 0 Then pt.Y = 0
                If pt.Y > height Then pt.Y = height
                points.Add(pt)
                dst1.Circle(points(i), dotSize, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
            Next

            Dim p1 As cv.Point2f, p2 As cv.Point2f
            If Rnd() * 2 - 1 >= 0 Then
                p1 = New cv.Point(Rnd() * width, 0)
                p2 = New cv.Point(Rnd() * width, height)
            Else
                p1 = New cv.Point(0, Rnd() * height)
                p2 = New cv.Point(width, Rnd() * height)
            End If

            If p1.X = p2.X Then p1.X += 1
            If p1.Y = p2.Y Then p1.Y += 1
            m = (p2.Y - p1.Y) / (p2.X - p1.X)
            bb = p2.Y - p2.X * m
            Dim startx = Math.Min(p1.X, p2.X)
            Dim incr = (Math.Max(p1.X, p2.X) - startx) / sliders.trackbar(1).Value
            Dim highLight = cv.Scalar.White
            If check.Box(0).Checked Then
                highLight = cv.Scalar.Gray
                dotSize = 5
            End If
            For i = 0 To sliders.trackbar(1).Value - 1
                Dim noiseOffsetX = (Rnd() * 2 - 1) * sliders.trackbar(2).Value
                Dim noiseOffsetY = (Rnd() * 2 - 1) * sliders.trackbar(2).Value
                Dim pt = New cv.Point(startx + i * incr + noiseOffsetX, Math.Max(0, Math.Min(m * (startx + i * incr) + bb + noiseOffsetY, height)))
                If pt.X < 0 Then pt.X = 0
                If pt.X > width Then pt.X = width
                If pt.Y < 0 Then pt.Y = 0
                If pt.Y > height Then pt.Y = height
                points.Add(pt)
                dst1.Circle(pt, dotSize, highLight, -1, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
End Class




' http://www.cs.cmu.edu/~youngwoo/doc/lineFittingTest.cpp
Public Class Fitline_EigenFit
    Inherits ocvbClass
    Dim noisyLine As Fitline_RawInput
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        noisyLine = New Fitline_RawInput(ocvb)
        noisyLine.sliders.trackbar(0).Value = 30
        noisyLine.sliders.trackbar(1).Value = 400
        label1 = "blue=GT, red=fitline, yellow=EigenFit"
        label2 = "Raw input (use sliders below to explore)"
        desc = "Remove outliers when trying to fit a line.  Fitline and the Eigen computation below produce the same result."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static eigenVec As New cv.Mat(2, 2, cv.MatType.CV_32F, 0), eigenVal As New cv.Mat(2, 2, cv.MatType.CV_32F, 0)
        Static theta As Single
        Static len As Single
        Static m2 As Single
        If ocvb.frameCount Mod 30 = 0 Then

            Static noisePointCount As Int32
            Static linePointCount As Int32
            Static lineNoise As Int32
            Static highlight As Boolean
            'If noisyLine.sliders.trackbar(0).Value <> noisePointCount Or noisyLine.sliders.trackbar(1).Value <> linePointCount Or
            '    noisyLine.sliders.trackbar(2).Value <> lineNoise Or noisyLine.check.Box(0).Checked <> highlight Or noisyLine.check.Box(1).Checked Then
            noisyLine.check.Box(1).Checked = True
            noisyLine.Run(ocvb)
            dst2 = noisyLine.dst1
            dst1.SetTo(0)
            noisyLine.check.Box(1).Checked = False
            'End If

            noisePointCount = noisyLine.sliders.trackbar(0).Value
            linePointCount = noisyLine.sliders.trackbar(1).Value
            lineNoise = noisyLine.sliders.trackbar(2).Value
            highlight = noisyLine.check.Box(0).Checked

            Dim width = src.Width

            Dim line = cv.Cv2.FitLine(noisyLine.points, cv.DistanceTypes.L2, 1, 0.01, 0.01)
            Dim m = line.Vy / line.Vx
            Dim bb = line.Y1 - m * line.X1
            Dim p1 = New cv.Point(0, bb)
            Dim p2 = New cv.Point(width, m * width + bb)
            dst1.Line(p1, p2, cv.Scalar.Red, 20, cv.LineTypes.AntiAlias)

            Dim pointMat = New cv.Mat(noisyLine.points.Count, 1, cv.MatType.CV_32FC2, noisyLine.points.ToArray)
            Dim mean = pointMat.Mean()
            Dim split() = pointMat.Split()
            Dim minX As Single, maxX As Single, minY As Single, maxY As Single
            split(0).MinMaxLoc(minX, maxX)
            split(1).MinMaxLoc(minY, maxY)


            Dim eigenInput As New cv.Vec4f
            For i = 0 To noisyLine.points.Count - 1
                Dim pt = noisyLine.points.Item(i)
                Dim x = pt.X - mean.Val0
                Dim y = pt.Y - mean.Val1
                eigenInput.Item0 += x * x
                eigenInput.Item1 += x * y
                eigenInput.Item3 += y * y
            Next
            eigenInput.Item2 = eigenInput.Item1

            Dim vec4f As New List(Of cv.Point2f)
            vec4f.Add(New cv.Point2f(eigenInput.Item0, eigenInput.Item1))
            vec4f.Add(New cv.Point2f(eigenInput.Item1, eigenInput.Item3))

            Dim D = New cv.Mat(2, 2, cv.MatType.CV_32FC1, vec4f.ToArray)
            cv.Cv2.Eigen(D, eigenVal, eigenVec)
            theta = Math.Atan2(eigenVec.Get(Of Single)(1, 0), eigenVec.Get(Of Single)(0, 0))

            len = Math.Sqrt(Math.Pow(maxX - minX, 2) + Math.Pow(maxY - minY, 2))

            p1 = New cv.Point2f(mean.Val0 - Math.Cos(theta) * Len / 2, mean.Val1 - Math.Sin(theta) * Len / 2)
            p2 = New cv.Point2f(mean.Val0 + Math.Cos(theta) * Len / 2, mean.Val1 + Math.Sin(theta) * Len / 2)
            m2 = (p2.Y - p1.Y) / (p2.X - p1.X)

            If Math.Abs(m2) > 1.0 Then
                dst1.Line(p1, p2, cv.Scalar.Yellow, 10, cv.LineTypes.AntiAlias)
            Else
                p1 = New cv.Point2f(mean.Val0 - Math.Cos(-theta) * Len / 2, mean.Val1 - Math.Sin(-theta) * Len / 2)
                p2 = New cv.Point2f(mean.Val0 + Math.Cos(-theta) * Len / 2, mean.Val1 + Math.Sin(-theta) * Len / 2)
                m2 = (p2.Y - p1.Y) / (p2.X - p1.X)
                dst1.Line(p1, p2, cv.Scalar.Yellow, 10, cv.LineTypes.AntiAlias)
            End If
            p1 = New cv.Point(0, noisyLine.bb)
            p2 = New cv.Point(width, noisyLine.m * width + noisyLine.bb)
            dst1.Line(p1, p2, cv.Scalar.Blue, 3, cv.LineTypes.AntiAlias)
        End If
        ocvb.trueText(New TTtext("GT m = " + Format(noisyLine.m, "#0.00") + " eigen m = " + Format(m2, "#0.00") + "    len = " + CStr(CInt(Len)) + vbCrLf +
                                              "Confidence = " + Format(eigenVal.Get(Of Single)(0, 0) / eigenVal.Get(Of Single)(1, 0), "#0.0") + vbCrLf +
                                              "theta: atan2(" + Format(eigenVec.Get(Of Single)(1, 0), "#0.0") + ", " + Format(eigenVec.Get(Of Single)(0, 0), "#0.0") + ") = " +
                                              Format(theta, "#0.0000"), 10, 60))
    End Sub
End Class




