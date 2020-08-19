Imports System.Xml.Serialization

<DebuggerDisplay("SoI: [{ItemType}]")> Public Class SourceItem

    Private _ItemType As String
    Private _ItemsPerMinute As Double

    <XmlAttribute> Public Property ItemsPerMinute As Double
        Get
            Return _ItemsPerMinute
        End Get
        Set
            _ItemsPerMinute = Value
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

End Class
