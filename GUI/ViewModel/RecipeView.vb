Imports System.Collections.ObjectModel
Imports Satisfactory.ObjectModel

Public Class RecipeView
    Inherits ItemView

End Class
Partial Class RecipeView
#Region "Base Properties"
    Public ReadOnly Property Name As String
        Get
            Return DirectCast(baseObj, Recipe).Name
        End Get
    End Property
    Public ReadOnly Property IsAlternativeRecipe As Boolean
        Get
            Return DirectCast(baseObj, Recipe).IsAlternativeRecipe
        End Get
    End Property
    Public ReadOnly Property OutputPerMinute As Double
        Get
            Return DirectCast(baseObj, Recipe).OutputPerMinute
        End Get
    End Property
    Public ReadOnly Property Costs As Double
        Get
            Return DirectCast(baseObj, Recipe).Costs
        End Get
    End Property
    Public ReadOnly Property ItemCategory As CategoryEnum
        Get
            Return baseObj.ItemCategory
        End Get
    End Property


    Private _Inputs As ObservableCollection(Of SourceItemView)
    Private _AdditionalOutputs As ObservableCollection(Of SourceItemView)

    Public ReadOnly Property Inputs As ObservableCollection(Of SourceItemView)
        Get
            If (_Inputs Is Nothing) Then
                Dim funcSelect = Function(x As SourceItem) New SourceItemView With {.baseObj = x}

                _Inputs = New ObservableCollection(Of SourceItemView)(DirectCast(baseObj, Recipe).Inputs.Select(funcSelect).ToList)
            End If

            Return _Inputs
        End Get
    End Property
    Public ReadOnly Property AdditionalOutputs As ObservableCollection(Of SourceItemView)
        Get
            If (_AdditionalOutputs Is Nothing) Then
                Dim funcSelect = Function(x As SourceItem) New SourceItemView With {.baseObj = x}
                _AdditionalOutputs = New ObservableCollection(Of SourceItemView)(DirectCast(baseObj, Recipe).AdditionalOutputs.Select(funcSelect).ToList)
            End If

            Return _AdditionalOutputs
        End Get
    End Property
#End Region
End Class

Partial Class RecipeView
#Region "Additional Properties"
    Private _IsActiveAlternativeRecipe As Boolean
    Public Property IsActiveAlternativeRecipe As Boolean
        Get
            If Me.IsAlternativeRecipe Then
                Return _IsActiveAlternativeRecipe
            Else
                Return False
            End If
        End Get
        Set
            If Me.IsAlternativeRecipe Then
                _IsActiveAlternativeRecipe = Value
            End If
            NotifyPropertyChanged()
        End Set
    End Property
#End Region
End Class

Partial Class RecipeView
#Region ""

#End Region
End Class
