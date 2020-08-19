Imports System.Xml.Serialization

Public Class Production
    Private _DataStore As DataStore
    Private _Recipe As Recipe
    Private _Productions As New List(Of Production)
    Private _AdditionalProductions As New List(Of Production)

    <XmlIgnore> Public Property DataStore As DataStore
        Get
            Return _DataStore
        End Get
        Set
            _DataStore = Value
        End Set
    End Property

    Public Property Recipe As Recipe
        Get
            Return _Recipe
        End Get
        Set
            _Recipe = Value
        End Set
    End Property
    Public Property ItemsPerMinute As Double

    Public ReadOnly Property Name As String
        Get
            Return Me.Recipe.Name
        End Get
    End Property
    Public ReadOnly Property ItemType As String
        Get
            Return Me.Recipe.ItemType
        End Get
    End Property

    Public ReadOnly Property Productions As List(Of Production)
        Get
            Return _Productions
        End Get
    End Property
    Public ReadOnly Property AdditionalProductions As List(Of Production)
        Get
            Return _AdditionalProductions
        End Get
    End Property

End Class
