
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports System.Threading.Tasks.Dataflow
Imports System.Windows.Threading

Public Class MainViewModel
    Inherits ViewModelBase
    Implements INotifyPropertyChanged

    Private _Recipes As List(Of ResourceView)
    Private _AlternateRecipes As ObservableCollection(Of ResourceView)
    Private _DefaultRecipes As ObservableCollection(Of ResourceView)
    Private _SelectedProduction As ProductionView
    Private _SaveSettingsCommand As ICommand
    Private _GetResources_FromGamepedia_Command As ICommand
    Private _MinerTypes As Dictionary(Of String, Integer)
    Private _MinerSpeeds As Dictionary(Of String, Integer)
    Private _ResourceRates As Dictionary(Of String, Integer)
    Private _GlobalResources As List(Of Tuple(Of String, Double))

    Public ReadOnly Property Recipes As List(Of ResourceView)
        Get
            If _Recipes Is Nothing Then _Recipes = New List(Of ResourceView)

            Return _Recipes
        End Get
    End Property
    Public ReadOnly Property OrderedRecipes As List(Of ResourceView)
        Get
            Return Recipes.OrderBy(Function(x) x.DisplayName).ToList
        End Get
    End Property
    Public ReadOnly Property AlternateRecipes As ObservableCollection(Of ResourceView)
        Get
            If (_AlternateRecipes Is Nothing) OrElse (Not _AlternateRecipes.Any) Then
                _AlternateRecipes = New ObservableCollection(Of ResourceView)(GetAlternateRecipes)
                AddHandler _AlternateRecipes.CollectionChanged, AddressOf ResetProductions
            End If

            Return _AlternateRecipes
        End Get
    End Property
    Public ReadOnly Property DefaultRecipes As ObservableCollection(Of ResourceView)
        Get
            If (_DefaultRecipes Is Nothing) OrElse (Not _DefaultRecipes.Any) Then
                _DefaultRecipes = New ObservableCollection(Of ResourceView)(GetDefaultRecipes)
            End If

            Return _DefaultRecipes
        End Get
    End Property
    Public ReadOnly Property Productions As New ObservableCollection(Of ProductionView)
    Public Property SelectedProduction As ProductionView
        Get
            Return _SelectedProduction
        End Get
        Set
            _SelectedProduction = Value
            NotifyPropertyChanged()
        End Set
    End Property

    Public Shared Property MinerType As MinerTypeEnum = MinerTypeEnum.MK1
    Public ReadOnly Property MinerTypes As Dictionary(Of String, Integer)
        Get
            If (_MinerTypes Is Nothing) Then
                _MinerTypes = New Dictionary(Of String, Integer)
                For Each myValue In [Enum].GetValues(GetType(MinerTypeEnum))
                    _MinerTypes.Add([Enum].GetName(GetType(MinerTypeEnum), myValue), myValue)
                Next
            End If

            Return _MinerTypes
        End Get
    End Property
    Public Shared Property MinerSpeed As MinerSpeedEnum = MinerSpeedEnum.Speed100
    Public ReadOnly Property MinerSpeeds As Dictionary(Of String, Integer)
        Get
            If (_MinerSpeeds Is Nothing) Then
                _MinerSpeeds = New Dictionary(Of String, Integer)
                For Each myValue In [Enum].GetValues(GetType(MinerSpeedEnum))
                    _MinerSpeeds.Add([Enum].GetName(GetType(MinerSpeedEnum), myValue), myValue)
                Next
            End If

            Return _MinerSpeeds
        End Get
    End Property
    Public Shared Property ResourceRate As ResourceRateEnum = ResourceRateEnum.Normal
    Public ReadOnly Property ResourceRates As Dictionary(Of String, Integer)
        Get
            If (_ResourceRates Is Nothing) Then
                _ResourceRates = New Dictionary(Of String, Integer)
                For Each myValue In [Enum].GetValues(GetType(ResourceRateEnum))
                    _ResourceRates.Add([Enum].GetName(GetType(ResourceRateEnum), myValue), myValue)
                Next
            End If

            Return _ResourceRates
        End Get
    End Property

    Public ReadOnly Property GlobalResources As List(Of Tuple(Of String, Double))
        Get
            If (_GlobalResources Is Nothing) OrElse (Not _GlobalResources.Any) Then
                _GlobalResources = New List(Of Tuple(Of String, Double))
                Dim myList = Me.Recipes.Where(Function(x) (x.BaseObject IsNot Nothing) AndAlso ((x.BaseObject.GlobalSources IsNot Nothing) AndAlso (x.BaseObject.GlobalSources.Any)))
                For Each myItem In myList
                    _GlobalResources.Add(Tuple.Create(Of String, Double)(myItem.BaseObject.Type, myItem.BaseObject.GlobalSources.Sum(Function(x) [Enum].Parse(GetType(ResourceRateEnum), x.GlobalSourcesType) * x.GlobalSourcesCount)))
                Next
            End If
            Return _GlobalResources
        End Get
    End Property

    Public Sub SetRecipes(thisRecipes As List(Of Resource), thisAlternateRecipes As List(Of String))
        Me.Recipes.Clear()
        Me.Recipes.AddRange(thisRecipes.OrderBy(Function(x) x.Name).ToList.Select(Function(x) New ResourceView(x)))

        Dim myAllResourceSum = Me.GlobalResources.Sum(Function(y) y.Item2)

        Resource.AllResources = thisRecipes
        'Resource.AllResourcesSum = myAllResourceSum


        Dim subPrepareResource = Sub(x As ResourceView)
                                     x.IsActive = thisAlternateRecipes.Contains(x.Name)
                                     AddHandler x.PropertyChanged, AddressOf ResourceChanged
                                 End Sub

        Me.Recipes.ForEach(subPrepareResource)

        NotifyPropertyChanged(NameOf(Recipes))
        NotifyPropertyChanged(NameOf(DefaultRecipes))
        NotifyPropertyChanged(NameOf(AlternateRecipes))

        ResetProductions()
    End Sub
    Private Function GetDefaultRecipes() As List(Of ResourceView)
        Return Me.Recipes.Where(Function(x) x.IsAlternativeRecipe = False).ToList
    End Function
    Private Function GetAlternateRecipes() As List(Of ResourceView)
        Return Me.Recipes.Where(Function(x) (x.IsAlternativeRecipe)).ToList
    End Function
    Private Function GetActiveAlternateRecipes() As List(Of ResourceView)
        Return Me.Recipes.Where(Function(x) (x.IsAlternativeRecipe) AndAlso (x.IsActive)).ToList
    End Function

    Public Function AddProduction(thisResourceView As ResourceView, thisAmount As Double) As ProductionView
        Dim myReturn As New ProductionView(Me, thisResourceView, thisAmount)
        myReturn.Expand(GetDefaultRecipes, GetActiveAlternateRecipes, MinerType, MinerSpeed, ResourceRate, Me.GlobalResources)

        Me.Productions.Add(myReturn)

        Return myReturn
    End Function
    Friend Sub ResetProductions()
        Dim myDefaults = GetDefaultRecipes()
        Dim myAlts = GetActiveAlternateRecipes()
        Dim myResetAndExpandFunction As Action(Of ProductionView) = Sub(x)
                                                                        x.ResetProductions()
                                                                        x.Expand(myDefaults, myAlts, MinerType, MinerSpeed, ResourceRate, Me.GlobalResources)
                                                                    End Sub
        Me.Productions.ToList.ForEach(myResetAndExpandFunction)
    End Sub

    Private Sub ResourceChanged(sender As Object, e As PropertyChangedEventArgs)
        If (sender.GetType.Equals(GetType(ResourceView))) Then
            Dim myItem = DirectCast(sender, ResourceView)
            If (myItem.IsAlternativeRecipe) AndAlso (e.PropertyName.Equals(NameOf(ResourceView.IsActive))) Then

                SyncUserAlternateRecipes(myItem, myItem.IsActive)
                ResetProductions()
                NotifyPropertyChanged(NameOf(AlternateRecipes))
            End If
        End If
    End Sub
    Private Sub SyncUserAlternateRecipes(thisRes As ResourceView, value As Boolean)
        If (value = True) AndAlso (Not My.Settings.ActiveAlternateRecipes.Contains(thisRes.Name)) Then
            My.Settings.ActiveAlternateRecipes.Add(thisRes.Name)
        ElseIf (value = False) AndAlso (My.Settings.ActiveAlternateRecipes.Contains(thisRes.Name)) Then
            My.Settings.ActiveAlternateRecipes.Remove(thisRes.Name)
        End If
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
    Private Function SaveSettingsCanExecute(sender As Object) As Boolean
        Return True
    End Function
    Private Sub SaveSettingExecute(sender As Object)
        My.Settings.Save()
    End Sub

    Public Property GetResources_FromGamepedia_Command As ICommand
        Get
            'GetResources_FromGamepedia_DataFlow
            If (_GetResources_FromGamepedia_Command Is Nothing) Then
                _GetResources_FromGamepedia_Command = New Command(AddressOf GetResources_FromGamepedia_Execute, AddressOf GetResources_FromGamepedia_CanExecute)
            End If
            Return _GetResources_FromGamepedia_Command
        End Get
        Set
            _GetResources_FromGamepedia_Command = Value
        End Set
    End Property
    Private Function GetResources_FromGamepedia_CanExecute(sender As Object) As Boolean
        Return True
    End Function
    Private Sub GetResources_FromGamepedia_Execute(parameter As Object)
        If (parameter Is Nothing) OrElse (Not parameter.GetType = GetType(MainWindow)) Then Exit Sub

        GetResources_FromGamepedia(New Uri("https://satisfactory.gamepedia.com/Satisfactory_Wiki"), DirectCast(parameter, MainWindow))
    End Sub
    Private Async Sub GetResources_FromGamepedia(thisUri As Uri, thisWindow As MainWindow)
        thisWindow.SetWaitCursor()

        Dim myDataFlow As New GetResources_FromGamepedia_DataFlow
        Dim myResult As New BufferBlock(Of Resource)

        myDataFlow.Output.LinkTo(myResult, New DataflowLinkOptions With {.PropagateCompletion = True})

        myDataFlow.Input.Post(thisUri)
        myDataFlow.Input.Complete()

        Await myDataFlow.Output.Completion

        Dim myRecipes As New List(Of Resource)
        myResult.TryReceiveAll(myRecipes)
        myResult.Complete()

        Dim mySatisResource As New SatifactoryResources
        mySatisResource.Recipes = myRecipes.Distinct().OrderBy(Function(x) x.Name).ToList

        My.Application.WriteToXML(Of SatifactoryResources)(mySatisResource, IO.Path.Combine(My.Application.Info.DirectoryPath, "Sources", "SatisfactoryRecipes.xml"))

        GC.Collect()
        GC.WaitForFullGCComplete()

        thisWindow.InitReloadUi()
    End Sub
End Class


