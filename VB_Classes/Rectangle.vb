﻿Imports cv = OpenCvSharp
Public Class Rectangle_Basics
    Inherits VBparent
    Public updateFrequency = 30
    Public rectangles As New List(Of cv.Rect)
    Public rotatedRectangles As New List(Of cv.RotatedRect)
    Public Sub New()
        initParent()

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Draw Rotated Rectangles (unchecked will draw rectangles)"
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Rectangle Count", 1, 255, 3)
        End If

        task.desc = "Draw the requested number of rectangles."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static typeCheckBox = findCheckBox("Draw Rotated Rectangles (unchecked will draw rectangles)")
        Static countSlider = findSlider("Rectangle Count")
        Static saveType = typeCheckBox.Checked
        If ocvb.frameCount Mod updateFrequency = 0 Or saveType <> typeCheckBox.checked Then
            saveType = typeCheckBox.checked
            dst1.SetTo(cv.Scalar.Black)
            rectangles.Clear()
            rotatedRectangles.Clear()
            Dim rCount = countSlider.Value
            For i = 0 To rCount - 1
                ' Dim nPoint = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim nPoint = New cv.Point2f(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))
                Dim width = msRNG.Next(0, src.Cols - nPoint.X - 1)
                Dim height = msRNG.Next(0, src.Rows - nPoint.Y - 1)
                Dim eSize = New cv.Size2f(CSng(msRNG.Next(0, src.Cols - nPoint.X - 1)), CSng(msRNG.Next(0, src.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(msRNG.Next(0, 1000) / 1000.0F)

                Dim nextColor = New cv.Scalar(ocvb.vecColors(i).Item0, ocvb.vecColors(i).Item1, ocvb.vecColors(i).Item2)
                If typeCheckBox.Checked Then
                    Dim r = New cv.RotatedRect(nPoint, eSize, angle)
                    drawRotatedRectangle(r, dst1, nextColor)
                    rotatedRectangles.Add(r)
                Else
                    Dim r = New cv.Rect(nPoint.X, nPoint.Y, width, height)
                    cv.Cv2.Rectangle(dst1, r, nextColor, -1)
                    rectangles.Add(r)
                End If
            Next
        End If
    End Sub
End Class




Public Class Rectangle_Rotated
    Inherits VBparent
    Public rect As Rectangle_Basics
    Public Sub New()
        initParent()
        rect = New Rectangle_Basics
        Dim rotatedCheck = findCheckBox("Draw Rotated Rectangles (unchecked will draw rectangles)")
        rotatedCheck.Checked = True
        task.desc = "Draw the requested number of rectangles."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        rect.src = src
        rect.Run()
        dst1 = rect.dst1
    End Sub
End Class








Public Class Rectangle_CComp
    Inherits VBparent
    Dim ccomp As CComp_Basics_FullImage
    Dim rMotion As Rectangle_Motion
    Public Sub New()
        initParent()
        rMotion = New Rectangle_Motion
        ccomp = New CComp_Basics_FullImage

        label2 = "Connected component features isolated by rect's"
        task.desc = "Isolate rectanguler regions around connected components"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        ccomp.src = src
        ccomp.Run()
        dst1 = ccomp.dst1.Clone

        If ocvb.frameCount Mod 2 = 0 Then rMotion.src = ccomp.dst1.Clone Else rMotion.src = New cv.Mat(ccomp.dst1.Size, cv.MatType.CV_8UC1, 0)
        rMotion.Run()
        If ocvb.frameCount Mod 2 = 0 Then
            dst2 = task.color
            For Each r In rMotion.mOverlap.enclosingRects
                dst2.Rectangle(r, cv.Scalar.Yellow, 2)
            Next
        End If
    End Sub
End Class








Public Class Rectangle_Overlap
    Inherits VBparent
    Public rect1 As cv.Rect
    Public rect2 As cv.Rect
    Public enclosingRect As cv.Rect
    Dim draw As Rectangle_Basics
    Public Sub New()
        initParent()

        draw = New Rectangle_Basics
        Dim countSlider = findSlider("Rectangle Count")
        countSlider.Value = 2

        task.desc = "Test if 2 rectangles overlap"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Or task.intermediateReview = caller Then
            draw.Run()
            dst1 = draw.dst1
        End If

        dst2.SetTo(0)
        Static typeCheckBox = findCheckBox("Draw Rotated Rectangles (unchecked will draw rectangles)")
        If typeCheckBox.Checked Then
            Dim r1 As cv.RotatedRect = draw.rotatedRectangles(0)
            Dim r2 As cv.RotatedRect = draw.rotatedRectangles(1)
            rect1 = r1.BoundingRect
            rect2 = r2.BoundingRect
            drawRotatedOutline(r1, dst2, cv.Scalar.Yellow)
            drawRotatedOutline(r2, dst2, cv.Scalar.Yellow)
        Else
            rect1 = draw.rectangles(0)
            rect2 = draw.rectangles(1)
        End If

        enclosingRect = New cv.Rect
        If rect1.IntersectsWith(rect2) Then
            enclosingRect = rect1.Union(rect2)
            dst2.Rectangle(enclosingRect, cv.Scalar.White, 4)
            label2 = "Rectangles intersect - red marks overlapping rectangle"
            dst2.Rectangle(rect1.Intersect(rect2), cv.Scalar.Red, -1)
        Else
            label2 = "Rectangles don't intersect"
        End If
        dst2.Rectangle(rect1, cv.Scalar.Yellow, 2)
        dst2.Rectangle(rect2, cv.Scalar.Yellow, 2)
    End Sub
End Class






Public Class Rectangle_Motion
    Inherits VBparent
    Public motion As Motion_Basics
    Public mOverlap As Rectangle_Intersection
    Public Sub New()
        initParent()
        motion = New Motion_Basics
        mOverlap = New Rectangle_Intersection
        label1 = "Yellow is pixel motion.  Red is all pixel motion"
        task.desc = "Motion rectangles often overlap.  This algorithm consolidates those rectangles in the RGB image."
    End Sub

    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        motion.src = src
        motion.Run()
        dst1 = motion.dst1.Clone
    End Sub
End Class






Public Class Rectangle_MotionDepth
    Inherits VBparent
    Public motion As Motion_Basics
    Dim colorize As Depth_ColorizerFastFade_CPP
    Public Sub New()
        initParent()
        colorize = New Depth_ColorizerFastFade_CPP
        motion = New Motion_Basics
        label1 = "Rectangles from contours of motion (unconsolidated)"
        label2 = "Pixel differences from motion (everything!)"
        task.desc = "Motion rectangles often overlap.  This algorithm consolidates those rectangles in the depth image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static lastDepth = task.depth32f
        cv.Cv2.Min(task.depth32f, lastDepth, motion.src)

        motion.Run()
        dst2 = motion.dst2
        If motion.resetAll Then
            lastDepth = task.depth32f
            colorize.src = task.depth32f
        Else
            colorize.src = motion.src.Clone
            lastDepth = motion.src
        End If
        colorize.Run()
        dst1 = colorize.dst1
    End Sub
End Class








Public Class Rectangle_Intersection
    Inherits VBparent
    Public rect1 As cv.Rect
    Public rect2 As cv.Rect
    Public inputRects As New List(Of cv.Rect)
    Dim draw As Rectangle_Basics
    Public enclosingRects As New List(Of cv.Rect)
    Dim otherRects As New List(Of cv.Rect)
    Public Sub New()
        initParent()

        task.desc = "Test if any number of rectangles overlap."
    End Sub
    Private Function findEnclosingRect(rects As List(Of cv.Rect)) As cv.Rect
        Dim enclosing = rects(0)
        Dim newOther As New List(Of cv.Rect)
        For i = 1 To rects.Count - 1
            Dim r1 = rects(i)
            If enclosing.IntersectsWith(r1) Then
                enclosing = enclosing.Union(r1)
            Else
                newOther.Add(r1)
            End If
        Next
        otherRects = New List(Of cv.Rect)(newOther)
        Return enclosing
    End Function
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Or task.intermediateReview = caller Then
            If draw Is Nothing Then draw = New Rectangle_Basics

            Static rotatedCheck = findCheckBox("Draw Rotated Rectangles (unchecked will draw rectangles)")
            rotatedCheck.Enabled = False

            Static countSlider = findSlider("Rectangle Count")
            countSlider.Value = msRNG.Next(2, 10)

            label1 = "Input rectangles = " + CStr(countSlider.value)

            draw.Run()
            dst1 = draw.dst1
            inputRects = New List(Of cv.Rect)(draw.rectangles)
        Else
            For Each r In inputRects
                dst1.Rectangle(r, cv.Scalar.Yellow, 1)
            Next
        End If

        Dim sortedRect As New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
        For Each r In inputRects
            sortedRect.Add(r.Width * r.Height, r)
        Next

        otherRects.Clear()
        For Each r In sortedRect
            otherRects.Add(r.Value)
        Next

        label2 = CStr(enclosingRects.Count) + " enclosing rectangles were found"
        enclosingRects.Clear()
        While otherRects.Count
            Dim enclosing = findEnclosingRect(otherRects)
            enclosingRects.Add(enclosing)
        End While

        dst2.SetTo(0)
        For Each r In enclosingRects
            dst2.Rectangle(r, cv.Scalar.Yellow, 2)
        Next
    End Sub
End Class








Public Class Rectangle_Union
    Inherits VBparent
    Dim draw As Rectangle_Basics
    Public inputRects As New List(Of cv.Rect)
    Public allRect As cv.Rect ' a rectangle covering all the input
    Public Sub New()
        initParent()
        task.desc = "Create a rectangle that contains all the input rectangles"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Or task.intermediateReview = caller Then
            If draw Is Nothing Then draw = New Rectangle_Basics

            Static rotatedCheck = findCheckBox("Draw Rotated Rectangles (unchecked will draw rectangles)")
            rotatedCheck.Enabled = False

            Static countSlider = findSlider("Rectangle Count")
            countSlider.Value = msRNG.Next(2, 10)

            label1 = "Input rectangles = " + CStr(draw.rectangles.Count)

            draw.Run()
            dst1 = draw.dst1
            inputRects = New List(Of cv.Rect)(draw.rectangles)
        Else
            dst1.SetTo(0)
            For Each r In inputRects
                dst1.Rectangle(r, cv.Scalar.Yellow, 1)
            Next
            label1 = "Input rectangles = " + CStr(inputRects.Count)
        End If

        If inputRects.Count = 0 Then Exit Sub
        allRect = inputRects(0)
        For i = 1 To inputRects.Count - 1
            Dim r = inputRects(i)
            If r.X < 0 Then r.X = 0
            If r.Y < 0 Then r.Y = 0
            If allRect.Width > 0 And allRect.Height > 0 Then
                allRect = r.Union(allRect)
                If allRect.X + allRect.Width >= dst1.Width Then allRect.Width = dst1.Width - allRect.X
                If allRect.Height >= dst1.Height Then allRect.Height = dst1.Height - allRect.Y
            End If
        Next
        If allRect.X + allRect.Width >= dst1.Width Then allRect.Width = dst1.Width - allRect.X
        If allRect.Y + allRect.Height >= dst1.Height Then allRect.Height = dst1.Height - allRect.Y
        dst1.Rectangle(allRect, cv.Scalar.Red, 2)
    End Sub
End Class








Public Class Rectangle_MultiOverlap
    Inherits VBparent
    Public inputRects As New List(Of cv.Rect)
    Public outputRects As New List(Of cv.Rect)
    Public Sub New()
        initParent()
        task.desc = "Given a group of rectangles, merge all the rectangles that overlap"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Then
            Static draw = New Rectangle_Basics
            Static rotatedCheck = findCheckBox("Draw Rotated Rectangles (unchecked will draw rectangles)")
            rotatedCheck.Enabled = False

            Static countSlider = findSlider("Rectangle Count")
            countSlider.Value = msRNG.Next(2, 10)

            label1 = "Input rectangles = " + CStr(draw.rectangles.Count)

            draw.Run()
            dst1 = draw.dst1
            inputRects = draw.rectangles
        End If

        Do
            Dim unionAdded = False
            For i = 0 To inputRects.Count - 1
                Dim r1 = inputRects(i)
                Dim rectCount = inputRects.Count
                For j = i + 1 To inputRects.Count - 1
                    Dim r2 = inputRects(j)
                    If r1.IntersectsWith(r2) Then
                        inputRects.RemoveAt(j)
                        inputRects.RemoveAt(i)
                        inputRects.Add(r1.Union(r2))
                        unionAdded = True
                        Exit For
                    End If
                Next
                If rectCount <> inputRects.Count Then Exit For
            Next
            If unionAdded = False Then Exit Do
        Loop
        outputRects = inputRects
        If standalone Then
            For Each r In outputRects
                dst1.Rectangle(r, cv.Scalar.Yellow, 2)
            Next
        End If
    End Sub
End Class