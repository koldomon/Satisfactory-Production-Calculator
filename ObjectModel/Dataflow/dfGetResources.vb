Imports System.Threading.Tasks.Dataflow
Imports HtmlAgilityPack

Public Class GetResources_FromGamepedia_DataFlow

    Public [Input] As BufferBlock(Of Uri)
    Private ParseList As TransformManyBlock(Of Uri, Tuple(Of String, HtmlDocument))
    Private ParseResource As TransformManyBlock(Of Tuple(Of String, HtmlDocument), Item)
    Public [Output] As BufferBlock(Of Item)

    Dim myExecuteSingleOptions As New ExecutionDataflowBlockOptions With {.EnsureOrdered = True, .MaxDegreeOfParallelism = 1, .TaskScheduler = TaskScheduler.Default}
    Dim myExecuteMultiOptions As New ExecutionDataflowBlockOptions With {.EnsureOrdered = False, .MaxDegreeOfParallelism = 1, .TaskScheduler = TaskScheduler.Default}

    Public Sub New()
        Me.Input = New BufferBlock(Of Uri)(myExecuteSingleOptions)
        Me.ParseList = New TransformManyBlock(Of Uri, Tuple(Of String, HtmlDocument))(AddressOf ParseListFunction, myExecuteSingleOptions)
        Me.ParseResource = New TransformManyBlock(Of Tuple(Of String, HtmlDocument), Item)(AddressOf ParsePageFunction, myExecuteMultiOptions)
        Me.Output = New BufferBlock(Of Item)

        BuildPipeline()
    End Sub

    Dim myLinkOptions As New DataflowLinkOptions With {.PropagateCompletion = True}
    Private Sub BuildPipeline()
        Me.Input.LinkTo(Me.ParseList, myLinkOptions)
        Me.ParseList.LinkTo(Me.ParseResource, myLinkOptions)
        Me.ParseResource.LinkTo(Me.Output, myLinkOptions)
    End Sub

    Private Function ParseListFunction(thisUri As Uri) As IEnumerable(Of Tuple(Of String, HtmlDocument))
        Dim myReturn As New List(Of Tuple(Of String, HtmlDocument))

        Dim myURIBuilder As New UriBuilder(thisUri)
        Dim myHtmlWeb As New HtmlWeb()
        Dim myDoc As HtmlDocument = myHtmlWeb.LoadFromWebAsync(myURIBuilder.Uri.AbsoluteUri).Result

        Dim myMainNode As HtmlNode = myDoc.DocumentNode.SelectSingleNode(".//tbody[tr/th[@class='navbox-title' and span='Items']]")
        Dim myItemNodes = myMainNode.SelectNodes(".//td[@class='navbox-list navbox-odd' or @class='navbox-list navbox-even']/div/span/a[2]")

        Dim FuncSelectReturn = Function(x As HtmlNode) As Tuple(Of String, HtmlDocument)
                                   Dim mySubDoc = myHtmlWeb.LoadFromWebAsync(New UriBuilder(myURIBuilder.Uri) With {.Path = x.GetAttributeValue("href", "")}.Uri.AbsoluteUri).Result
                                   Return New Tuple(Of String, HtmlDocument)(x.InnerText.Trim, mySubDoc)
                               End Function

        myReturn.AddRange(myItemNodes.Select(FuncSelectReturn))

        myURIBuilder = Nothing
        myHtmlWeb = Nothing
        myDoc = Nothing

        GC.Collect()

        Return myReturn
    End Function
    Private Function ParsePageFunction(thisItem As Tuple(Of String, HtmlDocument)) As List(Of Item)
        Dim myReturn As New List(Of Item)
        Dim myMainNode As HtmlNode

        Dim myItemType = GetNodeValue(thisItem.Item2.DocumentNode, ".//h1[@id='firstHeading']")
        Dim myItemCategory = GetItemCategory(myItemType)

        'WorldItems
        myMainNode = thisItem.Item2.DocumentNode.SelectSingleNode(".//table[@class='wikitable' and tbody/tr/th='Type']")
        If (myMainNode IsNot Nothing) Then
            Dim myWorldItem = myReturn.OfType(Of WorldItem).FirstOrDefault(Function(x) x.ItemType = myItemType)
            If (myWorldItem Is Nothing) Then myWorldItem = New WorldItem With {.ItemType = myItemType, .ItemCategory = myItemCategory}

            If (myItemCategory = CategoryEnum.BasicFluid) Then
                Select Case myItemType
                    Case "Water"
                        myWorldItem.PureCount = 100
                        myWorldItem.BaseValue = BaseProductionEnum.Water
                    Case "Crude Oil"
                        myWorldItem.BaseValue = BaseProductionEnum.CrudeOil
                End Select
            Else
                myWorldItem.BaseValue = BaseProductionEnum.Default
            End If

            Dim myGlobalResourcesNodes = myMainNode.SelectNodes(".//tr")
            If (myGlobalResourcesNodes IsNot Nothing) AndAlso (myGlobalResourcesNodes.Any) Then
                For Each myGlobalResourceNode In myGlobalResourcesNodes.Skip(1).Take(myGlobalResourcesNodes.Count - 2)
                    Dim myGlobalResource = ParseGlobalResourceNode(myGlobalResourceNode)
                    Select Case myGlobalResource.Item1
                        Case "Impure"
                            myWorldItem.ImpureCount = myGlobalResource.Item2
                        Case "Normal"
                            myWorldItem.NormalCount = myGlobalResource.Item2
                        Case "Pure"
                            myWorldItem.PureCount = myGlobalResource.Item2
                    End Select
                Next
            End If

            If (Not myReturn.Contains(myWorldItem)) Then myReturn.Add(myWorldItem)
        End If

        'normal Recipe page
        myMainNode = thisItem.Item2.DocumentNode.SelectSingleNode(".//table[@class='wikitable' and preceding-sibling::h3/span[@id='Crafting']]")
        If (myMainNode IsNot Nothing) Then
            Dim myRecipeNodes = myMainNode.SelectNodes(".//tr[@class='firstRow']")
            For Each myRecipeNode In myRecipeNodes
                Dim myNodes As New List(Of HtmlNode)
                myNodes.Add(myRecipeNode)

                Dim myAdditionalProductionNode = myRecipeNode.NextSibling
                If (myAdditionalProductionNode IsNot Nothing) AndAlso (myAdditionalProductionNode.GetAttributeValue("class", String.Empty) = String.Empty) Then myNodes.Add(myAdditionalProductionNode)

                Dim myRecipe = ParseRecipeNode(myNodes)
                If (Not (myRecipe.ItemCategory = CategoryEnum.BasicOre OrElse myRecipe.ItemCategory = CategoryEnum.BasicFluid)) Then
                    If (Not myReturn.Contains(myRecipe)) Then myReturn.Add(myRecipe)
                Else
                    Dim myWorldItem As New WorldItem With {.ItemType = myItemType, .ItemCategory = myItemCategory}

                    Select Case myRecipe.Name
                        Case "Water"
                            myWorldItem.PureCount = 100
                            myWorldItem.BaseValue = BaseProductionEnum.Water
                        Case "Crude Oil"
                            myWorldItem.BaseValue = BaseProductionEnum.CrudeOil
                        Case Else
                            myWorldItem.BaseValue = BaseProductionEnum.Default
                    End Select

                    If (Not myReturn.Contains(myWorldItem)) Then myReturn.Add(myWorldItem)
                End If
            Next
        End If

        'Collectables
        If (myReturn.Count = 0) Then
            Dim myResource = myReturn.FirstOrDefault(Function(x) x.ItemType = myItemType)
            If (myResource Is Nothing) Then
                If (myItemType.Equals("Power Slug")) Then
                    Dim myCollectableItem As CollectableItem = New CollectableItem With {.Name = String.Format("{0} Power Slug", thisItem.Item1), .ItemType = String.Format("{0} Power Slug", thisItem.Item1), .ItemCategory = CategoryEnum.Collectable}

                    Select Case thisItem.Item1
                        Case "Green"
                            myCollectableItem.Count = 409
                        Case "Yellow"
                            myCollectableItem.Count = 230
                        Case "Purple"
                            myCollectableItem.Count = 124
                    End Select

                    If (Not myReturn.Contains(myCollectableItem)) Then myReturn.Add(myCollectableItem)
                ElseIf (myItemType.Equals("Statue")) Then
                    Dim myCollectableItem As CollectableItem = New CollectableItem With {.Name = String.Format("{0}", thisItem.Item1), .ItemType = myItemType, .ItemCategory = CategoryEnum.Collectable}

                    If (Not myReturn.Contains(myCollectableItem)) Then myReturn.Add(myCollectableItem)
                ElseIf (myItemType.Equals("Water")) Then
                    Dim myWorldItem As New WorldItem With {.ItemType = myItemType, .ItemCategory = myItemCategory}
                    myWorldItem.PureCount = 100
                    myWorldItem.BaseValue = BaseProductionEnum.Water

                    If (Not myReturn.Contains(myWorldItem)) Then myReturn.Add(myWorldItem)
                Else

                    myResource = New CollectableItem With {.Name = myItemType, .ItemType = myItemType, .ItemCategory = myItemCategory, .Count = 1}
                    If (Not myReturn.Contains(myResource)) Then myReturn.Add(myResource)
                End If
            End If
        End If

        myMainNode = Nothing

        Return myReturn
    End Function

    Private Function ParseRecipeNode(thisNodes As List(Of HtmlNode)) As Recipe
        Dim myReturn As New Recipe

        myReturn.Name = GetNodeValue(thisNodes(0), ".//td[1]")
        myReturn.ItemCategory = GetItemCategory(myReturn.Name)
        myReturn.IsAlternativeRecipe = GetNodeValue(thisNodes(0), ".//td[1]/span/a").Equals("Alternate")

        Dim myInputs = GetInputs(thisNodes)
        If (myInputs IsNot Nothing) AndAlso (myInputs.Any) Then myReturn.Inputs.AddRange(myInputs)

        Dim myOutputs = GetOutputs(thisNodes)
        myReturn.ItemType = myOutputs(0).ItemType
        myReturn.OutputPerMinute = myOutputs(0).ItemsPerMinute

        If (myOutputs.Count > 1) Then
            myReturn.AdditionalOutputs.AddRange(myOutputs.Skip(1))
        End If

        Return myReturn
    End Function
    Private Function ParseGlobalResourceNode(thisNode As HtmlNode) As Tuple(Of String, Double)
        Dim myType As String = GetNodeValue(thisNode, ".//td[1]")
        Dim myCount As Double = GetNodeDoubleValue(thisNode, ".//td[2]")

        Return Tuple.Create(Of String, Double)(myType, myCount)
    End Function

    Private Function GetInputs(thisNodes As List(Of HtmlNode)) As List(Of SourceItem)
        Dim myReturn As New List(Of SourceItem)

        For Each myNode In thisNodes
            Dim myInputNodes = myNode.SelectNodes(".//td[(@colspan=6 or @colspan=12)]")
            For Each myInputNode In myInputNodes
                Dim myType = GetNodeValue(myInputNode, "./div/div/div")
                If (Not String.IsNullOrEmpty(myType)) Then
                    Dim myCount = GetNodeDoubleValue(myInputNode, "./div[2]")
                    If (myCount = 0) Then myCount = GetNodeDoubleValue(myInputNode, "./div/div")

                    myReturn.Add(New SourceItem With {
                             .ItemType = myType,
                             .ItemsPerMinute = myCount
                             })
                End If
            Next
        Next

        Return myReturn
    End Function
    Private Function GetOutputs(thisNodes As List(Of HtmlNode)) As List(Of SourceItem)
        Dim myReturn As New List(Of SourceItem)

        For Each myNode In thisNodes
            Dim myInputNodes = myNode.SelectNodes(".//td[@colspan=1 or @colspan=2]")
            If (myInputNodes IsNot Nothing) Then
                For Each myInputNode In myInputNodes
                    Dim myType = GetNodeValue(myInputNode, "./div/div/div")
                    Dim myCount = GetNodeDoubleValue(myInputNode, "./div[2]")
                    If myCount = 0 Then myCount = GetNodeDoubleValue(myInputNode, "./div/div")

                    myReturn.Add(New SourceItem With {
                             .ItemType = myType,
                             .ItemsPerMinute = myCount
                             })
                Next
            End If
        Next

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

    Private Function GetItemCategory(thisName As String) As CategoryEnum
        Select Case thisName
            Case "Limestone", "Coal", "Raw Quartz", "Sulfur", "Bauxite", "Uranium"
                Return CategoryEnum.BasicOre
            Case Else
                If thisName.EndsWith("Ore", StringComparison.InvariantCultureIgnoreCase) Then Return CategoryEnum.BasicOre
        End Select

        Select Case thisName
            Case "Water", "Crude Oil"
                Return CategoryEnum.BasicFluid
            Case "Unpackage Water", "Unpackage Oil"
                Return CategoryEnum.Item
            Case "Fuel", "Residual Fuel", "Unpackage Fuel", "Heavy Oil Residue", "Unpackage Heavy Oil Residue", "Turbofuel", "Liquid Biofuel", "Unpackage Liquid Biofuel", "Alumina Solution", "Sulfuric Acid"
                Return CategoryEnum.Fluid
            Case "Flower Petals", "Leaves", "Mycelia", "Wood", "Alien Carapace", "Alien Organs"
                Return CategoryEnum.Collectable
            Case Else
                If thisName.EndsWith("Slug", StringComparison.InvariantCultureIgnoreCase) Then Return CategoryEnum.Collectable
        End Select

        Select Case thisName
            Case "Polymer Resin", "Aluminum Scrap", "Black Powder", "Fine Black Powder"
                Return CategoryEnum.BasicItem
            Case Else
                If thisName.EndsWith("Concrete", StringComparison.InvariantCultureIgnoreCase) Then Return CategoryEnum.BasicItem
                If thisName.EndsWith("Quartz Crystal", StringComparison.InvariantCultureIgnoreCase) Then Return CategoryEnum.BasicItem
                If thisName.EndsWith("Silica", StringComparison.InvariantCultureIgnoreCase) Then Return CategoryEnum.BasicItem

                If thisName.EndsWith("Ingot", StringComparison.InvariantCultureIgnoreCase) Then Return CategoryEnum.Ingot
        End Select


        Return CategoryEnum.Item
    End Function
End Class
