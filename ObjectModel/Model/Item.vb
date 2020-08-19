Imports System.Xml.Serialization

Public MustInherit Class Item
    Private _ItemType As String
    Private _DataStore As DataStore
    Private _ItemCategory As CategoryEnum = CategoryEnum.Undefined

    <XmlIgnore> Public Property DataStore As DataStore
        Get
            Return _DataStore
        End Get
        Set
            _DataStore = Value
        End Set
    End Property
    <XmlAttribute> Public Property ItemType As String
        Get
            Return _ItemType
        End Get
        Set
            _ItemType = Value
        End Set
    End Property

    <XmlAttribute> Public Property ItemCategory As CategoryEnum
        Get
            Return _ItemCategory
        End Get
        Set
            _ItemCategory = Value
        End Set
    End Property

    <XmlIgnore> Public MustOverride ReadOnly Property Costs As Double
End Class
