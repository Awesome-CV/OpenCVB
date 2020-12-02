Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
Module recordPlaybackCommon
    Public bytesPerColor As Int64
    Public bytesPerDepth16 As Int64
    Public bytesPerRGBDepth As Int64
    Public bytesPerCloud As Int64
    Public Structure fileHeader
        Public pcBufferSize As integer ' indicates that a point cloud is in the data stream.

        Public colorWidth As integer
        Public colorHeight As integer
        Public colorElemsize As integer

        Public depthWidth As integer
        Public depthHeight As integer
        Public depth16Elemsize As integer

        Public RGBDepthWidth As integer
        Public RGBDepthHeight As integer
        Public RGBDepthElemsize As integer

        Public cloudWidth As integer
        Public cloudHeight As integer
        Public cloudElemsize As integer
    End Structure
    Public Sub writeHeader(binWrite As BinaryWriter)
        binWrite.Write(task.color.Width)
        binWrite.Write(task.color.Height)
        binWrite.Write(task.color.ElemSize)

        binWrite.Write(task.depth16.Width)
        binWrite.Write(task.depth16.Height)
        binWrite.Write(task.depth16.ElemSize)

        binWrite.Write(task.RGBDepth.Width)
        binWrite.Write(task.RGBDepth.Height)
        binWrite.Write(task.RGBDepth.ElemSize)

        binWrite.Write(task.pointCloud.Width)
        binWrite.Write(task.pointCloud.Height)
        binWrite.Write(task.pointCloud.ElemSize)
    End Sub
    Public Sub readHeader(ByRef header As fileHeader, binRead As BinaryReader)
        header.colorWidth = binRead.ReadInt32()
        header.colorHeight = binRead.ReadInt32()
        header.colorElemsize = binRead.ReadInt32()

        header.depthWidth = binRead.ReadInt32()
        header.depthHeight = binRead.ReadInt32()
        header.depth16Elemsize = binRead.ReadInt32()

        header.RGBDepthWidth = binRead.ReadInt32()
        header.RGBDepthHeight = binRead.ReadInt32()
        header.RGBDepthElemsize = binRead.ReadInt32()

        header.cloudWidth = binRead.ReadInt32()
        header.cloudHeight = binRead.ReadInt32()
        header.cloudElemsize = binRead.ReadInt32()
    End Sub
End Module




Public Class Replay_Record
    Inherits VBparent
    Dim binWrite As BinaryWriter
    Dim recordingActive As Boolean
    Dim colorBytes() As Byte
    Dim RGBDepthBytes() As Byte
    Dim depth16Bytes() As Byte
    Dim cloudBytes() As Byte
    Dim maxBytes As Single = 20000000000
    Dim recordingFilename As FileInfo
    Public Sub New()
        initParent()

        task.openFileDialogRequested = True
        task.openFileInitialDirectory = ocvb.parms.homeDir + "/Data/"
        task.openFileDialogName = GetSetting("OpenCVB", "ReplayFileName", "ReplayFileName", ocvb.parms.homeDir + "Recording.ocvb")
        task.openFileFilter = "ocvb (*.ocvb)|*.ocvb"
        task.openFileFilterIndex = 1
        task.openFileDialogTitle = "Select an OpenCVB bag file to create"
        task.initialStartSetting = False

        task.desc = "Create a recording of camera data that contains color, depth, RGBDepth, pointCloud, and IMU data in an .bob file."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static bytesTotal As Int64
        recordingFilename = New FileInfo(task.openFileDialogName)
        If ocvb.parms.useRecordedData And recordingFilename.Exists = False Then
            ocvb.trueText("Record the file: " + recordingFilename.FullName + " first before attempting to use it in the regression tests.", 10, 125)
            Exit Sub
        End If

        If task.fileStarted Then
            If recordingActive = False Then
                bytesPerColor = task.color.Total * task.color.ElemSize
                bytesPerRGBDepth = task.RGBDepth.Total * task.RGBDepth.ElemSize
                bytesPerDepth16 = task.depth16.Total * task.depth16.ElemSize
                ' start recording...
                ReDim colorBytes(bytesPerColor - 1)
                ReDim depth16Bytes(bytesPerDepth16 - 1)
                ReDim RGBDepthBytes(bytesPerRGBDepth - 1)
                Dim pcSize = task.pointCloud.Total * task.pointCloud.ElemSize
                ReDim cloudBytes(pcSize - 1)

                binWrite = New BinaryWriter(File.Open(recordingFilename.FullName, FileMode.Create))
                recordingActive = True
                writeHeader(binWrite)
            Else
                Marshal.Copy(task.color.Data, colorBytes, 0, colorBytes.Length)
                binWrite.Write(colorBytes)
                bytesTotal += colorBytes.Length

                Marshal.Copy(task.depth16.Data, depth16Bytes, 0, depth16Bytes.Length)
                binWrite.Write(depth16Bytes)
                bytesTotal += depth16Bytes.Length

                Marshal.Copy(task.RGBDepth.Data, RGBDepthBytes, 0, RGBDepthBytes.Length)
                binWrite.Write(RGBDepthBytes)
                bytesTotal += RGBDepthBytes.Length

                Marshal.Copy(task.pointCloud.Data, cloudBytes, 0, cloudBytes.Length)
                binWrite.Write(cloudBytes)
                bytesTotal += cloudBytes.Length

                If bytesTotal >= maxBytes Then
                    task.fileStarted = False
                    recordingActive = False
                Else
                    task.openFileSliderPercent = bytesTotal / maxBytes
                End If
            End If
        Else
            If recordingActive Then
                ' stop recording
                binWrite.Close()
                recordingActive = False
            End If
        End If
    End Sub
    Public Sub Close()
        If recordingFilename IsNot Nothing Then SaveSetting("OpenCVB", "ReplayFileName", "ReplayFileName", recordingFilename.FullName)
        If recordingActive Then binWrite.Close()
    End Sub
End Class





Public Class Replay_Play
    Inherits VBparent
    Dim binRead As BinaryReader
    Dim playbackActive As Boolean
    Dim colorBytes() As Byte
    Dim depth16Bytes() As Byte
    Dim RGBDepthBytes() As Byte
    Dim cloudBytes() As Byte
    Dim fh As New fileHeader
    Dim fs As FileStream
    Dim recordingFilename As FileInfo
    Public Sub New()
        initParent()
        task.openFileDialogRequested = True
        task.openFileInitialDirectory = ocvb.parms.homeDir + "/Data/"
        task.openFileDialogName = GetSetting("OpenCVB", "ReplayFileName", "ReplayFileName", ocvb.parms.homeDir + "Recording.ocvb")
        task.openFileFilter = "ocvb (*.ocvb)|*.ocvb"
        task.openFileFilterIndex = 1
        task.openFileDialogTitle = "Select an OpenCVB bag file to create"
        task.initialStartSetting = True

        task.desc = "Playback a file recorded by OpenCVB"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static bytesTotal As Int64
        recordingFilename = New FileInfo(task.openFileDialogName)
        If recordingFilename.Exists = False Then ocvb.trueText("File not found: " + recordingFilename.FullName, 10, 125)
        If task.fileStarted And recordingFilename.Exists Then
            Dim maxBytes = recordingFilename.Length
            If playbackActive Then
                colorBytes = binRead.ReadBytes(bytesPerColor)
                Dim tmpMat = New cv.Mat(fh.colorHeight, fh.colorWidth, cv.MatType.CV_8UC3, colorBytes)
                task.color = tmpMat.Resize(task.color.Size())
                bytesTotal += colorBytes.Length

                depth16Bytes = binRead.ReadBytes(bytesPerDepth16)
                tmpMat = New cv.Mat(fh.depthHeight, fh.depthWidth, cv.MatType.CV_16U, depth16Bytes)
                bytesTotal += depth16Bytes.Length

                RGBDepthBytes = binRead.ReadBytes(bytesPerRGBDepth)
                tmpMat = New cv.Mat(fh.RGBDepthHeight, fh.RGBDepthWidth, cv.MatType.CV_8UC3, RGBDepthBytes)
                task.RGBDepth = tmpMat.Resize(task.RGBDepth.Size())
                bytesTotal += RGBDepthBytes.Length

                cloudBytes = binRead.ReadBytes(bytesPerCloud)
                task.pointCloud = New cv.Mat(fh.cloudHeight, fh.cloudWidth, cv.MatType.CV_32FC3, cloudBytes)  ' we cannot resize the point cloud.
                bytesTotal += cloudBytes.Length

                ' restart the video at the beginning.
                If binRead.PeekChar < 0 Then
                    binRead.Close()
                    playbackActive = False
                    bytesTotal = 0
                End If
                task.openFileSliderPercent = bytesTotal / recordingFilename.Length
                dst1 = task.color.Clone()
                dst2 = task.RGBDepth.Clone()
            Else
                ' start playback...
                fs = New FileStream(recordingFilename.FullName, FileMode.Open, FileAccess.Read)
                binRead = New BinaryReader(fs)
                playbackActive = True
                readHeader(fh, binRead)

                bytesPerColor = fh.colorWidth * fh.colorHeight * fh.colorElemsize
                bytesPerDepth16 = fh.cloudWidth * fh.cloudHeight * fh.depth16Elemsize
                bytesPerRGBDepth = fh.colorWidth * fh.colorHeight * fh.RGBDepthElemsize
                bytesPerCloud = fh.cloudWidth * fh.cloudHeight * fh.cloudElemsize

                ReDim colorBytes(bytesPerColor - 1)
                ReDim RGBDepthBytes(bytesPerRGBDepth - 1)
                ReDim cloudBytes(bytesPerCloud - 1)
            End If
        Else
            If playbackActive Then
                ' stop playback
                binRead.Close()
                playbackActive = False
            End If
        End If
    End Sub
    Public Sub Close()
        If recordingFilename IsNot Nothing Then SaveSetting("OpenCVB", "ReplayFileName", "ReplayFileName", recordingFilename.FullName)
        If playbackActive Then binRead.Close()
    End Sub
End Class





Public Class Replay_OpenGL
    Inherits VBparent
    Dim ogl As OpenGL_Callbacks
    Dim replay As Replay_Play
    Public Sub New()
        initParent()
        ogl = New OpenGL_Callbacks()
        replay = New Replay_Play()
        task.desc = "Replay a recorded session with OpenGL"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        replay.Run()
        ogl.src = task.color
        ogl.Run()
    End Sub
End Class

