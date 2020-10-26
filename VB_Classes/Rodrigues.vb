Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module Rodrigues_Basics_Exports
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectRodrigues() As IntPtr
    End Function
End Module


Public Class Rodrigues_ValidateKinect
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Validate the Rodrigues calibration for Kinect camera (only)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.parms.camIndex <> VB_Classes.ActiveTask.algParms.camName.Kinect4AzureCam Then
            dst2.SetTo(0)
            ocvb.trueText("Only the Kinect4Azure camera is currently supported for the Rodrigues calibration", 10, 140)
            Exit Sub
        End If

        Dim out As IntPtr = KinectRodrigues()
        Dim msg = Marshal.PtrToStringAnsi(out)
        Dim split As String() = msg.Split(vbLf)

        Dim output As String = ""
        For i = 0 To split.Length - 1
            output += split(i) + vbCrLf
        Next
        ocvb.trueText(output)
    End Sub
End Class




Public Class Rodrigues_ValidateVector
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Validate the Rodrigues calibration for Stereolabs Zed 2 camera (only)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.parms.camIndex <> VB_Classes.ActiveTask.algParms.camName.StereoLabsZED2 Then
            dst2.SetTo(0)
            ocvb.trueText("Only the StereoLabs Zed 2 and Intel T265 cameras are supported for this Rodrigues validation")
            Exit Sub
        End If

        Dim rot = ocvb.parms.IMU_RotationMatrix
        Dim output = "IMU Rotation Matrix for Zed 2 camera" + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(rot(i * 3), "#0.000000") + vbTab + Format(rot(i * 3 + 1), "#0.0000000") + vbTab + Format(rot(i * 3 + 2), "#0.0000000") + vbCrLf
        Next

        Dim src As New cv.Mat(3, 3, cv.MatType.CV_32F, ocvb.parms.IMU_RotationMatrix)
        Dim dst1 As New cv.Mat(3, 1, src.Type)
        cv.Cv2.Rodrigues(src, dst1)

        output += vbCrLf + "Rotation matrix produces the following Rotation Vector after Rodrigues: " + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(dst1.Get(Of Single)(i), "#0.000000000") + vbTab
        Next

        output += vbCrLf + "Rotation Vector from IMU: " + vbCrLf
        output += vbTab + Format(ocvb.parms.IMU_RotationVector.X, "#0.000000000") + vbTab
        output += vbTab + Format(ocvb.parms.IMU_RotationVector.Y, "#0.000000000") + vbTab
        output += vbTab + Format(ocvb.parms.IMU_RotationVector.Z, "#0.000000000") + vbTab
        ocvb.trueText(output)
    End Sub
End Class





Public Class Rodrigues_RotationMatrix
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Display the contents of the IMU Rotation Matrix"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim rot = ocvb.parms.IMU_RotationMatrix
        Dim output = "IMU Rotation Matrix (rotate the camera to see if it is working)" + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(rot(i * 3), "#0.000000") + vbTab + Format(rot(i * 3 + 1), "#0.0000000") + vbTab + Format(rot(i * 3 + 2), "#0.0000000") + vbCrLf
        Next

        Dim src As New cv.Mat(3, 3, cv.MatType.CV_32F, ocvb.parms.IMU_RotationMatrix)
        Dim dst1 As New cv.Mat(3, 1, src.Type, 3)
        cv.Cv2.Rodrigues(src, dst1)

        output += vbCrLf + "Rotation matrix produces the following Rotation Vector after Rodrigues: " + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(dst1.Get(Of Single)(i), "#0.000000000") + vbTab
        Next
        ocvb.trueText(output)
    End Sub
End Class







Public Class Rodrigues_Extrinsics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Convert Camera extrinsics array to a Vector with Rodrigues"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim rot = ocvb.parms.extrinsics.rotation
        Dim output As String = "Extrinsics Rotation Matrix" + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(rot(i * 3), "#0.00") + vbTab + Format(rot(i * 3 + 1), "#0.00") + vbTab + Format(rot(i * 3 + 2), "#0.00") + vbCrLf
        Next

        Dim src32f As New cv.Mat(3, 3, cv.MatType.CV_32F, ocvb.parms.extrinsics.rotation)
        Dim src As New cv.Mat
        src32f.ConvertTo(src, cv.MatType.CV_64F)
        Dim Jacobian As New cv.Mat(9, 3, src.Type, 0)
        Dim dst1 As New cv.Mat(3, 1, src.Type, 3)
        cv.Cv2.Rodrigues(src, dst1)

        output += "Extrinsic Rotation matrix produces the following Rotation Vector after Rodrigues: " + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(dst1.Get(Of Double)(i), "#0.000000000") + vbTab
        Next
        ocvb.trueText(output)
    End Sub
End Class

