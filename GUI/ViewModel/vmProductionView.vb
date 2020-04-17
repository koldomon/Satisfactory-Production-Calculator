Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Runtime.CompilerServices
<DebuggerDisplay("Res: {Recipe.Name}")> Public Class ProductionView
    Inherits ViewModelBase
    Implements INotifyPropertyChanged

    Private _Recipe As ResourceView
    Private _ItemsPerMinute As Double
    Private _Resources As New List(Of ResourceView)
    Private _Productions As New List(Of ProductionView)
    Private _AdditionalItems As New List(Of ProductionView)
    Private _AllProductions As New List(Of ProductionView)
    Private _AllAdditionalItems As New List(Of ProductionView)
    Private _IsSelected As Boolean

    Protected Friend MainView As MainViewModel
    Protected Friend BaseObj As Production

    Public Sub New()
    End Sub

    Public Sub New(v As MainViewModel, thisRecipe As ResourceView, thisAmmount As Double)
        MainView = v
        BaseObj = New Production() With {.Recipe = thisRecipe.BaseObject, .ItemsPerMinute = thisAmmount}
    End Sub
    Public Sub New(v As MainViewModel, p As Production)
        MainView = v
        BaseObj = p
    End Sub

    'Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    'Friend Sub NotifyPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
    '    RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    'End Sub


    Public ReadOnly Property Recipe As ResourceView
        Get
            If _Recipe Is Nothing Then _Recipe = New ResourceView(BaseObj.Recipe)

            Return _Recipe
        End Get
    End Property
    Public Property ItemsPerMinute As Double
        Get
            If (BaseObj IsNot Nothing) Then
                Return BaseObj.ItemsPerMinute
            Else
                Return _ItemsPerMinute
            End If
        End Get
        Set
            If (BaseObj IsNot Nothing) Then
                BaseObj.ItemsPerMinute = Value
                MainView.HasChanged = True
                MainView.ResetProductions()
            Else
                _ItemsPerMinute = Value
            End If
            NotifyPropertyChanged()
            NotifyPropertyChanged(NameOf(RequiredMachines))
            NotifyPropertyChanged(NameOf(ProductionRate))
        End Set
    End Property
    Public ReadOnly Property Productions As List(Of ProductionView)
        Get
            If (_Productions Is Nothing) OrElse (Not _Productions.Any) Then
                If (BaseObj IsNot Nothing) AndAlso (BaseObj.Productions IsNot Nothing) Then
                    _Productions = BaseObj.Productions.Select(Function(x) New ProductionView(MainView, x)).ToList
                Else
                    Return Nothing
                End If
            End If

            Return _Productions
        End Get
    End Property
    Public ReadOnly Property AdditionalItems As List(Of ProductionView)
        Get
            If (_AdditionalItems Is Nothing) OrElse (Not _AdditionalItems.Any) Then
                _AdditionalItems = BaseObj.AdditionalItems.Select(Function(x) New ProductionView(MainView, x)).ToList
            End If

            Return _AdditionalItems
        End Get
    End Property
    Public ReadOnly Property Resources As List(Of ResourceView)
        Get
            If (_Resources Is Nothing) OrElse (Not _Resources.Any) Then
                _Resources = BaseObj.Resources.Select(Function(x) New ResourceView(x)).ToList
            End If
            Return _Resources
        End Get
    End Property
    Public ReadOnly Property AllProductions As List(Of ProductionView)
        Get
            If (_AllProductions Is Nothing) OrElse (Not _AllProductions.Any) Then
                _AllProductions = New List(Of ProductionView)

                If (Me.Productions Is Nothing) OrElse (Not Me.Productions.Any) Then Return _AllProductions

                Dim myReturn As List(Of ProductionView) = GetAllProductions()
                _AllProductions = myReturn
            End If
            Return _AllProductions
        End Get
    End Property
    Public ReadOnly Property AllAdditionalItems As List(Of ProductionView)
        Get
            If (_AllAdditionalItems Is Nothing) OrElse (Not _AllAdditionalItems.Any) Then
                _AllAdditionalItems = New List(Of ProductionView)

                If Me.AdditionalItems.Count = 0 Then Return _AllAdditionalItems

                Dim myReturn As List(Of ProductionView) = GetAllAdditionalItems()
                _AllAdditionalItems = myReturn
            End If
            Return _AllAdditionalItems
        End Get
    End Property

    Public ReadOnly Property RequiredMachines As Int32
        Get
            Dim myReturn As Double

            If Me.Recipe.ProductionRate > 0 Then
                myReturn = Math.Ceiling(Me.ItemsPerMinute / Me.Recipe.ProductionRate)
            Else
                myReturn = Math.Ceiling(Me.ItemsPerMinute)
            End If
            If Double.IsInfinity(myReturn) Then myReturn = 1

            Return Convert.ToInt32(myReturn)
        End Get
    End Property
    Public ReadOnly Property ProductionRate As Double
        Get
            Dim myReturn As Double
            myReturn = Me.ItemsPerMinute / (Me.RequiredMachines * Me.Recipe.ProductionRate)

            Return myReturn
        End Get
    End Property

    Friend Sub ResetProductions()
        Dim myResetFunction As Action(Of ProductionView) = Sub(x)
                                                               x.ResetProductions()
                                                               x.BaseObj.ResetProductions()
                                                           End Sub

        Me.Productions.ForEach(myResetFunction)
        Me.BaseObj.ResetProductions()

        Me._AdditionalItems.Clear()
        Me._Productions.Clear()
        Me._AllAdditionalItems.Clear()
        Me._AllProductions.Clear()

        NotifyPropertyChanged(NameOf(AdditionalItems))
        NotifyPropertyChanged(NameOf(Productions))
        NotifyPropertyChanged(NameOf(AllAdditionalItems))
        NotifyPropertyChanged(NameOf(AllProductions))
    End Sub
    Friend Sub CalculateProductions()
        If (Me.Productions IsNot Nothing) Then Me.Productions.ForEach(Sub(x) x.CalculateProductions())
        If (Me.AllProductions IsNot Nothing) Then Me.AllProductions.ForEach(Sub(x) x.CalculateProductions())

        Me.Recipe.UpdateProductionRate()

        NotifyPropertyChanged(NameOf(RequiredMachines))
        NotifyPropertyChanged(NameOf(ProductionRate))
    End Sub
    Friend Sub Expand(
            thisRecipes As List(Of ResourceView),
            thisAltRecipes As List(Of ResourceView),
            thisMinerType As MinerTypeEnum,
            thisMinerSpeed As MinerSpeedEnum,
            thisResourceRate As ResourceNodeTypeEnum,
            thisBeltSpeed As BeltSpeedEnum)

        BaseObj.Expand(thisRecipes.Select(Function(x) x.BaseObject).ToList, thisAltRecipes.Select(Function(x) x.BaseObject).ToList, thisMinerType, thisMinerSpeed, thisResourceRate, thisBeltSpeed)

        NotifyPropertyChanged(NameOf(Productions))
        NotifyPropertyChanged(NameOf(AllProductions))
    End Sub

    Public Property IsExpanded As Boolean

    Public Property IsSelected As Boolean
        Get
            Return _IsSelected
        End Get
        Set
            _IsSelected = Value
            MainView.SelectedProduction = Me
        End Set
    End Property

    Private Function GetAllProductions() As List(Of ProductionView)
        Dim myTempList As New List(Of Production)
        myTempList.AddRange(Me.Productions.Select(Function(x) ToProduction(x)))
        For Each myProduction In Me.Productions
            myTempList.AddRange(myProduction.AllProductions.Select(Function(x) ToProduction(x)))
        Next

        Dim myGroups = From myProduction In myTempList
                       Group By Recipe = myProduction.Recipe Into Productions = Group
                       Order By Recipe.Name
                       Select New ProductionView() With {._Recipe = New ResourceView(Recipe), .ItemsPerMinute = Productions.Sum(Function(x) x.ItemsPerMinute)}
        Dim myReturn = myGroups.OrderBy(Function(x) x.Recipe.Name).ToList
        Return myReturn
    End Function
    Private Function GetAllAdditionalItems() As List(Of ProductionView)
        Dim myTempList As New List(Of Production)
        myTempList.AddRange(Me.AdditionalItems.Select(Function(x) ToProduction(x)))
        For Each myProduction In Me.Productions
            myTempList.AddRange(myProduction.AdditionalItems.Select(Function(x) ToProduction(x)))
        Next

        Dim myGroups = From myAdditionalProduction In myTempList
                       Group By Recipe = myAdditionalProduction.Recipe Into AdditionalProductions = Group
                       Order By Recipe.Name
                       Select New ProductionView() With {._Recipe = New ResourceView(Recipe), .ItemsPerMinute = AdditionalProductions.Sum(Function(x) x.ItemsPerMinute)}
        Dim myReturn = myGroups.OrderBy(Function(x) x.Recipe.Name).ToList
        Return myReturn
    End Function
    Public Shared Function ToProduction(thisProductionView As ProductionView) As Production
        Dim myReturn As New Production() With {
            .Recipe = thisProductionView.Recipe.BaseObject,
            .ItemsPerMinute = thisProductionView.ItemsPerMinute
            }
        Return myReturn
    End Function
End Class
