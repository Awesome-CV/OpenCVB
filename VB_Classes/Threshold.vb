Imports cv = OpenCvSharp
Public Class Threshold_LaplacianFilter
    Inherits VBparent
    Dim edges As Filter_Laplacian
    Dim inrange As Depth_InRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        inrange = New Depth_InRange(ocvb)
        inrange.depth32fAfterMasking = True

        edges = New Filter_Laplacian(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "dist Threshold", 1, 100, 40)
        label1 = "Foreground Input"
        ocvb.desc = "Threshold the output of a Laplacian derivative, mask with depth foreground.  needs more work"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        edges.src = src
        edges.Run(ocvb)
        dst2 = edges.dst2
        inrange.src = getDepth32f(ocvb)
        inrange.Run(ocvb)
        dst1 = inrange.dst1

        Dim mask = dst1.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)
        dst2.SetTo(0, mask)
    End Sub
End Class

