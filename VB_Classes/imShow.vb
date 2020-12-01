Imports cv = OpenCvSharp
Public Class imShow_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
    End Sub
    Public Sub Run()
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        cv.Cv2.ImShow("color", src)
    End Sub
End Class







Public Class imShow_WaitKey
    Inherits VBparent
    Dim vDemo As Voronoi_Basics
    Public Sub New()
        initParent()
        vDemo = New Voronoi_Basics()

        task.desc = "You can use the HighGUI WaitKey call to pause an algorithm and review output one frame at a time."
    End Sub
    Public Sub Run()
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        vDemo.Run()
        cv.Cv2.ImShow("Hit space bar to advance to the next frame", vDemo.dst1)
        cv.Cv2.WaitKey(1000) ' It will halt the test all run if 0 but 0 is the useful value for debugging interactively.
        dst1 = vDemo.dst1
    End Sub
End Class
