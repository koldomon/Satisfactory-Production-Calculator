Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Runtime.CompilerServices

<DebuggerDisplay("Res: {Name}")> Public Class ResourceView
    Inherits ViewModelBase
    Implements INotifyPropertyChanged

    Protected Friend BaseObject As Resource
    Private _IsActive As Boolean

    Public Sub New()
    End Sub
    Public Sub New(o As Resource)
        BaseObject = o
    End Sub
    Public ReadOnly Property Name As String
        Get
            Return BaseObject.Name
        End Get
    End Property
    Public ReadOnly Property DisplayName As String
        Get
            Return String.Format("{0} [{1}]", Me.Name, Me.Type)
        End Get
    End Property
    Public ReadOnly Property Type As String
        Get
            Return BaseObject.Type
        End Get
    End Property
    Public ReadOnly Property Category As String
        Get
            Return BaseObject.Category
        End Get
    End Property
    'Public ReadOnly Property ProductionPerMinute As Double
    '    Get
    '        Return BaseObject.ProductionPerMinute
    '    End Get
    'End Property
    'Public ReadOnly Property ProductionPerMachine As Double
    '    Get
    '        Return BaseObject.ProductionPerMachine
    '    End Get
    'End Property
    Public Property ItemsPerMinute As Double
        Set(value As Double)
            BaseObject.ItemsPerMinute = value
        End Set
        Get
            Return BaseObject.ItemsPerMinute
        End Get
    End Property

    Public ReadOnly Property ProductionRate As Double
        Get
            Return BaseObject.GetProductionRate(MinerTypeEnum.MK1, MinerSpeedEnum.Speed100, ResourceRateEnum.Normal)
        End Get
    End Property
    Public ReadOnly Property ProductionCost As Double
        Get
            Return BaseObject.ProductionCost
        End Get
    End Property

    Public ReadOnly Property AdditionalProductions As List(Of ResourceView)
        Get
            If BaseObject.AdditionalProductions IsNot Nothing Then
                Return BaseObject.AdditionalProductions.Select(Function(x) New ResourceView(x)).ToList
            End If

            Return Nothing
        End Get
    End Property
    Public ReadOnly Property Resources As List(Of ResourceView)
        Get
            If (BaseObject.Resources IsNot Nothing) Then
                Return BaseObject.Resources.Select(Function(x) New ResourceView(x)).ToList
            End If

            Return Nothing
        End Get
    End Property
    Public ReadOnly Property IsAlternativeRecipe As Boolean
        Get
            Return BaseObject.IsAlternativeRecipe
        End Get
    End Property
    Public Property IsActive As Boolean
        Get
            Return _IsActive
        End Get
        Set
            _IsActive = Value
            NotifyPropertyChanged()
        End Set
    End Property

End Class
