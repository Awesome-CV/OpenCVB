Imports cv = OpenCvSharp
Imports OpenCvSharp.Dnn
Imports System.Net
Imports System.Linq
Imports System.IO

Public Class DNN_Test
    Inherits VBparent
    Dim net As Net
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        label2 = "Input Image"
        desc = "Download and use a Caffe database"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim modelFile As New FileInfo(ocvb.homeDir + "Data/bvlc_googlenet.caffemodel")
        If File.Exists(modelFile.FullName) = False Then
            ' this site is apparently gone.  caffemodel is in the Data directory in OpenCVB_HomeDir
            Dim client = HttpWebRequest.CreateHttp("http://dl.caffe.berkeleyvision.org/bvlc_googlenet.caffemodel")
            Dim response = client.GetResponse()
            Dim responseStream = response.GetResponseStream()
            Dim memory As New MemoryStream()
            responseStream.CopyTo(memory)
            File.WriteAllBytes(modelFile.FullName, memory.ToArray)
        End If
        net = Net.ReadNetFromCaffe(ocvb.homeDir + "Data/bvlc_googlenet.prototxt")

        Dim image = cv.Cv2.ImRead(ocvb.homeDir + "Data/space_shuttle.jpg")
        dst2 = image.Resize(dst2.Size())
        Dim inputBlob = CvDnn.BlobFromImage(image, 1, New cv.Size(224, 224), New cv.Scalar(104, 117, 123))
        net.SetInput(inputBlob, "data")
        ocvb.trueText("This example is not working.  Forward fails with 'blobs.size() != 0'.", 10, 100)
        'Dim prob = net.Forward("prob") ' <--- this fails in VB.Net but works in C# (below)
        ' finish this ...
    End Sub
End Class





Public Class DNN_Caffe_CS
    Inherits VBparent
    Dim caffeCS As CS_Classes.DNN
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        label2 = "Input Image"
        desc = "Download and use a Caffe database"

        Dim protoTxt = ocvb.HomeDir + "Data/bvlc_googlenet.prototxt"
        Dim modelFile = ocvb.HomeDir + "Data/bvlc_googlenet.caffemodel"
        Dim synsetWords = ocvb.HomeDir + "Data/synset_words.txt"
        caffeCS = New CS_Classes.DNN(protoTxt, modelFile, synsetWords)
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim image = cv.Cv2.ImRead(ocvb.HomeDir + "Data/space_shuttle.jpg")
        Dim str = caffeCS.Run(image)
        dst2 = image.Resize(dst2.Size())
        ocvb.trueText(str, 10, 100)
    End Sub
End Class





' https://github.com/twMr7/rscvdnn
Public Class DNN_Basics
    Inherits VBparent
    Dim net As Net
    Dim dnnPrepared As Boolean
    Dim crop As cv.Rect
    Dim dnnWidth As Int32, dnnHeight As Int32
    Dim testImage As cv.Mat
    Dim kalman(10) As Kalman_Basics
    Public rect As cv.Rect
    Dim classNames() = {"background", "aeroplane", "bicycle", "bird", "boat", "bottle", "bus", "car", "cat", "chair", "cow", "diningtable", "dog", "horse",
                        "motorbike", "person", "pottedplant", "sheep", "sofa", "train", "tvmonitor"}
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "DNN Scale Factor", 1, 10000, 78)
        sliders.setupTrackBar(1, "DNN MeanVal", 1, 255, 127)
        sliders.setupTrackBar(2, "DNN Confidence Threshold", 1, 100, 80)

        For i = 0 To kalman.Count - 1
            kalman(i) = New Kalman_Basics(ocvb)
            ReDim kalman(i).input(4 - 1)
            ReDim kalman(i).output(4 - 1)
        Next

        dnnWidth = src.Height ' height is always smaller than width...
        dnnHeight = src.Height
        crop = New cv.Rect(src.Width / 2 - dnnWidth / 2, src.Height / 2 - dnnHeight / 2, dnnWidth, dnnHeight)

        Dim infoText As New FileInfo(ocvb.HomeDir + "Data/MobileNetSSD_deploy.prototxt")
        If infoText.Exists Then
            Dim infoModel As New FileInfo(ocvb.HomeDir + "Data/MobileNetSSD_deploy.caffemodel")
            If infoModel.Exists Then
                net = CvDnn.ReadNetFromCaffe(infoText.FullName, infoModel.FullName)
                dnnPrepared = True
            End If
        End If
        If dnnPrepared = False Then
            ocvb.trueText("Caffe databases not found.  It should be in <OpenCVB_HomeDir>/Data.", 10, 100)
        End If
        desc = "Use OpenCV's dnn from Caffe file."
        label1 = "Cropped Input Image - must be square!"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If dnnPrepared Then
            Dim inScaleFactor = sliders.trackbar(0).Value / sliders.trackbar(0).Maximum ' should be 0.0078 by default...
            Dim meanVal = CSng(sliders.trackbar(1).Value)
            Dim inputBlob = CvDnn.BlobFromImage(src(crop), inScaleFactor, New cv.Size(300, 300), meanVal, False)
            src.CopyTo(dst2)
            src(crop).CopyTo(dst1(crop))
            net.SetInput(inputBlob, "data")

            Dim detection = net.Forward("detection_out")
            Dim detectionMat = New cv.Mat(detection.Size(2), detection.Size(3), cv.MatType.CV_32F, detection.Data)

            Dim confidenceThreshold = sliders.trackbar(2).Value / 100
            Dim rows = src(crop).Rows
            Dim cols = src(crop).Cols
            label2 = ""

            Dim kPoints As New List(Of cv.Point)
            For i = 0 To detectionMat.Rows - 1
                Dim confidence = detectionMat.Get(Of Single)(i, 2)
                If confidence > confidenceThreshold Then
                    Dim vec = detectionMat.Get(Of cv.Vec4f)(i, 3)
                    If kalman(i).input(0) = 0 And kalman(i).input(1) = 0 Then
                        kPoints.Add(New cv.Point2f(vec.Item0 * cols + crop.Left, vec.Item1 * rows + crop.Top))
                    Else
                        kPoints.Add(New cv.Point2f(kalman(i).input(0), kalman(i).input(1)))
                    End If
                End If
            Next

            Static activeKalman As Integer
            If kPoints.Count > activeKalman Then activeKalman = kPoints.Count
            For i = 0 To detectionMat.Rows - 1
                Dim confidence = detectionMat.Get(Of Single)(i, 2)
                If confidence > confidenceThreshold Then
                    Dim nextName = classNames(CInt(detectionMat.Get(Of Single)(i, 1)))
                    label2 += nextName + " "  ' display the name of what we found.
                    Dim vec = detectionMat.Get(Of cv.Vec4f)(i, 3)
                    rect = New cv.Rect(vec.Item0 * cols + crop.Left, vec.Item1 * rows + crop.Top, (vec.Item2 - vec.Item0) * cols, (vec.Item3 - vec.Item1) * rows)
                    rect = New cv.Rect(rect.X, rect.Y, Math.Min(dnnWidth, rect.Width), Math.Min(dnnHeight, rect.Height))

                    Dim pt = New cv.Point(rect.X, rect.Y)
                    Dim minIndex As Integer
                    Dim minDistance As Single = Single.MaxValue
                    For j = 0 To kPoints.Count - 1
                        Dim distance = Math.Sqrt((pt.X - kPoints(j).X) * (pt.X - kPoints(j).X) + (pt.Y - kPoints(j).Y) * (pt.Y - kPoints(j).Y))
                        If minDistance > distance Then
                            minIndex = j
                            minDistance = distance
                        End If
                    Next

                    If minIndex < kalman.Count Then
                        kalman(minIndex).input = {rect.X, rect.Y, rect.Width, rect.Height}
                        kalman(minIndex).Run(ocvb)
                        rect = New cv.Rect(kalman(minIndex).output(0), kalman(minIndex).output(1), kalman(minIndex).output(2), kalman(minIndex).output(3))
                    End If
                    dst2.Rectangle(rect, cv.Scalar.Yellow, 3, cv.LineTypes.AntiAlias)
                    rect.Width = src.Width / 12
                    rect.Height = src.Height / 16
                    dst2.Rectangle(rect, cv.Scalar.Black, -1)
                    ocvb.trueText(nextName, CInt(rect.X), CInt(rect.Y), 3)
                End If
            Next

            ' reinitialize any unused kalman filters.
            For i = kPoints.Count To activeKalman - 1
                If i < kalman.Count Then
                    kalman(i).input(0) = 0
                    kalman(i).input(1) = 0
                End If
            Next
        End If
    End Sub
End Class


