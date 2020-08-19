Imports System.Globalization
Imports System.IO
Imports System.Windows.Markup
Imports Satisfactory.ObjectModel

Class Application
    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

    Public Shared glbSourceFileFullName As String

    Private glbDataStoreViewModel As DataStoreView
    Private glbForm As MainWindow

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        glbSourceFileFullName = Path.Combine(My.Application.Info.DirectoryPath, "sources", "SatisfactoryRecipes2.xml")

        My.Settings.Upgrade()
        My.Settings.Reload()

        If (My.Settings.ActiveAlternateRecipes Is Nothing) Then
            Dim myPreviousSettings = My.Settings.GetPreviousVersion(NameOf(My.Settings.ActiveAlternateRecipes))
            If (myPreviousSettings IsNot Nothing) Then
                My.Settings.ActiveAlternateRecipes = myPreviousSettings
                My.Settings.Save()
            Else
                My.Settings.ActiveAlternateRecipes = New Specialized.StringCollection
            End If
        End If

        FrameworkElement.LanguageProperty.OverrideMetadata(GetType(FrameworkElement), New FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name)))

        Dim myItems As List(Of Item) = LoadItems()

        glbDataStoreViewModel = New DataStoreView
        glbDataStoreViewModel.SetItems(myItems, My.Settings.ActiveAlternateRecipes.Cast(Of String).ToList)

        glbForm = New MainWindow
        glbForm.DataContext = glbDataStoreViewModel
        glbForm.Show()
    End Sub
    Private Function LoadItems() As List(Of Item)
        Dim myStore = XMLHelper.ReadFromXML(Of DataStore)(glbSourceFileFullName)
        If (myStore IsNot Nothing) Then
            Return myStore.Items
        Else
            Throw New ApplicationException("Failed to load sources!")
        End If

        Return Nothing
    End Function

    Public Sub ReloadUI()
        Dim myOldForm = glbForm
        Dim myOldViewModel = glbDataStoreViewModel

        Dim myItems As List(Of Item) = LoadItems()

        Dim myNewViewModel = New DataStoreView
        myNewViewModel.SetItems(myItems, My.Settings.ActiveAlternateRecipes.Cast(Of String).ToList)

        For Each myProduction In myOldViewModel.Productions
            Dim myRecipe = myNewViewModel.Recipes.First(Function(x) x.Name = myProduction.Name AndAlso x.ItemType = myProduction.ItemType)
            Dim myItemsPerMinute = myProduction.ItemsPerMinute

            myNewViewModel.AddProduction(myRecipe, myProduction.ItemsPerMinute)
        Next

        glbDataStoreViewModel = myNewViewModel
        glbForm = New MainWindow
        glbForm.DataContext = glbDataStoreViewModel
        glbForm.Show()
        myOldForm.Close()
    End Sub


    Friend Sub LoadProductions(thisFile As String)
        If String.IsNullOrEmpty(thisFile) Then Exit Sub

        Dim myStore = XMLHelper.ReadFromXML(Of DataStore)(thisFile)

        If (myStore IsNot Nothing) AndAlso (myStore.Productions IsNot Nothing) AndAlso (myStore.Productions.Any()) Then
            glbDataStoreViewModel.Productions.Clear()

            For Each myProduction In myStore.Productions
                Dim myRecipe = glbDataStoreViewModel.Recipes.FirstOrDefault(Function(x) x.Name = myProduction.Name)
                If (myRecipe IsNot Nothing) Then glbDataStoreViewModel.AddProduction(myRecipe, myProduction.ItemsPerMinute)
            Next
        End If
    End Sub
    Friend Sub SaveProduction(thisFile As String)
        If String.IsNullOrEmpty(thisFile) Then Exit Sub

        Dim myStore As New DataStore()
        myStore.Productions.AddRange(glbDataStoreViewModel.Productions.Select(Function(x) x.baseObj))
        XMLHelper.WriteToXML(Of DataStore)(myStore, thisFile)
    End Sub
    'Private Function ReadFromXML(Of T)(f As String) As T
    '    If (String.IsNullOrEmpty(f)) Then Throw New ArgumentException(NameOf(f))

    '    Debug.Assert(IO.Path.GetExtension(f) = ".xml")
    '    Debug.Assert(IO.File.Exists(f), "File not be found!", String.Format(CultureInfo.DefaultThreadCurrentCulture, "FullPath: {0}", IO.Path.GetFullPath(f)))

    '    Dim myReturn As Object = Nothing

    '    Using myMemStream As MemoryStream = New MemoryStream()
    '        Using myFileReader As FileStream = File.OpenRead(f)
    '            myFileReader.CopyTo(myMemStream)
    '        End Using

    '        myMemStream.Seek(0, SeekOrigin.Begin)

    '        Try
    '            Using myXMLReader As XmlReader = XmlReader.Create(myMemStream,
    '                                                              New XmlReaderSettings With {
    '                                                              .CloseInput = True,
    '                                                              .IgnoreComments = True,
    '                                                              .IgnoreProcessingInstructions = True,
    '                                                              .IgnoreWhitespace = True
    '                                                              })

    '                Dim mySerializer As New XmlSerializer(GetType(T))
    '                AddHandler mySerializer.UnknownAttribute, AddressOf HandleUnknownAttribute
    '                AddHandler mySerializer.UnknownElement, AddressOf HandleUnknownElement

    '                If mySerializer.CanDeserialize(myXMLReader) Then
    '                    myReturn = mySerializer.Deserialize(myXMLReader)
    '                End If
    '            End Using
    '        Catch ex As Exception
    '            Debug.WriteLine(ex)
    '        End Try
    '    End Using

    '    If (myReturn IsNot Nothing) AndAlso (myReturn.GetType = GetType(T)) Then
    '        Return DirectCast(myReturn, T)
    '    End If

    '    Return Nothing
    'End Function

    'Public Function WriteToXML(Of T)(o As T, f As String) As String
    '    If (String.IsNullOrEmpty(f)) Then Throw New ArgumentException(NameOf(f))

    '    Debug.Assert(IO.Path.GetExtension(f) = ".xml")

    '    Using myMemStream As MemoryStream = New MemoryStream()
    '        Using myXMLWriter As XmlWriter = XmlWriter.Create(myMemStream,
    '                                                          New XmlWriterSettings With {
    '                                                          .Encoding = Text.Encoding.UTF8,
    '                                                          .Indent = True,
    '                                                          .CloseOutput = False,
    '                                                          .WriteEndDocumentOnClose = False
    '                                                          })

    '            Dim mySerializer As New XmlSerializer(GetType(T))
    '            mySerializer.Serialize(myXMLWriter, o)

    '            myXMLWriter.Flush()
    '            myXMLWriter.Close()
    '        End Using

    '        Using myFileWriter As FileStream = File.Create(f)
    '            myMemStream.Seek(0, SeekOrigin.Begin)
    '            myMemStream.CopyTo(myFileWriter)
    '            myFileWriter.Flush()
    '            myFileWriter.Close()
    '            Return IO.Path.GetFullPath(myFileWriter.Name)
    '        End Using
    '    End Using

    '    Return Nothing
    'End Function

    'Private Sub HandleUnknownAttribute(sender As Object, e As XmlAttributeEventArgs)
    '    Dim myEventString As String

    '    If (e.ObjectBeingDeserialized IsNot Nothing) Then
    '        myEventString = String.Format(CultureInfo.DefaultThreadCurrentCulture, "Object:'{0}'|Unknown Attr: '{1}'|Line:'{2:d4}'|Position:'{3:d3}'|Expected:'{4}'",
    '                                          New Object() {e.ObjectBeingDeserialized.GetType.Name,
    '                                                        e.Attr.Name,
    '                                                        e.LineNumber,
    '                                                        e.LinePosition,
    '                                                        e.ExpectedAttributes}
    '                                          )
    '    Else
    '        myEventString = String.Format(CultureInfo.DefaultThreadCurrentCulture, "Unknown Attr: '{1}'|Line:'{2:d4}'|Position:'{3:d3}'|Expected:'{4}'",
    '                                          New Object() {e.Attr.Name,
    '                                                        e.LineNumber,
    '                                                        e.LinePosition,
    '                                                        e.ExpectedAttributes}
    '                                          )
    '    End If

    '    Debug.WriteLine(myEventString)
    'End Sub
    'Private Sub HandleUnknownElement(sender As Object, e As XmlElementEventArgs)
    '    Dim myEventString As String

    '    If (e.ObjectBeingDeserialized IsNot Nothing) Then
    '        myEventString = String.Format(CultureInfo.DefaultThreadCurrentCulture, "Object:'{0}'|Unknown Element:'{1}'|Line:'{2:d4}'|Position:'{3:d3}'|Expected:'{4}'",
    '                                          New Object() {e.ObjectBeingDeserialized.GetType.Name,
    '                                                        e.Element.Name,
    '                                                        e.LineNumber,
    '                                                        e.LinePosition,
    '                                                        e.ExpectedElements}
    '                                          )
    '    Else
    '        myEventString = String.Format(CultureInfo.DefaultThreadCurrentCulture, "Unknown Element:'{1}'|Line:'{2:d4}'|Position:'{3:d3}'|Expected:'{4}'",
    '                                          New Object() {e.Element.Name,
    '                                                        e.LineNumber,
    '                                                        e.LinePosition,
    '                                                        e.ExpectedElements}
    '                                          )
    '    End If

    '    Debug.WriteLine(myEventString)
    'End Sub

End Class
