
Imports System.Collections.ObjectModel
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports System.Threading.Tasks.Dataflow
Imports System.Windows.Threading
Imports Microsoft.Win32

Public Class MainViewModel
    Inherits ViewModelBase
    Implements INotifyPropertyChanged

    Public Sub New()
        AddHandler Productions.CollectionChanged, AddressOf ProductionChanged
    End Sub

    Private Sub ProductionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
        Me.HasChanged = True
    End Sub

    Private _Recipes As List(Of ResourceView)
    Private _AlternateRecipes As ObservableCollection(Of ResourceView)
    Private _DefaultRecipes As ObservableCollection(Of ResourceView)
    Private _SelectedProduction As ProductionView
    Private _SaveSettingsCommand As ICommand
    Private _GetResources_FromGamepedia_Command As ICommand
    Private _MinerType As MinerTypeEnum = MinerTypeEnum.MK1
    Private _MinerSpeed As MinerSpeedEnum = MinerSpeedEnum.Speed100
    Private _ResourceNodeType As ResourceNodeTypeEnum = ResourceNodeTypeEnum.Normal
    Private _BeltSpeed As BeltSpeedEnum = BeltSpeedEnum.MK4
    Private _GlobalResources As List(Of ResourceView)

#Region "Global Resources"
    Public ReadOnly Property GlobalResources As List(Of ResourceView)
        Get
            If (_GlobalResources Is Nothing) Then _GlobalResources = New List(Of ResourceView)

            If (Not _GlobalResources.Any) Then
                Dim myList = Me.Recipes.Where(Function(x) (x.BaseObject IsNot Nothing) AndAlso ((x.BaseObject.GlobalSources IsNot Nothing) AndAlso (x.BaseObject.GlobalSources.Any))).ToList
                For Each myItem In myList
                    _GlobalResources.Add(New ResourceView(myItem.BaseObject) With {.ItemsPerMinute = myItem.BaseObject.GetGlobalSourcesSum(MainViewModel.CurrentMinerType, MainViewModel.CurrentMinerSpeed, MainViewModel.CurrentBeltSpeed)})
                Next
            End If

            Return _GlobalResources
        End Get
    End Property

    Friend Shared CurrentMinerType As MinerTypeEnum = MinerTypeEnum.MK1
    Public Property MinerType As MinerTypeEnum
        Get
            Return _MinerType
        End Get
        Set
            _MinerType = Value
            CurrentMinerType = _MinerType

            ResetGlobalResources()
            CalculateProductions()
            NotifyPropertyChanged()
        End Set
    End Property

    Friend Shared CurrentMinerSpeed As MinerSpeedEnum = MinerSpeedEnum.Speed100
    Public Property MinerSpeed As MinerSpeedEnum
        Get
            Return _MinerSpeed
        End Get
        Set
            _MinerSpeed = Value
            CurrentMinerSpeed = _MinerSpeed

            ResetGlobalResources()
            CalculateProductions()
            NotifyPropertyChanged()
        End Set
    End Property

    Friend Shared CurrentBeltSpeed As BeltSpeedEnum = BeltSpeedEnum.MK4
    Public Property BeltSpeed As BeltSpeedEnum
        Get
            Return _BeltSpeed
        End Get
        Set
            _BeltSpeed = Value
            CurrentBeltSpeed = _BeltSpeed

            ResetGlobalResources()
            CalculateProductions()
            NotifyPropertyChanged()
        End Set
    End Property

    Friend Shared CurrentResourceNodeType As ResourceNodeTypeEnum = ResourceNodeTypeEnum.Normal

    Public Property ResourceNodeType As ResourceNodeTypeEnum
        Get
            Return _ResourceNodeType
        End Get
        Set
            _ResourceNodeType = Value
            CurrentResourceNodeType = _ResourceNodeType

            ResetGlobalResources()
            CalculateProductions()
            NotifyPropertyChanged()
        End Set
    End Property

    Friend Sub ResetGlobalResources()
        _GlobalResources = Nothing
        NotifyPropertyChanged(NameOf(GlobalResources))
    End Sub
#End Region

#Region "Recipes"
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
    Public Sub SetRecipes(thisRecipes As List(Of Resource), thisAlternateRecipes As List(Of String))
        Me.Recipes.Clear()
        Me.Recipes.AddRange(thisRecipes.OrderBy(Function(x) x.Name).ToList.Select(Function(x) New ResourceView(x)))

        Dim myAllResourceSum = Me.GlobalResources.Sum(Function(y) y.ItemsPerMinute)

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
#End Region

#Region "Productions"
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

    Public Function AddProduction(thisResourceView As ResourceView, thisAmount As Double) As ProductionView
        Dim myReturn As New ProductionView(Me, thisResourceView, thisAmount)
        myReturn.Expand(GetDefaultRecipes, GetActiveAlternateRecipes, MainViewModel.CurrentMinerType, MainViewModel.CurrentMinerSpeed, MainViewModel.CurrentResourceNodeType, MainViewModel.CurrentBeltSpeed)

        Me.Productions.Add(myReturn)
        Me.HasChanged = True

        Return myReturn
    End Function

    Friend Sub CalculateProductions()
        Me.Recipes.ToList.ForEach(Sub(x) x.UpdateProductionRate())
        Me.Productions.ToList.ForEach(Sub(x) x.CalculateProductions())
    End Sub

    Friend Sub ResetProductions()
        Dim myDefaults = GetDefaultRecipes()
        Dim myAlts = GetActiveAlternateRecipes()
        Dim myResetAndExpandFunction As Action(Of ProductionView) = Sub(x)
                                                                        x.ResetProductions()
                                                                        x.Expand(myDefaults, myAlts, MainViewModel.CurrentMinerType, MainViewModel.CurrentMinerSpeed, MainViewModel.CurrentResourceNodeType, MainViewModel.CurrentBeltSpeed)
                                                                    End Sub
        Me.Productions.ToList.ForEach(myResetAndExpandFunction)
    End Sub
#End Region


#Region "UserSettings"
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
#End Region

#Region "Update Resources from web"
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

        My.Application.WriteToXML(Of SatifactoryResources)(mySatisResource, IO.Path.Combine(My.Application.Info.DirectoryPath, "sources", "SatisfactoryRecipes.xml"))

        GC.Collect()
        GC.WaitForFullGCComplete()

        thisWindow.InitReloadUi()
    End Sub
#End Region

#Region "Production File"
    Friend Property HasChanged As Boolean = False

    Private _CurrentFile As String
    Friend ReadOnly Property CurrentFile As String
        Get
            If (String.IsNullOrEmpty(_CurrentFile)) Then _CurrentFile = GetProductionFile()

            Return _CurrentFile
        End Get
    End Property

    Private Function GetProductionFile() As String
        Dim myReturn As String

        Dim myOFD As New OpenFileDialog()
        myOFD.DefaultExt = ".xml"
        myOFD.Filter = "XML Files (*.xml)|*.xml"
        myOFD.Multiselect = False
        myOFD.InitialDirectory = My.Computer.FileSystem.SpecialDirectories.MyDocuments
        myOFD.Title = "Select Satisfactory production file..."
        myOFD.CheckFileExists = False
        myOFD.CheckPathExists = False

        Dim myDialogResult = myOFD.ShowDialog
        If (myDialogResult.HasValue) AndAlso (myDialogResult.Value = True) AndAlso (IO.Path.GetExtension(myOFD.FileName).Equals(".xml")) Then myReturn = myOFD.FileName

        Return myReturn
    End Function

    Friend Sub GetNewFile()
        _CurrentFile = GetProductionFile()
    End Sub
#End Region
End Class


