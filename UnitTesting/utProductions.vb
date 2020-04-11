Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Xml
Imports System.Xml.Serialization
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Satisfactory

<TestClass()> Public Class TestViewModel
    Public Shared glbViewModel As MainViewModel

    <ClassInitialize> Public Shared Sub InitClass(thisContext As TestContext)
        If (thisContext Is Nothing) Then Throw New ArgumentNullException(NameOf(thisContext))

        If (My.Settings.ActiveAlternateRecipes Is Nothing) Then My.Settings.ActiveAlternateRecipes = New Specialized.StringCollection

        Dim myRecipes As List(Of Resource) = LoadRecipes()
        If myRecipes Is Nothing Then Throw New NullReferenceException("Failed to load SatisfactoryRecipes.xml")

        glbViewModel = New MainViewModel
        glbViewModel.SetRecipes(myRecipes, My.Settings.ActiveAlternateRecipes.Cast(Of String).ToList)
    End Sub

    <DataTestMethod, DynamicData(NameOf(GetRecipes), DynamicDataSourceType.Method)>
    Public Sub TestAllProductions(thisRecipe As ResourceView)
        Dim myProdView = glbViewModel.AddProduction(thisRecipe, 10)
        Assert.IsNotNull(myProdView.Productions)
        Assert.IsNotNull(myProdView.AdditionalItems)

    End Sub

    Private Shared Iterator Function GetRecipes() As IEnumerable(Of Object())
        For Each myRecipe In glbViewModel.Recipes
            Yield New Object() {myRecipe}
        Next
    End Function

    Private Shared Function LoadRecipes() As List(Of Resource)
        Dim myStore = ReadFromXML(Of SatifactoryResources)(IO.Path.Combine(".\sources", "SatisfactoryRecipes.xml"))
        If (myStore IsNot Nothing) Then
            Return myStore.Recipes
        End If

        Return Nothing
    End Function

    Private Shared Function ReadFromXML(Of T)(f As String) As T
        If (String.IsNullOrEmpty(f)) Then Throw New ArgumentException(NameOf(f))

        Debug.Assert(IO.Path.GetExtension(f) = ".xml")
        Debug.Assert(IO.File.Exists(f), "File not be found!", String.Format(CultureInfo.DefaultThreadCurrentCulture, "FullPath: {0}", IO.Path.GetFullPath(f)))

        Dim myReturn As Object = Nothing

        Using myMemStream As MemoryStream = New MemoryStream()
            Using myFileReader As FileStream = File.OpenRead(f)
                myFileReader.CopyTo(myMemStream)
            End Using

            myMemStream.Seek(0, SeekOrigin.Begin)

            Try
                Using myXMLReader As XmlReader = XmlReader.Create(myMemStream,
                                                                  New XmlReaderSettings With {
                                                                  .CloseInput = True,
                                                                  .IgnoreComments = True,
                                                                  .IgnoreProcessingInstructions = True,
                                                                  .IgnoreWhitespace = True
                                                                  })

                    Dim mySerializer As New XmlSerializer(GetType(T))
                    AddHandler mySerializer.UnknownAttribute, AddressOf HandleUnknownAttribute
                    AddHandler mySerializer.UnknownElement, AddressOf HandleUnknownElement

                    If mySerializer.CanDeserialize(myXMLReader) Then
                        myReturn = mySerializer.Deserialize(myXMLReader)
                    End If
                End Using
            Catch ex As Exception
                Debug.WriteLine(ex)
            End Try
        End Using

        If (myReturn IsNot Nothing) AndAlso (myReturn.GetType = GetType(T)) Then
            Return DirectCast(myReturn, T)
        End If

        Return Nothing
    End Function

    Public Shared Function WriteToXML(Of T)(o As T, f As String) As String
        If (String.IsNullOrEmpty(f)) Then Throw New ArgumentException(NameOf(f))

        Debug.Assert(IO.Path.GetExtension(f) = ".xml")

        Using myMemStream As MemoryStream = New MemoryStream()
            Using myXMLWriter As XmlWriter = XmlWriter.Create(myMemStream,
                                                              New XmlWriterSettings With {
                                                              .Encoding = Text.Encoding.UTF8,
                                                              .Indent = True,
                                                              .CloseOutput = False,
                                                              .WriteEndDocumentOnClose = False
                                                              })

                Dim mySerializer As New XmlSerializer(GetType(T))
                mySerializer.Serialize(myXMLWriter, o)

                myXMLWriter.Flush()
                myXMLWriter.Close()
            End Using

            Using myFileWriter As FileStream = File.Create(f)
                myMemStream.Seek(0, SeekOrigin.Begin)
                myMemStream.CopyTo(myFileWriter)
                myFileWriter.Flush()
                myFileWriter.Close()
                Return IO.Path.GetFullPath(myFileWriter.Name)
            End Using
        End Using

        Return Nothing
    End Function

    Private Shared Sub HandleUnknownAttribute(sender As Object, e As XmlAttributeEventArgs)
        Dim myEventString As String

        If (e.ObjectBeingDeserialized IsNot Nothing) Then
            myEventString = String.Format(CultureInfo.DefaultThreadCurrentCulture, "Object:'{0}'|Unknown Attr: '{1}'|Line:'{2:d4}'|Position:'{3:d3}'|Expected:'{4}'",
                                              New Object() {e.ObjectBeingDeserialized.GetType.Name,
                                                            e.Attr.Name,
                                                            e.LineNumber,
                                                            e.LinePosition,
                                                            e.ExpectedAttributes}
                                              )
        Else
            myEventString = String.Format(CultureInfo.DefaultThreadCurrentCulture, "Unknown Attr: '{1}'|Line:'{2:d4}'|Position:'{3:d3}'|Expected:'{4}'",
                                              New Object() {e.Attr.Name,
                                                            e.LineNumber,
                                                            e.LinePosition,
                                                            e.ExpectedAttributes}
                                              )
        End If

        Debug.WriteLine(myEventString)
    End Sub
    Private Shared Sub HandleUnknownElement(sender As Object, e As XmlElementEventArgs)
        Dim myEventString As String

        If (e.ObjectBeingDeserialized IsNot Nothing) Then
            myEventString = String.Format(CultureInfo.DefaultThreadCurrentCulture, "Object:'{0}'|Unknown Element:'{1}'|Line:'{2:d4}'|Position:'{3:d3}'|Expected:'{4}'",
                                              New Object() {e.ObjectBeingDeserialized.GetType.Name,
                                                            e.Element.Name,
                                                            e.LineNumber,
                                                            e.LinePosition,
                                                            e.ExpectedElements}
                                              )
        Else
            myEventString = String.Format(CultureInfo.DefaultThreadCurrentCulture, "Unknown Element:'{1}'|Line:'{2:d4}'|Position:'{3:d3}'|Expected:'{4}'",
                                              New Object() {e.Element.Name,
                                                            e.LineNumber,
                                                            e.LinePosition,
                                                            e.ExpectedElements}
                                              )
        End If

        Debug.WriteLine(myEventString)
    End Sub

End Class