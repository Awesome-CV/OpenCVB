Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://docs.opencv.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_Harris
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Corner block size", 1, 21, 3)
        sliders.setupTrackBar2(ocvb, caller, "Corner aperture size", 1, 21, 3)
        sliders.setupTrackBar3(ocvb, caller, "Corner quality level", 1, 100, 50)
        ocvb.desc = "Find corners using Eigen values and vectors"
        ocvb.label2 = "Corner Eigen values"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static color As New cv.Mat
        Static gray As New cv.Mat
        Static mc As New cv.Mat
        Static minval As Double, maxval As Double

        If ocvb.frameCount Mod 30 = 0 Then
            color = ocvb.color.Clone()
            gray = color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            mc = New cv.Mat(gray.Size(), cv.MatType.CV_32FC1, 0)
            dst1 = New cv.Mat(gray.Size(), cv.MatType.CV_8U, 0)
            Dim blocksize = sliders.TrackBar1.Value
            If blocksize Mod 2 = 0 Then blocksize += 1
            Dim aperture = sliders.TrackBar2.Value
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
        End If

        color.CopyTo(dst1)
        For j = 0 To gray.Rows - 1
            For i = 0 To gray.Cols - 1
                If mc.Get(Of Single)(j, i) > minval + (maxval - minval) * sliders.TrackBar3.Value / sliders.TrackBar3.Maximum Then
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
    Dim good As Features_GoodFeatures
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        good = New Features_GoodFeatures(ocvb, caller)
        sliders.setupTrackBar1(ocvb, caller, "SubPix kernel Size", 1, 20, 3)

        ocvb.desc = "Use PreCornerDetect to find features in the image."
        ocvb.label1 = "Output of GoodFeatures"
        ocvb.label2 = "Refined good features"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        good.Run(ocvb)
        If good.goodFeatures.Count = 0 Then Exit Sub ' no good features right now...
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim winSize = New cv.Size(sliders.TrackBar1.Value, sliders.TrackBar1.Value)
        cv.Cv2.CornerSubPix(gray, good.goodFeatures, winSize, New cv.Size(-1, -1), term)

        ocvb.color.CopyTo(dst2)
        Dim p As New cv.Point
        For i = 0 To good.goodFeatures.Count - 1
            p.X = CInt(good.goodFeatures(i).X)
            p.Y = CInt(good.goodFeatures(i).Y)
            cv.Cv2.Circle(dst2, p, 3, New cv.Scalar(0, 0, 255), -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class




Public Class Corners_PreCornerDetect
    Inherits ocvbClass
    Dim median As Math_Median_CDF
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        median = New Math_Median_CDF(ocvb, caller)
        sliders.setupTrackBar1(ocvb, caller, "kernel Size", 1, 20, 19)

        ocvb.desc = "Use PreCornerDetect to find features in the image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim ksize = sliders.TrackBar1.Value
        If ksize Mod 2 = 0 Then ksize += 1
        Dim prob As New cv.Mat
        cv.Cv2.PreCornerDetect(gray, prob, ksize)

        cv.Cv2.Normalize(prob, prob, 0, 255, cv.NormTypes.MinMax)
        prob.ConvertTo(gray, cv.MatType.CV_8U)
        median.src = gray.Clone()
        median.Run(ocvb)
        dst1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = gray.Threshold(160, 255, cv.ThresholdTypes.BinaryInv).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.label2 = "median = " + CStr(median.medianVal)
    End Sub
End Class



Module corners_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Corners_ShiTomasi(grayPtr As IntPtr, dstPtr As IntPtr, rows As Int32, cols As Int32, blocksize As Int32, aperture As Int32)
    End Sub
End Module



' https://docs.opencv.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_ShiTomasi_CPP
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Corner block size", 1, 21, 3)
        sliders.setupTrackBar2(ocvb, caller, "Corner aperture size", 1, 21, 3)
        sliders.setupTrackBar3(ocvb, caller, "Corner quality level", 1, 100, 50)
        ocvb.desc = "Find corners using Eigen values and vectors"
        ocvb.label2 = "Corner Eigen values"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static color As New cv.Mat
        Static minval As Double, maxval As Double
        Dim crows = ocvb.color.Rows, ccols = ocvb.color.Cols
        Dim data(crows * ccols) As Byte
        Static data32f(crows * ccols) As Single
        Static output As New cv.Mat(crows, ccols, cv.MatType.CV_32FC1, data32f)

        If ocvb.frameCount Mod 30 = 0 Then
            Dim blocksize = sliders.TrackBar1.Value
            If blocksize Mod 2 = 0 Then blocksize += 1
            Dim aperture = sliders.TrackBar2.Value
            If aperture Mod 2 = 0 Then aperture += 1

            color = ocvb.color.Clone()
            Dim gray = color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
            Dim handle32f = GCHandle.Alloc(data32f, GCHandleType.Pinned)
            Dim tmp As New cv.Mat(crows, ccols, cv.MatType.CV_8U, data)
            gray.CopyTo(tmp)
            Corners_ShiTomasi(tmp.Data, output.Data, crows, ccols, blocksize, aperture)
            handle.Free()
            handle32f.Free()

            output.MinMaxLoc(minval, maxval)
        End If

        color.CopyTo(output)
        For j = 0 To crows - 1
            For i = 0 To ccols - 1
                If output.Get(Of Single)(j, i) > minval + (maxval - minval) * sliders.TrackBar3.Value / sliders.TrackBar3.Maximum Then
                    output.Circle(New cv.Point(i, j), 4, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                    output.Circle(New cv.Point(i, j), 2, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                End If
            Next
        Next

        Dim stNormal As New cv.Mat
        cv.Cv2.Normalize(output, stNormal, 127, 255, cv.NormTypes.MinMax)
        stNormal.ConvertTo(dst2, cv.MatType.CV_8U)
    End Sub
End Class
