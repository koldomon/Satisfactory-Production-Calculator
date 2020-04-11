Imports System.Threading.Tasks.Dataflow
Imports HtmlAgilityPack

Public Class GetResources_FromGamepedia_DataFlow

    Public [Input] As BufferBlock(Of Uri)
    Private ParseList As TransformManyBlock(Of Uri, Tuple(Of String, HtmlDocument))
    Private ParseResource As TransformManyBlock(Of Tuple(Of String, HtmlDocument), Resource)
    Public [Output] As BufferBlock(Of Resource)

    Dim myExecuteSingleOptions As New ExecutionDataflowBlockOptions With {.EnsureOrdered = True, .MaxDegreeOfParallelism = 1, .TaskScheduler = TaskScheduler.Default}
    Dim myExecuteMultiOptions As New ExecutionDataflowBlockOptions With {.EnsureOrdered = False, .MaxDegreeOfParallelism = 1, .TaskScheduler = TaskScheduler.Default}

    Public Sub New()
        Me.Input = New BufferBlock(Of Uri)(myExecuteSingleOptions)
        Me.ParseList = New TransformManyBlock(Of Uri, Tuple(Of String, HtmlDocument))(AddressOf ParseListFunction, myExecuteSingleOptions)
        Me.ParseResource = New TransformManyBlock(Of Tuple(Of String, HtmlDocument), Resource)(AddressOf ParseResourceFunction, myExecuteMultiOptions)
        Me.Output = New BufferBlock(Of Resource)

        BuildPipeline()
    End Sub

    Dim myLinkOptions As New DataflowLinkOptions With {.PropagateCompletion = True}
    Private Sub BuildPipeline()
        Me.Input.LinkTo(Me.ParseList, myLinkOptions)
        Me.ParseList.LinkTo(Me.ParseResource, myLinkOptions)
        Me.ParseResource.LinkTo(Me.Output, myLinkOptions)
    End Sub

    Private Async Function ParseListFunction(thisUri As Uri) As Task(Of IEnumerable(Of Tuple(Of String, HtmlDocument)))
        Dim myReturn As New List(Of Tuple(Of String, HtmlDocument))

        Dim myURIBuilder As New UriBuilder(thisUri)
        Dim myHtmlWeb As New HtmlWeb()
        Dim myDoc As HtmlDocument = Await myHtmlWeb.LoadFromWebAsync(myURIBuilder.Uri.AbsoluteUri)

        Dim myMainNode As HtmlNode = myDoc.DocumentNode.SelectSingleNode(".//tbody[tr/th[@class='navbox-title' and span='Items']]")
        Dim myItemNodes = myMainNode.SelectNodes(".//td[@class='navbox-list navbox-odd' or @class='navbox-list navbox-even']/div/span/a[2]")

        Dim FuncSelectReturn = Function(x As HtmlNode) As Tuple(Of String, HtmlDocument)
                                   Dim mySubDoc = myHtmlWeb.Load(New UriBuilder(myURIBuilder.Uri) With {.Path = x.GetAttributeValue("href", "")}.Uri.AbsoluteUri)
                                   Return New Tuple(Of String, HtmlDocument)(x.InnerText.Trim, mySubDoc)
                               End Function

        myReturn.AddRange(myItemNodes.Select(FuncSelectReturn))

        myURIBuilder = Nothing
        myHtmlWeb = Nothing
        myDoc = Nothing

        GC.Collect()

        Return myReturn
    End Function

    Private Function ParseResourceFunction(thisItem As Tuple(Of String, HtmlDocument)) As List(Of Resource)
        Dim myReturn As New List(Of Resource)
        Dim myMainNode As HtmlNode

        Dim myItemType = GetNodeValue(thisItem.Item2.DocumentNode, ".//h1[@id='firstHeading']")

        myMainNode = thisItem.Item2.DocumentNode.SelectSingleNode(".//table[@class='wikitable' and preceding-sibling::h3/span[@id='Crafting']]")
        If (myMainNode IsNot Nothing) Then
            Dim myRecipeNodes = myMainNode.SelectNodes(".//tr[@class='firstRow']")
            For Each myRecipeNode In myRecipeNodes
                Dim myNodes As New List(Of HtmlNode)
                myNodes.Add(myRecipeNode)

                Dim myAddNode = myRecipeNode.NextSibling
                If (myAddNode IsNot Nothing) AndAlso (myAddNode.GetAttributeValue("class", String.Empty) = String.Empty) Then myNodes.Add(myAddNode)
                Dim myRecipe = ParseRecipeNode(myNodes)
                If (Not myReturn.Contains(myRecipe)) Then myReturn.Add(myRecipe)
            Next
        End If

        myMainNode = thisItem.Item2.DocumentNode.SelectSingleNode(".//table[@class='wikitable' and tbody/tr/th='Type']")
        If (myMainNode IsNot Nothing) Then
            Dim myResource = myReturn.FirstOrDefault(Function(x) x.Name = myItemType AndAlso x.Type = myItemType)
            If (myResource Is Nothing) Then myResource = New Resource With {.Name = myItemType, .Type = myItemType, .Category = GetCategory(myItemType)}

            Dim myGlobalResourcesNodes = myMainNode.SelectNodes(".//tr")
            If (myGlobalResourcesNodes IsNot Nothing) AndAlso (myGlobalResourcesNodes.Any) Then

                If myResource.ProductionPerMinute > 0 Then
                    myResource.ProductionPerMachine = myResource.ProductionPerMinute
                    myResource.ProductionPerMinute = 0
                End If
                myResource.GlobalSources = myGlobalResourcesNodes.Skip(1).Take(myGlobalResourcesNodes.Count - 2).Select(Function(x) ParseGlobalResourceNode(x)).ToList
            End If

            If (Not myReturn.Contains(myResource)) Then myReturn.Add(myResource)
        End If

        If (myReturn.Count = 0) Then
            Dim myResource = myReturn.FirstOrDefault(Function(x) x.Name = myItemType AndAlso x.Type = myItemType)
            If (myResource Is Nothing) Then
                If myItemType.Equals("Power Slug") Then
                    myReturn.Add(New Resource With {.Name = "Green Power Slug", .Type = "Green Power Slug", .Category = GetCategory("Green Power Slug"), .ProductionPerMinute = 1})
                    myReturn.Add(New Resource With {.Name = "Yellow Power Slug", .Type = "Yellow Power Slug", .Category = GetCategory("Yellow Power Slug"), .ProductionPerMinute = 0.5})
                    myReturn.Add(New Resource With {.Name = "Purple Power Slug", .Type = "Purple Power Slug", .Category = GetCategory("Purple Power Slug"), .ProductionPerMinute = 0.25})
                Else
                    myResource = New Resource With {.Name = myItemType, .Type = myItemType, .Category = GetCategory(myItemType), .ProductionPerMinute = 1}
                    myReturn.Add(myResource)
                End If
            End If
        End If

        myMainNode = Nothing

        'GC.Collect()

        Return myReturn
    End Function

    Private Function ParseRecipeNode(thisNodes As List(Of HtmlNode)) As Resource
        Dim myReturn As New Resource

        myReturn.Name = GetNodeValue(thisNodes(0), ".//td[1]")
        myReturn.Category = GetCategory(myReturn.Name)
        myReturn.IsAlternativeRecipe = GetNodeValue(thisNodes(0), ".//td[1]/span/a").Equals("Alternate")

        Dim myInputs = GetInputs(thisNodes)
        If (myInputs IsNot Nothing) AndAlso (myInputs.Any) Then
            myReturn.Resources = myInputs
        End If

        Dim myOutputs = GetOutputs(thisNodes)
        myReturn.Type = myOutputs(0).Item1
        If (Not myReturn.Type.Equals("Water")) Then
            myReturn.ProductionPerMinute = myOutputs(0).Item2
        Else
            myReturn.ProductionPerMachine = myOutputs(0).Item2
        End If
        If (myOutputs.Count > 1) Then
            myReturn.AdditionalProductions = myOutputs.Skip(1).Select(Function(x) New Resource With {
                                                                                 .Type = x.Item1,
                                                                                 .ItemsPerMinute = x.Item2
                                                                                 }).ToList
        End If

        Return myReturn
    End Function
    Private Function ParseGlobalResourceNode(thisNode As HtmlNode) As Resource
        Dim myReturn As New Resource

        myReturn.GlobalSourcesType = GetNodeValue(thisNode, ".//td[1]")
        myReturn.GlobalSourcesCount = GetNodeDoubleValue(thisNode, ".//td[2]")

        Return myReturn
    End Function

    Private Function GetInputs(thisNodes As List(Of HtmlNode)) As List(Of Resource)
        Dim myReturn As New List(Of Resource)

        For Each myNode In thisNodes
            Dim myInputNodes = myNode.SelectNodes(".//td[(@colspan=6 or @colspan=12)]")
            For Each myInputNode In myInputNodes
                Dim myType = GetNodeValue(myInputNode, "./div/div/div")
                If (Not String.IsNullOrEmpty(myType)) Then
                    Dim myCount = GetNodeDoubleValue(myInputNode, "./span")
                    If (myCount = 0) Then myCount = GetNodeDoubleValue(myInputNode, "./div/div")

                    myReturn.Add(New Resource With {
                             .Type = myType,
                             .ItemsPerMinute = myCount
                             })
                End If
            Next
        Next

        Return myReturn
    End Function


    Private Function GetOutputs(thisNodes As List(Of HtmlNode)) As List(Of Tuple(Of String, Double))
        Dim myReturn As New List(Of Tuple(Of String, Double))

        For Each myNode In thisNodes
            Dim myInputNodes = myNode.SelectNodes(".//td[@colspan=1 or @colspan=2]")
            If (myInputNodes IsNot Nothing) Then
                For Each myInputNode In myInputNodes
                    Dim myName = GetNodeValue(myInputNode, "./div/div/div")
                    Dim myCount = GetNodeDoubleValue(myInputNode, "./span")
                    myReturn.Add(Tuple.Create(Of String, Double)(myName, myCount))
                Next
            End If
        Next

        Return myReturn
    End Function
    Private Function GetNodeDoubleValue(myInputNode As HtmlNode, thisXPath As String) As Double
        Dim myReturn As Double

        Dim myValue As String = GetNodeValue(myInputNode, thisXPath)
        If (Not String.IsNullOrEmpty(myValue)) Then
            If myValue.Contains(" / min") Then
                myValue = myValue.Replace(" / min", String.Empty)
            ElseIf myValue.ToLower.Contains(" ×") Then
                myValue = myValue.ToLower.Replace(" ×", String.Empty)
            End If
            Double.TryParse(myValue, Globalization.NumberStyles.Float, Globalization.CultureInfo.GetCultureInfo("en").NumberFormat, myReturn)
        End If

            Return myReturn
    End Function
    Private Function GetNodeValue(thisNode As HtmlNode, thisXPath As String) As String
        Dim myNode = thisNode.SelectSingleNode(thisXPath)
        If (myNode IsNot Nothing) Then
            If (myNode.ChildNodes IsNot Nothing) AndAlso (myNode.ChildNodes.Any) Then
                Return myNode.ChildNodes(0).InnerText.Trim
            Else
                Return myNode.InnerText.Trim
            End If
        End If

        Return String.Empty
    End Function
    Private Function GetCategory(thisName As String) As String
        Select Case thisName
            Case "Limestone", "Coal", "Raw Quartz", "Sulfur", "Bauxite", "Uranium"
                Return "Ore"
            Case Else
                If thisName.EndsWith("Ore", StringComparison.InvariantCultureIgnoreCase) Then Return "Ore"
        End Select

        Select Case thisName
            Case "Water", "Unpackage Water", "Crude Oil", "Unpackage Oil"
                Return "BasicFluid"
            Case "Fuel", "Residual Fuel", "Unpackage Fuel", "Heavy Oil Residue", "Unpackage Heavy Oil Residue", "Turbofuel", "Liquid Biofuel", "Unpackage Liquid Biofuel", "Alumina Solution", "Sulfuric Acid"
                Return "Fluid"
            Case "Flower Petals", "Leaves", "Mycelia", "Wood", "Alien Carapace", "Alien Organs"
                Return "Collectable"
            Case Else
                If thisName.EndsWith("Slug", StringComparison.InvariantCultureIgnoreCase) Then Return "Collectable"
        End Select

        Select Case thisName
            Case "Polymer Resin", "Aluminum Scrap", "Black Powder", "Fine Black Powder"
                Return "BasicMaterial"
            Case Else
                If thisName.EndsWith("Concrete", StringComparison.InvariantCultureIgnoreCase) Then Return "BasicMaterial"
                If thisName.EndsWith("Quartz Crystal", StringComparison.InvariantCultureIgnoreCase) Then Return "BasicMaterial"
                If thisName.EndsWith("Silica", StringComparison.InvariantCultureIgnoreCase) Then Return "BasicMaterial"

                If thisName.EndsWith("Ingot", StringComparison.InvariantCultureIgnoreCase) Then Return "Ingot"
        End Select


        Return "Item"
    End Function

End Class


Public Class GetResources_FromSatisfactoryCalculator_DataFlow

    Public [Input] As BufferBlock(Of Uri)
    Private ParseList As TransformManyBlock(Of Uri, Tuple(Of String, HtmlDocument))
    Private ParseResource As TransformManyBlock(Of Tuple(Of String, HtmlDocument), Resource)
    Public [Output] As BufferBlock(Of Resource)

    Dim myExecuteSingleOptions As New ExecutionDataflowBlockOptions With {.EnsureOrdered = True, .MaxDegreeOfParallelism = 1, .TaskScheduler = TaskScheduler.Default}
    Dim myExecuteMultiOptions As New ExecutionDataflowBlockOptions With {.EnsureOrdered = False, .MaxDegreeOfParallelism = 8, .TaskScheduler = TaskScheduler.Default}

    Dim myLinkOptions As New DataflowLinkOptions With {.PropagateCompletion = True}

    Public Sub New()
        Me.Input = New BufferBlock(Of Uri)(myExecuteSingleOptions)
        Me.ParseList = New TransformManyBlock(Of Uri, Tuple(Of String, HtmlDocument))(AddressOf ParseListFunction, myExecuteSingleOptions)
        Me.ParseResource = New TransformManyBlock(Of Tuple(Of String, HtmlDocument), Resource)(AddressOf ParseResourceFunction, myExecuteMultiOptions)
        Me.Output = New BufferBlock(Of Resource)

        BuildPipeline()
    End Sub
    Private Sub BuildPipeline()
        Me.Input.LinkTo(Me.ParseList, myLinkOptions)
        Me.ParseList.LinkTo(Me.ParseResource, myLinkOptions)
        Me.ParseResource.LinkTo(Me.Output, myLinkOptions)
    End Sub

    Private Function ParseListFunction(arg As Uri) As IEnumerable(Of Tuple(Of String, HtmlDocument))
        Throw New NotImplementedException()
    End Function


    Private Function ParseResourceFunction(arg As Tuple(Of String, HtmlDocument)) As IEnumerable(Of Resource)
        Throw New NotImplementedException()
    End Function

End Class
