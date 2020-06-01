Imports cv = OpenCvSharp
' http://ptgmedia.pearsoncmg.com/images/0672320665/downloads/The%20Game%20of%20Life.html
Public Class CellAuto_Life
    Inherits ocvbClass
    Dim random As Random_Points
    Dim grid As cv.Mat
    Dim nextgrid As cv.Mat
    Dim factor = 8
    Dim generation As Integer
    Public population As Integer
    Private Function CountNeighbors(cellX As Integer, cellY As Integer) As Integer
        If cellX > 0 And cellY > 0 Then
            If grid.Get(Of Byte)(cellY - 1, cellX - 1) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY - 1, cellX) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY, cellX - 1) Then CountNeighbors += 1
        End If
        If cellX < grid.Width - 1 And cellY < grid.Height - 1 Then
            If grid.Get(Of Byte)(cellY + 1, cellX + 1) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY + 1, cellX) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY, cellX + 1) Then CountNeighbors += 1
        End If
        If cellX > 0 And cellY < grid.Height - 1 Then
            If grid.Get(Of Byte)(cellY + 1, cellX - 1) Then CountNeighbors += 1
        End If
        If cellX < grid.Width - 1 And cellY > 0 Then
            If grid.Get(Of Byte)(cellY - 1, cellX + 1) Then CountNeighbors += 1
        End If
        Return CountNeighbors
    End Function
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New cv.Mat(src.Height / factor, src.Width / factor, cv.MatType.CV_8UC1).SetTo(0)
        nextgrid = grid.Clone()

        random = New Random_Points(ocvb)
        random.rangeRect = New cv.Rect(0, 0, grid.Width, grid.Height)
        random.sliders.TrackBar1.Value = grid.Width * grid.Height * 0.3 ' we want about 30% of cells filled.
        ocvb.desc = "Use OpenCV to implement the Game of Life"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static savePointCount As Integer
        If random.sliders.TrackBar1.Value <> savePointCount Or generation = 0 Then
            random.Run(ocvb)
            generation = 0
            savePointCount = random.sliders.TrackBar1.Value
            For i = 0 To random.Points.Count - 1
                grid.Set(Of Byte)(random.Points(i).Y, random.Points(i).X, 1)
            Next
        End If
        generation += 1

        population = 0
        dst1.SetTo(0)
        For y = 0 To grid.Height - 1
            For x = 0 To grid.Width - 1
                Dim neighbors = CountNeighbors(x, y)
                If neighbors = 2 Or neighbors = 3 Then
                    If neighbors = 2 Then
                        nextgrid.Set(Of Byte)(y, x, grid.Get(Of Byte)(y, x))
                    Else
                        nextgrid.Set(Of Byte)(y, x, 1)
                    End If
                Else
                    nextgrid.Set(Of Byte)(y, x, 0)
                End If
                If nextgrid.Get(Of Byte)(y, x) Then
                    Dim pt = New cv.Point(x, y) * factor
                    dst1.Circle(pt, factor / 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                    population += 1
                End If
            Next
        Next

        Static lastPopulation As Integer
        Const countInit = 200
        Static countdown As Integer = countInit
        Dim countdownText = ""
        If lastPopulation = population Then
            countdown -= 1
            countdownText = " Restart in " + CStr(countdown)
            If countdown = 0 Then
                countdownText = ""
                generation = 0
                countdown = countInit
            End If
        End If
        lastPopulation = population
        label1 = "Population " + CStr(population) + " Generation = " + CStr(generation) + countdownText
        grid = nextgrid.Clone()
    End Sub
End Class






' http://ptgmedia.pearsoncmg.com/images/0672320665/downloads/The%20Game%20of%20Life.html
Public Class CellAuto_LifePopulation
    Inherits ocvbClass
    Dim plot As Plot_OverTime
    Dim game As CellAuto_Life
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        game = New CellAuto_Life(ocvb)

        plot = New Plot_OverTime(ocvb)
        plot.dst1 = dst2
        plot.maxScale = 2000
        plot.plotCount = 1

        ocvb.desc = "Show Game of Life display with plot of population"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        game.Run(ocvb)
        dst1 = game.dst1

        plot.plotData = New cv.Scalar(game.population, 0, 0)
        plot.Run(ocvb)
        dst2 = plot.dst1
    End Sub
End Class






' https://mathworld.wolfram.com/ElementaryCellularAutomaton.html
Public Class CellAuto_Basics
    Inherits ocvbClass
    Public i18 As New List(Of String)
    Public Function createCells(outStr As String) As cv.Mat
        Dim inputCombo = "111,110,101,100,011,010,001,000"
        Dim input(,) = {{1, 1, 1}, {1, 1, 0}, {1, 0, 1}, {1, 0, 0}, {0, 1, 1}, {0, 1, 0}, {0, 0, 1}, {0, 0, 0}}

        Dim outcomes(8 - 1) As Byte
        For i = 0 To outcomes.Length - 1
            outcomes(i) = Integer.Parse(outStr.Substring(i, 1))
        Next

        Dim dst = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, 0)
        dst.Set(Of Byte)(0, dst.Width / 2, 1)
        For y = 0 To dst.Height - 2
            For x = 1 To dst.Width - 2
                Dim x1 = dst.Get(Of Byte)(y, x - 1)
                Dim x2 = dst.Get(Of Byte)(y, x)
                Dim x3 = dst.Get(Of Byte)(y, x + 1)
                For i = 0 To input.Length - 1
                    If x1 = input(i, 0) And x2 = input(i, 1) And x3 = input(i, 2) Then
                        dst.Set(Of Byte)(y + 1, x, outcomes(i))
                        Exit For
                    End If
                Next
            Next
        Next
        Return dst.ConvertScaleAbs(255)
    End Function

    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        i18.Add("00011110 Rule 30 (chaotic)")
        i18.Add("00110110 Rule 54")
        i18.Add("00111100 Rule 60")
        i18.Add("00111110 Rule 62")
        i18.Add("01011010 Rule 90")
        i18.Add("01011110 Rule 94")
        i18.Add("01100110 Rule 102")
        i18.Add("01101110 Rule 110")
        i18.Add("01111010 Rule 122")

        i18.Add("01111110 Rule 126")
        i18.Add("10010110 Rule 150")
        i18.Add("10011110 Rule 158")
        i18.Add("10110110 Rule 182")
        i18.Add("10111100 Rule 188")
        i18.Add("10111110 Rule 190")
        i18.Add("11011100 Rule 220")
        i18.Add("11011110 Rule 222")
        i18.Add("11111010 Rule 250")

        Dim inputCombo = "111,110,101,100,011,010,001,000"
        Dim label = "The 18 most interesting automata from the first 256 in 'New Kind of Science'" + vbCrLf + "The input combinations are: " + inputCombo
        combo.Setup(ocvb, label + vbCrLf + "output below:", i18)

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Rotate through the different rules"
        check.Box(0).Checked = True

        ocvb.desc = "Visualize the 30 interesting examples from the first 256 in 'New Kind of Science'"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 2 Then dst2 = createCells(combo.Box.Text) Else dst1 = createCells(combo.Box.Text)
        If check.Box(0).Checked Then
            Dim index = combo.Box.SelectedIndex
            If index + 1 < i18.Count - 1 Then combo.Box.SelectedIndex += 1 Else combo.Box.SelectedIndex = 0
        End If
    End Sub
End Class


' https://mathworld.wolfram.com/ElementaryCellularAutomaton.html
Public Class CellAuto_Basics_MP
    Inherits ocvbClass
    Dim cell As CellAuto_Basics
    Dim i18 As New List(Of String)
    Dim i18Index As Integer
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        cell = New CellAuto_Basics(ocvb)
        i18 = cell.i18

        ocvb.desc = "Multi-threaded version of CellAuto_Basics"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Parallel.For(0, 2,
          Sub(i)
              Select Case i
                  Case 0
                      label1 = i18.ElementAt(i18Index)
                      dst1 = cell.createCells(label1)
                  Case 1
                      If i18Index + 1 < i18.Count - 1 Then i18Index += 1 Else i18Index = 0
                      label2 = i18.ElementAt(i18Index)
                      dst2 = cell.createCells(label2)
              End Select
          End Sub)
    End Sub
End Class




' https://mathworld.wolfram.com/ElementaryCellularAutomaton.html
Public Class CellAuto_All256
    Inherits ocvbClass
    Dim cell As CellAuto_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        cell = New CellAuto_Basics(ocvb)
        cell.combo.Visible = False ' won't need this...

        sliders.setupTrackBar1(ocvb, "Current Rule", 0, 255, 0)
        ocvb.desc = "Run through all 256 combinations of outcomes"
    End Sub
    Private Function createOutcome(val As Integer) As String
        Dim outstr As String = ""
        For i = 0 To 8 - 1
            outstr = CStr(val Mod 2) + outstr
            val = Math.Floor(val / 2)
        Next
        Return outstr
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        Dim index = sliders.TrackBar1.Value
        Dim mtOn = cell.check.Box(0).Checked
        Parallel.For(0, 2,
          Sub(i)
              Select Case i
                  Case 0
                      label1 = createOutcome(index) + " index = " + CStr(index)
                      dst1 = cell.createCells(label1)
                  Case 1
                      If mtOn = False Then Exit Sub
                      If index < 255 Then index += 1 Else index = 0
                      label2 = createOutcome(index) + " index = " + CStr(index)
                      dst2 = cell.createCells(label2)
              End Select
          End Sub)
        sliders.TrackBar1.Value = index
    End Sub
End Class