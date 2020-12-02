Imports cv = OpenCvSharp
Public Class Voxels_Basics_MT
    Inherits VBparent
    Public inrange As Depth_InRange
    Public grid As Thread_Grid
    Public voxels(1) As Single
    Public voxelMat As cv.Mat
    Public minDepth As Single
    Public maxDepth As Single
    Public Sub New()
        initParent()
        check.Setup(caller, 1)
        check.Box(0).Text = "Display intermediate results"
        check.Box(0).Checked = True

        inrange = New Depth_InRange()

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Histogram Bins", 2, 200, 100)

        grid = New Thread_Grid()
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 16
        gridHeightSlider.Value = 16

        label2 = "Voxels labeled with their median distance"
        task.desc = "Use multi-threading to get median depth values as voxels."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim split() = task.pointCloud.Split()

        Static minSlider = findSlider("InRange Min Depth")
        Static maxSlider = findSlider("InRange Max Depth")
        minDepth = minSlider.Value
        maxDepth = maxSlider.Value

        inrange.minVal = minDepth
        inrange.maxVal = maxDepth
        inrange.src = split(2) * 1000
        inrange.Run()

        grid.src = split(2)
        grid.Run()

        If voxels.Length <> grid.roiList.Count Then ReDim voxels(grid.roiList.Count - 1)

        Dim bins = sliders.trackbar(0).Value
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim count = inrange.depthMask(roi).CountNonZero()
            If count > 0 Then
                voxels(i) = inrange.src(roi).Mean(inrange.depthMask(roi)).Item(0)
            Else
                voxels(i) = 0
            End If
        End Sub)
        voxelMat = New cv.Mat(voxels.Length, 1, cv.MatType.CV_32F)
        If check.Box(0).Checked Then
            dst1 = task.RGBDepth.Clone()
            dst1.SetTo(cv.Scalar.White, grid.gridMask)
            Dim nearColor = cv.Scalar.Yellow
            Dim farColor = cv.Scalar.Blue
            Dim img = New cv.Mat(split(2).Size, cv.MatType.CV_8UC3, 0)
            Parallel.For(0, grid.roiList.Count,
                Sub(i)
                    Dim roi = grid.roiList(i)
                    If voxels(i) >= minDepth And voxels(i) <= maxDepth Then
                        voxelMat.Set(Of Single)(i, 0, voxels(i))
                        Dim v = 255 * (voxels(i) - minDepth) / (maxDepth - minDepth)
                        Dim color = New cv.Scalar(((256 - v) * nearColor(0) + v * farColor(0)) >> 8,
                                                  ((256 - v) * nearColor(1) + v * farColor(1)) >> 8,
                                                  ((256 - v) * nearColor(2) + v * farColor(2)) >> 8)
                        img(roi).SetTo(color, inrange.depthMask(roi))
                    End If
                End Sub)
            dst2 = img.Resize(dst1.Size)
        End If
        voxelMat *= 255 / (maxDepth - minDepth) ' do the normalize manually to use the min and max Depth (more stable image)
    End Sub
End Class

