Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes

Public Class OpenGL_Basics
    Inherits VBparent
    Dim memMapWriter As MemoryMappedViewAccessor
    Dim pipeName As String ' this is name of pipe to the OpenGL task.  It is dynamic and increments.
    Dim pipe As NamedPipeServerStream
    Dim startInfo As New ProcessStartInfo
    Dim memMapbufferSize As integer
    Dim memMapFile As MemoryMappedFile
    Dim memMapPtr As IntPtr
    Dim rgbBuffer(0) As Byte
    Dim dataBuffer(0) As Byte
    Dim textureBuffer(0) As Byte
    Dim pointCloudBuffer(0) As Byte
    Public memMapValues(39) As Double
    Public pointSize As integer = 2
    Public dataInput As New cv.Mat
    Public textureInput As New cv.Mat
    Public FOV As Single = 150
    Public yaw As Single = 0
    Public pitch As Single = 0
    Public roll As Single = 0
    Public zNear As Single = 0
    Public zFar As Single = 10.0
    Public eye As New cv.Vec3f(0, 0, -40)
    Public scaleXYZ As New cv.Vec3f(10, 10, 1)
    Public zTrans As Single = 0.5
    Public OpenGLTitle As String = "OpenGL_Basics"
    Public imageLabel As String
    Public pointCloudInput As New cv.Mat
    Dim openGLHeight = 1200
    Dim openGLWidth = 1500
    Dim inrange As Depth_InRange
    Public Sub New()
        initParent()
        inrange = New Depth_InRange
        task.desc = "Create an OpenGL window and update it with images"
    End Sub
    Private Sub memMapUpdate()
        Dim timeConversionUnits As Double = 1000
        Dim imuAlphaFactor As Double = 0.98 ' theta is a mix of acceleration data and gyro data.
        If ocvb.parms.cameraName <> VB_Classes.ActiveTask.algParms.camNames.D435i Then
            timeConversionUnits = 1000 * 1000
            imuAlphaFactor = 0.99
        End If
        For i = 0 To memMapValues.Length - 1
            ' only change this if you are changing the data in the OpenGL C++ code at the same time...
            memMapValues(i) = Choose(i + 1, ocvb.frameCount, ocvb.parms.intrinsicsLeft.fx, ocvb.parms.intrinsicsLeft.fy,
                                     ocvb.parms.intrinsicsLeft.ppx, ocvb.parms.intrinsicsLeft.ppy, src.Width, src.Height, src.ElemSize * src.Total,
                                     dataInput.Total * dataInput.ElemSize, FOV, yaw, pitch, roll, zNear, zFar, pointSize, dataInput.Width, dataInput.Height,
                                     task.IMU_AngularVelocity.X, task.IMU_AngularVelocity.Y, task.IMU_AngularVelocity.Z,
                                     task.IMU_Acceleration.X, task.IMU_Acceleration.Y, task.IMU_Acceleration.Z, task.IMU_TimeStamp,
                                     1, eye.Item0 / 100, eye.Item1 / 100, eye.Item2 / 100, zTrans,
                                     scaleXYZ.Item0 / 10, scaleXYZ.Item1 / 10, scaleXYZ.Item2 / 10, timeConversionUnits, imuAlphaFactor,
                                     imageLabel.Length, pointCloudInput.Width, pointCloudInput.Height, textureInput.Total * textureInput.ElemSize)
        Next
    End Sub
    Private Sub startOpenGLWindow()
        ' first setup the named pipe that will be used to feed data to the OpenGL window
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        PipeTaskIndex += 1
        pipe = New NamedPipeServerStream(pipeName, PipeDirection.InOut, 1)

        memMapbufferSize = 8 * memMapValues.Length - 1

        startInfo.FileName = OpenGLTitle + ".exe"
        startInfo.Arguments = CStr(openGLWidth) + " " + CStr(openGLHeight) + " " + CStr(memMapbufferSize) + " " + pipeName
        If ocvb.parms.ShowConsoleLog = False Then startInfo.WindowStyle = ProcessWindowStyle.Hidden
        Process.Start(startInfo)

        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("OpenCVBControl", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)

        imageLabel = OpenGLTitle ' default title - can be overridden with each image.
        pipe.WaitForConnection()
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone Then
            src = src
            pointCloudInput = task.pointCloud
        End If

        inrange.Run()
        pointCloudInput.SetTo(0, inrange.noDepthMask)

        Dim pcSize = pointCloudInput.Total * pointCloudInput.ElemSize
        If ocvb.frameCount = 0 Then startOpenGLWindow()
        Dim readPipe(4) As Byte ' we read 4 bytes because that is the signal that the other end of the named pipe wrote 4 bytes to indicate iteration complete.
        If ocvb.frameCount > 0 And pipe IsNot Nothing Then
            Dim bytesRead = pipe.Read(readPipe, 0, 4)
            If bytesRead = 0 Then ocvb.trueText("The OpenGL process appears to have stopped.", 20, 100)
        End If

        Dim rgb = src.CvtColor(cv.ColorConversionCodes.BGR2RGB) ' OpenGL needs RGB, not BGR
        If rgb.Width Then ReDim rgbBuffer(rgb.Total * rgb.ElemSize - 1)
        If dataInput.Width Then ReDim dataBuffer(dataInput.Total * dataInput.ElemSize - 1)
        If textureInput.Width Then ReDim textureBuffer(textureInput.Total * textureInput.ElemSize - 1)
        If pointCloudInput.Width Then ReDim pointCloudBuffer(pointCloudInput.Total * pointCloudInput.ElemSize - 1)

        memMapUpdate()

        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)

        If rgb.Width > 0 Then Marshal.Copy(rgb.Data, rgbBuffer, 0, rgbBuffer.Length)
        If dataInput.Width > 0 Then Marshal.Copy(dataInput.Data, dataBuffer, 0, dataBuffer.Length)
        If textureInput.Width > 0 Then Marshal.Copy(textureInput.Data, textureBuffer, 0, textureBuffer.Length)
        If pointCloudInput.Width > 0 Then Marshal.Copy(pointCloudInput.Data, pointCloudBuffer, 0, pcSize)

        If pipe.IsConnected Then
            On Error Resume Next
            If rgb.Width > 0 Then pipe.Write(rgbBuffer, 0, rgbBuffer.Length)
            If dataInput.Width > 0 Then pipe.Write(dataBuffer, 0, dataBuffer.Length)
            If textureInput.Width > 0 Then pipe.Write(textureBuffer, 0, textureBuffer.Length)
            If pointCloudInput.Width > 0 Then pipe.Write(pointCloudBuffer, 0, pointCloudBuffer.Length)
            Dim buff = System.Text.Encoding.UTF8.GetBytes(imageLabel)
            pipe.Write(buff, 0, imageLabel.Length)
        End If
    End Sub
    Public Sub Close()
        Dim proc = Process.GetProcessesByName(OpenGLTitle)
        For i = 0 To proc.Count - 1
            proc(i).CloseMainWindow()
        Next i
        If memMapPtr <> 0 Then Marshal.FreeHGlobal(memMapPtr)
    End Sub
End Class



Module OpenGL_Sliders_Module
    Public Sub setOpenGLsliders(caller As String, sliders As OptionsSliders)
        sliders.Setup(caller, 15)
        sliders.setupTrackBar(0, "OpenGL FOV", 1, 180, 150)
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D435i Then sliders.trackbar(0).Value = 135
        sliders.setupTrackBar(1, "OpenGL yaw (degrees)", -180, 180, -3)
        sliders.setupTrackBar(2, "OpenGL pitch (degrees)", -180, 180, 3)
        sliders.setupTrackBar(3, "OpenGL roll (degrees)", -180, 180, 0)

        sliders.setupTrackBar(4, "OpenGL zNear", 0, 100, 0)
        sliders.setupTrackBar(5, "OpenGL zFar", -50, 200, 20)
        sliders.setupTrackBar(6, "OpenGL Point Size", 1, 20, 2)
        sliders.setupTrackBar(7, "zTrans", -1000, 1000, 50)

        sliders.setupTrackBar(8, "OpenGL Eye X", -180, 180, 0)
        sliders.setupTrackBar(9, "OpenGL Eye Y", -180, 180, 0)
        sliders.setupTrackBar(10, "OpenGL Eye Z", -180, 180, -40)

        sliders.setupTrackBar(11, "OpenGL Scale X", 1, 100, 10)
        sliders.setupTrackBar(12, "OpenGL Scale Y", 1, 100, 10)
        sliders.setupTrackBar(13, "OpenGL Scale Z", 1, 100, 1)
    End Sub
End Module




Public Class OpenGL_Options
    Inherits VBparent
    Public OpenGL As OpenGL_Basics
    Public Sub New()
        initParent()
        OpenGL = New OpenGL_Basics()
        setOpenGLsliders(caller, sliders)
        task.desc = "Adjust point size and FOV in OpenGL"
        label1 = ""
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        OpenGL.FOV = sliders.trackbar(0).Value
        OpenGL.yaw = sliders.trackbar(1).Value
        OpenGL.pitch = sliders.trackbar(2).Value
        OpenGL.roll = sliders.trackbar(3).Value

        OpenGL.zNear = sliders.trackbar(4).Value
        OpenGL.zFar = sliders.trackbar(5).Value
        OpenGL.pointSize = sliders.trackbar(6).Value
        OpenGL.zTrans = sliders.trackbar(7).Value / 100

        OpenGL.eye.Item0 = sliders.trackbar(8).Value
        OpenGL.eye.Item1 = sliders.trackbar(9).Value
        OpenGL.eye.Item2 = sliders.trackbar(10).Value

        OpenGL.scaleXYZ.Item0 = sliders.trackbar(11).Value
        OpenGL.scaleXYZ.Item1 = sliders.trackbar(12).Value
        OpenGL.scaleXYZ.Item2 = sliders.trackbar(13).Value

        OpenGL.src = src
        OpenGL.pointCloudInput = task.pointCloud
        OpenGL.Run()
    End Sub
End Class




Public Class OpenGL_Callbacks
    Inherits VBparent
    Public ogl As OpenGL_Basics
    Public Sub New()
        initParent()
        ogl = New OpenGL_Basics()
        ogl.OpenGLTitle = "OpenGL_Callbacks"
        task.desc = "Show the point cloud of 3D data and use callbacks to modify view."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        ogl.src = src
        ogl.pointCloudInput = task.pointCloud
        ogl.Run()
    End Sub
End Class




'https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
Public Class OpenGL_IMU
    Inherits VBparent
    Public ogl As OpenGL_Options
    Public imu As IMU_GVector

    Public Sub New()
        initParent()
        imu = New IMU_GVector()

        ogl = New OpenGL_Options()
        ogl.OpenGL.OpenGLTitle = "OpenGL_IMU"
        ogl.sliders.trackbar(1).Value = 0 ' pitch
        ogl.sliders.trackbar(2).Value = 0 ' yaw
        ogl.sliders.trackbar(3).Value = 0 ' roll
        task.desc = "Show how to use IMU coordinates in OpenGL"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        imu.Run()
        ogl.OpenGL.dataInput = New cv.Mat(100, 100, cv.MatType.CV_32F, 0)
        ogl.src = src
        ogl.Run() ' we are not moving any images to OpenGL - just the IMU value which are already in the memory mapped file.
    End Sub
End Class






' https://docs.opencv.org/3.4/d1/d1d/tutorial_histo3D.html
Public Class OpenGL_3Ddata
    Inherits VBparent
    Dim colors As Palette_Gradient
    Public ogl As OpenGL_Options
    Dim histInput() As Byte
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram Red/Green/Blue bins", 1, 128, 32) ' why 128 and not 256? There is some limit on the max pinned memory.  Not sure...
        End If

        ogl = New OpenGL_Options()
        ogl.OpenGL.OpenGLTitle = "OpenGL_3Ddata"
        ogl.sliders.trackbar(1).Value = -10
        ogl.sliders.trackbar(6).Value = 5
        ogl.sliders.trackbar(2).Value = 10

        colors = New Palette_Gradient()
        colors.color1 = cv.Scalar.Yellow
        colors.color2 = cv.Scalar.Blue
        colors.Run()
        ogl.OpenGL.src = dst1.Clone() ' only need to set this once.

        label1 = "Input to Histogram 3D"
        task.desc = "Plot the results of a 3D histogram in OpenGL."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim bins = sliders.trackbar(0).Value

        If histInput Is Nothing Then ReDim histInput(src.Total * src.ElemSize - 1)
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim handleRGB = GCHandle.Alloc(histInput, GCHandleType.Pinned) ' and pin it as well
        Dim dstPtr = Histogram_3D_RGB(handleRGB.AddrOfPinnedObject(), src.Rows, src.Cols, bins)
        handleRGB.Free() ' free the pinned memory...
        Dim dstData(bins * bins * bins - 1) As Single
        Marshal.Copy(dstPtr, dstData, 0, dstData.Length)
        Dim histogram = New cv.Mat(bins, bins, cv.MatType.CV_32FC(bins), dstData)
        histogram = histogram.Normalize(255)

        ogl.OpenGL.dataInput = histogram.Clone()
        ogl.src = src
        ogl.Run()
    End Sub
End Class




Public Class OpenGL_Draw3D
    Inherits VBparent
    Dim circle As Draw_Circles
    Public ogl As OpenGL_Options
    Public Sub New()
        initParent()
        circle = New Draw_Circles()
        circle.sliders.trackbar(0).Value = 5

        ogl = New OpenGL_Options()
        ogl.OpenGL.OpenGLTitle = "OpenGL_3DShapes"
        ogl.sliders.trackbar(0).Value = 80
        ogl.sliders.trackbar(8).Value = -140
        ogl.sliders.trackbar(9).Value = -180
        ogl.sliders.trackbar(6).Value = 16
        ogl.sliders.trackbar(10).Value = -30
        task.pointCloud = New cv.Mat ' we are not using the point cloud when displaying data.
        label2 = "Grayscale image sent to OpenGL"
        task.desc = "Draw in an image show it in 3D in OpenGL without any explicit math"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        circle.Run()
        dst2 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ogl.OpenGL.dataInput = dst2
        ogl.OpenGL.src = New cv.Mat(1, ocvb.vecColors.Length - 1, cv.MatType.CV_8UC3, ocvb.vecColors.ToArray)
        ogl.src = src
        ogl.Run()
    End Sub
End Class





Public Class OpenGL_Voxels
    Inherits VBparent
    Public voxels As Voxels_Basics_MT
    Public ogl As OpenGL_Basics
    Public Sub New()
        initParent()
        voxels = New Voxels_Basics_MT()

        ogl = New OpenGL_Basics()
        ogl.OpenGLTitle = "OpenGL_Voxels"
        task.desc = "Show the voxel representation in OpenGL"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        voxels.src = src
        voxels.Run()
        Static intermediateResults = findCheckBox("Display intermediate results")
        If intermediateResults.checked Then
            dst1 = voxels.dst1
            dst2 = voxels.dst2
        End If

        ogl.dataInput = New cv.Mat(voxels.grid.tilesPerCol, voxels.grid.tilesPerRow, cv.MatType.CV_32F, voxels.voxels)
        ogl.src = src
        ogl.Run()
    End Sub
End Class






' https://open.gl/transformations
' https://www.codeproject.com/Articles/1247960/Learning-Basic-Math-Used-In-3D-Graphics-Engines
Public Class OpenGL_GravityTransform
    Inherits VBparent
    Public ogl As OpenGL_Basics
    Public gCloud As Depth_PointCloud_IMU
    Public Sub New()
        initParent()

        gCloud = New Depth_PointCloud_IMU()

        ogl = New OpenGL_Basics()
        ogl.OpenGLTitle = "OpenGL_Callbacks"

        task.desc = "Use the IMU's acceleration values to build the transformation matrix of an OpenGL viewer"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        gCloud.src = task.pointCloud
        gCloud.Run()

        ogl.pointCloudInput = gCloud.imuPointCloud
        ogl.src = src
        ogl.Run()
    End Sub
End Class






Public Class OpenGL_Reduced
    Inherits VBparent
    Dim reduction As Reduction_PointCloud
    Public ogl As OpenGL_Basics
    Public Sub New()
        initParent()
        reduction = New Reduction_PointCloud()

        ogl = New OpenGL_Basics()
        ogl.OpenGLTitle = "OpenGL_Callbacks"
        task.desc = "Use the reduced depth pointcloud in OpenGL"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        reduction.Run()
        dst1 = reduction.dst1

        ogl.pointCloudInput = task.pointCloud
        ogl.src = src
        ogl.Run()
    End Sub
End Class







Public Class OpenGL_Floor
    Inherits VBparent
    Dim plane As StructuredDepth_LinearizeFloor
    Dim ogl As OpenGL_Basics
    Public Sub New()
        initParent()
        ogl = New OpenGL_Basics()
        ogl.OpenGLTitle = "OpenGL_Callbacks"

        plane = New StructuredDepth_LinearizeFloor()
        task.desc = "Convert depth cloud floor to a plane and visualize it with OpenGL"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        plane.Run()
        dst1 = plane.dst1
        dst2 = plane.dst2

        ogl.pointCloudInput = plane.imuPointCloud
        ogl.src = src
        ogl.Run()
    End Sub
End Class








Public Class OpenGL_FloorPlane
    Inherits VBparent
    Public ogl As OpenGL_Basics
    Public plane As StructuredDepth_LinearizeFloor
    Public Sub New()
        initParent()
        ogl = New OpenGL_Basics()
        ogl.OpenGLTitle = "OpenGL_FloorPlane"
        plane = New StructuredDepth_LinearizeFloor()
        task.desc = "Show the floor in the pointcloud as a plane"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        plane.Run()
        dst1 = plane.dst1
        dst2 = plane.dst2

        Dim floorColor = task.color.Mean(plane.maskPlane)
        Dim data As New cv.Mat(4, 1, cv.MatType.CV_32F, 0)
        data.Set(Of Single)(0, 0, floorColor.Item(0))
        data.Set(Of Single)(1, 0, floorColor.Item(0))
        data.Set(Of Single)(2, 0, floorColor.Item(0))
        data.Set(Of Single)(3, 0, plane.floor.floorYplane)
        ogl.dataInput = data
        ogl.pointCloudInput = plane.imuPointCloud
        ogl.src = src
        ogl.Run()
    End Sub
End Class










Public Class OpenGL_FloorTexture
    Inherits VBparent
    Dim floor As OpenGL_FloorPlane
    Dim shuffle As Texture_Shuffle
    Public Sub New()
        initParent()
        shuffle = New Texture_Shuffle()
        floor = New OpenGL_FloorPlane()
        task.desc = "Texture the plane of the floor with a good sample of the texture from the mask"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        floor.plane.Run()
        dst1 = floor.plane.dst1
        dst2 = floor.plane.dst2

        shuffle.src = floor.plane.maskPlane
        shuffle.Run()
        floor.ogl.textureInput = shuffle.rgbaTexture

        Dim data = New cv.Mat(4, 1, cv.MatType.CV_32F, 0)
        data.Set(Of Single)(0, 0, ocvb.maxZ)
        data.Set(Of Single)(1, 0, 0)
        data.Set(Of Single)(2, 0, 0)
        data.Set(Of Single)(3, 0, floor.plane.floor.floorYplane)
        floor.ogl.dataInput = data
        floor.ogl.pointCloudInput = floor.plane.imuPointCloud
        floor.ogl.pointCloudInput.SetTo(0, floor.plane.maskPlane)
        floor.ogl.src = src
        floor.ogl.Run()
    End Sub
End Class







Public Class OpenGL_DepthSliceH
    Inherits VBparent
    Public ogl As OpenGL_Basics
    Dim slices As StructuredDepth_MultiSliceH
    Public Sub New()
        initParent()

        slices = New StructuredDepth_MultiSliceH()
        ogl = New OpenGL_Basics()
        ogl.OpenGLTitle = "OpenGL_Callbacks"

        task.desc = "View depth slices in 3D with OpenGL"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        slices.Run()
        dst1 = slices.dst1
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(slices.maskPlane, mask)

        ogl.pointCloudInput = task.pointCloud.Clone
        ogl.pointCloudInput.SetTo(0, mask)
        ogl.src = New cv.Mat(mask.Size, cv.MatType.CV_8UC3, cv.Scalar.White)
        ogl.Run()
    End Sub
End Class








Public Class OpenGL_StableDepth
    Inherits VBparent
    Dim pcValid As Depth_PointCloud_Stable
    Public ogl As OpenGL_Options
    Public Sub New()
        initParent()

        pcValid = New Depth_PointCloud_Stable
        ogl = New OpenGL_Options

        task.desc = "Use the extrema stableDepth as input the an OpenGL display"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        pcValid.Run()
        task.pointCloud = pcValid.stableCloud
        ogl.src = task.color
        ogl.Run()
    End Sub
End Class






Public Class OpenGL_StabilizedDepth
    Inherits VBparent
    Dim stable As Depth_Stabilizer
    Public ogl As OpenGL_Callbacks
    Public Sub New()
        initParent()

        stable = New Depth_Stabilizer
        ogl = New OpenGL_Callbacks

        label2 = "32-bit format stabilized depth data"
        task.desc = "Use the depth_stabilizer output as input the an OpenGL display"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim split = cv.Cv2.Split(task.pointCloud)

        stable.src = src
        If stable.src.Type <> cv.MatType.CV_32F Then stable.src = split(2) * 1000

        stable.Run()
        dst1 = stable.dst1
        dst2 = stable.dst2

        split(2) = stable.dst2 * 0.001
        cv.Cv2.Merge(split, task.pointCloud)
        ogl.src = task.color
        ogl.Run()
    End Sub
End Class








Public Class OpenGL_StableDepthMouse
    Inherits VBparent
    Dim pcValid As Depth_PointCloud_Stable
    Public ogl As OpenGL_Callbacks
    Public Sub New()
        initParent()

        pcValid = New Depth_PointCloud_Stable
        ogl = New OpenGL_Callbacks

        task.desc = "Use the extrema stableDepth as input the an OpenGL display"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        pcValid.Run()
        dst1 = pcValid.dst1
        dst2 = pcValid.dst2

        task.pointCloud = pcValid.stableCloud
        ogl.src = task.color
        ogl.Run()
    End Sub
End Class







Public Class OpenGL_SmoothSurfaces
    Inherits VBparent
    Dim smooth As Depth_SmoothSurfaces
    Public ogl As OpenGL_Callbacks
    Public Sub New()
        initParent()

        smooth = New Depth_SmoothSurfaces
        ogl = New OpenGL_Callbacks

        task.desc = "Filter the 3D image to show only smooth surfaces."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        smooth.Run()
        dst1 = smooth.dst1
        dst2 = smooth.dst2

        smooth.pcValid.stableCloud.CopyTo(task.pointCloud, smooth.dst2)
        ogl.src = task.color
        ogl.Run()
    End Sub
End Class







Public Class OpenGL_Stable
    Inherits VBparent
    Dim stable As Stable_Pointcloud
    Dim ogl As OpenGL_Callbacks
    Public Sub New()
        initParent()
        stable = New Stable_Pointcloud
        ogl = New OpenGL_Callbacks
        task.desc = "Use the Stable_PointCloud in 3D"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        stable.Run() ' Updates Task.pointcloud
        dst1 = stable.dst1
        dst2 = stable.dst2

        ogl.src = task.color
        ogl.Run()

        label2 = stable.label2
    End Sub
End Class