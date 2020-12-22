Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class Random_Points
    Inherits VBparent
    Public Points(0) As cv.Point
    Public Points2f(0) As cv.Point2f
    Public rangeRect As cv.Rect
    Public plotPoints As Boolean = False
    Public countSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Random Pixel Count", 1, src.Cols * src.Rows, 20)
            countSlider = sliders.trackbar(0)
        Else
            countSlider = findSlider("Random Pixel Count")
        End If

        rangeRect = New cv.Rect(0, 0, src.Cols, src.Rows)
        task.desc = "Create a uniform random mask with a specificied number of pixels."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If Points.Length <> countSlider.Value Then
            ReDim Points(countSlider.Value - 1)
            ReDim Points2f(countSlider.Value - 1)
        End If
        dst1.SetTo(0)
        For i = 0 To Points.Length - 1
            Dim x = msRNG.Next(rangeRect.X, rangeRect.X + rangeRect.Width)
            Dim y = msRNG.Next(rangeRect.Y, rangeRect.Y + rangeRect.Height)
            Points(i) = New cv.Point2f(x, y)
            Points2f(i) = New cv.Point2f(x, y)
            If standalone Or plotPoints = True Then cv.Cv2.Circle(dst1, Points(i), 3, cv.Scalar.Gray, -1, cv.LineTypes.AntiAlias, 0)
        Next
    End Sub
End Class




Public Class Random_Shuffle
    Inherits VBparent
    Dim myRNG As New cv.RNG
    Public Sub New()
        initParent()
        task.desc = "Use randomShuffle to reorder an image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        src.CopyTo(dst1)
        cv.Cv2.RandShuffle(dst1, 1.0, myRNG) ' don't remove that myRNG!  It will fail in RandShuffle.
        label1 = "Random_shuffle - wave at camera"
    End Sub
End Class



Public Class Random_LUTMask
    Inherits VBparent
    Dim random As Random_Points
    Dim km As kMeans_Basics
    Public Sub New()
        initParent()
        km = New kMeans_Basics()
        random = New Random_Points()
        task.desc = "Use a random Look-Up-Table to modify few colors in a kmeans image."
        label2 = "kmeans run To Get colors"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static lutMat As cv.Mat
        If lutMat Is Nothing Or ocvb.frameCount Mod 10 = 0 Then
            random.Run()
            lutMat = cv.Mat.Zeros(New cv.Size(1, 256), cv.MatType.CV_8UC3)
            Dim lutIndex = 0
            km.src = src
            km.Run()
            dst1 = km.dst1
            For i = 0 To random.Points.Length - 1
                Dim x = random.Points(i).X
                Dim y = random.Points(i).Y
                lutMat.Set(lutIndex, 0, dst1.Get(Of cv.Vec3b)(y, x))
                lutIndex += 1
                If lutIndex >= lutMat.Rows Then Exit For
            Next
        End If
        dst2 = src.LUT(lutMat)
        label1 = "Using kmeans colors with interpolation"
    End Sub
End Class



Public Class Random_UniformDist
    Inherits VBparent
    Public minVal = 0
    Public maxVal = 255
    Public Sub New()
        initParent()
        task.desc = "Create a uniform distribution."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U)
        cv.Cv2.Randu(dst1, minVal, maxVal)
    End Sub
End Class



Public Class Random_NormalDist
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Random_NormalDist Blue Mean", 0, 255, 125)
            sliders.setupTrackBar(1, "Random_NormalDist Green Mean", 0, 255, 25)
            sliders.setupTrackBar(2, "Random_NormalDist Red Mean", 0, 255, 180)
            sliders.setupTrackBar(3, "Random_NormalDist Stdev", 0, 255, 50)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Use Grayscale image"
        End If

        task.desc = "Create a normal distribution in all 3 colors with a variable standard deviation."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static grayCheck = findCheckBox("Use Grayscale image")
        If grayCheck.checked And dst1.Channels <> 1 Then dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        cv.Cv2.Randn(dst1, New cv.Scalar(sliders.trackbar(0).Value, sliders.trackbar(1).Value, sliders.trackbar(2).Value), cv.Scalar.All(sliders.trackbar(3).Value))
    End Sub
End Class



Public Class Random_CheckUniformSmoothed
    Inherits VBparent
    Dim histogram As Histogram_KalmanSmoothed
    Dim rUniform As Random_UniformDist
    Public Sub New()
        initParent()
        histogram = New Histogram_KalmanSmoothed
        histogram.sliders.trackbar(0).Value = 255

        rUniform = New Random_UniformDist()

        task.desc = "Display the smoothed histogram for a uniform distribution."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        rUniform.src = src
        rUniform.Run()
        dst1 = rUniform.dst1
        histogram.src = dst1
        histogram.plotHist.maxRange = 255
        histogram.Run()
        dst2 = histogram.dst1
    End Sub
End Class






Public Class Random_CheckUniformDist
    Inherits VBparent
    Dim histogram As Histogram_Basics
    Dim rUniform As Random_UniformDist
    Public Sub New()
        initParent()
        histogram = New Histogram_Basics()
        histogram.sliders.trackbar(0).Value = 255

        rUniform = New Random_UniformDist()

        task.desc = "Display the histogram for a uniform distribution."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        rUniform.src = src
        rUniform.Run()
        dst1 = rUniform.dst1
        histogram.src = dst1
        histogram.plotRequested = True
        histogram.Run()
        dst2 = histogram.dst1
    End Sub
End Class






Public Class Random_CheckNormalDist
    Inherits VBparent
    Dim histogram As Histogram_Basics
    Dim normalDist As Random_NormalDist
    Public Sub New()
        initParent()
        histogram = New Histogram_Basics()
        histogram.sliders.trackbar(0).Value = 255
        normalDist = New Random_NormalDist()
        task.desc = "Display the histogram for a Normal distribution."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        normalDist.src = src
        normalDist.Run()
        dst1 = normalDist.dst1
        histogram.src = dst1
        histogram.plotRequested = True
        histogram.Run()
        dst2 = histogram.dst1
    End Sub
End Class





Public Class Random_CheckNormalDistSmoothed
    Inherits VBparent
    Dim histogram As Histogram_KalmanSmoothed
    Dim normalDist As Random_NormalDist
    Public Sub New()
        initParent()
        histogram = New Histogram_KalmanSmoothed
        histogram.sliders.trackbar(0).Value = 255
        histogram.plotHist.minRange = 1
        normalDist = New Random_NormalDist()
        task.desc = "Display the histogram for a Normal distribution."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        normalDist.src = src
        normalDist.Run()
        dst1 = normalDist.dst1
        histogram.src = dst1
        histogram.Run()
        dst2 = histogram.dst1
    End Sub
End Class





Module Random_PatternGenerator_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_PatternGenerator_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Random_PatternGenerator_Close(Random_PatternGeneratorPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_PatternGenerator_Run(Random_PatternGeneratorPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function


    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_DiscreteDistribution_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Random_DiscreteDistribution_Close(rPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_DiscreteDistribution_Run(rPtr As IntPtr, rows As Integer, cols As Integer, channels As Integer) As IntPtr
    End Function
End Module







Public Class Random_PatternGenerator_CPP
    Inherits VBparent
    Dim Random_PatternGenerator As IntPtr
    Public Sub New()
        initParent()
        Random_PatternGenerator = Random_PatternGenerator_Open()
        task.desc = "Generate random patterns for use with 'Random Pattern Calibration'"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim imagePtr = Random_PatternGenerator_Run(Random_PatternGenerator, src.Rows, src.Cols)

        If imagePtr <> 0 Then
            Dim dstData(src.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
        End If
    End Sub
    Public Sub Close()
        Random_PatternGenerator_Close(Random_PatternGenerator)
    End Sub
End Class








Public Class Random_CustomDistribution
    Inherits VBparent
    Public inputCDF As cv.Mat ' place a cumulative distribution function here (or just put the histogram that reflects the desired random number distribution)
    Public outputRandom = New cv.Mat(10000, 1, cv.MatType.CV_32S, 0) ' allocate the desired number of random numbers - size can be just one to get the next random value
    Public outputHistogram As cv.Mat
    Public plotHist As Plot_Histogram
    Public Sub New()
        initParent()
        Dim loadedDice() As Single = {1, 3, 0.5, 0.5, 0.75, 0.25}
        inputCDF = New cv.Mat(loadedDice.Length, 1, cv.MatType.CV_32F, loadedDice)

        If standalone Then plotHist = New Plot_Histogram()

        task.desc = "Create a custom random number distribution from any histogram"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim lastValue = inputCDF.Get(Of Single)(inputCDF.Rows - 1, 0)
        If Not (lastValue > 0.99 And lastValue <= 1.0) Then ' convert the input histogram to a cdf.
            inputCDF *= 1 / (inputCDF.Sum().Item(0))
            For i = 1 To inputCDF.Rows - 1
                inputCDF.Set(Of Single)(i, 0, inputCDF.Get(Of Single)(i - 1, 0) + inputCDF.Get(Of Single)(i, 0))
            Next
        End If
        outputHistogram = New cv.Mat(inputCDF.Size(), cv.MatType.CV_32F, 0)
        Dim size = outputHistogram.Rows
        For i = 0 To outputRandom.rows - 1
            Dim uniformR1 = msRNG.NextDouble()
            For j = 0 To size - 1
                If uniformR1 < inputCDF.Get(Of Single)(j, 0) Then
                    outputHistogram.Set(Of Single)(j, 0, outputHistogram.Get(Of Single)(j, 0) + 1)
                    outputRandom.set(Of Integer)(i, 0, j) ' the output is an integer reflecting a bin in the histogram.
                    Exit For
                End If
            Next
        Next

        If standalone Then
            plotHist.hist = outputHistogram
            plotHist.Run()
            dst1 = plotHist.dst1
        End If
    End Sub
End Class






' https://www.khanacademy.org/computing/computer-programming/programming-natural-simulations/programming-randomness/a/custom-distribution-of-random-numbers
Public Class Random_MonteCarlo
    Inherits VBparent
    Public plotHist As Plot_Histogram
    Public outputRandom = New cv.Mat(4000, 1, cv.MatType.CV_32S, 0) ' allocate the desired number of random numbers - size can be just one to get the next random value
    Public Sub New()
        initParent()
        plotHist = New Plot_Histogram()
        plotHist.fixedMaxVal = 100

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Number of bins", 1, 255, 91)
        End If
        task.desc = "Generate random numbers but prefer higher values - a linearly increasing random distribution"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim dimension = sliders.trackbar(0).Value
        Dim histogram = New cv.Mat(dimension, 1, cv.MatType.CV_32F, 0)
        For i = 0 To outputRandom.rows - 1
            While (1)
                Dim r1 = msRNG.NextDouble()
                Dim r2 = msRNG.NextDouble()
                If r2 < r1 Then
                    Dim index = CInt(dimension * r1)
                    histogram.Set(Of Single)(index, 0, histogram.Get(Of Single)(index, 0) + 1)
                    outputRandom.set(Of Integer)(i, 0, index)
                    Exit While
                End If
            End While
        Next

        If standalone Then
            plotHist.hist = histogram
            plotHist.Run()
            dst1 = plotHist.dst1
        End If
    End Sub
End Class






Public Class Random_CustomHistogram
    Inherits VBparent
    Public random As Random_CustomDistribution
    Public hist As Histogram_Simple
    Public saveHist As cv.Mat
    Public Sub New()
        initParent()

        random = New Random_CustomDistribution()
        random.outputRandom = New cv.Mat(1000, 1, cv.MatType.CV_32S, 0)

        hist = New Histogram_Simple()
        hist.sliders.trackbar(0).Value = 255

        label1 = "Histogram of the grayscale image"
        label2 = "Histogram of the resulting random numbers"

        task.desc = "Create a random number distribution that reflects histogram of a grayscale image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static saveBins As Integer
        If hist.sliders.trackbar(0).Value <> saveBins Then
            saveBins = hist.sliders.trackbar(0).Value
            hist.src = src
            hist.plotHist.fixedMaxVal = 0 ' we are sharing the plothist with the code below...
            hist.Run()
            dst1 = hist.dst1.Clone()
            saveHist = hist.plotHist.hist.Clone()
        End If

        random.inputCDF = saveHist ' it will convert the histogram into a cdf where the last value must be near one.
        random.Run()

        If standalone Then
            hist.plotHist.fixedMaxVal = 100
            hist.plotHist.hist = random.outputHistogram
            hist.plotHist.Run()
            dst2 = hist.plotHist.dst1
        End If
    End Sub
End Class







' https://github.com/spmallick/learnopencv/tree/master/Photoshop-Filters-In-OpenCV
Public Class Random_60sTV
    Inherits VBparent
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Range of noise to apply (from 0 to this value)", 0, 255, 50)
            sliders.setupTrackBar(1, "Percentage of pixels to include noise", 0, 100, 20)
        End If
        task.desc = "Imitate an old TV appearance using randomness."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static valSlider = findSlider("Range of noise to apply (from 0 to this value)")
        Static threshSlider = findSlider("Percentage of pixels to include noise")
        Dim val = valSlider.value
        Dim thresh = threshSlider.value

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        For y = 0 To dst1.Height - 1
            For x = 0 To dst1.Width - 1
                If 255 * Rnd() <= thresh Then
                    Dim v = dst1.Get(Of Byte)(y, x)
                    dst1.Set(Of Byte)(y, x, If(2 * Rnd() = 0, Math.Min(v + (val + 1) * Rnd(), 255), Math.Max(v - (val + 1) * Rnd(), 0)))
                End If
            Next
        Next
    End Sub
End Class






' https://github.com/spmallick/learnopencv/tree/master/Photoshop-Filters-In-OpenCV
Public Class Random_60sTVFaster
    Inherits VBparent
    Dim random As Random_UniformDist
    Dim mats As Mat_4to1
    Public Sub New()
        initParent()

        mats = New Mat_4to1
        random = New Random_UniformDist
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Range of noise to apply (from 0 to this value)", 0, 255, 100)
            sliders.setupTrackBar(1, "Percentage of pixels to include noise", 0, 100, 20)
        End If
        label2 = "Changed pixels, add/sub mask, plusMask, minusMask"
        task.desc = "A faster way to apply noise to imitate an old TV appearance using randomness."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static valSlider = findSlider("Range of noise to apply (from 0 to this value)")
        Static percentSlider = findSlider("Percentage of pixels to include noise")

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        random.Run()
        mats.mat(0) = random.dst1.Threshold(255 - percentSlider.value * 255 / 100, 255, cv.ThresholdTypes.Binary)
        Dim nochangeMask = random.dst1.Threshold(255 - percentSlider.value * 255 / 100, 255, cv.ThresholdTypes.BinaryInv)

        random.Run()
        Dim valMask = random.dst1.Threshold(valSlider.value, 255, cv.ThresholdTypes.BinaryInv)
        Dim valMat As New cv.Mat(valMask.Size, cv.MatType.CV_8U, 0)
        random.dst1.CopyTo(valMat, valMask)

        random.Run()
        Dim plusMask = random.dst1.Threshold(127, 255, cv.ThresholdTypes.Binary).SetTo(0, nochangeMask)
        Dim minusMask = random.dst1.Threshold(127, 255, cv.ThresholdTypes.BinaryInv).SetTo(0, nochangeMask)

        mats.mat(2) = plusMask
        mats.mat(3) = minusMask
        mats.mat(1) = plusMask + minusMask

        cv.Cv2.Add(dst1, valMat, dst1, plusMask)
        cv.Cv2.Subtract(dst1, valMat, dst1, minusMask)
        mats.Run()
        dst2 = mats.dst1
    End Sub
End Class







' https://github.com/spmallick/learnopencv/tree/master/Photoshop-Filters-In-OpenCV
Public Class Random_60sTVFastSimple
    Inherits VBparent
    Dim random As Random_UniformDist
    Public Sub New()
        initParent()

        random = New Random_UniformDist
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Range of noise to apply (from 0 to this value)", 0, 255, 100)
            sliders.setupTrackBar(1, "Percentage of pixels to include noise", 0, 100, 20)
        End If
        task.desc = "Remove diagnostics from the faster algorithm to simplify code."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static valSlider = findSlider("Range of noise to apply (from 0 to this value)")
        Static percentSlider = findSlider("Percentage of pixels to include noise")

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        random.Run()
        Dim nochangeMask = random.dst1.Threshold(255 - percentSlider.value * 255 / 100, 255, cv.ThresholdTypes.BinaryInv)

        random.Run()
        Dim valMask = random.dst1.Threshold(valSlider.value, 255, cv.ThresholdTypes.BinaryInv)
        Dim valMat As New cv.Mat(valMask.Size, cv.MatType.CV_8U, 0)
        random.dst1.CopyTo(valMat, valMask)

        random.Run()
        Dim plusMask = random.dst1.Threshold(127, 255, cv.ThresholdTypes.Binary).SetTo(0, nochangeMask)
        Dim minusMask = random.dst1.Threshold(127, 255, cv.ThresholdTypes.BinaryInv).SetTo(0, nochangeMask)

        cv.Cv2.Add(dst1, valMat, dst1, plusMask)
        cv.Cv2.Subtract(dst1, valMat, dst1, minusMask)
    End Sub
End Class