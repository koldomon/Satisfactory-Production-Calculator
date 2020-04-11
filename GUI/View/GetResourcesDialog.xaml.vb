Imports System.Net.Http
Imports System.Threading.Tasks.Dataflow
Imports HtmlAgilityPack

Public Class GetResourcesDialog
    Dim InBlock As BufferBlock(Of Tuple(Of String, Uri))
    Dim LoadBlock As TransformBlock(Of Tuple(Of String, Uri), Tuple(Of String, HtmlDocument))
    Dim ProcessBlock As TransformManyBlock(Of Tuple(Of String, HtmlDocument), Resource)
    Dim OutBlock As BufferBlock(Of Resource)

    Private Sub GetResourcesDialog_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Dim myExecutionOptions As New ExecutionDataflowBlockOptions With {.EnsureOrdered = False}

        InBlock = New BufferBlock(Of Tuple(Of String, Uri))(myExecutionOptions)
        LoadBlock = New TransformBlock(Of Tuple(Of String, Uri), Tuple(Of String, HtmlDocument))(AddressOf LoadItemPage, New ExecutionDataflowBlockOptions With {.EnsureOrdered = False, .MaxDegreeOfParallelism = 1})
        ProcessBlock = New TransformManyBlock(Of Tuple(Of String, HtmlDocument), Resource)(AddressOf ProcessItemPage, New ExecutionDataflowBlockOptions With {.EnsureOrdered = False, .MaxDegreeOfParallelism = 1})
        OutBlock = New BufferBlock(Of Resource)(myExecutionOptions)

        Dim myLinkOptions As New DataflowLinkOptions With {.PropagateCompletion = True}

        InBlock.LinkTo(LoadBlock, myLinkOptions)
        LoadBlock.LinkTo(ProcessBlock, myLinkOptions)
        ProcessBlock.LinkTo(OutBlock, myLinkOptions)
    End Sub

    Private Sub btnStart_Click(sender As Object, e As RoutedEventArgs)
        Dispatcher.BeginInvoke(Threading.DispatcherPriority.Background, Sub() Process())
    End Sub

    Private Async Sub Process()
        GetWebResource()
        ParseIndexPage()

        Await ProcessBlock.Completion

        Dim mySatisResource As New SatifactoryResources
        Dim myResource As New Resource
        While OutBlock.TryReceive(myResource)
            mySatisResource.Recipes.Add(myResource)
        End While

        If ProcessBlock.Completion.IsCompleted = False Then Throw New Exception()

        OutBlock.Complete()

        My.Application.WriteToXML(Of SatifactoryResources)(mySatisResource, IO.Path.Combine(My.Application.Info.DirectoryPath, "Sources", "SatisfactoryRecipes.xml"))
    End Sub

    Private WebURI As UriBuilder
    Private WebClient As New HtmlWeb
    Private ItemIndexPage As HtmlDocument
    Private ItemURIs As New List(Of Tuple(Of String, Uri))
    Private Items As New List(Of Resource)
    Private Sub GetWebResource()
        WebURI = New UriBuilder(txtSourceUrl.Text)

        ItemIndexPage = WebClient.LoadFromWebAsync(WebURI.Uri.AbsoluteUri).Result

        Dispatcher.BeginInvoke(Threading.DispatcherPriority.Normal, Sub() WriteToHTML(ItemIndexPage.Text))
    End Sub

    Private Sub ParseIndexPage()
        Dim myItemNodes = ItemIndexPage.DocumentNode.SelectNodes("//h6[@class='m-0' and a]/a")

        For Each myNode In myItemNodes
            Dim myURI As String = String.Empty
            Dim myNameNode As HtmlNode
            Dim myName As String = String.Empty

            myURI = myNode.GetAttributeValue("href", String.Empty)
            myNameNode = myNode.SelectSingleNode("strong")
            If (myNameNode IsNot Nothing) Then myName = myNameNode.InnerText.Trim

            Dim myItemUri = New UriBuilder(WebURI.Scheme, WebURI.Host, WebURI.Port, myURI)
            InBlock.Post(New Tuple(Of String, Uri)(myName, myItemUri.Uri))
        Next
        InBlock.Complete()
    End Sub


    Private Async Function LoadItemPage(thisItems As Tuple(Of String, Uri)) As Task(Of Tuple(Of String, HtmlDocument))
        Dim myDoc = Await WebClient.LoadFromWebAsync(thisItems.Item2.AbsoluteUri)
        If myDoc IsNot Nothing Then
            Return New Tuple(Of String, HtmlDocument)(thisItems.Item1, myDoc)
        Else
            Return Nothing
        End If
    End Function
    Private Function ProcessItemPage(thisItems As Tuple(Of String, HtmlDocument)) As List(Of Resource)
        Dim myReturns As New List(Of Resource)
        Dim myNodes As HtmlNodeCollection

        Select Case GetCategory(thisItems.Item1)
            Case "Ore", "BasicFluid", "BasicItem"
                'Ores and Fluids
                myNodes = thisItems.Item2.DocumentNode.SelectNodes(".//table[@class='table mb-0']/tbody/tr[@class='table-secondary']")
                If (myNodes IsNot Nothing) AndAlso (myNodes.Any) Then
                    myReturns.Add(ProcessBasicResource(thisItems.Item1, myNodes.ToList))
                    Return myReturns
                Else
                    myReturns.Add(New Resource With {.Name = thisItems.Item1, .Type = thisItems.Item1, .Category = GetCategory(thisItems.Item1), .IsAlternativeRecipe = False})
                    Return myReturns
                End If
            Case Else
                'normal Items
                myNodes = thisItems.Item2.DocumentNode.SelectNodes(".//div[@class='card mt-3' and (div[@class='card-header border-bottom-0'] or div[@class='card-header'])]")
                If (myNodes IsNot Nothing) AndAlso (myNodes.Any) Then
                    myReturns.AddRange(ProcessItemResource(thisItems.Item1, myNodes.ToList))
                    Return myReturns
                End If
        End Select

        If (Not myReturns.Any) Then
            myReturns.Add(New Resource With {.Name = thisItems.Item1, .Type = thisItems.Item1, .Category = GetCategory(thisItems.Item1), .IsAlternativeRecipe = False})
        End If

        Return myReturns
    End Function

    Private Function ProcessItemResource(thisName As String, thisNodes As List(Of HtmlNode)) As List(Of Resource)
        Dim myReturns As New List(Of Resource)

        For Each myCategoryNode In thisNodes
            Dim myHeadNode = myCategoryNode.SelectSingleNode(".//div/strong")
            Dim myHead = myHeadNode.InnerText.Trim
            If myHead.ToLower.Contains("recipe") Then
                Dim myIsAlternate = GetAlternate(myHead)

                Dim myRecipeNodes = myCategoryNode.SelectNodes(".//div[@class='card-body border-top']")
                For Each myRecipeNode In myRecipeNodes
                    'Get Name
                    Dim myNameNode = myRecipeNode.SelectSingleNode(".//div/h5")
                    Dim myName = myNameNode.InnerText.Trim

                    'Create new Resource
                    Dim myResource = New Resource With {.Type = thisName, .Category = GetCategory(myName), .IsAlternativeRecipe = myIsAlternate}

                    'Set name
                    If myIsAlternate = False Then
                        myResource.Name = myName
                    Else
                        myResource.Name = myName.Replace("Alternate: ", String.Empty).Trim
                    End If

                    'Parse RecipeItems
                    Dim myChildNodes = myRecipeNode.SelectNodes(".//div[@class='col-6' and div]")

                    'Parse Inputs
                    Dim myInputNodes = myChildNodes(0).SelectNodes(".//div")
                    For Each myInputNode In myInputNodes
                        Dim myInputName = myInputNode.SelectSingleNode(".//a").InnerText.Trim
                        Dim myInputPerMinuteText = myInputNode.SelectSingleNode(".//span/em/small").InnerText
                        Dim myInputPerMinute = GetItemsPerMinute(myInputPerMinuteText)
                        If (myResource.Resources Is Nothing) Then myResource.Resources = New List(Of Resource)
                        myResource.Resources.Add(New Resource With {.Type = myInputName, .ItemsPerMinute = myInputPerMinute})
                    Next


                    'Parse Outputs
                    Dim myOutputNodes = myChildNodes(1).SelectNodes(".//div")

                    Dim myOutputNode = myOutputNodes.First(Function(x) x.InnerText.Contains(thisName))
                    Dim myOutputName = myOutputNode.SelectSingleNode(".//a").InnerText.Trim
                    Dim myOutputPerMinuteText = myOutputNode.SelectSingleNode(".//span/em/small").InnerText
                    Dim myOutputPerMinute = GetItemsPerMinute(myOutputPerMinuteText)
                    myResource.ProductionPerMinute = myOutputPerMinute

                    'Parse Additional Outputs
                    If (myOutputNodes.Count > 1) Then
                        Dim myAdditionalOutputNodes = myOutputNodes.Where(Function(x) Not x.InnerText.Contains(thisName))
                        For Each myAdditionalOutputNode In myAdditionalOutputNodes
                            Dim myAdditionalOutputName = myAdditionalOutputNode.SelectSingleNode(".//a").InnerText.Trim
                            Dim myAdditionalOutputPerMinuteText = myAdditionalOutputNode.SelectSingleNode(".//span/em/small").InnerText
                            Dim myAdditionalOutputPerMinute = GetItemsPerMinute(myAdditionalOutputPerMinuteText)
                            If myResource.AdditionalProductions Is Nothing Then myResource.AdditionalProductions = New List(Of Resource)
                            myResource.AdditionalProductions.Add(New Resource With {.Type = myAdditionalOutputName, .ProductionPerMinute = myAdditionalOutputPerMinute})
                        Next
                    End If

                    Dispatcher.BeginInvoke(Threading.DispatcherPriority.Normal, Sub() WriteToLog(String.Format("{0}: {1}", myResource.Name, myResource.Category)))

                    myReturns.Add(myResource)
                Next
            End If
        Next

        Return myReturns
    End Function
    Private Function ProcessBasicResource(thisName As String, thisNodes As List(Of HtmlNode)) As Resource
        Dim myReturn As Resource = Nothing

        'Dim myNameNode = thisNodes(0).SelectSingleNode(".//td/h5")
        'Dim myName = myNameNode.InnerText.Trim
        myReturn = New Resource With {.Name = thisName, .Type = thisName, .Category = GetCategory(thisName), .IsAlternativeRecipe = False}
        Select Case myReturn.Category
            Case "Ore"
                myReturn.ProductionPerMachine = 120
            Case "BasicFluid"
                myReturn.ProductionPerMachine = 120
            Case "BasicItem"
                myReturn.ProductionPerMachine = 120
        End Select
        Dispatcher.BeginInvoke(Threading.DispatcherPriority.Normal, Sub() WriteToLog(String.Format("{0}: {1}", myReturn.Name, myReturn.Category)))

        Return myReturn
    End Function

    Private Function GetAlternate(thisName As String) As Boolean
        If thisName.StartsWith("Alternate") Then
            Return True
        Else
            Return False
        End If
    End Function
    Private Function GetCategory(thisName As String) As String
        Select Case thisName
            Case "Limestone", "Coal", "Raw Quartz", "Sulfur", "Bauxite", "Uranium"
                Return "Ore"
            Case Else
                If thisName.EndsWith("Ore", StringComparison.InvariantCultureIgnoreCase) Then Return "Ore"
        End Select

        Select Case thisName
            Case "Water", "Crude Oil"
                Return "BasicFluid"
            Case "Heavy Oil Residue", "Fuel", "Liquid Biofuel", "Turbofuel", "Alumina Solution", "Sulfuric Acid"
                Return "Fluid"
            Case "Flower Petals", "Leaves", "Mycelia", "Wood"
                Return "BasicItem"
            Case Else
                If thisName.EndsWith("Slug", StringComparison.InvariantCultureIgnoreCase) Then Return "BasicItem"
        End Select

        Select Case thisName
            Case "Concrete", "Quartz Crystal", "Silica", "Polymer Resin", "Aluminum Scrap", "Black Powder"
                Return "BasicMaterial"
            Case Else
                If thisName.EndsWith("Ingot", StringComparison.InvariantCultureIgnoreCase) Then Return "Ingot"
        End Select

        Return "Item"
    End Function
    Private Function GetItemsPerMinute(thisText As String) As Double
        Dim myReturn As Double
        Dim myNumberFormatProvider = Globalization.CultureInfo.GetCultureInfo("en").NumberFormat
        Dim myItemsPerMinuteRexEx As New System.Text.RegularExpressions.Regex("(?<=\()(?<item>[0-9.]*)")
        Dim myMatch = myItemsPerMinuteRexEx.Match(thisText)
        If (myMatch IsNot Nothing) Then
            If Double.TryParse(myMatch.Value, Globalization.NumberStyles.Number, myNumberFormatProvider, myReturn) Then
                Return myReturn
            End If
        End If

        Return myReturn
    End Function
    Private Sub WriteToHTML(thisText As String)
        txtHTML.AppendText(thisText)
        txtHTML.AppendText(Environment.NewLine)
    End Sub
    Private Sub WriteToLog(thisText As String)
        txtLog.AppendText(thisText)
        txtLog.AppendText(Environment.NewLine)
    End Sub
End Class
