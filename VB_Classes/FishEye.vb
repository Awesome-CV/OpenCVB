
Imports cv = OpenCvSharp
Public Class FishEye_Rectified
    Inherits VBparent
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Dim kMatRight As New cv.Mat, dMatRight As New cv.Mat, rMatRight As New cv.Mat, pMatRight As New cv.Mat
    Dim rightViewMap1 As New cv.Mat, rightViewMap2 As New cv.Mat
    Dim t265Rect As cv.Rect
    Dim t265Original As cv.Rect
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        Dim minDisp = 0
        Dim dispOffset = 112
        Dim numDisp = dispOffset - minDisp
        Dim maxDisp = minDisp + numDisp
        Dim stereo_width_px = 300
        Dim stereo_height_px = 300
        Dim stereo_fov_rad = 90.0 * cv.Cv2.PI / 180.0
        Dim stereo_focal_px = stereo_height_px / 2 / Math.Tan(stereo_fov_rad / 2)
        Dim stereo_cx = CDbl((stereo_height_px - 1.0) / 2.0 + maxDisp)
        Dim stereo_cy = (stereo_height_px - 1.0) / 2.0

        ' very specific to the image from T265 camera.
        t265Rect = New cv.Rect(42, 0, 763, 720) ' the T265 left/right views were clipped to fit in the 1280x720 image that is used throughout opencvb.
        t265Original = New cv.Rect(42, 0, 848, 800) ' the T265 left/right views were clipped to fit in the 1280x720 image that is used throughout opencvb.

        'undistortSetup(ocvb, kMatRight, dMatRight, rMatRight, pMatRight, maxDisp, stereo_height_px, ocvb.parms.intrinsicsRight)

        Dim kright() As Double = {ocvb.parms.intrinsicsRight.fx, 0, ocvb.parms.intrinsicsRight.ppx, 0, ocvb.parms.intrinsicsRight.fy,
                                                          ocvb.parms.intrinsicsRight.ppy, 0, 0, 1}
        Dim dright() As Double = {ocvb.parms.intrinsicsRight.coeffs(0), ocvb.parms.intrinsicsRight.coeffs(1),
                                 ocvb.parms.intrinsicsRight.coeffs(2), ocvb.parms.intrinsicsRight.coeffs(3)}
        Dim pright() As Double = {stereo_focal_px, 0, stereo_cx, 0, 0, stereo_focal_px, stereo_cy, 0, 0, 0, 1, 0}

        Dim r = ocvb.parms.extrinsics.rotation
        Dim rright() As Double = {r(0), r(1), r(2), r(3), r(4), r(5), r(6), r(7), r(8)}
        pright(3) = ocvb.parms.extrinsics.translation(0) * stereo_focal_px

        kMatRight = New cv.Mat(3, 3, cv.MatType.CV_64FC1, kright)
        dMatRight = New cv.Mat(1, 4, cv.MatType.CV_64FC1, dright)
        rMatRight = New cv.Mat(3, 3, cv.MatType.CV_64FC1, rright)
        pMatRight = New cv.Mat(3, 4, cv.MatType.CV_64FC1, pright)

        cv.Cv2.FishEye.InitUndistortRectifyMap(kMatRight, dMatRight, rMatRight, pMatRight, New cv.Size(t265Original.Width, t265Original.Height),
                                               cv.MatType.CV_32FC1, rightViewMap1, rightViewMap2)

        ocvb.desc = "Use OpenCV's FishEye API to undistort a fisheye lens input - needs more work"
        label1 = "Left View"
        label2 = "Right View"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        label1 = "Left View (no fisheye lens present)"
        label2 = "Right View (no fisheye lens present)"
        leftView = ocvb.leftView
        rightView = ocvb.rightView
        dst1 = leftView
        dst2 = rightView
    End Sub
End Class





Public Class FishEye_Raw
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Display the Raw FishEye images for the T265 (only)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        label1 = "Left Fisheye Image"
        label2 = "Right Fisheye Image"
        dst1 = ocvb.leftView
        dst2 = ocvb.rightView
    End Sub
End Class

