Imports cv = OpenCvSharp
Imports OpenCvSharp.Aruco.CvAruco

' https://github.com/shimat/opencvsharp_samples/blob/master/SamplesCS/Samples/ArucoSample.cs
Public Class Aruco_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Show how to use the Aruco markers and rotate the image accordingly."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim tmp = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/aruco_markers_photo.jpg")
        Static detectorParameters = cv.Aruco.DetectorParameters.Create()
        detectorParameters.CornerRefinementMethod = cv.Aruco.CornerRefineMethod.Subpix
        detectorParameters.CornerRefinementWinSize = 9
        Dim dictionary = cv.Aruco.CvAruco.GetPredefinedDictionary(cv.Aruco.PredefinedDictionaryName.Dict4X4_1000)
        Dim corners()() As cv.Point2f = Nothing
        Dim ids() As Int32 = Nothing
        Dim rejectedPoints()() As cv.Point2f = Nothing
        ' this fails!  Cannot cast a Mat to an InputArray!  Bug?
        ' cv.Aruco.CvAruco.DetectMarkers(tmp, dictionary, corners, ids, detectorParameters, rejectedPoints)
        ocvb.putText(New TTtext("This algorithm is currently failing in VB.Net (works in C#)." + vbCrLf +
                                                  "The DetectMarkers API works in C# but fails in VB.Net." + vbCrLf +
                                                  "To see the correct output, use Aruco_CS.", 10, 140, RESULT1))
    End Sub
End Class




Public Class Aruco_CS
    Inherits ocvbClass
    Dim aruco As New CS_Classes.Aruco_Detect
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        label1 = "Original Image with marker ID's"
        label2 = "Normalized image after WarpPerspective."
        ocvb.desc = "Testing the Aruco marker detection in C#"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim tmp = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/aruco_markers_photo.jpg")
        aruco.Run(tmp)
        dst1 = aruco.detectedMarkers.Resize(src.Size())

        dst2.SetTo(0)
        dst2(New cv.Rect(0, 0, dst2.Height, dst2.Height)) = aruco.normalizedImage.Resize(New cv.Size(dst1.Height, dst1.Height))
    End Sub
End Class
