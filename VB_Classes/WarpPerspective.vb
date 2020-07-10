Imports cv = OpenCvSharp

' http://opencvexamples.blogspot.com/
Public Class WarpPerspective_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller, 3)
        sliders.setupTrackBar(0, "Warped Width", 0, ocvb.color.Cols, ocvb.color.Cols - 50)
        sliders.setupTrackBar(1, "Warped Height", 0, ocvb.color.Rows, ocvb.color.Rows - 50)
        sliders.setupTrackBar(2, "Warped Angle", 0, 360, 0)
        ocvb.desc = "Use WarpPerspective to transform input images."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim srcPt() = {New cv.Point2f(0, 0), New cv.Point2f(0, src.Height), New cv.Point2f(src.Width, 0), New cv.Point2f(src.Width, src.Height)}
        Dim pts() = {New cv.Point2f(0, 0), New cv.Point2f(0, src.Height), New cv.Point2f(src.Width, 0), New cv.Point2f(sliders.sliders(0).Value, sliders.sliders(1).Value)}

        Dim perpectiveTranx = cv.Cv2.GetPerspectiveTransform(srcPt, pts)
        cv.Cv2.WarpPerspective(src, dst1, perpectiveTranx, New cv.Size(src.Cols, src.Rows), cv.InterpolationFlags.Cubic, cv.BorderTypes.Constant, cv.Scalar.White)

        Dim center = New cv.Point2f(src.Cols / 2, src.Rows / 2)
        Dim angle = sliders.sliders(2).Value
        Dim rotationMatrix = cv.Cv2.GetRotationMatrix2D(center, angle, 1.0)
        cv.Cv2.WarpAffine(dst1, dst2, rotationMatrix, src.Size(), cv.InterpolationFlags.Nearest)
    End Sub
End Class

