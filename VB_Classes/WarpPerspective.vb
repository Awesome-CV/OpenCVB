Imports cv = OpenCvSharp

' http://opencvexamples.blogspot.com/
Public Class WarpPerspective_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Warped Width", 0, src.Cols, src.Cols - 50)
        sliders.setupTrackBar(1, "Warped Height", 0, src.Rows, src.Rows - 50)
        sliders.setupTrackBar(2, "Warped Angle", 0, 360, 0)
        task.desc = "Use WarpPerspective to transform input images."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim srcPt() = {New cv.Point2f(0, 0), New cv.Point2f(0, src.Height), New cv.Point2f(src.Width, 0), New cv.Point2f(src.Width, src.Height)}
        Dim pts() = {New cv.Point2f(0, 0), New cv.Point2f(0, src.Height), New cv.Point2f(src.Width, 0), New cv.Point2f(sliders.trackbar(0).Value, sliders.trackbar(1).Value)}

        Dim perpectiveTranx = cv.Cv2.GetPerspectiveTransform(srcPt, pts)
        cv.Cv2.WarpPerspective(src, dst1, perpectiveTranx, New cv.Size(src.Cols, src.Rows), cv.InterpolationFlags.Cubic, cv.BorderTypes.Constant, cv.Scalar.White)

        Dim center = New cv.Point2f(src.Cols / 2, src.Rows / 2)
        Dim angle = sliders.trackbar(2).Value
        Dim rotationMatrix = cv.Cv2.GetRotationMatrix2D(center, angle, 1.0)
        cv.Cv2.WarpAffine(dst1, dst2, rotationMatrix, src.Size(), cv.InterpolationFlags.Nearest)
    End Sub
End Class







Public Class WarpPerspective_3D
    Inherits VBparent
    Dim warp As WarpPerspective_Basics
    Public Sub New()
        initParent()
        warp = New WarpPerspective_Basics()
        task.desc = "Use WarpAffine on a 3D point cloud"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me

    End Sub
End Class


