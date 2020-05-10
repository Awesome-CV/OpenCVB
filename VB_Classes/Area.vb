Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module MinTriangle_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub MinTriangle_Run(inputPtr As IntPtr, numberOfPoints As Int32, outputTriangle As IntPtr)
    End Sub
End Module

Public Class Area_MinTriangle_CPP
    Inherits ocvbClass
    Dim numberOfPoints As Int32
    Public srcPoints() As cv.Point2f
    Public srcData() As Byte
    Public dstData() As Byte
    Public triangle As cv.Mat
    Private Sub setup(ocvb As AlgorithmData)
        numberOfPoints = sliders.TrackBar1.Value
        ReDim srcPoints(numberOfPoints)
        ReDim srcData(numberOfPoints * Marshal.SizeOf(numberOfPoints) * 2 - 1) ' input is a list of points.
        ReDim dstData(3 * Marshal.SizeOf(numberOfPoints) * 2 - 1) ' minTriangle returns 3 points
    End Sub
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Area Number of Points", 1, 30, 5)
        sliders.setupTrackBar2(ocvb, caller, "Area size", 10, 300, 200)
        setup(ocvb)

        ocvb.desc = "Find minimum containing triangle for a set of points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If numberOfPoints <> sliders.TrackBar1.Value Then setup(ocvb)
        Dim squareWidth = sliders.TrackBar2.Value / 2

        dst = ocvb.color.EmptyClone().SetTo(0)
        For i = 0 To srcPoints.Length - 1
            srcPoints(i).X = ocvb.ms_rng.Next(ocvb.color.Width / 2 - squareWidth, ocvb.color.Width / 2 + squareWidth)
            srcPoints(i).Y = ocvb.ms_rng.Next(ocvb.color.Height / 2 - squareWidth, ocvb.color.Height / 2 + squareWidth)
            dst.Circle(srcPoints(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        Dim input As New cv.Mat(numberOfPoints, 1, cv.MatType.CV_32FC2, srcPoints)
        Marshal.Copy(input.Data, srcData, 0, srcData.Length)
        Dim srcHandle = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim dstHandle = GCHandle.Alloc(dstData, GCHandleType.Pinned)
        MinTriangle_Run(srcHandle.AddrOfPinnedObject(), numberOfPoints, dstHandle.AddrOfPinnedObject)
        srcHandle.Free() ' free the pinned memory...
        dstHandle.Free()
        triangle = New cv.Mat(3, 1, cv.MatType.CV_32FC2, dstData)

        For i = 0 To 2
            Dim p1 = triangle.Get(Of cv.Point2f)(i)
            Dim p2 = triangle.Get(Of cv.Point2f)((i + 1) Mod 3)
            dst.Line(p1, p2, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        Next
		MyBase.Finish(ocvb)
    End Sub
End Class




Public Class Area_MinRect
    Inherits ocvbClass
    Dim numberOfPoints As Int32
    Public srcPoints() As cv.Point2f
    Public minRect As cv.RotatedRect
    Private Sub setup(ocvb As AlgorithmData)
        numberOfPoints = sliders.TrackBar1.Value
        ReDim srcPoints(numberOfPoints)
    End Sub
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Area Number of Points", 1, 200, 5)
        sliders.setupTrackBar2(ocvb, caller, "Area size", 10, 300, 200)
        setup(ocvb)

        ocvb.desc = "Find minimum containing rectangle for a set of points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If numberOfPoints <> sliders.TrackBar1.Value Then setup(ocvb)
        Dim squareWidth = sliders.TrackBar2.Value / 2

        dst = ocvb.color.EmptyClone.SetTo(0)
        For i = 0 To srcPoints.Length - 1
            srcPoints(i).X = ocvb.ms_rng.Next(ocvb.color.Width / 2 - squareWidth, ocvb.color.Width / 2 + squareWidth)
            srcPoints(i).Y = ocvb.ms_rng.Next(ocvb.color.Height / 2 - squareWidth, ocvb.color.Height / 2 + squareWidth)
            dst.Circle(srcPoints(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        minRect = cv.Cv2.MinAreaRect(srcPoints)
        drawRotatedRectangle(minRect, dst, cv.Scalar.Yellow)
        MyBase.Finish(ocvb)
    End Sub
End Class





Public Class Area_MinMotionRect
    Inherits ocvbClass
    Dim input As BGSubtract_MOG
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        input = New BGSubtract_MOG(ocvb, caller)
        input.sliders.TrackBar1.Value = 100 ' low threshold to maximize motion
        ocvb.desc = "Use minRectArea to encompass detected motion"
        ocvb.label1 = "MinRectArea of MOG motion"
    End Sub

    Private Function motionRectangles(gray As cv.Mat, rColors() As cv.Vec3b) As cv.Mat
        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(gray, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        For i = 0 To contours.Length - 1
            Dim minRect = cv.Cv2.MinAreaRect(contours(i))
            Dim nextColor = New cv.Scalar(rColors(i Mod 255).Item0, rColors(i Mod 255).Item1, rColors(i Mod 255).Item2)
            drawRotatedRectangle(minRect, gray, nextColor)
        Next
        Return gray
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        input.src = ocvb.color
        input.Run(ocvb)
        Dim gray As cv.Mat
        If input.dst.Channels = 1 Then gray = input.dst Else gray = input.dst.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst = motionRectangles(gray, ocvb.rColors)
        dst.SetTo(cv.Scalar.All(255), gray)

        gray = ocvb.result2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = motionRectangles(gray, ocvb.rColors)
        dst2.SetTo(cv.Scalar.All(255), gray)
        MyBase.Finish(ocvb)
    End Sub
    Public Sub MyDispose()
        input.Dispose()
    End Sub
End Class







Public Class Area_FindNonZero
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.label1 = "Non-zero original points"
        ocvb.label2 = "Coordinates of non-zero points"
        ocvb.desc = "Use FindNonZero API to get coordinates of non-zero points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
        Dim srcPoints(10 - 1) As cv.Point ' doesn't really matter how many there are.
        For i = 0 To srcPoints.Length - 1
            srcPoints(i).X = ocvb.ms_rng.Next(0, ocvb.color.Width)
            srcPoints(i).Y = ocvb.ms_rng.Next(0, ocvb.color.Height)
            gray.Set(Of Byte)(srcPoints(i).Y, srcPoints(i).X, 255)
        Next

        Dim nonzero = gray.FindNonZero()

        dst = gray.EmptyClone().SetTo(0)
        ' mark the points so they are visible...
        For i = 0 To srcPoints.Length - 1
            dst.Circle(srcPoints(i), 5, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        Dim outstr As String = "Coordinates of the non-zero points (ordered by row - top to bottom): " + vbCrLf + vbCrLf
        For i = 0 To srcPoints.Length - 1
            Dim pt = nonzero.Get(Of cv.Point)(0, i)
            outstr += "X = " + vbTab + CStr(pt.X) + vbTab + " y = " + vbTab + CStr(pt.Y) + vbCrLf
        Next
        ocvb.putText(New ActiveClass.TrueType(outstr, 10, 50, RESULT2))
		MyBase.Finish(ocvb)
    End Sub
End Class
