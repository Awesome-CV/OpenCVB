﻿Imports System.IO
Imports System.Text.RegularExpressions
Module UI_GeneratorMain
    Sub Main()
        Dim VBcodeDir As New DirectoryInfo(CurDir() + "/../../vb_classes")
        Dim fileNames As New List(Of String)
        Dim fileEntries As String() = Directory.GetFiles(VBcodeDir.FullName)

        Dim pythonAppDir As New IO.DirectoryInfo(VBcodeDir.FullName + "/Python/")

        ' we only want to review the python files that are included in the VB_Classes Project.  Other Python files may be support modules or just experiments.
        Dim projFile As New FileInfo(VBcodeDir.FullName + "/VB_Classes.vbproj")
        Dim readProj = New StreamReader(projFile.FullName)
        While readProj.EndOfStream = False
            Dim line = readProj.ReadLine()
            If Trim(line).StartsWith("<Content Include=") Then
                If InStr(line, "Python") Then
                    Dim startName = InStr(line, "Python")
                    line = Mid(line, startName)
                    Dim endName = InStr(line, """")
                    line = Mid(line, 1, endName - 1)
                    line = Mid(line, Len("Python/") + 1)
                    fileNames.Add(VBcodeDir.FullName + "\Python\" + line)
                End If
            End If
            If Trim(line).StartsWith("<Compile Include=") Then
                If InStr(line, ".vb""") Then
                    Dim startname = InStr(line, "=") + 2
                    line = Mid(line, startname)
                    Dim endName = InStr(line, """")
                    line = Mid(line, 1, endName - 1)
                    If line.Contains("AlgorithmList.vb") = False And line.Contains("My Project") = False Then fileNames.Add(VBcodeDir.FullName + "/" + line)
                End If
            End If
        End While
        readProj.Close()

        Dim className As String = ""
        Dim CodeLineCount As Int32
        Dim sortedNames As New SortedList(Of String, Int32)
        Dim sIndex As Integer
        For Each fileName In fileNames
            If fileName.EndsWith(".py") Then
                Dim fileinfo As New FileInfo(fileName)
                sortedNames.Add(fileinfo.Name, sIndex)
                sIndex += 1
                fileName = fileinfo.FullName
            Else
                If fileName.EndsWith("ocvbClass.vb") = False Then
                    Dim nextFile As New System.IO.StreamReader(fileName)
                    While nextFile.Peek() <> -1
                        Dim line = Trim(nextFile.ReadLine())
                        line = Replace(line, vbTab, "")
                        If line IsNot Nothing Then
                            If line.Substring(0, 1) <> "'" Then
                                If Len(line) > 0 Then CodeLineCount += 1
                                If LCase(line).StartsWith("public class") Then
                                    Dim split As String() = Regex.Split(line, "\W+")
                                    ' next line must be "Inherits ocvbClass"
                                    Dim line2 = Trim(nextFile.ReadLine())
                                    CodeLineCount += 1
                                    If line2.StartsWith(vbTab) Then line2 = Mid(line2, 2)
                                    If LCase(line2) = "inherits ocvbclass" Then className = split(2) ' public class <classname>
                                End If
                                If LCase(line).StartsWith("public sub new(ocvb as algorithmdata") Then
                                    sortedNames.Add(className, sIndex)
                                    sIndex += 1
                                End If
                            End If
                        End If
                    End While
                End If
            End If
        Next

        Dim cleanNames As New List(Of String)
        Dim lastName As String = ""
        For i = 0 To sortedNames.Count - 1
            Dim nextName = sortedNames.ElementAt(i).Key
            If nextName <> lastName + ".py" Then cleanNames.Add(nextName)
            lastName = nextName
        Next

        Dim listInfo As New FileInfo(CurDir() + "/../../UI_Generator/AlgorithmList.vb")
        Dim sw As New StreamWriter(listInfo.FullName)
        sw.WriteLine("' this file is automatically generated in a pre-build step.  Do not waste your time modifying manually.")
        sw.WriteLine("Module AlgorithmNameList")
        sw.WriteLine("Public callerSliderCounts() as integer")
        sw.WriteLine("Public callerCheckBoxCounts() as integer")
        sw.WriteLine("Public callerNames() as string = {")
        For i = 0 To cleanNames.Count - 1
            If i < cleanNames.Count - 1 Then
                sw.WriteLine("""" + cleanNames.Item(i) + """,")
            Else
                sw.WriteLine("""" + cleanNames.Item(i) + """")
            End If
        Next
        sw.WriteLine("}")
        sw.WriteLine("end module")
        sw.WriteLine("Public Class algorithmList")

        sw.WriteLine("Public Function createAlgorithm(ocvb As AlgorithmData, algorithmName as string) As Object")
        sw.WriteLine(vbTab + "redim callerSliderCounts(" + CStr(cleanNames.Count - 1) + ")")
        sw.WriteLine(vbTab + "redim callerCheckboxCounts(" + CStr(cleanNames.Count - 1) + ")")
        sw.WriteLine("Select Case ucase(algorithmName)")
        For i = 0 To cleanNames.Count - 1
            Dim nextName = cleanNames.Item(i)
            sw.WriteLine(vbTab + "case """ + UCase(nextName) + """")
            If nextName.EndsWith(".py") Then
                sw.WriteLine(vbTab + vbTab + "ocvb.PythonFileName = """ + pythonAppDir.FullName + nextName + """")
                sw.WriteLine(vbTab + vbTab + "return new Python_Run(ocvb)")
            Else
                sw.WriteLine(vbTab + vbTab + "return new " + nextName + "(ocvb)")
            End If
        Next
        sw.WriteLine("End Select")
        sw.WriteLine("return nothing")
        sw.WriteLine("End Function")

        sw.WriteLine("End Class")
        sw.Close()

        Dim textInfo As New FileInfo(VBcodeDir.FullName + "/../Data/AlgorithmList.txt")
        sw = New StreamWriter(textInfo.FullName)
        sw.WriteLine("CodeLineCount = " + CStr(CodeLineCount))
        For i = 0 To cleanNames.Count - 1
            sw.WriteLine(cleanNames.Item(i))
        Next
        sw.Close()

        Dim FilesInfo As New FileInfo(VBcodeDir.FullName + "/../Data/FileNames.txt")
        sw = New StreamWriter(FilesInfo.FullName)
        For i = 0 To fileNames.Count - 1
            sw.WriteLine(fileNames.Item(i))
        Next
        sw.Close()
    End Sub
End Module
