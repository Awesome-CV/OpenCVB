Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module OpticalFlowModule_Exports
    ' https://docs.opencv.org/3.4/db/d7f/tutorial_js_lucas_kanade.html
    Public Function opticalFlow_Dense(oldGray As cv.Mat, gray As cv.Mat, pyrScale As Single, levels As integer, winSize As integer, iterations As integer,
                                polyN As Single, polySigma As Single, OpticalFlowFlags As cv.OpticalFlowFlags) As cv.Mat
        Dim flow As New cv.Mat
        If pyrScale >= 1 Then pyrScale = 0.99

        ' When running "Test All", the OpenGL code requires full resolution which switches to low resolution (if active) after completion.
        ' The first frame after switching will mean oldgray is full resolution and gray is low resolution.  This "If" avoids the problem.
        ' if another algorithm lexically follows the OpenGL algorithms, this may change (or be deleted!)
        If oldGray.Size() <> gray.Size() Then oldGray = gray.Clone()

        cv.Cv2.CalcOpticalFlowFarneback(oldGray, gray, flow, pyrScale, levels, winSize, iterations, polyN, polySigma, OpticalFlowFlags)
        Dim flowVec(1) As cv.Mat
        cv.Cv2.Split(flow, flowVec)

        Dim hsv As New cv.Mat
        Dim hsv0 As New cv.Mat
        Dim hsv1 As New cv.Mat(gray.Rows, gray.Cols, cv.MatType.CV_8UC1, 255)
        Dim hsv2 As New cv.Mat

        Dim magnitude As New cv.Mat
        Dim angle As New cv.Mat
        cv.Cv2.CartToPolar(flowVec(0), flowVec(1), magnitude, angle)
        angle.ConvertTo(hsv0, cv.MatType.CV_8UC1, 180 / Math.PI / 2)
        cv.Cv2.Normalize(magnitude, hsv2, 0, 255, cv.NormTypes.MinMax, cv.MatType.CV_8UC1)

        Dim hsvVec() As cv.Mat = {hsv0, hsv1, hsv2}
        cv.Cv2.Merge(hsvVec, hsv)
        Return hsv
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OpticalFlow_CPP_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OpticalFlow_CPP_Close(sPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OpticalFlow_CPP_Run(sPtr As IntPtr, rgbPtr As IntPtr, rows As integer, cols As integer) As IntPtr
    End Function
    Public Sub calcOpticalFlowPyrLK_Native(gray1 As cv.Mat, gray2 As cv.Mat, features1 As cv.Mat, features2 As cv.Mat)
        Dim hGray1 As GCHandle
        Dim hGray2 As GCHandle
        Dim hF1 As GCHandle
        Dim hF2 As GCHandle

        Dim grayData1(gray1.Total - 1)
        Dim grayData2(gray2.Total - 1)
        Dim fData1(features1.Total * features1.ElemSize - 1)
        Dim fData2(features2.Total * features2.ElemSize - 1)
        hGray1 = GCHandle.Alloc(grayData1, GCHandleType.Pinned)
        hGray2 = GCHandle.Alloc(grayData2, GCHandleType.Pinned)
        hF1 = GCHandle.Alloc(fData1, GCHandleType.Pinned)
        hF2 = GCHandle.Alloc(fData2, GCHandleType.Pinned)
    End Sub
End Module





Public Class OpticalFlow_DenseOptions
    Inherits VBparent

    Public pyrScale As Single
    Public levels As integer
    Public winSize As integer
    Public iterations As integer
    Public polyN As Single
    Public polySigma As Single
    Public OpticalFlowFlags As cv.OpticalFlowFlags
    Public outputScaling As integer
    Public Sub New()
        initParent()
        radio.Setup(caller, 5)
        radio.check(0).Text = "FarnebackGaussian"
        radio.check(1).Text = "LkGetMinEigenvals"
        radio.check(2).Text = "None"
        radio.check(3).Text = "PyrAReady"
        radio.check(4).Text = "PyrBReady"
        radio.check(0).Checked = True

        sliders.Setup(caller, 6)
        sliders.setupTrackBar(0, "Optical Flow pyrScale", 1, 100, 35)
        sliders.setupTrackBar(1, "Optical Flow Levels", 1, 10, 1)
        sliders.setupTrackBar(2, "Optical Flow winSize", 1, 9, 1)
        sliders.setupTrackBar(3, "Optical Flow Iterations", 1, 10, 1)
        sliders.setupTrackBar(4, "Optical Flow PolyN", 1, 15, 5)
        sliders.setupTrackBar(5, "Optical Flow Scaling Output", 1, 100, 50)

        label1 = "No output - just option settings..."
        task.desc = "Use dense optical flow algorithm options"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        pyrScale = sliders.trackbar(0).Value / sliders.trackbar(0).Maximum
        levels = sliders.trackbar(1).Value
        winSize = sliders.trackbar(2).Value
        iterations = sliders.trackbar(3).Value
        If winSize Mod 2 = 0 Then winSize += 1
        polyN = sliders.trackbar(4).Value
        If polyN Mod 2 = 0 Then polyN += 1
        polySigma = 1.5
        If polyN <= 5 Then polySigma = 1.1

        Static frm = findfrm("OpticalFlow_DenseOptions Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                OpticalFlowFlags = Choose(i + 1, cv.OpticalFlowFlags.FarnebackGaussian, cv.OpticalFlowFlags.LkGetMinEigenvals, cv.OpticalFlowFlags.None,
                                                     cv.OpticalFlowFlags.PyrAReady, cv.OpticalFlowFlags.PyrBReady, cv.OpticalFlowFlags.UseInitialFlow)
                Exit For
            End If
        Next
        outputScaling = sliders.trackbar(5).Value
    End Sub
End Class




Public Class OpticalFlow_DenseBasics
    Inherits VBparent
    Dim flow As OpticalFlow_DenseOptions
    Public Sub New()
        initParent()
        flow = New OpticalFlow_DenseOptions()
        task.desc = "Use dense optical flow algorithm  "
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static oldGray As New cv.Mat
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If ocvb.frameCount > 0 Then
            flow.Run()

            Dim hsv = opticalFlow_Dense(oldGray, src, flow.pyrScale, flow.levels, flow.winSize, flow.iterations, flow.polyN, flow.polySigma, flow.OpticalFlowFlags)

            dst1 = hsv.CvtColor(cv.ColorConversionCodes.HSV2RGB)
            dst1 = dst1.ConvertScaleAbs(flow.outputScaling)
            dst2 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        oldGray = src.Clone()
    End Sub
End Class




Public Class OpticalFlow_DenseBasics_MT
    Inherits VBparent

    Public grid As Thread_Grid
    Dim accum As New cv.Mat
    Dim flow As OpticalFlow_DenseOptions
    Public Sub New()
        initParent()
        grid = New Thread_Grid()
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        Static gridBorderSlider = findSlider("ThreadGrid Border")
        gridWidthSlider.Value = src.Cols / 4
        gridHeightSlider.Value = src.Rows / 4
        gridHeightSlider.Value = 5

        flow = New OpticalFlow_DenseOptions()
        flow.sliders.trackbar(0).Value = 75

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Correlation Threshold", 0, 1000, 1000)

        task.desc = "MultiThread dense optical flow algorithm  "
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static oldGray As New cv.Mat

        If ocvb.frameCount > 0 Then
            grid.Run()
            flow.Run()

            Dim CCthreshold = CSng(sliders.trackbar(0).Value / sliders.trackbar(0).Maximum)
            Parallel.For(0, grid.borderList.Count,
            Sub(i)
                Dim broi = grid.borderList(i)
                Dim roi = grid.roiList(i)
                Dim correlation As New cv.Mat
                Dim img = src(roi)
                cv.Cv2.MatchTemplate(img, accum(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                If CCthreshold > correlation.Get(Of Single)(0, 0) Then
                    img.CopyTo(accum(roi))
                    Dim gray = accum(broi).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                    Dim hsv = opticalFlow_Dense(oldGray(broi), gray, flow.pyrScale, flow.levels, flow.winSize, flow.iterations, flow.polyN, flow.polySigma, flow.OpticalFlowFlags)
                    Dim tROI = New cv.Rect(roi.X - broi.X, roi.Y - broi.Y, roi.Width, roi.Height)
                    dst1(roi) = hsv(tROI).CvtColor(cv.ColorConversionCodes.HSV2RGB)
                    dst1(roi) = dst1(roi).ConvertScaleAbs(flow.outputScaling)
                Else
                    dst1(roi).SetTo(0)
                End If
                oldGray(roi) = accum(roi).Clone()
            End Sub)
            dst2 = accum.Clone()
            dst2.SetTo(cv.Scalar.All(255), grid.gridMask)
        Else
            oldGray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            accum = src.Clone()
        End If
    End Sub
End Class





Public Class OpticalFlow_Sparse
    Inherits VBparent

    Public features As New List(Of cv.Point2f)

    Dim good As Features_GoodFeatures
    Dim lastFrame As cv.Mat
    Dim sumScale As cv.Mat, sScale As cv.Mat
    Dim errScale As cv.Mat, qScale As cv.Mat, rScale As cv.Mat
    Public Sub New()
        initParent()
        good = New Features_GoodFeatures()

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "OpticalFlow window", 1, 20, 3)
        sliders.setupTrackBar(1, "OpticalFlow Max Pixels Distance", 1, 100, 30)

        radio.Setup(caller, 6)
        radio.check(0).Text = "FarnebackGaussian"
        radio.check(1).Text = "LkGetMinEigenvals"
        radio.check(2).Text = "None"
        radio.check(3).Text = "PyrAReady"
        radio.check(4).Text = "PyrBReady"
        radio.check(5).Text = "UseInitialFlow"
        radio.check(5).Enabled = False
        radio.check(0).Checked = True

        task.desc = "Show the optical flow of a sparse matrix."
        label1 = ""
        label2 = ""
    End Sub
    Private Sub kalmanFilter()
        Dim f1err As New cv.Mat
        cv.Cv2.Add(errScale, qScale, f1err)
        For i = 0 To errScale.Rows - 1
            Dim gainScale = f1err.Get(Of Double)(i, 0) / (f1err.Get(Of Double)(i, 0) + rScale.Get(Of Double)(i, 0))
            sScale.Set(Of Double)(i, 0, sScale.Get(Of Double)(i, 0) + gainScale * (sumScale.Get(Of Double)(i, 0) - sScale.Get(Of Double)(i, 0)))
            errScale.Set(Of Double)(i, 0, (1 - gainScale) * f1err.Get(Of Double)(i, 0))
        Next
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        dst1 = src.Clone()
        dst2 = src.Clone()

        Dim OpticalFlowFlag As cv.OpticalFlowFlags
        Static frm = findfrm("OpticalFlow_Sparse Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                OpticalFlowFlag = Choose(i + 1, cv.OpticalFlowFlags.FarnebackGaussian, cv.OpticalFlowFlags.LkGetMinEigenvals, cv.OpticalFlowFlags.None,
                                                     cv.OpticalFlowFlags.PyrAReady, cv.OpticalFlowFlags.PyrBReady, cv.OpticalFlowFlags.UseInitialFlow)
                Exit For
            End If
        Next

        If ocvb.frameCount = 0 Then
            errScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 1)
            qScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.004)
            rScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.5)
            sumScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
            sScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
        End If

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        good.src = src
        good.Run()
        features = good.goodFeatures
        Dim features1 = New cv.Mat(features.Count, 1, cv.MatType.CV_32FC2, features.ToArray)
        Dim features2 = New cv.Mat
        If lastFrame IsNot Nothing Then
            Dim status As New cv.Mat
            Dim err As New cv.Mat
            Dim winSize As New cv.Size(3, 3)
            cv.Cv2.CalcOpticalFlowPyrLK(src, lastFrame, features1, features2, status, err, winSize, 3, term, OpticalFlowFlag)
            features = New List(Of cv.Point2f)
            Dim lastFeatures As New List(Of cv.Point2f)
            For i = 0 To status.Rows - 1
                If status.Get(Of Byte)(i, 0) Then
                    Dim pt1 = features1.Get(Of cv.Point2f)(i, 0)
                    Dim pt2 = features2.Get(Of cv.Point2f)(i, 0)
                    Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                    If length < 30 Then
                        features.Add(pt1)
                        lastFeatures.Add(pt2)
                        dst1.Line(pt1, pt2, cv.Scalar.Red, 5, cv.LineTypes.AntiAlias)
                        dst2.Circle(pt1, 5, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                        dst2.Circle(pt2, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                    End If
                End If
            Next
            label1 = "Matched " + CStr(features.Count) + " points "

            If ocvb.frameCount Mod 10 = 0 Then lastFrame = src.Clone()
        Else
            lastFrame = src.Clone()
        End If
    End Sub
End Class

