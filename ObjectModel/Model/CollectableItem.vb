Imports System.Xml.Serialization

<DebuggerDisplay("Col: [{ItemType}]")> Public Class CollectableItem
    Inherits Item
    Implements IEquatable(Of CollectableItem)

    Private _Count As Double
    Private _Costs As Double?
    Private _Name As String

    <XmlAttribute> Public Property Name As String
        Get
            Return _Name
        End Get
        Set
            _Name = Value
        End Set
    End Property

    <XmlAttribute> Public Property Count As Double
        Get
            Return _Count
        End Get
        Set
            _Count = Value
        End Set
    End Property

    <XmlIgnore> Public Overrides ReadOnly Property Costs As Double
        Get
            If (Not _Costs.HasValue) Then _Costs = 1 / Me.Count

            Return _Costs
        End Get
    End Property
    Public Overrides Function Equals(other As Object) As Boolean
        If (other Is Nothing) Then Return False
        If (Not other.GetType.Equals(Me.GetType)) Then Return False

        Return DirectCast(Me, IEquatable(Of CollectableItem)).Equals(TryCast(other, CollectableItem))
    End Function
    Public Overloads Function Equals(other As CollectableItem) As Boolean Implements IEquatable(Of CollectableItem).Equals
        If (other Is Nothing) Then Return False

        If (Not String.IsNullOrEmpty(Me.Name)) AndAlso (Not String.IsNullOrEmpty(other.Name)) AndAlso (Not String.IsNullOrEmpty(Me.ItemType)) AndAlso (Not String.IsNullOrEmpty(other.ItemType)) Then
            Return Me.Name.Equals(other.Name) AndAlso Me.ItemType.Equals(other.ItemType)
        Else
            Return False
        End If
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return String.Format("{0}", Me.ItemType).GetHashCode
    End Function
End Class
