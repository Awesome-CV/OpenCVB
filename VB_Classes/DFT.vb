Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module dft_Module
    Public Function inverseDFT(complexImage As cv.Mat) As cv.Mat
        Dim invDFT As New cv.Mat
        cv.Cv2.Dft(complexImage, invDFT, cv.DftFlags.Inverse Or cv.DftFlags.RealOutput)
        invDFT = invDFT.Normalize(0, 255, cv.NormTypes.MinMax)
        Dim inverse8u As New cv.Mat
        invDFT.ConvertTo(inverse8u, cv.MatType.CV_8U)
        Return inverse8u
    End Function
End Module




' http://stackoverflow.com/questions/19761526/how-to-do-inverse-dft-in-opencv
Public Class DFT_Basics
    Inherits VBparent
    Dim mats As Mat_4to1
    Public magnitude As New cv.Mat
    Public spectrum As New cv.Mat
    Public complexImage As New cv.Mat
    Public gray As cv.Mat
    Public rows As integer
    Public cols As integer
    Public Sub New()
        initParent()
        mats = New Mat_4to1()
        mats.noLines = True

        task.desc = "Explore the Discrete Fourier Transform."
        label1 = "Image after inverse DFT"
        label2 = "DFT_Basics Spectrum Magnitude"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        gray = src
        If src.Channels = 3 Then gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        rows = cv.Cv2.GetOptimalDFTSize(gray.Rows)
        cols = cv.Cv2.GetOptimalDFTSize(gray.Cols)
        Dim padded = New cv.Mat(gray.Width, gray.Height, cv.MatType.CV_8UC3)
        cv.Cv2.CopyMakeBorder(gray, padded, 0, rows - gray.Rows, 0, cols - gray.Cols, cv.BorderTypes.Constant, cv.Scalar.All(0))
        Dim padded32 As New cv.Mat
        padded.ConvertTo(padded32, cv.MatType.CV_32F)
        Dim planes() = {padded32, New cv.Mat(padded.Size(), cv.MatType.CV_32F, 0)}
        cv.Cv2.Merge(planes, complexImage)
        cv.Cv2.Dft(complexImage, complexImage)

        If standalone or task.intermediateReview = caller Then
            ' compute the magnitude And switch to logarithmic scale => log(1 + sqrt(Re(DFT(I))^2 + Im(DFT(I))^2))
            planes = complexImage.Split

            cv.Cv2.Magnitude(planes(0), planes(1), magnitude)
            magnitude += cv.Scalar.All(1) ' switch To logarithmic scale
            cv.Cv2.Log(magnitude, magnitude)

            ' crop the spectrum, if it has an odd number of rows Or columns
            spectrum = magnitude(New cv.Rect(0, 0, magnitude.Cols And -2, magnitude.Rows And -2))
            ' Transform the matrix with float values into range 0-255
            spectrum = spectrum.Normalize(0, 255, cv.NormTypes.MinMax)
            spectrum.ConvertTo(padded, cv.MatType.CV_8U)

            ' rearrange the quadrants of Fourier image  so that the origin is at the image center
            Dim cx = CInt(padded.Cols / 2)
            Dim cy = CInt(padded.Rows / 2)

            mats.mat(3) = padded(New cv.Rect(0, 0, cx, cy)).Clone()
            mats.mat(2) = padded(New cv.Rect(cx, 0, cx, cy)).Clone()
            mats.mat(1) = padded(New cv.Rect(0, cy, cx, cy)).Clone()
            mats.mat(0) = padded(New cv.Rect(cx, cy, cx, cy)).Clone()
            mats.Run()
            dst2 = mats.dst1

            dst1 = inverseDFT(complexImage)
        End If
    End Sub
End Class





' http://opencvexamples.blogspot.com/
Public Class DFT_Inverse
    Inherits VBparent
    Dim mats As Mat_2to1
    Public Sub New()
        initParent()
        mats = New Mat_2to1()
        task.desc = "Take the inverse of the Discrete Fourier Transform."
        label1 = "Image after Inverse DFT"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray32f As New cv.Mat
        src.ConvertTo(gray32f, cv.MatType.CV_32F)
        Dim planes() = {gray32f, New cv.Mat(gray32f.Size(), cv.MatType.CV_32F, 0)}
        Dim complex As New cv.Mat, complexImage As New cv.Mat
        cv.Cv2.Merge(planes, complex)
        cv.Cv2.Dft(complex, complexImage)

        dst1 = inverseDFT(complexImage)

        Dim diff As New cv.Mat
        cv.Cv2.Absdiff(src, dst1, diff)
        mats.mat(0) = diff.Threshold(0, 255, cv.ThresholdTypes.Binary)
        mats.mat(1) = (diff * 50).ToMat
        mats.Run()
        If mats.mat(0).countnonzero() > 0 Then
            dst2 = mats.dst1
            label2 = "Mask of difference (top) and relative diff (bot)"
        Else
            label2 = "InverseDFT reproduced original"
            dst2.SetTo(0)
        End If
    End Sub
End Class





' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class DFT_ButterworthFilter_MT
    Inherits VBparent
    Public dft As DFT_Basics
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "DFT B Filter - Radius", 1, src.Rows, src.Rows)
            sliders.setupTrackBar(1, "DFT B Filter - Order", 1, src.Rows, 2)
        End If
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 6)
            radio.check(0).Text = "DFT Flags ComplexOutput"
            radio.check(1).Text = "DFT Flags Inverse"
            radio.check(2).Text = "DFT Flags None"
            radio.check(3).Text = "DFT Flags RealOutput"
            radio.check(4).Text = "DFT Flags Rows"
            radio.check(5).Text = "DFT Flags Scale"
            radio.check(0).Checked = True
        End If

        dft = New DFT_Basics()
        task.desc = "Use the Butterworth filter on a DFT image - color image input."
        label1 = "Image with Butterworth Low Pass Filter Applied"
        label2 = "Same filter with radius / 2"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        dft.src = src
        dft.Run()

        Static radius As Integer
        Static order As Integer
        Static butterworthFilter(1) As cv.Mat
        ' only create the filter if radius or order has changed.
        If radius <> sliders.trackbar(0).Value Or order <> sliders.trackbar(1).Value Then
            radius = sliders.trackbar(0).Value
            order = sliders.trackbar(1).Value

            Parallel.For(0, 2,
            Sub(k)
                Dim r = radius / (k + 1), rNext As Double
                butterworthFilter(k) = New cv.Mat(dft.complexImage.Size, cv.MatType.CV_32FC2)
                Dim tmp As New cv.Mat(butterworthFilter(k).Size(), cv.MatType.CV_32F, 0)
                Dim center As New cv.Point(butterworthFilter(k).Rows / 2, butterworthFilter(k).Cols / 2)
                For i = 0 To butterworthFilter(k).Rows - 1
                    For j = 0 To butterworthFilter(k).Cols - 1
                        rNext = Math.Sqrt(Math.Pow(i - center.X, 2) + Math.Pow(j - center.Y, 2))
                        tmp.Set(Of Single)(i, j, 1 / (1 + Math.Pow(rNext / r, 2 * order)))
                    Next
                Next
                Dim tmpMerge() = {tmp, tmp}
                cv.Cv2.Merge(tmpMerge, butterworthFilter(k))
            End Sub)
        End If

        Dim dftFlag As cv.DctFlags
        Static frm = findfrm("DFT_ButterworthFilter_MT Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                dftFlag = Choose(i + 1, cv.DftFlags.ComplexOutput, cv.DftFlags.Inverse, cv.DftFlags.None,
                                        cv.DftFlags.RealOutput, cv.DftFlags.Rows, cv.DftFlags.Scale)
            End If
        Next

        Parallel.For(0, 2,
       Sub(k)
           Dim complex As New cv.Mat
           cv.Cv2.MulSpectrums(butterworthFilter(k), dft.complexImage, complex, dftFlag)
           If k = 0 Then dst1 = inverseDFT(complex) Else dst2 = inverseDFT(complex)
       End Sub)
    End Sub
End Class






' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class DFT_ButterworthDepth
    Inherits VBparent
    Dim bfilter As DFT_ButterworthFilter_MT
    Public Sub New()
        initParent()
        bfilter = New DFT_ButterworthFilter_MT()

        task.desc = "Use the Butterworth filter on a DFT image - RGBDepth as input."
        label1 = "Image with Butterworth Low Pass Filter Applied"
        label2 = "Same filter with radius / 2"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        bfilter.src = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        bfilter.Run()
        dst1 = bfilter.dst1
        dst2 = bfilter.dst2
    End Sub
End Class

