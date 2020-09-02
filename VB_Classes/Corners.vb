Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://docs.opencv.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_Harris
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Corner block size", 1, 21, 3)
        sliders.setupTrackBar(1, "Corner aperture size", 1, 21, 3)
        sliders.setupTrackBar(2, "Corner quality level", 1, 100, 50)
        desc = "Find corners using Eigen values and vectors"
        label2 = "Corner Eigen values"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static color As New cv.Mat
        Static gray As New cv.Mat
        Static mc As New cv.Mat
        Static minval As Double, maxval As Double

        gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        mc = New cv.Mat(gray.Size(), cv.MatType.CV_32FC1, 0)
        dst1 = New cv.Mat(gray.Size(), cv.MatType.CV_8U, 0)
        Dim blocksize = sliders.trackbar(0).Value
        If blocksize Mod 2 = 0 Then blocksize += 1
        Dim aperture = sliders.trackbar(1).Value
        If aperture Mod 2 = 0 Then aperture += 1
        cv.Cv2.CornerEigenValsAndVecs(gray, dst1, blocksize, aperture, cv.BorderTypes.Default)

        For j = 0 To gray.Rows - 1
            For i = 0 To gray.Cols - 1
                Dim lambda_1 = dst1.Get(Of cv.Vec6f)(j, i)(0)
                Dim lambda_2 = dst1.Get(Of cv.Vec6f)(j, i)(1)
                mc.Set(Of Single)(j, i, lambda_1 * lambda_2 - 0.04 * Math.Pow(lambda_1 + lambda_2, 2))
            Next
        Next

        mc.MinMaxLoc(minval, maxval)

        src.CopyTo(dst1)
        For j = 0 To gray.Rows - 1
            For i = 0 To gray.Cols - 1
                If mc.Get(Of Single)(j, i) > minval + (maxval - minval) * sliders.trackbar(2).Value / sliders.trackbar(2).Maximum Then
                    dst1.Circle(New cv.Point(i, j), 4, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                    dst1.Circle(New cv.Point(i, j), 2, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                End If
            Next
        Next

        Dim McNormal As New cv.Mat
        cv.Cv2.Normalize(mc, McNormal, 127, 255, cv.NormTypes.MinMax)
        McNormal.ConvertTo(dst2, cv.MatType.CV_8U)
    End Sub
End Class




Public Class Corners_SubPix
    Inherits ocvbClass
    Public good As Features_GoodFeatures
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        good = New Features_GoodFeatures(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "SubPix kernel Size", 1, 20, 3)
        label1 = "Output of GoodFeatures"
        desc = "Use PreCornerDetect to find features in the image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        good.src = src
        good.Run(ocvb)
        If good.goodFeatures.Count = 0 Then Exit Sub ' no good features right now...
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim winSize = New cv.Size(sliders.trackbar(0).Value, sliders.trackbar(0).Value)
        cv.Cv2.CornerSubPix(gray, good.goodFeatures, winSize, New cv.Size(-1, -1), term)

        src.CopyTo(dst1)
        Dim p As New cv.Point
        For i = 0 To good.goodFeatures.Count - 1
            p.X = CInt(good.goodFeatures(i).X)
            p.Y = CInt(good.goodFeatures(i).Y)
            cv.Cv2.Circle(dst1, p, 3, New cv.Scalar(0, 0, 255), -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class




Public Class Corners_PreCornerDetect
    Inherits ocvbClass
    Dim median As Math_Median_CDF
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        median = New Math_Median_CDF(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "kernel Size", 1, 20, 19)

        desc = "Use PreCornerDetect to find features in the image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim ksize = sliders.trackbar(0).Value
        If ksize Mod 2 = 0 Then ksize += 1
        Dim prob As New cv.Mat
        cv.Cv2.PreCornerDetect(gray, prob, ksize)

        cv.Cv2.Normalize(prob, prob, 0, 255, cv.NormTypes.MinMax)
        prob.ConvertTo(gray, cv.MatType.CV_8U)
        median.src = gray.Clone()
        median.Run(ocvb)
        dst1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = gray.Threshold(160, 255, cv.ThresholdTypes.BinaryInv).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        label2 = "median = " + CStr(median.medianVal)
    End Sub
End Class



Module corners_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Corners_ShiTomasi(grayPtr As IntPtr, rows As Int32, cols As Int32, blocksize As Int32, aperture As Int32) As IntPtr
    End Function
End Module



' https://docs.opencv.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_ShiTomasi_CPP
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Corner block size", 1, 21, 3)
        sliders.setupTrackBar(1, "Corner aperture size", 1, 21, 3)
        sliders.setupTrackBar(2, "Corner quality level", 1, 100, 50)
        sliders.setupTrackBar(3, "Corner normalize alpha", 1, 255, 127)
        desc = "Find corners using Eigen values and vectors"
        label2 = "Corner Eigen values"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim data(src.Total - 1) As Byte

        Dim blocksize = If(sliders.trackbar(0).Value Mod 2, sliders.trackbar(0).Value, sliders.trackbar(0).Value + 1)
        Dim aperture = If(sliders.trackbar(1).Value Mod 2, sliders.trackbar(1).Value, sliders.trackbar(1).Value + 1)

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Marshal.Copy(dst1.Data, data, 0, data.Length)
        Dim imagePtr = Corners_ShiTomasi(handle.AddrOfPinnedObject, src.Rows, src.Cols, blocksize, aperture)
        handle.Free()

        Dim output As New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32F, imagePtr)

        Dim stNormal As New cv.Mat
        cv.Cv2.Normalize(output, stNormal, sliders.trackbar(3).Value, 255, cv.NormTypes.MinMax)
        stNormal.ConvertTo(dst2, cv.MatType.CV_8U)
    End Sub
End Class