Imports Satisfactory.ObjectModel

Public Class ProductionView
    Inherits ViewModelBase
#Region "Base Object"
    Private _baseObj As Production

    Public Property baseObj As Production
        Get
            Return _baseObj
        End Get
        Set
            _baseObj = Value
        End Set
    End Property
#End Region

#Region "Base DataStoreView"
    Private _baseStoreView As DataStoreView

    Public Property baseStoreView As DataStoreView
        Get
            Return _baseStoreView
        End Get
        Set
            _baseStoreView = Value
        End Set
    End Property
#End Region
End Class

Partial Class ProductionView
    Private _ItemsPerMinute As Double
#Region "Base Properties"

    Public ReadOnly Property Name As String
        Get
            Return Me.Recipe.Name
        End Get
    End Property
    Public Property ItemsPerMinute As Double
        Get
            Return baseObj.ItemsPerMinute
        End Get
        Set
            baseObj.ItemsPerMinute = Value
            NotifyPropertyChanged()
        End Set
    End Property

    Public ReadOnly Property ItemType As String
        Get
            Return Me.Recipe.ItemType
        End Get
    End Property
    Public Property IsSelected As Boolean
    Public Property Recipe As RecipeView

    Public Property RequiredMachines As Double
    Public Property ProductionRate As Double

#End Region

End Class
