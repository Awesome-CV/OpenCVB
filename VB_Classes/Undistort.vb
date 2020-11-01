Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module undistort_Mats
    Public Sub undistortSetup(ocvb As VBocvb, ByRef kMatLeft As cv.Mat, ByRef dMatLeft As cv.Mat, ByRef rMatLeft As cv.Mat, ByRef pMatLeft As cv.Mat,
                       maxDisp As integer, stereo_height_px As integer, intrinsics As ActiveTask.intrinsics_VB)
        Dim kLeft(8) As Double
        Dim rLeft(8) As Double
        Dim dLeft(4) As Double
        Dim pLeft(11) As Double

        kLeft = {intrinsics.fx, 0, intrinsics.ppx, 0,
                 intrinsics.fy, intrinsics.ppy, 0, 0, 1}
        dLeft = {intrinsics.coeffs(0), intrinsics.coeffs(1),
                 intrinsics.coeffs(2), intrinsics.coeffs(3)}

        ' We need To determine what focal length our undistorted images should have
        ' In order To Set up the camera matrices For initUndistortRectifyMap.  We
        ' could use stereoRectify, but here we show how To derive these projection
        ' matrices from the calibration And a desired height And field Of view
        ' We calculate the undistorted focal length:
        '
        '         h
        ' -----------------
        '  \      |      /
        '    \    | f  /
        '     \   |   /
        '      \ fov /
        '        \|/
        Dim stereo_fov_rad = CDbl(90 * (Math.PI / 180))  ' 90 degree desired fov
        Dim stereo_focal_px = CDbl(stereo_height_px / 2 / Math.Tan(stereo_fov_rad / 2))

        ' The stereo algorithm needs max_disp extra pixels In order To produce valid
        ' disparity On the desired output region. This changes the width, but the
        ' center Of projection should be On the center Of the cropped image
        Dim stereo_width_px = stereo_height_px + maxDisp
        Dim stereo_cx = (stereo_height_px - 1) / 2 + maxDisp
        Dim stereo_cy = (stereo_height_px - 1) / 2

        ' Construct the left And right projection matrices, the only difference Is
        ' that the right projection matrix should have a shift along the x axis Of
        ' baseline*focal_length
        pLeft = {stereo_focal_px, 0, stereo_cx, 0, 0, stereo_focal_px, stereo_cy, 0, 0, 0, 1, 0}

        kMatLeft = New cv.Mat(3, 3, cv.MatType.CV_64F, kLeft)
        dMatLeft = New cv.Mat(1, 4, cv.MatType.CV_64F, dLeft)

        rMatLeft = cv.Mat.Eye(3, 3, cv.MatType.CV_64F).ToMat() ' We Set the left rotation to identity
        pMatLeft = New cv.Mat(3, 4, cv.MatType.CV_64F, pLeft)
    End Sub
End Module







' https://stackoverflow.com/questions/26602981/correct-barrel-distortion-in-opencv-manually-without-chessboard-image
Public Class Undistort_Basics
    Inherits VBparent
    Dim leftViewMap1 As New cv.Mat
    Dim leftViewMap2 As New cv.Mat
    Dim saveK As integer, saveD As integer, saveR As integer, saveP As integer
    Dim maxDisp As integer
    Dim stereo_cx As integer
    Dim stereo_cy As integer
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "undistort intrinsics Left", 1, 200, 100)

        sliders.setupTrackBar(1, "undistort intrinsics coeff's", -1000, 1000, 100)
        sliders.setupTrackBar(2, "undistort stereo height", 1, src.Rows, src.Rows)
        sliders.setupTrackBar(3, "undistort Offset left/right", 1, 200, 112)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Restore Original matrices"
        check.Box(0).Checked = True

        label1 = "Left Image with sliders applied"
        ocvb.desc = "Use sliders to control the undistort OpenCV API - Painterly"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Static kMatLeft As cv.Mat, dMatLeft As cv.Mat, rMatLeft As cv.Mat, pMatLeft As cv.Mat
        Static kMat As cv.Mat, dMat As cv.Mat
        Dim rawWidth = ocvb.leftView.Width
        Dim rawHeight = ocvb.leftView.Height
        If check.Box(0).Checked Then
            check.Box(0).Checked = False

            sliders.trackbar(0).Value = 100
            sliders.trackbar(1).Value = 100

            maxDisp = sliders.trackbar(3).Value
            Dim stereo_height_px = sliders.trackbar(2).Value
            undistortSetup(ocvb, kMatLeft, dMatLeft, rMatLeft, pMatLeft, maxDisp, stereo_height_px, ocvb.parms.intrinsicsLeft)

            ' the intrinsic coeff's on the Intel RS2 series are always zero.  Here we just make up some numbers so we can show the impact.
            If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D435i Then
                Dim d() As Double = {0.5, -2, 1.5, 0.5}
                dMatLeft = New cv.Mat(1, 4, cv.MatType.CV_64F, d)
            End If
        End If
        If saveK <> sliders.trackbar(0).Value Then
            saveK = sliders.trackbar(0).Value
            kMat = kMatLeft * sliders.trackbar(0).Value / 100
        End If
        If saveD <> sliders.trackbar(1).Value Then
            saveD = sliders.trackbar(1).Value
            dMat = dMatLeft * sliders.trackbar(1).Value / 100
        End If
        If saveP <> sliders.trackbar(3).Value Or saveR <> sliders.trackbar(2).Value Then
            saveP = sliders.trackbar(3).Value
            maxDisp = saveP
            saveR = sliders.trackbar(2).Value
            Dim stereo_height_px = saveR ' heightXheight pixel stereo output
            Dim stereo_fov_rad = CDbl(90 * (Math.PI / 180))  ' 90 degree desired fov
            Dim stereo_focal_px = CDbl(stereo_height_px / 2 / Math.Tan(stereo_fov_rad / 2))
            stereo_cx = (stereo_height_px - 1) / 2 + maxDisp
            stereo_cy = (stereo_height_px - 1) / 2
            Dim pLeft = {stereo_focal_px, 0, stereo_cx, 0, 0, stereo_focal_px, stereo_cy, 0, 0, 0, 1, 0}
            pMatLeft = New cv.Mat(3, 4, cv.MatType.CV_64F, pLeft)
        End If

        cv.Cv2.FishEye.InitUndistortRectifyMap(kMat, dMat, rMatLeft, pMatLeft, New cv.Size(rawWidth, rawHeight),
                                               cv.MatType.CV_32FC1, leftViewMap1, leftViewMap2)
        dst1 = ocvb.leftView.Remap(leftViewMap1, leftViewMap2, cv.InterpolationFlags.Linear).Resize(src.Size())
        dst2 = src.Remap(leftViewMap1, leftViewMap2, cv.InterpolationFlags.Linear).Resize(src.Size())
    End Sub
End Class

