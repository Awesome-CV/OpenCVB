Imports cv = OpenCvSharp
Imports OpenCvSharp.XImgProc

' https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
' https://www.learnopencv.com/otsu-thresholding-with-opencv/?ck_subscriber_id=785741175
' https://github.com/spmallick/learnopencv/tree/master/otsu-method?ck_subscriber_id=785741175
Public Class Binarize_Basics
    Inherits VBparent
    Public thresholdType = cv.ThresholdTypes.Otsu
    Dim minRange = 0
    Dim maxRange = 255
    Public histogram As New cv.Mat
    Public meanScalar As cv.Scalar
    Public mask As New cv.Mat
    Dim blur As Blur_Basics
    Public Sub New()
        initParent()
        blur = New Blur_Basics()
        mask = New cv.Mat(src.Size, cv.MatType.CV_8U, 255)
        task.desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        meanScalar = cv.Cv2.Mean(src, mask)

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim dimensions() = New Integer() {maxRange}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
        blur.src = input
        blur.Run()
        cv.Cv2.CalcHist(New cv.Mat() {blur.dst1}, New Integer() {0}, mask, histogram, 1, dimensions, ranges)
        dst1 = blur.dst1.Threshold(meanScalar(0), 255, thresholdType)
    End Sub
End Class





'https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
Public Class Binarize_OTSU
    Inherits VBparent
    Dim mats1 As Mat_4to1
    Dim mats2 As Mat_4to1
    Dim plotHist As Plot_Histogram
    Dim binarize As Binarize_Basics
    Public Sub New()
        initParent()
        binarize = New Binarize_Basics()

        plotHist = New Plot_Histogram()

        mats1 = New Mat_4to1()
        mats2 = New Mat_4to1()

        label1 = "Threshold 1) binary 2) Binary+OTSU 3) OTSU 4) OTSU+Blur"
        label2 = "Histograms correspond to images on the left"
        task.desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        dst2.SetTo(0)

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        binarize.meanScalar = cv.Cv2.Mean(input)
        binarize.src = input
        Static blurkernelslider = findSlider("Blur Kernel Size")
        Dim kernelsize = blurkernelslider.value
        blurkernelslider.value = 0
        For i = 0 To 4 - 1
            binarize.thresholdType = Choose(i + 1, cv.ThresholdTypes.Binary, cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu,
                                            cv.ThresholdTypes.Otsu, cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu)
            If i = 3 Then blurkernelslider.value = kernelsize ' only blur the last request
            binarize.Run()
            mats1.mat(i) = binarize.dst1.Clone()
            plotHist.hist = binarize.histogram.Clone()
            plotHist.Run()
            mats2.mat(i) = plotHist.dst1.Clone()
        Next

        mats1.Run()
        dst1 = mats1.dst1
        mats2.Run()
        dst2 = mats2.dst1
    End Sub
End Class





Public Class Binarize_Niblack_Sauvola
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
            sliders.setupTrackBar(1, "Niblack k", -1000, 1000, -200)
            sliders.setupTrackBar(2, "Sauvola k", -1000, 1000, 100)
            sliders.setupTrackBar(3, "Sauvola r", 1, 100, 64)
        End If
        task.desc = "Binarize an image using Niblack and Sauvola"
        label1 = "Binarize Niblack"
        label2 = "Binarize Sauvola"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim grayBin As New cv.Mat
        cv.Extensions.Binarizer.Niblack(input, grayBin, kernelSize, sliders.trackbar(1).Value / 1000)
        dst1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Extensions.Binarizer.Sauvola(input, grayBin, kernelSize, sliders.trackbar(2).Value / 1000, sliders.trackbar(3).Value)
        dst2 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Niblack_Nick
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
            sliders.setupTrackBar(1, "Niblack k", -1000, 1000, -200)
            sliders.setupTrackBar(2, "Nick k", -1000, 1000, 100)
        End If
        task.desc = "Binarize an image using Niblack and Nick"
        label1 = "Binarize Niblack"
        label2 = "Binarize Nick"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim grayBin As New cv.Mat
        cv.Extensions.Binarizer.Niblack(input, grayBin, kernelSize, sliders.trackbar(1).Value / 1000)
        dst1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Extensions.Binarizer.Nick(input, grayBin, kernelSize, sliders.trackbar(2).Value / 1000)
        dst2 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Bernson
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
            sliders.setupTrackBar(1, "Contrast min", 0, 255, 50)
            sliders.setupTrackBar(2, "bg Threshold", 0, 255, 100)
        End If
        label1 = "Binarize Bernson (Draw Enabled)"

        task.drawRect = New cv.Rect(100, 100, 100, 100)
        task.desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayBin = gray.Clone()
        If task.drawRect = New cv.Rect() Then
            cv.Extensions.Binarizer.Bernsen(gray, grayBin, kernelSize, sliders.trackbar(1).Value, sliders.trackbar(2).Value)
        Else
            cv.Extensions.Binarizer.Bernsen(gray(task.drawRect), grayBin(task.drawRect), kernelSize, sliders.trackbar(1).Value, sliders.trackbar(2).Value)
        End If
        dst1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Bernson_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 32
        gridHeightSlider.Value = 32

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
            sliders.setupTrackBar(1, "Contrast min", 0, 255, 50)
            sliders.setupTrackBar(2, "bg Threshold", 0, 255, 100)
        End If
        task.desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
        label1 = "Binarize Bernson"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        grid.Run()
        Dim contrastMin = sliders.trackbar(1).Value
        Dim bgThreshold = sliders.trackbar(2).Value

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Parallel.ForEach(Of cv.Rect)(grid.roiList,
            Sub(roi)
                Dim grayBin = input(roi).Clone()
                cv.Extensions.Binarizer.Bernsen(input(roi), grayBin, kernelSize, contrastMin, bgThreshold)
                dst1(roi) = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End Sub)
    End Sub
End Class








Public Class Binarize_Reduction
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Dim basics As Binarize_Basics
    Public Sub New()
        initParent()
        basics = New Binarize_Basics
        reduction = New Reduction_Basics
        Dim reductionRadio = findRadio("Use bitwise reduction")
        reductionRadio.Checked = True
        Dim reductionSlider = findSlider("Reduction factor")
        reductionSlider.Value = 256
        label1 = "Binarize output from reduction"
        label2 = "Binarize Basics Output"
        task.desc = "Binarize an image using reduction"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        reduction.src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        reduction.Run()
        dst1 = reduction.dst1.Threshold(reduction.maskVal / 2, 255, cv.ThresholdTypes.Binary)

        basics.src = reduction.src
        basics.Run()
        dst2 = basics.dst1
    End Sub
End Class






Public Class Binarize_Simple
    Inherits VBparent
    Public meanScalar As cv.Scalar
    Public mask As New cv.Mat
    Dim blur As Blur_Basics
    Public Sub New()
        initParent()
        mask = New cv.Mat(src.Size, cv.MatType.CV_8U, 255)
        blur = New Blur_Basics()

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Use Blur algorithm"
            check.Box(0).Checked = True
        End If

        task.desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src.Clone
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If mask.Width Then
            Dim tmp As New cv.Mat(input.Size, cv.MatType.CV_8U, 255)
            input.CopyTo(tmp, mask)
            meanScalar = cv.Cv2.Mean(tmp, mask)
        Else
            meanScalar = cv.Cv2.Mean(input)
        End If

        Static blurCheck = findCheckBox("Use Blur algorithm")
        If blurCheck.checked Then
            blur.src = input
            blur.Run()
            dst1 = blur.dst1.Threshold(meanScalar(0), 255, cv.ThresholdTypes.Binary)
        Else
            dst1 = input.Threshold(meanScalar(0), 255, cv.ThresholdTypes.Binary)
        End If
    End Sub
End Class






Public Class Binarize_Recurse
    Inherits VBparent
    Dim binarize As Binarize_Simple
    Public mats As Mat_4Click
    Public Sub New()
        initParent()
        binarize = New Binarize_Simple
        mats = New Mat_4Click
        ocvb.quadrantIndex = QUAD3
        label1 = "Lighter half, lightest, darker half, darkest"
        task.desc = "Binarize an image twice using masks"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        binarize.src = gray
        binarize.mask = New cv.Mat
        binarize.run()
        mats.mat(0) = binarize.dst1.Clone

        binarize.src = gray
        binarize.mask = mats.mat(0)
        binarize.Run()
        mats.mat(1) = binarize.dst1.Clone

        cv.Cv2.BitwiseNot(mats.mat(0), mats.mat(2))
        binarize.src = gray
        binarize.mask = mats.mat(0).Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        binarize.Run()
        mats.mat(3) = binarize.dst1.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)

        mats.Run()
        dst1 = mats.dst1
        dst2 = mats.dst2
        label2 = mats.label2
    End Sub
End Class
