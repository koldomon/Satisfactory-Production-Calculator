Imports System.Xml.Serialization
<DebuggerDisplay("Prd: {Recipe.Name}")> Public Class Production
    Implements IEquatable(Of Production)

    Public Property Recipe As Resource
    Public Property ItemsPerMinute As Double
    Public Property Resources As New List(Of Resource)
    Public Property Productions As New List(Of Production)
    Public Property AdditionalItems As New List(Of Production)


    Friend Sub Expand(thisRecipes As List(Of Resource),
                      thisAltRecipes As List(Of Resource),
                      thisMinerType As MinerTypeEnum,
                      thisMinerSpeed As MinerSpeedEnum,
                      thisResourceRate As ResourceRateEnum,
                      thisGlobalResources As List(Of Tuple(Of String, Double)))
        If (Me.Recipe.Resources Is Nothing) Then Exit Sub

        For Each myResource In Me.Recipe.Resources
            Dim mySelectedResource As Resource = GetRecipe(myResource, thisRecipes, thisAltRecipes, thisMinerType, thisMinerSpeed, thisResourceRate)
            Dim myProduction = New Production With {.Recipe = mySelectedResource, .ItemsPerMinute = (Me.ItemsPerMinute / Me.Recipe.GetProductionRate(thisMinerType, thisMinerSpeed, thisResourceRate)) * myResource.ItemsPerMinute}
            myProduction.Expand(thisRecipes, thisAltRecipes, thisMinerType, thisMinerSpeed, thisResourceRate, thisGlobalResources)
            Me.Productions.Add(myProduction)

            If myProduction.AdditionalItems IsNot Nothing AndAlso myProduction.AdditionalItems.Any Then
                Me.AdditionalItems.AddRange(myProduction.AdditionalItems)
            End If
        Next

        If (Me.Recipe.AdditionalResources IsNot Nothing) AndAlso (Me.Recipe.AdditionalResources.Any) Then
            For Each myAdditionalResource In Me.Recipe.AdditionalResources
                Dim myAdditionalProduction = New Production With {.Recipe = myAdditionalResource, .ItemsPerMinute = Me.ItemsPerMinute * (myAdditionalResource.ItemsPerMinute / Me.Recipe.GetProductionRate(thisMinerType, thisMinerSpeed, thisResourceRate))}
                Me.AdditionalItems.Add(myAdditionalProduction)
            Next
        End If

    End Sub

    Private Function GetRecipe(thisResource As Resource,
                               thisRecipes As List(Of Resource),
                               thisAlternateRecipes As List(Of Resource),
                               thisMinerType As MinerTypeEnum,
                               thisMinerSpeed As MinerSpeedEnum,
                               thisResourceRate As ResourceRateEnum) As Resource

        Dim myReturn As Resource = Nothing
        Dim myList As List(Of Resource)

        Dim myOrgResource = Resource.AllResources.Where(Function(x) x.Name.StartsWith(thisResource.Type) AndAlso x.Type = thisResource.Type).OrderBy(Function(x) x.ProductionCost).FirstOrDefault

        If (myOrgResource IsNot Nothing) Then
            'Resource is an alternate recipe, so look in the whole list
            If (myOrgResource.IsAlternativeRecipe) Then
                myList = Resource.AllResources.Where(Function(x) x.Type = thisResource.Type).OrderBy(Function(x) x.ProductionCost).ToList

                myReturn = myList.FirstOrDefault
                If (myReturn IsNot Nothing) Then Return myReturn
            End If

            'Look in the alternate recipes first
            If (thisAlternateRecipes IsNot Nothing) AndAlso (thisAlternateRecipes.Any) Then
                myList = thisAlternateRecipes.Where(Function(x) x.Type = thisResource.Type).OrderBy(Function(x) x.ProductionCost).ToList
                myReturn = myList.FirstOrDefault
                If (myReturn IsNot Nothing) Then Return myReturn
            End If

            'Then look in the normal recipes
            Dim myTmpList = thisRecipes.Where(Function(x) (x.Type = thisResource.Type) AndAlso (x.Category = myOrgResource.Category)).OrderBy(Function(x) x.ProductionCost)
            If (myTmpList.Any) Then myReturn = myTmpList.FirstOrDefault
            If (myReturn IsNot Nothing) Then Return myReturn

            'Then look in the additional productions
            myList = thisRecipes.Where(Function(x) (x.AdditionalResources IsNot Nothing) AndAlso (x.AdditionalResources.Where(Function(y) y.Type = thisResource.Type).Any)).OrderBy(Function(x) x.ProductionCost).ToList
            myReturn = myList.FirstOrDefault
            If (myReturn IsNot Nothing) Then Return myReturn

            myReturn = New Resource With {.Name = String.Format("{0} [Missing]", thisResource.Type), .Type = thisResource.Type}
            Return myReturn
        End If

        Return Nothing
    End Function

    Friend Sub ResetProductions()
        Me.Productions.ForEach(Sub(x) x.ResetProductions())
        Me.Productions.Clear()
    End Sub

    Public Overrides Function Equals(other As Object) As Boolean
        If (other Is Nothing) Then Return False
        If (Not other.GetType.Equals(Me.GetType)) Then Return False

        Return DirectCast(Me, IEquatable(Of Production)).Equals(TryCast(other, Production))
    End Function
    Public Overloads Function Equals(other As Production) As Boolean Implements IEquatable(Of Production).Equals
        If (other Is Nothing) Then Return False

        Return Me.Recipe.Equals(other.Recipe)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return String.Format("{0}{1}{2}", Me.Recipe.Name, Me.Recipe.Type, Me.ItemsPerMinute).GetHashCode
    End Function

End Class
