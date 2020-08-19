Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports Satisfactory.ObjectModel

Public Class DataStoreView
    Inherits ViewModelBase

#Region "Base Object"

    Private _baseObj As New DataStore
    Public Property baseObj As DataStore
        Get
            Return _baseObj
        End Get
        Set
            _baseObj = Value
        End Set
    End Property

    Public Property SelectedProduction As ProductionView
    Public Property HasChanged As Boolean
    Public Property CurrentFile As String

    Public Sub SetItems(thisList As List(Of Item), thisActiveAlternativeRecipes As List(Of String))
        If (thisList Is Nothing) Then Throw New ArgumentNullException(NameOf(thisList))

        Me.baseObj.SetItems(thisList)
        For Each myItem In Me.Items
            myItem.baseStoreView = Me
        Next

        For Each myRecipe In Me.AlternativeRecipes
            AddHandler myRecipe.PropertyChanged, AddressOf AlternativeRecipeChanged
            If thisActiveAlternativeRecipes.Contains(myRecipe.Name) Then
                myRecipe.IsActiveAlternativeRecipe = True
            End If
        Next
    End Sub

    Private Sub AlternativeRecipeChanged(sender As Object, e As PropertyChangedEventArgs)
        If (e.PropertyName.Equals(NameOf(RecipeView.IsActiveAlternativeRecipe))) Then
            ResetActiveAlternateiveRecipes()
            SyncUserAlternateRecipes(sender)
        End If
    End Sub

    Friend Sub GetNewFile()
        Throw New NotImplementedException()
    End Sub


#End Region
End Class

Partial Class DataStoreView
#Region "Global Settings"
    Private _MinerType As MinerTypeEnum = MinerTypeEnum.MK1
    Private _MinerSpeed As MinerSpeedEnum = MinerSpeedEnum.Speed100
    Private _ResourceNodeType As ResourceNodeTypeEnum = ResourceNodeTypeEnum.Normal
    Private _BeltSpeed As BeltSpeedEnum = BeltSpeedEnum.MK5

    Public Property MinerType As MinerTypeEnum
        Get
            Return _MinerType
        End Get
        Set
            _MinerType = Value
            NotifyPropertyChanged()
        End Set
    End Property
    Public Property MinerSpeed As MinerSpeedEnum
        Get
            Return _MinerSpeed
        End Get
        Set
            _MinerSpeed = Value
            NotifyPropertyChanged()
        End Set
    End Property
    Public Property BeltSpeed As BeltSpeedEnum
        Get
            Return _BeltSpeed
        End Get
        Set
            _BeltSpeed = Value
            NotifyPropertyChanged()
        End Set
    End Property
    Public Property ResourceNodeType As ResourceNodeTypeEnum
        Get
            Return _ResourceNodeType
        End Get
        Set
            _ResourceNodeType = Value
            NotifyPropertyChanged()
        End Set
    End Property
#End Region
End Class

Partial Class DataStoreView
#Region "Base Collections"
    Private _Items As ObservableCollection(Of ItemView)
    Private _Productions As ObservableCollection(Of ProductionView)

    Public ReadOnly Property Items As ObservableCollection(Of ItemView)
        Get
            If (_Items Is Nothing) Then
                Dim funcSelect = Function(x As Item)
                                     Select Case x.GetType
                                         Case GetType(WorldItem)
                                             Return New WorldItemView With {.baseObj = x, .baseStoreView = Me}
                                         Case GetType(CollectableItem)
                                             Return New CollectableItemView With {.baseObj = x, .baseStoreView = Me}
                                         Case GetType(Recipe)
                                             Return New RecipeView With {.baseObj = x, .baseStoreView = Me}
                                         Case Else
                                             Return New ItemView With {.baseObj = x, .baseStoreView = Me}
                                     End Select
                                 End Function
                _Items = New ObservableCollection(Of ItemView)(baseObj.Items.Select(funcSelect).ToList)
            End If

            Return _Items
        End Get
    End Property
    Public ReadOnly Property Productions As ObservableCollection(Of ProductionView)
        Get
            If (_Productions Is Nothing) Then
                Dim funcSelect = Function(x As Production)
                                     Select Case x.GetType
                                         Case Else
                                             Return New ProductionView With {.baseObj = x, .baseStoreView = Me}
                                     End Select
                                 End Function
                _Productions = New ObservableCollection(Of ProductionView)(baseObj.Productions.Select(funcSelect).ToList)
            End If
            Return _Productions
        End Get
    End Property
    Private Sub ResetProductions()
        _Productions = Nothing
        NotifyPropertyChanged(NameOf(Productions))
    End Sub
#End Region
End Class
Partial Class DataStoreView
#Region "Additional Collections"
    Private _Recipes As ObservableCollection(Of RecipeView)
    Private _WorldItems As ObservableCollection(Of WorldItemView)
    Private _CollectableItems As ObservableCollection(Of CollectableItemView)
    Private _DefaultRecipes As ObservableCollection(Of RecipeView)
    Private _AlternativeRecipes As ObservableCollection(Of RecipeView)
    Private _ActiveAlternativeRecipes As ObservableCollection(Of RecipeView)

    Public ReadOnly Property Recipes As ObservableCollection(Of RecipeView)
        Get
            If (_Recipes Is Nothing) Then _Recipes = New ObservableCollection(Of RecipeView)(Me.Items.OfType(Of RecipeView))

            Return _Recipes
        End Get
    End Property
    Public ReadOnly Property WorldItems As ObservableCollection(Of WorldItemView)
        Get
            If (_WorldItems Is Nothing) Then _WorldItems = New ObservableCollection(Of WorldItemView)(Me.Items.OfType(Of WorldItemView))

            Return _WorldItems
        End Get
    End Property
    Public ReadOnly Property CollectableItems As ObservableCollection(Of CollectableItemView)
        Get
            If (_CollectableItems Is Nothing) Then _CollectableItems = New ObservableCollection(Of CollectableItemView)(Me.Items.OfType(Of CollectableItemView))

            Return _CollectableItems
        End Get
    End Property
    Public ReadOnly Property DefaultRecipes As ObservableCollection(Of RecipeView)
        Get
            If (_DefaultRecipes Is Nothing) Then _DefaultRecipes = New ObservableCollection(Of RecipeView)(Me.Recipes.Where(Function(x) Not x.IsAlternativeRecipe))

            Return _DefaultRecipes
        End Get
    End Property
    Public ReadOnly Property AlternativeRecipes As ObservableCollection(Of RecipeView)
        Get
            If (_AlternativeRecipes Is Nothing) Then _AlternativeRecipes = New ObservableCollection(Of RecipeView)(Me.Recipes.Where(Function(x) x.IsAlternativeRecipe))

            Return _AlternativeRecipes
        End Get
    End Property
    Public ReadOnly Property ActiveAlternativeRecipes As ObservableCollection(Of RecipeView)
        Get
            If (_ActiveAlternativeRecipes Is Nothing) Then _ActiveAlternativeRecipes = New ObservableCollection(Of RecipeView)(Me.AlternativeRecipes.Where(Function(x) x.IsActiveAlternativeRecipe))

            Return _ActiveAlternativeRecipes
        End Get
    End Property
    Private Sub ResetActiveAlternateiveRecipes()
        _ActiveAlternativeRecipes = Nothing
        NotifyPropertyChanged(NameOf(ActiveAlternativeRecipes))
    End Sub
#End Region
End Class
Partial Class DataStoreView
#Region "User Settings"
    Private _SaveSettingsCommand As ICommand

    Private Sub SyncUserAlternateRecipes(thisRecipe As RecipeView)
        If (thisRecipe.IsActiveAlternativeRecipe) AndAlso (Not My.Settings.ActiveAlternateRecipes.Contains(thisRecipe.Name)) Then
            My.Settings.ActiveAlternateRecipes.Add(thisRecipe.Name)
        ElseIf (Not thisRecipe.IsActiveAlternativeRecipe) AndAlso (My.Settings.ActiveAlternateRecipes.Contains(thisRecipe.Name)) Then
            My.Settings.ActiveAlternateRecipes.Remove(thisRecipe.Name)
        End If
        SaveSettingsCommand.Execute(Nothing)
    End Sub

    Public Property SaveSettingsCommand As ICommand
        Get
            If _SaveSettingsCommand Is Nothing Then
                _SaveSettingsCommand = New Command(AddressOf SaveSettingExecute, AddressOf SaveSettingsCanExecute)
            End If
            Return _SaveSettingsCommand
        End Get
        Set
            _SaveSettingsCommand = Value
        End Set
    End Property
    Private Function SaveSettingsCanExecute(parameter As Object) As Boolean
        Return True
    End Function
    Private Sub SaveSettingExecute(parameter As Object)
        My.Settings.Save()
    End Sub
#End Region
End Class
Partial Class DataStoreView
#Region "Production"
    Friend Function AddProduction(thisRecipeView As RecipeView, thisItemsPerMinute As Double) As ProductionView
        Dim myReturn As ProductionView = Nothing

        Dim myProduction = baseObj.AddProduction(thisRecipeView.baseObj, thisItemsPerMinute)
        myReturn = New ProductionView() With {.baseObj = myProduction, .Recipe = thisRecipeView, .baseStoreView = Me}
        Me.Productions.Add(myReturn)
        NotifyPropertyChanged(NameOf(Productions))

        Return myReturn
    End Function

    'Friend Function AddProduction(thisRecipe As Recipe, thisItemsPerMinute As Double) As ProductionView
    '    Dim myRecipe = Me.Recipes.FirstOrDefault(Function(x) x.Name = thisRecipe.Name)
    '    Return Me.AddProduction(myRecipe, thisItemsPerMinute)
    'End Function

#End Region
End Class
