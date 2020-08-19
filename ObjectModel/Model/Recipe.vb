Imports System.Xml.Serialization

<DebuggerDisplay("Rec: {Name} [{ItemType}]")> Public Class Recipe
    Inherits Item
    Implements IEquatable(Of Recipe)

    Private _Name As String
    Private _IsAlternativeRecipe As Boolean
    Private _Inputs As New List(Of SourceItem)
    Private _OutputPerMinute As Double
    Private _AdditionalOutputs As New List(Of SourceItem)
    Private _Costs As Double?

    <XmlAttribute> Public Property Name As String
        Get
            Return _Name
        End Get
        Set
            _Name = Value
        End Set
    End Property

    <XmlAttribute> Public Property IsAlternativeRecipe As Boolean
        Get
            Return _IsAlternativeRecipe
        End Get
        Set
            _IsAlternativeRecipe = Value
        End Set
    End Property

    <XmlArray> Public ReadOnly Property Inputs As List(Of SourceItem)
        Get
            Return _Inputs
        End Get
    End Property

    <XmlAttribute> Public Property OutputPerMinute As Double
        Get
            Return _OutputPerMinute
        End Get
        Set
            _OutputPerMinute = Value
        End Set
    End Property

    <XmlArray> Public ReadOnly Property AdditionalOutputs As List(Of SourceItem)
        Get
            Return _AdditionalOutputs
        End Get
    End Property

    <XmlIgnore> Public Overrides ReadOnly Property Costs As Double
        Get
            If (Not _Costs.HasValue) Then Return -1

            Return _Costs
        End Get
    End Property

    Friend Sub SetCosts(value As Double)
        If value > 0 AndAlso Not Double.IsInfinity(value) Then
            _Costs = value
        End If
    End Sub

    Public Overrides Function Equals(other As Object) As Boolean
        If (other Is Nothing) Then Return False
        If (Not other.GetType.Equals(Me.GetType)) Then Return False

        Return DirectCast(Me, IEquatable(Of Recipe)).Equals(TryCast(other, Recipe))
    End Function
    Public Overloads Function Equals(other As Recipe) As Boolean Implements IEquatable(Of Recipe).Equals
        If (other Is Nothing) Then Return False

        If (Not String.IsNullOrEmpty(Me.Name)) AndAlso (Not String.IsNullOrEmpty(other.Name)) AndAlso (Not String.IsNullOrEmpty(Me.ItemType)) AndAlso (Not String.IsNullOrEmpty(other.ItemType)) Then
            Return Me.Name.Equals(other.Name) AndAlso Me.ItemType.Equals(other.ItemType)
        Else
            Return False
        End If
    End Function
    Public Overrides Function GetHashCode() As Integer
        Return String.Format("{0}|{1}", Me.ItemType, Me.Name).GetHashCode
    End Function
End Class
