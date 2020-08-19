Imports System.Threading.Tasks.Dataflow
Imports System.Xml.Serialization

<XmlRoot(ElementName:="Satisfactory", [Namespace]:="")> Public Class DataStore

    Private _Items As New List(Of Item)
    Private _Productions As New List(Of Production)

    <XmlArray(ElementName:="Items"), XmlArrayItem(Type:=GetType(Recipe), ElementName:="Recipe"), XmlArrayItem(Type:=GetType(WorldItem), ElementName:="WorldItem"), XmlArrayItem(Type:=GetType(CollectableItem), ElementName:="CollectableItem")>
    Public Property Items As List(Of Item)
        Get
            Return _Items
        End Get
        Friend Set
            _Items = Value
        End Set
    End Property

    <XmlArray(ElementName:="Productions"), XmlArrayItem(Type:=GetType(Production), ElementName:="Production")>
    Public Property Productions As List(Of Production)
        Get
            Return _Productions
        End Get
        Friend Set
            _Productions = Value
        End Set
    End Property


    Public Function GetRecipe(thisName As String) As Item
        Return Me.Items.OfType(Of Recipe).FirstOrDefault(Function(x) x.Name.Equals(thisName))
    End Function
    Public Function SetItems(thisList As List(Of Item)) As Boolean
        If (thisList IsNot Nothing) AndAlso (thisList.Any) Then
            _Items = thisList
            _Items.ForEach(Sub(x) x.DataStore = Me)
            CalculateRecipeCosts()
            Return True
        End If

        Return False
    End Function
    Public Sub CalculateRecipeCosts()
        Dim funcFilter = Function(x As Recipe) x.Costs = -1

        For i = 0 To 9
            Dim myRecipes = Me.Items.OfType(Of Recipe).Where(funcFilter).ToList
            If (Not myRecipes.Any) Then Exit For

            For Each myRecipe In myRecipes
                Dim myCosts = GetRecipeCost(myRecipe)
                If myCosts.HasValue Then
                    myRecipe.SetCosts(myCosts.Value)
                    Trace.WriteLine(String.Format("{0};{1};{2};{3};{4}", myRecipe.Name, myRecipe.OutputPerMinute, myRecipe.ItemType, myRecipe.Costs, String.Empty))
                End If
            Next
        Next
    End Sub
    Private Function GetRecipeCost(thisRecipe As Recipe) As Double?
        Dim myReturn As Double = 0
        Dim myTraceLog As New Text.StringBuilder

        For Each myInput In thisRecipe.Inputs
            Dim myItem = Me.FindRecipe(myInput.ItemType)

            If (myItem IsNot Nothing) Then
                Dim myInputCosts = (myInput.ItemsPerMinute * myItem.Costs) / thisRecipe.OutputPerMinute

                If myItem.GetType.Equals(GetType(Recipe)) Then
                    myTraceLog.AppendLine(String.Format("{0};{1};{2};{3};{4}", String.Empty, myInput.ItemsPerMinute, DirectCast(myItem, Recipe).Name, myItem.Costs, myInputCosts))
                Else
                    myTraceLog.AppendLine(String.Format("{0};{1};{2};{3};{4}", String.Empty, myInput.ItemsPerMinute, myItem.ItemType, myItem.Costs, myInputCosts))
                End If
                myReturn += myInputCosts
            Else
                Return Nothing
            End If
        Next

        Trace.Write(myTraceLog.ToString)
        Return myReturn
    End Function
    Private Function FindRecipe(itemType As String) As Item
        Dim myReturn As Item = Nothing

        myReturn = Me.Items.OfType(Of WorldItem).Where(Function(x) x.ItemType = itemType).OrderBy(Function(x) x.Costs).FirstOrDefault
        If (myReturn IsNot Nothing) Then Return myReturn

        myReturn = Me.Items.OfType(Of CollectableItem).Where(Function(x) x.ItemType = itemType).OrderBy(Function(x) x.Costs).FirstOrDefault
        If (myReturn IsNot Nothing) Then Return myReturn

        myReturn = Me.Items.OfType(Of Recipe).Where(Function(x) x.Costs > 0 AndAlso x.ItemType = itemType).OrderBy(Function(x) x.Costs).FirstOrDefault
        If (myReturn IsNot Nothing) Then Return myReturn

        Return myReturn
    End Function

    Public Function AddProduction(thisRecipe As Recipe, thisItemsPerMinute As Double) As Production
        Dim myReturn As New Production With {.Recipe = thisRecipe, .ItemsPerMinute = thisItemsPerMinute, .DataStore = Me}
        Me.Productions.Add(myReturn)

        Return myReturn
    End Function

    Public Sub LoadRecipesFromGamepedia()
        Dim myDataFlow As New GetResources_FromGamepedia_DataFlow
        Dim myResult As New BufferBlock(Of Item)

        myDataFlow.Output.LinkTo(myResult, New DataflowLinkOptions With {.PropagateCompletion = True})

        myDataFlow.Input.Post(New Uri("https://satisfactory.gamepedia.com/Satisfactory_Wiki"))
        myDataFlow.Input.Complete()

        myDataFlow.Output.Completion.Wait()

        Dim myRecipes As New List(Of Item)
        myResult.TryReceiveAll(myRecipes)
        myResult.Complete()

        Me.SetItems(myRecipes.Distinct.ToList)

        GC.Collect()
        GC.WaitForFullGCComplete()
    End Sub
End Class
