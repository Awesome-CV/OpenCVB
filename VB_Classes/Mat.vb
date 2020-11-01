Imports cv = OpenCvSharp
Public Class Mat_Repeat
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Use the repeat method to replicate data."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Dim small = src.Resize(New cv.Size(src.Cols / 10, src.Rows / 10))
        dst1 = small.Repeat(10, 10)
        small = ocvb.RGBDepth.Resize(New cv.Size(src.Cols / 10, src.Rows / 10))
        dst2 = small.Repeat(10, 10)
    End Sub
End Class








Public Class Mat_PointToMat
    Inherits VBparent
    Dim mask As Random_Points
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        mask = New Random_Points(ocvb)
        mask.plotPoints = True
        label1 = "Random_Points points (original)"
        label2 = "Random_Points points after format change"
        ocvb.desc = "Convert pointf3 into a mat of points"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        mask.Run(ocvb) ' generates a set of points
        dst1 = mask.dst1
        Dim rows = mask.Points.Length
        Dim pMat = New cv.Mat(rows, 1, cv.MatType.CV_32SC2, mask.Points)
        Dim indexer = pMat.GetGenericIndexer(Of cv.Vec2i)()
        dst2.SetTo(0)
        Dim white = New cv.Vec3b(255, 255, 255)
        For i = 0 To rows - 1
            dst2.Set(Of cv.Vec3b)(indexer(i).Item1, indexer(i).Item0, White)
        Next
    End Sub
End Class






Public Class Mat_MatToPoint
    Inherits VBparent
    Dim mask As Random_Points
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        mask = New Random_Points(ocvb)
        ocvb.desc = "Convert a mat into a vector of points."
        label1 = "Reconstructed RGB Image"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Dim points(src.Total - 1) As cv.Vec3b
        Dim vec As New cv.Vec3b
        Dim index As integer = 0
        Dim m3b = src.Clone()
        Dim indexer = m3b.GetGenericIndexer(Of cv.Vec3b)()
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                vec = indexer(y, x)
                points(index) = New cv.Vec3b(vec.Item0, vec.Item1, vec.Item2)
                index += 1
            Next
        Next
        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, points)
    End Sub
End Class







Public Class Mat_Transpose
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Transpose a Mat and show results."
        label1 = "Color Image Transposed"
        label2 = "Color Image Transposed back (artifacts)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Dim trColor = src.T()
        dst1 = trColor.ToMat.Resize(New cv.Size(src.Cols, src.Rows))
        Dim trBack = dst1.T()
        dst2 = trBack.ToMat.Resize(src.Size())
    End Sub
End Class






' https://csharp.hotexamples.com/examples/OpenCvSharp/Mat/-/php-mat-class-examples.html#0x95f170f4714e3258c220a78eacceeee99591440b9885a2997bbbc6b3aebdcf1c-19,,37,
Public Class Mat_Tricks
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        label1 = "Image squeezed into square Mat"
        label2 = "Mat transposed around the diagonal"
        ocvb.desc = "Show some Mat tricks."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Dim mat = src.Resize(New cv.Size(src.Height, src.Height))
        Dim roi = New cv.Rect(0, 0, mat.Width, mat.Height)
        dst1(roi) = mat
        dst2(roi) = mat.T
    End Sub
End Class




Public Class Mat_4to1
    Inherits VBparent
    Dim mat1 As cv.Mat
    Dim mat2 As cv.Mat
    Dim mat3 As cv.Mat
    Dim mat4 As cv.Mat
    Public mat() As cv.Mat = {mat1, mat2, mat3, mat4}
    Public noLines As Boolean ' if they want lines or not...
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        mat1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, 0)
        mat2 = mat1.Clone()
        mat3 = mat1.Clone()
        mat4 = mat1.Clone()
        mat = {mat1, mat2, mat3, mat4}

        label1 = "Combining 4 images into one"
        label2 = "Click any quadrant at left to view it below"
        ocvb.desc = "Use one Mat for up to 4 images"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Static nSize = New cv.Size(src.Width / 2, src.Height / 2)
        Static roiTopLeft = New cv.Rect(0, 0, nSize.Width, nSize.Height)
        Static roiTopRight = New cv.Rect(nSize.Width, 0, nSize.Width, nSize.Height)
        Static roibotLeft = New cv.Rect(0, nSize.Height, nSize.Width, nSize.Height)
        Static roibotRight = New cv.Rect(nSize.Width, nSize.Height, nSize.Width, nSize.Height)
        If standalone Then
            mat1 = src
            mat2 = ocvb.RGBDepth
            mat3 = ocvb.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            mat4 = ocvb.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            mat = {mat1, mat2, mat3, mat4}
        End If
        For i = 0 To 4 - 1
            Dim tmp = mat(i).Clone
            If tmp.Channels <> dst1.Channels Then tmp = mat(i).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            Dim roi = Choose(i + 1, roiTopLeft, roiTopRight, roibotLeft, roibotRight)
            dst1(roi) = tmp.Resize(nSize)
        Next
        If noLines = False Then
            dst1.Line(New cv.Point(0, dst1.Height / 2), New cv.Point(dst1.Width, dst1.Height / 2), cv.Scalar.White, 2)
            dst1.Line(New cv.Point(dst1.Width / 2, 0), New cv.Point(dst1.Width / 2, dst1.Height), cv.Scalar.White, 2)
        End If
        dst2 = mat(ocvb.quadrantIndex)
    End Sub
End Class








Public Class Mat_2to1
    Inherits VBparent
    Dim mat1 As cv.Mat
    Dim mat2 As cv.Mat
    Public mat() = {mat1, mat2}
    Public noLines As Boolean ' if they want lines or not...
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        mat1 = New cv.Mat(New cv.Size(src.Rows, src.Cols), cv.MatType.CV_8UC3, 0)
        mat2 = mat1.Clone()
        mat = {mat1, mat2}
        dst1 = dst2

        label1 = ""
        ocvb.desc = "Fill a Mat with 2 images"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Static nSize = New cv.Size(src.Width, src.Height / 2)
        Static roiTop = New cv.Rect(0, 0, nSize.Width, nSize.Height)
        Static roibot = New cv.Rect(0, nSize.Height, nSize.Width, nSize.Height)
        If standalone Then
            mat1 = src
            mat2 = ocvb.RGBDepth
            mat = {mat1, mat2}
        End If
        dst1.SetTo(0)
        If dst1.Type <> mat(0).Type Then dst1 = New cv.Mat(src.Size(), mat(0).type)
        For i = 0 To 1
            Dim roi = Choose(i + 1, roiTop, roibot)
            If mat(i).Empty = False Then dst1(roi) = mat(i).Resize(nSize)
        Next
        If noLines = False Then
            dst1.Line(New cv.Point(0, dst1.Height / 2), New cv.Point(dst1.Width, dst1.Height / 2), cv.Scalar.White, 2)
        End If
    End Sub
End Class








Public Class Mat_ImageXYZ_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Public xyDepth As cv.Mat
    Public xyzPlanes() As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        grid = New Thread_Grid(ocvb)
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 32
        gridHeightSlider.Value = 32

        xyDepth = New cv.Mat(src.Size(), cv.MatType.CV_32FC3, 0)
        Dim xyz As New cv.Point3f
        For xyz.Y = 0 To xyDepth.Height - 1
            For xyz.X = 0 To xyDepth.Width - 1
                xyDepth.Set(Of cv.Point3f)(xyz.Y, xyz.X, xyz)
            Next
        Next
        cv.Cv2.Split(xyDepth, xyzPlanes)

        ocvb.desc = "Create a cv.Point3f vector with x, y, and z."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        grid.Run(ocvb)
        Dim depth32f = getDepth32f(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
          Sub(roi)
              xyzPlanes(2)(roi) = depth32f(roi)
          End Sub)

        cv.Cv2.Merge(xyzPlanes, xyDepth)
        If standalone Then ocvb.trueText("Mat built with X, Y, and Z (Depth)", 10, 125)
    End Sub
End Class





' https://csharp.hotexamples.com/examples/OpenCvSharp/MatExpr/-/php-matexpr-class-examples.html
' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/MatOperations.cs
Public Class Mat_RowColRange
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        label1 = "BitwiseNot of RowRange and ColRange"
        ocvb.desc = "Perform operation on a range of cols and/or Rows."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Dim midX = src.Width / 2
        Dim midY = src.Height / 2
        dst1 = src
        cv.Cv2.BitwiseNot(dst1.RowRange(midY - 50, midY + 50), dst1.RowRange(midY - 50, midY + 50))
        cv.Cv2.BitwiseNot(dst1.ColRange(midX - 50, midX + 50), dst1.ColRange(midX - 50, midX + 50))
    End Sub
End Class





Public Class Mat_Managed
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        label1 = "Color change is in the managed cv.vec3b array"
        ocvb.desc = "There is a limited ability to use Mat data in Managed code directly."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Static autoRand As New Random()
        Static img(src.Total) As cv.Vec3b
        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, img)
        Static nextColor As cv.Vec3b
        If ocvb.frameCount Mod 30 = 0 Then
            If nextColor = New cv.Vec3b(0, 0, 255) Then nextColor = New cv.Vec3b(0, 255, 0) Else nextColor = New cv.Vec3b(0, 0, 255)
        End If
        For i = 0 To img.Length - 1
            img(i) = nextColor
        Next
        Dim rect As New cv.Rect(autoRand.Next(0, src.Width - 50), autoRand.Next(0, src.Height - 50), 50, 50)
        dst1(rect).SetTo(0)
    End Sub
End Class






Public Class Mat_MultiplyReview
    Inherits VBparent
    Dim flow As Font_FlowText
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        flow = New Font_FlowText(ocvb)
        ocvb.desc = "Review matrix multiplication"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Dim a(,) = {{1, 4, 2}, {2, 5, 1}}
        Dim b(,) = {{3, 4, 2}, {3, 5, 7}, {1, 2, 1}}
        Dim nextLine = ""
        flow.msgs.Add("Matrix a")
        For i = 0 To a.GetLength(0) - 1
            nextLine = ""
            For j = 0 To a.GetLength(1) - 1
                nextLine += CStr(a(i, j)) + vbTab
            Next
            flow.msgs.Add(nextLine)
        Next

        flow.msgs.Add("Matrix b")
        For i = 0 To b.GetLength(0) - 1
            nextLine = ""
            For j = 0 To b.GetLength(1) - 1
                nextLine += CStr(b(i, j)) + vbTab
            Next
            flow.msgs.Add(nextLine)
        Next

        Dim c(a.GetLength(0) - 1, a.GetLength(1) - 1) As Integer
        Dim input(a.GetLength(0) - 1, a.GetLength(1) - 1) As String
        For i = 0 To c.GetLength(0) - 1
            For j = 0 To c.GetLength(1) - 1
                input(i, j) = ""
                For k = 0 To c.GetLength(1) - 1
                    c(i, j) += a(i, k) * b(k, j)
                    input(i, j) += CStr(a(i, k)) + "*" + CStr(b(k, j)) + If(k < c.GetLength(1) - 1, " + ", vbTab)
                Next
            Next
        Next


        flow.msgs.Add("Matrix c = a X b")
        For i = 0 To a.GetLength(0) - 1
            nextLine = ""
            For j = 0 To a.GetLength(1) - 1
                nextLine += CStr(c(i, j)) + " = " + input(i, j)
            Next
            flow.msgs.Add(nextLine)
        Next

        flow.Run(ocvb)
    End Sub
End Class






' https://stackoverflow.com/questions/11015119/inverse-matrix-opencv-matrix-inv-not-working-properly
Public Class Mat_Inverse
    Inherits VBparent
    Dim flow As Font_FlowText
    Public matrix(,) As Single = {{1.1688, 0.23, 62.2}, {-0.013, 1.225, -6.29}, {0, 0, 1}}
    Public validateInverse As Boolean
    Public inverse As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        flow = New Font_FlowText(ocvb)
        radio.Setup(ocvb, caller, 6)
        radio.check(0).Text = "Cholesky"
        radio.check(1).Text = "Eig (works but results are incorrect)"
        radio.check(2).Text = "LU"
        radio.check(3).Text = "Normal (not working)"
        radio.check(4).Text = "QR (not working)"
        radio.check(5).Text = "SVD (works but results are incorrect)"
        radio.check(0).Checked = True
        radio.check(3).Enabled = False ' not accepted!
        radio.check(4).Enabled = False ' not accepted!

        ocvb.desc = "Given a 3x3 matrix, invert it and present results."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Dim nextline = ""

        Dim decompType = cv.DecompTypes.Cholesky
        Static frm = findForm("Mat_Inverse Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                decompType = Choose(i + 1, cv.DecompTypes.Cholesky, cv.DecompTypes.Eig, cv.DecompTypes.LU, cv.DecompTypes.Normal,
                                         cv.DecompTypes.QR, cv.DecompTypes.SVD)
            End If
        Next

        If standalone Or validateInverse Then
            flow.msgs.Add("Matrix Input")
            For i = 0 To matrix.GetLength(0) - 1
                nextline = ""
                For j = 0 To matrix.GetLength(1) - 1
                    nextline += CStr(matrix(i, j)) + vbTab
                Next
                flow.msgs.Add(nextline)
            Next
        End If

        Dim input = New cv.Mat(3, 3, cv.MatType.CV_32F, matrix)
        cv.Cv2.Invert(input, inverse, decompType)

        If standalone Or validateInverse Then
            flow.msgs.Add("Matrix Inverse")
            For i = 0 To matrix.GetLength(0) - 1
                nextline = ""
                For j = 0 To matrix.GetLength(1) - 1
                    nextline += CStr(inverse.Get(Of Single)(j, i)) + vbTab
                Next
                flow.msgs.Add(nextline)
            Next

            Dim identity = (input * inverse).ToMat

            flow.msgs.Add("Verify Inverse is correct")
            For i = 0 To matrix.GetLength(0) - 1
                nextline = ""
                For j = 0 To matrix.GetLength(1) - 1
                    nextline += CStr(identity.Get(Of Single)(j, i)) + vbTab
                Next
                flow.msgs.Add(nextline)
            Next
        End If

        flow.Run(ocvb)
    End Sub
End Class
